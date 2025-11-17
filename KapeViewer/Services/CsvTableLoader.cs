using CsvHelper;
using CsvHelper.Configuration;
using System.Data;
using System.Globalization;
using System.IO;

namespace KapeViewer.Services;

/// <summary>
/// Loads CSV files into DataTable format for DataGrid binding.
/// </summary>
public class CsvTableLoader
{
    /// <summary>
    /// Loads a CSV file into a DataTable using streaming for efficient memory usage.
    /// </summary>
    /// <param name="filePath">Full path to the CSV file</param>
    /// <returns>DataTable containing the CSV data, or null if loading fails</returns>
    public DataTable? LoadCsv(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return null;
        }

        try
        {
            var dataTable = new DataTable();

            using var reader = new StreamReader(filePath);
            
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                BadDataFound = null, // Ignore bad data instead of throwing
                MissingFieldFound = null, // Ignore missing fields
                TrimOptions = TrimOptions.Trim
            };

            using var csv = new CsvReader(reader, config);
            using var dataReader = new CsvDataReader(csv);
            
            // Stream CSV data directly into DataTable
            dataTable.Load(dataReader);

            return dataTable;
        }
        catch (Exception ex)
        {
            // Log error and return null - caller will handle the error
            System.Diagnostics.Debug.WriteLine($"Error loading CSV file {filePath}: {ex.Message}");
            throw new InvalidOperationException($"Failed to load CSV file: {Path.GetFileName(filePath)}", ex);
        }
    }
}
