using Microsoft.Data.Sqlite;
using System.IO;
using KapeViewer.Models;

namespace KapeViewer.Services
{
    public class DatabaseService : IDisposable
    {
        private readonly string _dbPath;
        private readonly SqliteConnection _connection;

        public DatabaseService()
        {
            // Create a unique temporary file for the database
            _dbPath = Path.GetTempFileName();
            
            // Initialize connection
            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = _dbPath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Cache = SqliteCacheMode.Shared
            }.ToString();

            _connection = new SqliteConnection(connectionString);
            _connection.Open();

            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using var command = _connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Events (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Timestamp TEXT NOT NULL,
                    Source TEXT,
                    GroupName TEXT,
                    Description TEXT,
                    OriginalTimeString TEXT
                );
                
                CREATE INDEX IF NOT EXISTS IX_Events_Timestamp ON Events(Timestamp);
                CREATE INDEX IF NOT EXISTS IX_Events_GroupName ON Events(GroupName);
            ";
            command.ExecuteNonQuery();
        }

        public void BulkInsertEvents(IEnumerable<TimelineEvent> events)
        {
            using var transaction = _connection.BeginTransaction();
            using var command = _connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                INSERT INTO Events (Timestamp, Source, GroupName, Description, OriginalTimeString)
                VALUES ($timestamp, $source, $groupName, $description, $originalTime)
            ";

            var pTimestamp = command.CreateParameter(); pTimestamp.ParameterName = "$timestamp"; command.Parameters.Add(pTimestamp);
            var pSource = command.CreateParameter(); pSource.ParameterName = "$source"; command.Parameters.Add(pSource);
            var pGroupName = command.CreateParameter(); pGroupName.ParameterName = "$groupName"; command.Parameters.Add(pGroupName);
            var pDescription = command.CreateParameter(); pDescription.ParameterName = "$description"; command.Parameters.Add(pDescription);
            var pOriginalTime = command.CreateParameter(); pOriginalTime.ParameterName = "$originalTime"; command.Parameters.Add(pOriginalTime);

            foreach (var evt in events)
            {
                // Store as ISO 8601 string for correct sorting/filtering in SQLite
                pTimestamp.Value = evt.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff");
                pSource.Value = evt.Source ?? (object)DBNull.Value;
                pGroupName.Value = evt.GroupName ?? (object)DBNull.Value;
                pDescription.Value = evt.Description ?? (object)DBNull.Value;
                pOriginalTime.Value = evt.OriginalTimeString ?? (object)DBNull.Value;

                command.ExecuteNonQuery();
            }

            transaction.Commit();
        }

        public List<TimelineEvent> GetEvents(FilterCriteria criteria, int offset, int limit)
        {
            var events = new List<TimelineEvent>();
            using var command = _connection.CreateCommand();
            
            var whereClause = BuildFilterQuery(command, criteria);
            var query = $"SELECT Timestamp, Source, GroupName, Description, OriginalTimeString FROM Events {whereClause} ORDER BY Timestamp ASC LIMIT $limit OFFSET $offset";
            
            command.CommandText = query;
            var pLimit = command.CreateParameter(); pLimit.ParameterName = "$limit"; pLimit.Value = limit; command.Parameters.Add(pLimit);
            var pOffset = command.CreateParameter(); pOffset.ParameterName = "$offset"; pOffset.Value = offset; command.Parameters.Add(pOffset);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                events.Add(new TimelineEvent
                {
                    Timestamp = DateTime.Parse(reader.GetString(0)),
                    Source = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                    GroupName = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    Description = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                    OriginalTimeString = reader.IsDBNull(4) ? string.Empty : reader.GetString(4)
                });
            }

            return events;
        }

        public int GetEventCount(FilterCriteria criteria)
        {
            using var command = _connection.CreateCommand();
            var whereClause = BuildFilterQuery(command, criteria);
            command.CommandText = $"SELECT COUNT(*) FROM Events {whereClause}";
            return Convert.ToInt32(command.ExecuteScalar());
        }

        public Dictionary<string, int> GetEventCountsByGroup(FilterCriteria criteria)
        {
            var counts = new Dictionary<string, int>();
            using var command = _connection.CreateCommand();
            var whereClause = BuildFilterQuery(command, criteria);
            command.CommandText = $"SELECT GroupName, COUNT(*) FROM Events {whereClause} GROUP BY GroupName ORDER BY COUNT(*) DESC";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var group = reader.IsDBNull(0) ? "Unknown" : reader.GetString(0);
                var count = reader.GetInt32(1);
                counts[group] = count;
            }
            return counts;
        }

        private string BuildFilterQuery(SqliteCommand command, FilterCriteria criteria)
        {
            var sb = new System.Text.StringBuilder("WHERE 1=1");

            if (criteria.FromDate.HasValue)
            {
                sb.Append(" AND Timestamp >= $fromDate");
                var p = command.CreateParameter(); p.ParameterName = "$fromDate"; 
                p.Value = criteria.FromDate.Value.ToString("yyyy-MM-dd HH:mm:ss.fff");
                command.Parameters.Add(p);
            }

            if (criteria.ToDate.HasValue)
            {
                sb.Append(" AND Timestamp <= $toDate");
                var p = command.CreateParameter(); p.ParameterName = "$toDate"; 
                p.Value = criteria.ToDate.Value.ToString("yyyy-MM-dd HH:mm:ss.fff");
                command.Parameters.Add(p);
            }

            if (criteria.HasSourceFilter && criteria.SourceGroup != "All")
            {
                sb.Append(" AND GroupName = $groupName");
                var p = command.CreateParameter(); p.ParameterName = "$groupName"; 
                p.Value = criteria.SourceGroup;
                command.Parameters.Add(p);
            }

            if (criteria.HasSearchFilter)
            {
                var terms = ParseSearchTerms(criteria.SearchText);
                int termIndex = 0;

                foreach (var term in terms)
                {
                    string paramName = $"$term{termIndex}";
                    string value = $"%{term.Value}%";
                    
                    var p = command.CreateParameter(); 
                    p.ParameterName = paramName; 
                    p.Value = value;
                    command.Parameters.Add(p);

                    sb.Append(" AND ");
                    
                    if (term.Field == SearchField.All)
                    {
                        if (term.IsExclusion)
                        {
                            sb.Append($"(Source NOT LIKE {paramName} AND Description NOT LIKE {paramName} AND Timestamp NOT LIKE {paramName})");
                        }
                        else
                        {
                            sb.Append($"(Source LIKE {paramName} OR Description LIKE {paramName} OR Timestamp LIKE {paramName})");
                        }
                    }
                    else
                    {
                        string column = term.Field switch
                        {
                            SearchField.Source => "Source",
                            SearchField.Group => "GroupName",
                            SearchField.Description => "Description",
                            _ => "Source"
                        };
                        
                        string op = term.IsExclusion ? "NOT LIKE" : "LIKE";
                        sb.Append($"{column} {op} {paramName}");
                    }

                    termIndex++;
                }
            }

            return sb.ToString();
        }

        private enum SearchField { All, Source, Group, Description }
        private struct SearchTerm
        {
            public SearchField Field;
            public string Value;
            public bool IsExclusion;
        }

        private List<SearchTerm> ParseSearchTerms(string searchText)
        {
            var terms = new List<SearchTerm>();
            var parts = searchText.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                var term = new SearchTerm { Field = SearchField.All, Value = part, IsExclusion = false };
                string processingPart = part;

                if (processingPart.StartsWith("-"))
                {
                    term.IsExclusion = true;
                    processingPart = processingPart.Substring(1);
                }

                if (processingPart.Contains(":"))
                {
                    var split = processingPart.Split(new[] { ':' }, 2);
                    string prefix = split[0].ToLowerInvariant();
                    string value = split[1];

                    if (string.IsNullOrEmpty(value)) continue;

                    term.Value = value;
                    term.Field = prefix switch
                    {
                        "source" or "src" => SearchField.Source,
                        "group" or "grp" => SearchField.Group,
                        "desc" or "msg" => SearchField.Description,
                        _ => SearchField.All
                    };
                    
                    // If prefix wasn't recognized, it falls back to All, but Value is just the part after colon.
                    // Actually if prefix is unknown, maybe we should treat the whole thing as value?
                    // For now, let's assume if it has colon but not a valid prefix, it's just text.
                    if (term.Field == SearchField.All)
                    {
                        term.Value = processingPart; // Revert to full string if prefix unknown
                    }
                }
                else
                {
                    term.Value = processingPart;
                }

                terms.Add(term);
            }

            return terms;
        }

        public void Dispose()
        {
            _connection.Close();
            _connection.Dispose();
            
            // Clean up temporary database file
            try
            {
                if (File.Exists(_dbPath))
                {
                    File.Delete(_dbPath);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
