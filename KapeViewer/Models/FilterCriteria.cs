namespace KapeViewer.Models
{
    public class FilterCriteria
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string SourceGroup { get; set; } = "All";
        public string SearchText { get; set; } = string.Empty;
        public bool IsUtcMode { get; set; } = true;

        public bool HasTimeFilter => FromDate.HasValue || ToDate.HasValue;
        public bool HasSourceFilter => !string.IsNullOrEmpty(SourceGroup) && SourceGroup != "All";
        public bool HasSearchFilter => !string.IsNullOrWhiteSpace(SearchText);
    }
}
