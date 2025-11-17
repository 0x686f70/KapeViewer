using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using KapeViewer.Models;
using KapeViewer.Services;
using Ookii.Dialogs.Wpf;

namespace KapeViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string _currentCasePath = string.Empty;
        private List<GroupNode> _groups = new();
        private readonly CsvScanner _csvScanner = new();
        private readonly CsvTableLoader _csvTableLoader = new();
        private readonly TimelineBuilder _timelineBuilder = new();
        private DataTable? _currentTable;
        private List<TimelineEvent> _allTimelineEvents = new();
        private List<TimelineEvent> _filteredTimelineEvents = new();
        private readonly FilterCriteria _filterCriteria = new();
        private DispatcherTimer? _searchDebounceTimer;
        private bool _isUtcMode = true;
        private Dictionary<string, bool> _columnVisibility = new();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void OpenCase_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new VistaFolderBrowserDialog
            {
                Description = "Select KAPE case folder",
                UseDescriptionForTitle = true
            };

            if (!string.IsNullOrEmpty(_currentCasePath))
            {
                dialog.SelectedPath = _currentCasePath;
            }

            if (dialog.ShowDialog(this) == true)
            {
                _currentCasePath = dialog.SelectedPath;
                LoadCase(_currentCasePath);
            }
        }

        private void LoadCase(string casePath)
        {
            try
            {
                // Scan folder for CSV files
                _groups = _csvScanner.ScanFolder(casePath);

                // Populate TreeView
                FileTreeView.ItemsSource = _groups;

                // Populate Source filter ComboBox
                PopulateSourceFilter();

                // Update status bar
                StatusBarCasePath.Text = casePath;
                int totalFiles = _groups.Sum(g => g.FileCount);
                StatusBarRowCount.Text = $"Files: {totalFiles}";
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error loading case folder: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void PopulateSourceFilter()
        {
            // Clear existing items
            SourceFilterComboBox.Items.Clear();

            // Add "All" option
            SourceFilterComboBox.Items.Add("All");

            // Add discovered group names
            foreach (var group in _groups.OrderBy(g => g.Name))
            {
                SourceFilterComboBox.Items.Add(group.Name);
            }

            // Select "All" by default
            SourceFilterComboBox.SelectedIndex = 0;
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            // Check if selected item is a CsvFileItem
            if (e.NewValue is CsvFileItem csvFile)
            {
                LoadCsvFile(csvFile);
            }
        }

        private void LoadCsvFile(CsvFileItem csvFile)
        {
            try
            {
                // Show busy cursor
                Cursor = System.Windows.Input.Cursors.Wait;

                // Load CSV file into DataTable
                _currentTable = _csvTableLoader.LoadCsv(csvFile.FullPath);

                if (_currentTable != null)
                {
                    // Bind DataTable to DataGrid
                    TableDataGrid.ItemsSource = _currentTable.DefaultView;

                    // Initialize column visibility for new file
                    _columnVisibility.Clear();
                    foreach (var column in TableDataGrid.Columns)
                    {
                        _columnVisibility[column.Header.ToString() ?? string.Empty] = true;
                    }

                    // Switch to Table tab automatically
                    MainTabControl.SelectedIndex = 0;

                    // Update status bar with file name and row count
                    StatusBarCasePath.Text = csvFile.FullPath;
                    StatusBarViewName.Text = "Table";
                    StatusBarRowCount.Text = $"Rows: {_currentTable.Rows.Count:N0}";
                }
                else
                {
                    MessageBox.Show(
                        $"Failed to load CSV file: {csvFile.FileName}",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error loading CSV file '{csvFile.FileName}':\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                // Restore cursor
                Cursor = System.Windows.Input.Cursors.Arrow;
            }
        }

        private async void BuildTimeline_Click(object sender, RoutedEventArgs e)
        {
            // Check if case is loaded
            if (_groups.Count == 0)
            {
                MessageBox.Show(
                    "Please open a case folder first.",
                    "No Case Loaded",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            // Get all CSV files from all groups
            var allFiles = _groups.SelectMany(g => g.Files).ToList();

            if (allFiles.Count == 0)
            {
                MessageBox.Show(
                    "No CSV files found in the case folder.",
                    "No Files",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            // Create progress dialog
            var progressDialog = new ProgressDialog
            {
                Owner = this
            };

            var cts = new CancellationTokenSource();
            progressDialog.SetCancellationTokenSource(cts);

            var progress = new Progress<int>(percentage =>
            {
                progressDialog.UpdateProgress(percentage);
            });

            // Show progress dialog and start timeline build
            progressDialog.Show();

            try
            {
                // Build timeline asynchronously
                _allTimelineEvents = await _timelineBuilder.BuildTimelineAsync(
                    allFiles,
                    progress,
                    cts.Token);

                // Close progress dialog
                progressDialog.Close();

                // Bind to Timeline ListView
                TimelineListView.ItemsSource = _allTimelineEvents;

                // Switch to Timeline tab automatically
                MainTabControl.SelectedIndex = 1;

                // Update status bar with event count
                StatusBarCasePath.Text = _currentCasePath;
                StatusBarViewName.Text = "Timeline";
                StatusBarRowCount.Text = $"Events: {_allTimelineEvents.Count:N0}";

                MessageBox.Show(
                    $"Timeline built successfully!\n\nTotal events: {_allTimelineEvents.Count:N0}\nFiles processed: {allFiles.Count}",
                    "Timeline Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (OperationCanceledException)
            {
                progressDialog.Close();
                MessageBox.Show(
                    "Timeline build was cancelled.",
                    "Cancelled",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                progressDialog.Close();
                MessageBox.Show(
                    $"Error building timeline:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        #region Filter Methods

        private void ApplyFilters()
        {
            // Determine which view is active and apply appropriate filters
            if (MainTabControl.SelectedIndex == 0)
            {
                // Table view
                ApplyTableFilters();
            }
            else
            {
                // Timeline view
                ApplyTimelineFilters();
            }
        }

        private void ApplyTableFilters()
        {
            if (_currentTable == null) return;

            var dataView = _currentTable.DefaultView;
            var filters = new List<string>();

            // Time range filter (detect time columns dynamically)
            if (_filterCriteria.HasTimeFilter)
            {
                var timeColumn = DetectTimeColumnInTable(_currentTable);
                if (timeColumn != null)
                {
                    if (_filterCriteria.FromDate.HasValue)
                    {
                        var fromDate = _filterCriteria.FromDate.Value;
                        filters.Add($"[{timeColumn}] >= #{fromDate:yyyy-MM-dd HH:mm:ss}#");
                    }
                    if (_filterCriteria.ToDate.HasValue)
                    {
                        var toDate = _filterCriteria.ToDate.Value;
                        filters.Add($"[{timeColumn}] <= #{toDate:yyyy-MM-dd HH:mm:ss}#");
                    }
                }
            }

            // Quick search filter (all visible columns)
            if (_filterCriteria.HasSearchFilter)
            {
                filters.Add(BuildRowFilter(dataView, _filterCriteria.SearchText));
            }

            dataView.RowFilter = filters.Count > 0 ? string.Join(" AND ", filters) : string.Empty;
            UpdateRowCount();
        }

        private string? DetectTimeColumnInTable(DataTable table)
        {
            var timePatterns = new[] { "timecreated", "timestamp", "eventtime", "lastwritetime", "created", "modified", "accessed" };
            
            foreach (DataColumn column in table.Columns)
            {
                var columnName = column.ColumnName.ToLowerInvariant().Replace(" ", "");
                if (timePatterns.Any(pattern => columnName.Contains(pattern)))
                {
                    return column.ColumnName;
                }
            }
            
            return null;
        }

        private string BuildRowFilter(DataView dataView, string searchText)
        {
            if (dataView.Table == null) return string.Empty;
            
            var escapedText = searchText.Replace("'", "''");
            var columnFilters = dataView.Table.Columns.Cast<DataColumn>()
                .Select(c => $"CONVERT([{c.ColumnName}], 'System.String') LIKE '%{escapedText}%'");
            return $"({string.Join(" OR ", columnFilters)})";
        }

        private void ApplyTimelineFilters()
        {
            if (_allTimelineEvents == null || _allTimelineEvents.Count == 0) return;

            _filteredTimelineEvents = _allTimelineEvents
                .Where(e =>
                {
                    // Time range filter
                    if (_filterCriteria.FromDate.HasValue && e.Timestamp < _filterCriteria.FromDate.Value)
                        return false;
                    if (_filterCriteria.ToDate.HasValue && e.Timestamp > _filterCriteria.ToDate.Value)
                        return false;

                    // Source group filter
                    if (_filterCriteria.HasSourceFilter && e.GroupName != _filterCriteria.SourceGroup)
                        return false;

                    // Quick search filter
                    if (_filterCriteria.HasSearchFilter)
                    {
                        var search = _filterCriteria.SearchText.ToLowerInvariant();
                        return e.Source.ToLowerInvariant().Contains(search) ||
                               e.Description.ToLowerInvariant().Contains(search) ||
                               e.Timestamp.ToString().Contains(search);
                    }

                    return true;
                })
                .Select(e => new TimelineEvent
                {
                    Timestamp = DisplayTime(e.Timestamp, _isUtcMode),
                    Source = e.Source,
                    GroupName = e.GroupName,
                    Description = e.Description,
                    OriginalTimeString = e.OriginalTimeString
                })
                .ToList();

            TimelineListView.ItemsSource = _filteredTimelineEvents;
            UpdateRowCount();
        }

        private void UpdateRowCount()
        {
            if (MainTabControl.SelectedIndex == 0)
            {
                // Table view
                if (_currentTable != null)
                {
                    var filteredCount = _currentTable.DefaultView.Count;
                    RowCountLabel.Text = $"Rows: {filteredCount:N0}";
                    StatusBarRowCount.Text = $"Rows: {filteredCount:N0}";
                }
            }
            else
            {
                // Timeline view
                var count = _filteredTimelineEvents.Count > 0 ? _filteredTimelineEvents.Count : _allTimelineEvents.Count;
                RowCountLabel.Text = $"Events: {count:N0}";
                StatusBarRowCount.Text = $"Events: {count:N0}";
            }
        }

        #endregion

        #region Filter Event Handlers

        private void DateFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;

            _filterCriteria.FromDate = FromDatePicker.SelectedDate;
            _filterCriteria.ToDate = ToDatePicker.SelectedDate;
            ApplyFilters();
        }

        private void SourceFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;

            _filterCriteria.SourceGroup = SourceFilterComboBox.SelectedItem?.ToString() ?? "All";
            ApplyFilters();
        }

        private void QuickSearch_Changed(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded) return;

            // Debounce search input (300ms delay)
            _searchDebounceTimer?.Stop();
            _searchDebounceTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(300)
            };
            _searchDebounceTimer.Tick += (s, args) =>
            {
                _searchDebounceTimer.Stop();
                _filterCriteria.SearchText = QuickSearchTextBox.Text;
                ApplyFilters();
            };
            _searchDebounceTimer.Start();
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_currentCasePath))
            {
                LoadCase(_currentCasePath);
            }
        }

        #endregion

        #region Placeholder Event Handlers

        private void ToggleUtcLocal_Click(object sender, RoutedEventArgs e)
        {
            // Toggle UTC/Local mode
            _isUtcMode = !_isUtcMode;
            _filterCriteria.IsUtcMode = _isUtcMode;

            // Update button label
            UtcLocalToggle.Content = _isUtcMode ? "UTC" : "Local";

            // Refresh the current view to show converted timestamps
            RefreshTimeDisplay();
        }

        private void RefreshTimeDisplay()
        {
            if (MainTabControl.SelectedIndex == 0)
            {
                // Table view - refresh DataGrid
                if (_currentTable != null)
                {
                    TableDataGrid.Items.Refresh();
                }
            }
            else
            {
                // Timeline view - refresh ListView
                TimelineListView.Items.Refresh();
            }
        }

        private DateTime DisplayTime(DateTime utc, bool isUtcMode)
        {
            if (isUtcMode)
                return DateTime.SpecifyKind(utc, DateTimeKind.Utc);
            else
                return utc.ToLocalTime();
        }

        private void ExportTable_Click(object sender, RoutedEventArgs e)
        {
            // Only works for Table view
            if (MainTabControl.SelectedIndex != 0 || _currentTable == null)
            {
                MessageBox.Show(
                    "Please load a CSV file in Table view before exporting.",
                    "No Data to Export",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            // Show SaveFileDialog
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                DefaultExt = ".csv",
                FileName = "export.csv"
            };

            if (dialog.ShowDialog(this) == true)
            {
                try
                {
                    ExportTableToCsv(dialog.FileName);
                    MessageBox.Show(
                        $"Table exported successfully to:\n{dialog.FileName}",
                        "Export Complete",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Error exporting table:\n{ex.Message}",
                        "Export Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        private void ExportTableToCsv(string filePath)
        {
            if (_currentTable == null) return;

            using var writer = new StreamWriter(filePath);
            using var csv = new CsvHelper.CsvWriter(writer, System.Globalization.CultureInfo.InvariantCulture);

            // Get filtered DataView
            var dataView = _currentTable.DefaultView;

            // Write headers for visible columns only
            var visibleColumns = TableDataGrid.Columns
                .Where(col => col.Visibility == Visibility.Visible)
                .Select(col => col.Header.ToString() ?? string.Empty)
                .ToList();

            foreach (var header in visibleColumns)
            {
                csv.WriteField(header);
            }
            csv.NextRecord();

            // Write data rows (respects current filter)
            foreach (System.Data.DataRowView rowView in dataView)
            {
                foreach (var columnName in visibleColumns)
                {
                    var value = rowView[columnName];
                    csv.WriteField(value);
                }
                csv.NextRecord();
            }
        }

        private void ExportTimeline_Click(object sender, RoutedEventArgs e)
        {
            // Check if timeline has been built
            if (_allTimelineEvents == null || _allTimelineEvents.Count == 0)
            {
                MessageBox.Show(
                    "Please build the global timeline before exporting.",
                    "No Timeline Data",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            // Show SaveFileDialog
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                DefaultExt = ".csv",
                FileName = "timeline_export.csv"
            };

            if (dialog.ShowDialog(this) == true)
            {
                try
                {
                    ExportTimelineToCsv(dialog.FileName);
                    MessageBox.Show(
                        $"Timeline exported successfully to:\n{dialog.FileName}",
                        "Export Complete",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Error exporting timeline:\n{ex.Message}",
                        "Export Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        private void ExportTimelineToCsv(string filePath)
        {
            using var writer = new StreamWriter(filePath);
            using var csv = new CsvHelper.CsvWriter(writer, System.Globalization.CultureInfo.InvariantCulture);

            // Write headers
            csv.WriteField("Timestamp");
            csv.WriteField("Source");
            csv.WriteField("GroupName");
            csv.WriteField("Description");
            csv.NextRecord();

            // Get events to export (use filtered if filters are applied, otherwise all)
            var eventsToExport = _filteredTimelineEvents.Count > 0 ? _filteredTimelineEvents : _allTimelineEvents;

            // Write data rows
            foreach (var evt in eventsToExport)
            {
                // Apply UTC/Local conversion based on current mode
                var displayTimestamp = DisplayTime(evt.Timestamp, _isUtcMode);
                csv.WriteField(displayTimestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                csv.WriteField(evt.Source);
                csv.WriteField(evt.GroupName);
                csv.WriteField(evt.Description);
                csv.NextRecord();
            }
        }

        private void ShowColumnsDialog_Click(object sender, RoutedEventArgs e)
        {
            // Only works for Table view
            if (MainTabControl.SelectedIndex != 0 || _currentTable == null)
            {
                MessageBox.Show(
                    "Column visibility can only be changed in Table view with a loaded CSV file.",
                    "Table View Required",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            // Initialize column visibility dictionary if empty
            if (_columnVisibility.Count == 0)
            {
                foreach (var column in TableDataGrid.Columns)
                {
                    _columnVisibility[column.Header.ToString() ?? string.Empty] = column.Visibility == Visibility.Visible;
                }
            }

            // Create and show dialog
            var dialog = new ColumnsDialog
            {
                Owner = this
            };
            dialog.SetColumns(_columnVisibility);

            if (dialog.ShowDialog() == true)
            {
                // Apply column visibility changes
                _columnVisibility = dialog.GetColumnVisibility();
                ApplyColumnVisibility();
            }
        }

        private void ApplyColumnVisibility()
        {
            foreach (var column in TableDataGrid.Columns)
            {
                var headerText = column.Header.ToString() ?? string.Empty;
                if (_columnVisibility.TryGetValue(headerText, out bool isVisible))
                {
                    column.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
                }
            }
        }

        private void CopyRows_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string clipboardText;

                if (MainTabControl.SelectedIndex == 0)
                {
                    // Table view - copy selected rows from DataGrid
                    clipboardText = CopyTableRows();
                }
                else
                {
                    // Timeline view - copy selected rows from ListView
                    clipboardText = CopyTimelineRows();
                }

                if (!string.IsNullOrEmpty(clipboardText))
                {
                    Clipboard.SetText(clipboardText);
                    StatusBarRowCount.Text = $"Copied {clipboardText.Split('\n').Length - 1} rows to clipboard";
                }
                else
                {
                    MessageBox.Show(
                        "No rows selected. Please select one or more rows to copy.",
                        "No Selection",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error copying to clipboard:\n{ex.Message}",
                    "Copy Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private string CopyTableRows()
        {
            if (TableDataGrid.SelectedItems.Count == 0)
                return string.Empty;

            var sb = new System.Text.StringBuilder();

            // Get visible columns
            var visibleColumns = TableDataGrid.Columns
                .Where(col => col.Visibility == Visibility.Visible)
                .Select(col => col.Header.ToString() ?? string.Empty)
                .ToList();

            // Write headers (tab-separated for Excel compatibility)
            sb.AppendLine(string.Join("\t", visibleColumns));

            // Write selected rows
            foreach (System.Data.DataRowView rowView in TableDataGrid.SelectedItems)
            {
                var values = visibleColumns.Select(colName => rowView[colName]?.ToString() ?? string.Empty);
                sb.AppendLine(string.Join("\t", values));
            }

            return sb.ToString();
        }

        private string CopyTimelineRows()
        {
            if (TimelineListView.SelectedItems.Count == 0)
                return string.Empty;

            var sb = new System.Text.StringBuilder();

            // Write headers (tab-separated for Excel compatibility)
            sb.AppendLine("Timestamp\tSource\tDescription");

            // Write selected rows
            foreach (TimelineEvent evt in TimelineListView.SelectedItems)
            {
                var timestamp = DisplayTime(evt.Timestamp, _isUtcMode).ToString("yyyy-MM-dd HH:mm:ss.fff");
                sb.AppendLine($"{timestamp}\t{evt.Source}\t{evt.Description}");
            }

            return sb.ToString();
        }

        private void AutoSizeColumns_Click(object sender, RoutedEventArgs e)
        {
            // Only works for Table view
            if (MainTabControl.SelectedIndex != 0 || _currentTable == null)
            {
                MessageBox.Show(
                    "Auto-size columns can only be used in Table view with a loaded CSV file.",
                    "Table View Required",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            try
            {
                // Show busy cursor
                Cursor = System.Windows.Input.Cursors.Wait;

                // Auto-size all visible columns
                foreach (var column in TableDataGrid.Columns)
                {
                    if (column.Visibility == Visibility.Visible)
                    {
                        // Use DataGrid's built-in auto-sizing
                        // This will measure based on visible rows (virtualization limits measurement automatically)
                        column.Width = DataGridLength.Auto;
                        
                        // Force update
                        TableDataGrid.UpdateLayout();
                        
                        // Get the actual width and set it as fixed to improve performance
                        var actualWidth = column.ActualWidth;
                        column.Width = new DataGridLength(actualWidth);
                    }
                }

                StatusBarRowCount.Text = "Columns auto-sized";
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error auto-sizing columns:\n{ex.Message}",
                    "Auto-size Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                // Restore cursor
                Cursor = System.Windows.Input.Cursors.Arrow;
            }
        }

        #endregion
    }
}