using CsvHelper;
using CsvHelper.Configuration;
using KapeViewer.Models;
using System.Globalization;
using System.IO;

namespace KapeViewer.Services;

/// <summary>
/// Builds merged timeline from multiple CSV files.
/// </summary>
public class TimelineBuilder
{
    private static readonly string[] TimeColumnPatterns = new[]
    {
        "timecreated",
        "timestamp",
        "eventtime",
        "lastwritetime",
        "created",
        "modified",
        "accessed"
    };

    /// <summary>
    /// Builds a merged timeline from all CSV files asynchronously.
    /// </summary>
    /// <param name="files">List of CSV files to process</param>
    /// <param name="progress">Progress reporter for UI updates</param>
    /// <param name="cancellationToken">Cancellation token for long operations</param>
    /// <returns>List of TimelineEvent objects sorted by timestamp</returns>
    public async Task<List<TimelineEvent>> BuildTimelineAsync(
        List<CsvFileItem> files,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var allEvents = new List<TimelineEvent>();
        int processedFiles = 0;

        await Task.Run(() =>
        {
            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var events = ProcessCsvFile(file);
                    allEvents.AddRange(events);
                }
                catch (Exception ex)
                {
                    // Log error but continue with other files
                    System.Diagnostics.Debug.WriteLine($"Error processing {file.FileName}: {ex.Message}");
                }

                processedFiles++;
                progress?.Report((int)((double)processedFiles / files.Count * 100));
            }
        }, cancellationToken);

        // Sort all events by timestamp ascending
        allEvents.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));

        return allEvents;
    }

    /// <summary>
    /// Processes a single CSV file and extracts timeline events.
    /// </summary>
    private List<TimelineEvent> ProcessCsvFile(CsvFileItem file)
    {
        var events = new List<TimelineEvent>();

        if (!File.Exists(file.FullPath))
        {
            return events;
        }

        using var reader = new StreamReader(file.FullPath);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            BadDataFound = null,
            MissingFieldFound = null,
            TrimOptions = TrimOptions.Trim
        };

        using var csv = new CsvReader(reader, config);

        // Read header to detect time column
        csv.Read();
        csv.ReadHeader();
        var headers = csv.HeaderRecord;

        if (headers == null || headers.Length == 0)
        {
            return events;
        }

        string? timeColumn = DetectTimeColumn(headers);
        if (timeColumn == null)
        {
            // No time column found, skip this file
            return events;
        }

        int timeColumnIndex = Array.IndexOf(headers, timeColumn);

        // Process each row
        while (csv.Read())
        {
            try
            {
                string? timeValue = csv.GetField(timeColumnIndex);
                if (string.IsNullOrWhiteSpace(timeValue))
                {
                    continue;
                }

                DateTime? timestamp = ParseTimestamp(timeValue);
                if (!timestamp.HasValue)
                {
                    // Skip rows with unparseable dates
                    continue;
                }

                // Generate description from up to 6 non-time columns
                string description = GenerateDescription(csv, headers, timeColumnIndex);

                events.Add(new TimelineEvent
                {
                    Timestamp = timestamp.Value,
                    Source = file.FileName,
                    GroupName = file.GroupName,
                    Description = description,
                    OriginalTimeString = timeValue
                });
            }
            catch
            {
                // Skip problematic rows without crashing
                continue;
            }
        }

        return events;
    }

    /// <summary>
    /// Detects the time column from CSV headers using pattern matching.
    /// </summary>
    private string? DetectTimeColumn(string[] headers)
    {
        foreach (var header in headers)
        {
            string normalizedHeader = header.ToLowerInvariant()
                .Replace(" ", "")
                .Replace("_", "")
                .Replace("-", "");

            foreach (var pattern in TimeColumnPatterns)
            {
                if (normalizedHeader.Contains(pattern))
                {
                    return header; // Return original header name
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Parses timestamp with multiple format support.
    /// </summary>
    private DateTime? ParseTimestamp(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        // Try DateTimeOffset.TryParse (preserves timezone)
        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTimeOffset))
        {
            return dateTimeOffset.UtcDateTime;
        }

        // Check if value is Unix epoch (numeric > 1000000000)
        if (long.TryParse(value, out long unixTimestamp) && unixTimestamp > 1000000000)
        {
            try
            {
                return DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).UtcDateTime;
            }
            catch
            {
                // Try milliseconds
                try
                {
                    return DateTimeOffset.FromUnixTimeMilliseconds(unixTimestamp).UtcDateTime;
                }
                catch
                {
                    // Invalid Unix timestamp
                }
            }
        }

        // Try common date formats with TryParseExact
        string[] formats = new[]
        {
            "yyyy-MM-ddTHH:mm:ss",
            "yyyy-MM-ddTHH:mm:ssZ",
            "yyyy-MM-ddTHH:mm:ss.fff",
            "yyyy-MM-ddTHH:mm:ss.fffZ",
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-dd HH:mm:ss.fff",
            "MM/dd/yyyy HH:mm:ss",
            "MM/dd/yyyy hh:mm:ss tt",
            "dd/MM/yyyy HH:mm:ss",
            "dd/MM/yyyy hh:mm:ss tt",
            "yyyy/MM/dd HH:mm:ss",
            "M/d/yyyy h:mm:ss tt",
            "M/d/yyyy HH:mm:ss"
        };

        foreach (var format in formats)
        {
            if (DateTime.TryParseExact(value, format, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsedDate))
            {
                return parsedDate;
            }
        }

        // Last attempt: try standard DateTime.TryParse
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var finalDate))
        {
            return finalDate;
        }

        return null;
    }

    /// <summary>
    /// Generates description by concatenating up to 6 non-time columns.
    /// </summary>
    private string GenerateDescription(CsvReader csv, string[] headers, int timeColumnIndex)
    {
        var descriptionParts = new List<string>();
        int columnCount = 0;

        for (int i = 0; i < headers.Length && columnCount < 6; i++)
        {
            if (i == timeColumnIndex)
            {
                continue; // Skip time column
            }

            string? value = csv.GetField(i);
            if (!string.IsNullOrWhiteSpace(value))
            {
                // Truncate long values to 100 characters
                if (value.Length > 100)
                {
                    value = value.Substring(0, 97) + "...";
                }

                descriptionParts.Add($"{headers[i]}={value}");
                columnCount++;
            }
        }

        return string.Join(", ", descriptionParts);
    }
}
