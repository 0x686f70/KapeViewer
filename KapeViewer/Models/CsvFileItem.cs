namespace KapeViewer.Models;

/// <summary>
/// Represents an individual CSV file in the case folder.
/// </summary>
public class CsvFileItem
{
    public string FileName { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string DisplayName => FileName;
}
