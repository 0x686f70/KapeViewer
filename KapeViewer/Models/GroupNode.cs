using System.Collections.ObjectModel;

namespace KapeViewer.Models;

/// <summary>
/// Represents a top-level folder group in the TreeView.
/// </summary>
public class GroupNode
{
    public string Name { get; set; } = string.Empty;
    public ObservableCollection<CsvFileItem> Files { get; set; } = new();
    public int FileCount => Files.Count;
}
