using System.Windows;

namespace KapeViewer
{
    /// <summary>
    /// Interaction logic for ColumnsDialog.xaml
    /// </summary>
    public partial class ColumnsDialog : Window
    {
        public class ColumnVisibilityItem
        {
            public string ColumnName { get; set; } = string.Empty;
            public bool IsVisible { get; set; }
        }

        public List<ColumnVisibilityItem> ColumnItems { get; private set; } = new();

        public ColumnsDialog()
        {
            InitializeComponent();
        }

        public void SetColumns(Dictionary<string, bool> columnVisibility)
        {
            ColumnItems = columnVisibility
                .Select(kvp => new ColumnVisibilityItem
                {
                    ColumnName = kvp.Key,
                    IsVisible = kvp.Value
                })
                .OrderBy(item => item.ColumnName)
                .ToList();

            ColumnsListBox.ItemsSource = ColumnItems;
        }

        public Dictionary<string, bool> GetColumnVisibility()
        {
            return ColumnItems.ToDictionary(item => item.ColumnName, item => item.IsVisible);
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in ColumnItems)
            {
                item.IsVisible = true;
            }
            ColumnsListBox.Items.Refresh();
        }

        private void DeselectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in ColumnItems)
            {
                item.IsVisible = false;
            }
            ColumnsListBox.Items.Refresh();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
