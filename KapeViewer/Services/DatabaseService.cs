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
            
            var query = "SELECT Timestamp, Source, GroupName, Description, OriginalTimeString FROM Events WHERE 1=1";
            
            if (criteria.FromDate.HasValue)
            {
                query += " AND Timestamp >= $fromDate";
                var p = command.CreateParameter(); p.ParameterName = "$fromDate"; 
                p.Value = criteria.FromDate.Value.ToString("yyyy-MM-dd HH:mm:ss.fff");
                command.Parameters.Add(p);
            }

            if (criteria.ToDate.HasValue)
            {
                query += " AND Timestamp <= $toDate";
                var p = command.CreateParameter(); p.ParameterName = "$toDate"; 
                p.Value = criteria.ToDate.Value.ToString("yyyy-MM-dd HH:mm:ss.fff");
                command.Parameters.Add(p);
            }

            if (criteria.HasSourceFilter && criteria.SourceGroup != "All")
            {
                query += " AND GroupName = $groupName";
                var p = command.CreateParameter(); p.ParameterName = "$groupName"; 
                p.Value = criteria.SourceGroup;
                command.Parameters.Add(p);
            }

            if (criteria.HasSearchFilter)
            {
                query += " AND (Source LIKE $search OR Description LIKE $search OR Timestamp LIKE $search)";
                var p = command.CreateParameter(); p.ParameterName = "$search"; 
                p.Value = $"%{criteria.SearchText}%";
                command.Parameters.Add(p);
            }

            query += " ORDER BY Timestamp ASC LIMIT $limit OFFSET $offset";
            
            var pLimit = command.CreateParameter(); pLimit.ParameterName = "$limit"; pLimit.Value = limit; command.Parameters.Add(pLimit);
            var pOffset = command.CreateParameter(); pOffset.ParameterName = "$offset"; pOffset.Value = offset; command.Parameters.Add(pOffset);

            command.CommandText = query;

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
            var query = "SELECT COUNT(*) FROM Events WHERE 1=1";

            if (criteria.FromDate.HasValue)
            {
                query += " AND Timestamp >= $fromDate";
                var p = command.CreateParameter(); p.ParameterName = "$fromDate"; 
                p.Value = criteria.FromDate.Value.ToString("yyyy-MM-dd HH:mm:ss.fff");
                command.Parameters.Add(p);
            }

            if (criteria.ToDate.HasValue)
            {
                query += " AND Timestamp <= $toDate";
                var p = command.CreateParameter(); p.ParameterName = "$toDate"; 
                p.Value = criteria.ToDate.Value.ToString("yyyy-MM-dd HH:mm:ss.fff");
                command.Parameters.Add(p);
            }

            if (criteria.HasSourceFilter && criteria.SourceGroup != "All")
            {
                query += " AND GroupName = $groupName";
                var p = command.CreateParameter(); p.ParameterName = "$groupName"; 
                p.Value = criteria.SourceGroup;
                command.Parameters.Add(p);
            }

            if (criteria.HasSearchFilter)
            {
                query += " AND (Source LIKE $search OR Description LIKE $search OR Timestamp LIKE $search)";
                var p = command.CreateParameter(); p.ParameterName = "$search"; 
                p.Value = $"%{criteria.SearchText}%";
                command.Parameters.Add(p);
            }

            command.CommandText = query;
            return Convert.ToInt32(command.ExecuteScalar());
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
