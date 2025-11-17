namespace KapeViewer.Models;

/// <summary>
/// Represents a single event in the merged timeline.
/// </summary>
public class TimelineEvent
{
    public DateTime Timestamp { get; set; }
    public string Source { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string OriginalTimeString { get; set; } = string.Empty;
}
