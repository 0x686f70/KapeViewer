# KAPE Viewer Design Document

## Overview

KAPE Viewer is a WPF desktop application built on .NET 8.0 that provides DFIR engineers with efficient browsing and analysis capabilities for KAPE/!EZParser CSV outputs. The application uses a Model-Service-View architecture with WPF MVVM patterns, leveraging CsvHelper for high-performance CSV parsing and DataGrid virtualization for handling large datasets.

## Architecture

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    MainWindow (View)                     │
│  ┌────────────────────────────────────────────────────┐ │
│  │  Menu Bar (File, Tools, Tabs, View, Help)          │ │
│  └────────────────────────────────────────────────────┘ │
│  ┌────────────────────────────────────────────────────┐ │
│  │  Toolbar (Filters: From/To, Source, Search)        │ │
│  └────────────────────────────────────────────────────┘ │
│  ┌──────────────┬─────────────────────────────────────┐ │
│  │  TreeView    │  TabControl                         │ │
│  │  (Groups &   │  ┌─────────────┬─────────────────┐ │ │
│  │   Files)     │  │ Table Tab   │ Timeline Tab    │ │ │
│  │              │  │ (DataGrid)  │ (ListView)      │ │ │
│  │              │  └─────────────┴─────────────────┘ │ │
│  └──────────────┴─────────────────────────────────────┘ │
│  ┌────────────────────────────────────────────────────┐ │
│  │  Status Bar (Case Path, View, Row Count)           │ │
│  └────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│                    Services Layer                        │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  │
│  │ CsvScanner   │  │CsvTableLoader│  │TimelineBuilder│ │
│  └──────────────┘  └──────────────┘  └──────────────┘  │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│                     Models Layer                         │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  │
│  │ GroupNode    │  │ CsvFileItem  │  │TimelineEvent │  │
│  └──────────────┘  └──────────────┘  └──────────────┘  │
└─────────────────────────────────────────────────────────┘
```

### Technology Stack

- **Framework**: .NET 8.0 (net8.0-windows)
- **UI Framework**: WPF with XAML
- **CSV Parsing**: CsvHelper library with CsvDataReader for streaming
- **Dialogs**: Ookii.Dialogs.Wpf for folder selection
- **Data Binding**: WPF DataGrid with DataTable binding
- **Performance**: DataGrid virtualization, streaming CSV reads

## Components and Interfaces

### 1. Models

#### GroupNode.cs
Represents a top-level folder group in the TreeView.

```csharp
public class GroupNode
{
    public string Name { get; set; }
    public ObservableCollection<CsvFileItem> Files { get; set; }
    public int FileCount => Files.Count;
}
```

**Responsibilities:**
- Store group name (e.g., "ProgramExecution", "EventLogs")
- Maintain collection of CSV files within the group
- Provide file count for display

#### CsvFileItem.cs
Represents an individual CSV file.

```csharp
public class CsvFileItem
{
    public string FileName { get; set; }
    public string FullPath { get; set; }
    public string GroupName { get; set; }
    public long FileSize { get; set; }
    public string DisplayName => FileName;
}
```

**Responsibilities:**
- Store file metadata (name, path, group, size)
- Provide display name for TreeView binding

#### TimelineEvent.cs
Represents a single event in the merged timeline.

```csharp
public class TimelineEvent
{
    public DateTime Timestamp { get; set; }
    public string Source { get; set; }
    public string GroupName { get; set; }
    public string Description { get; set; }
    public string OriginalTimeString { get; set; }
}
```

**Responsibilities:**
- Store parsed timestamp for sorting (stored as UTC internally)
- Store source CSV filename
- Store group name for source filtering
- Store concatenated description from multiple columns
- Preserve original time string for display

### 2. Services

#### CsvScanner.cs
Scans directories and groups CSV files.

```csharp
public class CsvScanner
{
    public List<GroupNode> ScanFolder(string rootPath)
    public void RefreshScan(string rootPath, List<GroupNode> existingGroups)
}
```

**Responsibilities:**
- Recursively scan for *.csv files
- Group files by first-level subfolder
- Assign ungrouped files to "Other" category
- Support refresh operation

**Algorithm:**
1. Use `Directory.EnumerateFiles(rootPath, "*.csv", SearchOption.AllDirectories)`
2. For each file, extract relative path from root
3. Determine group name from first path segment
4. Create GroupNode if not exists
5. Add CsvFileItem to appropriate group
6. Sort groups alphabetically

#### CsvTableLoader.cs
Loads CSV files into DataTable format.

```csharp
public class CsvTableLoader
{
    public DataTable LoadCsv(string filePath)
    public DataTable ApplyFilters(DataTable source, FilterCriteria criteria)
}
```

**Responsibilities:**
- Stream CSV data using CsvHelper's CsvDataReader
- Convert to DataTable for DataGrid binding
- Apply filters (time range, text search, source)
- Handle malformed CSV gracefully

**Algorithm:**
1. Open file with StreamReader
2. Create CsvReader with CsvHelper configuration
3. Use CsvDataReader to get IDataReader interface
4. Call DataTable.Load(dataReader) for efficient streaming
5. Return DataTable for binding

**Performance Considerations:**
- Use CsvDataReader instead of reading all records into memory
- Enable DataGrid virtualization for large datasets
- Apply filters on DataView for efficient filtering

#### TimelineBuilder.cs
Builds merged timeline from multiple CSV files.

```csharp
public class TimelineBuilder
{
    public async Task<List<TimelineEvent>> BuildTimelineAsync(
        List<CsvFileItem> files, 
        IProgress<int> progress,
        CancellationToken cancellationToken)
    private string DetectTimeColumn(CsvReader csv)
    private DateTime? ParseTimestamp(string value)
}
```

**Responsibilities:**
- Process all CSV files in case folder asynchronously
- Auto-detect time columns using heuristics
- Parse timestamps with multiple format support
- Create TimelineEvent objects with GroupName mapping
- Sort events chronologically
- Report progress for long operations
- Support cancellation for long-running operations

**Time Column Detection Algorithm:**
1. Read CSV header row only (first line)
2. Optionally peek at 1-2 data rows to validate format
3. Normalize column names (lowercase, remove spaces)
4. Check against patterns: timecreated, timestamp, eventtime, lastwritetime, created, modified, accessed
5. Return first matching column name
6. If no match, return null (skip file)

**Timestamp Parsing Strategy:**
- Try DateTimeOffset.TryParse with InvariantCulture (preserves timezone)
- Check if value is Unix epoch (numeric > 1000000000), convert to DateTime
- Try DateTime.TryParseExact with common formats:
  - ISO 8601: "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-ddTHH:mm:ssZ"
  - US format: "MM/dd/yyyy HH:mm:ss"
  - UK format: "dd/MM/yyyy HH:mm:ss"
  - KAPE common: "yyyy-MM-dd HH:mm:ss.fff"
- Store all timestamps as UTC internally
- If all fail, skip row (no crash)

**Description Generation:**
- Take up to 6 non-time columns
- Format as "Column1=Value1, Column2=Value2, ..."
- Truncate long values to 100 characters
- Skip empty values

**GroupName Mapping:**
- For each CsvFileItem, use its GroupName property
- Assign to TimelineEvent.GroupName for source filtering

### 3. MainWindow (View)

#### MainWindow.xaml
Defines the UI layout using WPF controls.

**Layout Structure:**
```xml
<DockPanel>
  <Menu DockPanel.Dock="Top">
    <!-- File, Tools, Tabs, View, Help -->
  </Menu>
  <ToolBarTray DockPanel.Dock="Top">
    <ToolBar>
      <!-- Complete toolbar with all controls -->
    </ToolBar>
  </ToolBarTray>
  <StatusBar DockPanel.Dock="Bottom">
    <!-- Case Path, View Name, Row Count -->
  </StatusBar>
  <Grid>
    <Grid.ColumnDefinitions>
      <ColumnDefinition Width="250"/>
      <ColumnDefinition Width="5"/>
      <ColumnDefinition Width="*"/>
    </Grid.ColumnDefinitions>
    <TreeView Grid.Column="0"/>
    <GridSplitter Grid.Column="1"/>
    <TabControl Grid.Column="2">
      <TabItem Header="Table">
        <DataGrid/>
      </TabItem>
      <TabItem Header="Timeline">
        <ListView/>
      </TabItem>
    </TabControl>
  </Grid>
</DockPanel>
```

### Toolbar Specification

The toolbar contains all filter controls and quick-access buttons, arranged left to right:

1. **Open Case Button** (Folder icon)
   - Opens VistaFolderBrowserDialog
   - Triggers case folder scan

2. **Refresh Button** (Refresh icon)
   - Re-scans current case folder
   - Preserves current filters

3. **Build Global Timeline Button**
   - Merges all CSVs into timeline
   - Shows progress dialog with cancel option

4. **Separator**

5. **From DateTimePicker**
   - Label: "From:"
   - Nullable DateTime
   - Triggers filter on change

6. **To DateTimePicker**
   - Label: "To:"
   - Nullable DateTime
   - Triggers filter on change

7. **UTC/Local Toggle Button**
   - Checkable button
   - Label shows current mode
   - Converts all displayed timestamps

8. **Separator**

9. **Source Filter ComboBox**
   - Label: "Source:"
   - Items: "All", then group names
   - Triggers filter on selection

10. **Quick Search TextBox**
    - Label: "Search:"
    - Placeholder: "Search all columns..."
    - Triggers filter on text change (debounced 300ms)

11. **Separator**

12. **Export Dropdown Button**
    - Menu items: "Export Current Table", "Export Timeline"
    - Opens SaveFileDialog

13. **Columns Button**
    - Opens column visibility dialog
    - Shows checkboxes for all columns

14. **Copy Button**
    - Copies selected rows to clipboard as CSV
    - Includes headers

15. **Auto-size Columns Button**
    - Adjusts column widths to content
    - Uses visible rows only (max 500 rows for performance)

16. **Spacer** (pushes next item to right)

17. **Row Count Label**
    - Format: "Rows: 12,345" or "Events: 12,345"
    - Updates on filter changes

**Key Control Configurations:**

**TreeView:**
- HierarchicalDataTemplate for GroupNode → CsvFileItem
- SelectionMode: Single
- Event: SelectedItemChanged

**DataGrid:**
- AutoGenerateColumns: True
- IsReadOnly: True
- EnableRowVirtualization: True
- EnableColumnVirtualization: True
- SelectionMode: Extended
- CanUserSortColumns: True
- CanUserReorderColumns: True

**Timeline ListView:**
- View: GridView with 3 columns (Timestamp, Source, Description)
- VirtualizingPanel.IsVirtualizing: True
- VirtualizingPanel.VirtualizationMode: Recycling

#### MainWindow.xaml.cs
Implements event handlers and UI logic.

**Key Methods:**
```csharp
// Menu/Toolbar Actions
private void OpenCase_Click(object sender, RoutedEventArgs e)
private void Refresh_Click(object sender, RoutedEventArgs e)
private async void BuildTimeline_Click(object sender, RoutedEventArgs e)
private void ExportTable_Click(object sender, RoutedEventArgs e)
private void ExportTimeline_Click(object sender, RoutedEventArgs e)
private void CopyRows_Click(object sender, RoutedEventArgs e)
private void ShowColumnsDialog_Click(object sender, RoutedEventArgs e)
private void AutoSizeColumns_Click(object sender, RoutedEventArgs e)
private void ToggleUtcLocal_Click(object sender, RoutedEventArgs e)

// Navigation
private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)

// Filtering
private void ApplyFilters()
private void ApplyTableFilters()
private void ApplyTimelineFilters()
private string BuildRowFilter(DataView dataView, string searchText)

// Display
private void UpdateStatusBar()
private DateTime DisplayTime(DateTime utc, bool isUtcMode)

// Helpers
private void ShowBusyCursor(bool isBusy)
private void UpdateRowCount()
```

**State Management:**
- `string _currentCasePath` - Current case folder path
- `List<GroupNode> _groups` - All discovered groups and files
- `DataTable _currentTable` - Current CSV data for Table tab
- `List<TimelineEvent> _allTimelineEvents` - All timeline events (unfiltered)
- `List<TimelineEvent> _filteredTimelineEvents` - Filtered timeline events
- `FilterCriteria _filterCriteria` - Current filter settings
- `bool _isUtcMode` - UTC/Local display mode
- `Dictionary<string, bool> _columnVisibility` - Column show/hide state
- `CsvFileItem _currentFile` - Currently selected CSV file
- `string _currentGroup` - Currently selected group
- `string _currentView` - "Table" or "Timeline"
- `CancellationTokenSource _timelineCts` - For canceling timeline build

## Data Models

### FilterCriteria
```csharp
public class FilterCriteria
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string SourceGroup { get; set; } // "All" or specific group name
    public string SearchText { get; set; }
    public bool IsUtcMode { get; set; }
    
    public bool HasTimeFilter => FromDate.HasValue || ToDate.HasValue;
    public bool HasSourceFilter => !string.IsNullOrEmpty(SourceGroup) && SourceGroup != "All";
    public bool HasSearchFilter => !string.IsNullOrWhiteSpace(SearchText);
}
```

### Filter Application Strategy

**For Table View (DataTable):**
```csharp
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
                filters.Add($"[{timeColumn}] >= #{_filterCriteria.FromDate:yyyy-MM-dd HH:mm:ss}#");
            if (_filterCriteria.ToDate.HasValue)
                filters.Add($"[{timeColumn}] <= #{_filterCriteria.ToDate:yyyy-MM-dd HH:mm:ss}#");
        }
    }
    
    // Quick search filter (all visible columns)
    if (_filterCriteria.HasSearchFilter)
    {
        filters.Add(BuildRowFilter(dataView, _filterCriteria.SearchText));
    }
    
    dataView.RowFilter = string.Join(" AND ", filters);
    UpdateRowCount();
}

private string BuildRowFilter(DataView dataView, string searchText)
{
    var escapedText = searchText.Replace("'", "''");
    var columnFilters = dataView.Table.Columns.Cast<DataColumn>()
        .Where(c => IsColumnVisible(c.ColumnName))
        .Select(c => $"CONVERT([{c.ColumnName}], 'System.String') LIKE '%{escapedText}%'");
    return $"({string.Join(" OR ", columnFilters)})";
}
```

**For Timeline View (List<TimelineEvent>):**
```csharp
private void ApplyTimelineFilters()
{
    if (_allTimelineEvents == null) return;
    
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
        .ToList();
    
    TimelineListView.ItemsSource = _filteredTimelineEvents;
    UpdateRowCount();
}
```

### UTC/Local Time Display Strategy

**Internal Storage:**
- All timestamps stored as UTC (DateTime with DateTimeKind.Utc)
- Conversion happens only at display time

**Display Conversion:**
```csharp
private DateTime DisplayTime(DateTime utc, bool isUtcMode)
{
    if (isUtcMode)
        return DateTime.SpecifyKind(utc, DateTimeKind.Utc);
    else
        return utc.ToLocalTime();
}
```

**DataGrid Implementation:**
- Use DataGridTemplateColumn with IValueConverter for time columns
- Converter checks _isUtcMode flag and converts accordingly
- Avoids modifying source DataTable

**ListView Implementation:**
- Bind to computed property or use converter
- Update ItemsSource when toggle changes

## Error Handling

### CSV Parsing Errors
- **Strategy**: Try-catch around CsvReader operations
- **Action**: Log error, show MessageBox with filename, skip file
- **User Impact**: Minimal - other files continue to load

### File Access Errors
- **Strategy**: Check file existence and permissions before reading
- **Action**: Show MessageBox with specific error (file locked, access denied)
- **User Impact**: User can close locking application and retry

### Timeline Building Errors
- **Strategy**: Try-catch per CSV file, continue with remaining files
- **Action**: Log skipped files, show summary at end
- **User Impact**: Partial timeline generated, user informed of skipped files

### Memory Constraints
- **Strategy**: Use streaming reads, virtualization, dispose resources
- **Action**: Monitor memory usage, show warning if approaching limits
- **User Impact**: Application remains responsive with large datasets

### Invalid Date Formats
- **Strategy**: Multiple parse attempts with different formats
- **Action**: Skip rows that cannot be parsed, no crash
- **User Impact**: Timeline may have fewer events, but remains functional

## Testing Strategy

### Unit Testing
- **CsvScanner**: Test grouping logic with mock file structures
- **TimelineBuilder**: Test time column detection with various CSV headers
- **TimelineBuilder**: Test timestamp parsing with different formats
- **FilterCriteria**: Test filter logic with sample data

### Integration Testing
- **CSV Loading**: Test with real KAPE output samples
- **Timeline Merge**: Test with SampleData/parsed folder
- **Filter Application**: Test combined filters on large datasets
- **Export**: Verify exported CSV matches displayed data

### UI Testing
- **TreeView Navigation**: Verify file selection loads correct table
- **Tab Switching**: Verify Table/Timeline tabs display correct data
- **Menu Actions**: Verify all menu items trigger correct operations
- **Toolbar Filters**: Verify filters update DataGrid and ListView

### Performance Testing
- **Large CSV Files**: Test with 100K+ row CSV files
- **Many Files**: Test with 100+ CSV files in case folder
- **Timeline Build**: Measure time to merge 50+ CSV files
- **Filter Performance**: Measure filter application time on large datasets

**Performance Targets (Best Effort):**
- Load 100K row CSV: < 2 seconds (SSD), < 5 seconds (HDD)
- Build timeline from 50 files: < 10 seconds
- Apply filters: < 500ms
- UI remains responsive during operations (async/await)
- Auto-size columns: < 1 second on filtered dataset

**Note:** Actual performance depends on hardware (CPU, disk speed, RAM) and CSV complexity.

### Manual Testing with SampleData
1. Open SampleData/parsed folder
2. Verify all groups appear in TreeView
3. Click each CSV file, verify table loads
4. Build Global Timeline, verify events appear sorted
5. Apply From/To filters, verify row count updates
6. Apply Source filter, verify only selected group shows
7. Enter Quick Search text, verify filtering works
8. Export current table, verify CSV opens in Excel
9. Toggle UTC/Local, verify timestamps convert
10. Show/hide columns, verify DataGrid updates

## Implementation Notes

### WPF Best Practices
- Use data binding instead of manual UI updates
- Implement INotifyPropertyChanged for dynamic properties
- Use Commands for menu/toolbar actions (optional, can use Click events)
- Dispose IDisposable resources (StreamReader, CsvReader)

### Performance Optimizations
- Enable DataGrid virtualization
- Use CsvDataReader for streaming
- Apply filters on DataView, not in-memory collections
- Use background tasks for timeline building (Task.Run)
- Show progress dialog for long operations

### Export and Clipboard Operations

**Export Current Table:**
- Export DataView with current RowFilter applied (respects all filters)
- Export only visible columns (respects column visibility settings)
- Include column headers
- Use CsvHelper for proper CSV formatting and escaping

**Export Timeline:**
- Export _filteredTimelineEvents (respects all filters)
- Include Timestamp, Source, GroupName, Description columns
- Apply UTC/Local conversion based on current mode
- Format timestamps consistently

**Copy to Clipboard:**
- Copy selected rows from DataGrid or ListView
- Format as CSV with headers
- Use tab-separated values for Excel compatibility
- Handle multi-row selection

**Auto-size Columns:**
- Measure column widths based on visible rows only
- Limit measurement to first 500 rows after filtering to avoid lag
- Use DataGrid's built-in column width calculation
- Skip hidden columns

### User Experience
- Show busy cursor during operations
- Display progress bar for timeline building with cancel button
- Provide meaningful error messages
- Remember last opened case folder
- Auto-select first file after case load
- Preserve filter settings between file selections
- Debounce Quick Search input (300ms delay)

### Deployment
- Publish as self-contained single-file executable
- Include all dependencies (no .NET runtime required)
- Target win-x64 platform
- Executable size: ~80-100 MB (self-contained)

**Publish Command:**
```bash
dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true --self-contained true
```

**Output Location:**
```
bin/Release/net8.0-windows/win-x64/publish/KapeViewer.exe
```

## Security Considerations

### File System Access
- Validate folder paths before scanning
- Handle access denied errors gracefully
- Do not follow symbolic links (prevent directory traversal)

### CSV Injection
- Display CSV data as read-only (no editing)
- Do not execute formulas from CSV files
- Sanitize data before export (escape special characters)

### Memory Safety
- Limit maximum row count for single CSV (e.g., 1 million rows)
- Dispose streams and readers properly
- Use streaming reads to avoid loading entire files

## Version Control Plan

### Git Repository Initialization

If repository is not initialized:
```bash
git init
git branch -M main
git add .
git commit -m "chore: initial commit"
git remote add origin <repository-url>
git push -u origin main
```

### Development Milestones and Commits

Each milestone must be committed with Conventional Commits format after build/runtime errors are fixed.

**Milestone 1: Project Scaffolding**
- Create solution and project structure
- Add NuGet packages (CsvHelper, Ookii.Dialogs.Wpf)
- Create Models, Services folders
- Create basic App.xaml and MainWindow.xaml

```bash
git add .
git commit -m "chore: scaffold WPF project with models and services structure"
git push -u origin main
```

**Milestone 2: CSV Scanner and File Discovery**
- Implement CsvScanner.cs
- Implement GroupNode.cs and CsvFileItem.cs
- Add TreeView binding in MainWindow
- Test with SampleData/parsed

```bash
git add .
git commit -m "feat: implement CSV scanner and file grouping"
git push
```

**Milestone 3: Table View and CSV Loading**
- Implement CsvTableLoader.cs
- Add DataGrid with virtualization
- Implement TreeView selection handler
- Test loading individual CSV files

```bash
git add .
git commit -m "feat: implement table view with CSV loading"
git push
```

**Milestone 4: Timeline Merge**
- Implement TimelineBuilder.cs with async support
- Implement TimelineEvent.cs with GroupName
- Add Timeline ListView
- Add Build Global Timeline button
- Add progress dialog with cancellation

```bash
git add .
git commit -m "feat: implement global timeline merge with progress"
git push
```

**Milestone 5: Toolbar and Filters**
- Add complete toolbar with all 17 controls
- Implement filter application for Table and Timeline
- Add UTC/Local toggle with time conversion
- Add Source filter ComboBox
- Add Quick Search with debouncing

```bash
git add .
git commit -m "feat: add toolbar filters and UTC/Local toggle"
git push
```

**Milestone 6: Column Management and Export**
- Implement Columns dialog for visibility control
- Implement Export Current Table
- Implement Export Timeline
- Implement Copy to Clipboard
- Implement Auto-size Columns

```bash
git add .
git commit -m "feat: add column management and export functionality"
git push
```

**Milestone 7: Menu Bar and Polish**
- Add complete menu bar (File, Tools, View, Help)
- Add status bar with case path and counts
- Add keyboard shortcuts
- Add error handling and user feedback
- Final testing with SampleData/parsed

```bash
git add .
git commit -m "feat: add menu bar and final polish"
git push
```

**Milestone 8: Bug Fixes and Optimization**
- Fix any runtime errors
- Optimize performance
- Add missing error handling
- Update documentation

```bash
git add .
git commit -m "fix: resolve runtime errors and optimize performance"
git push
```

### Commit Message Format

- **feat:** New feature implementation
- **fix:** Bug fixes
- **chore:** Maintenance tasks (scaffolding, dependencies)
- **docs:** Documentation updates
- **refactor:** Code refactoring without feature changes
- **test:** Adding or updating tests

## Future Enhancements (Out of Scope)

- Charts and visualizations (OxyPlot/LiveCharts)
- Advanced filtering with regex support
- Bookmarking/tagging events
- Multi-case comparison
- Export to other formats (JSON, XML)
- Plugin system for custom parsers
- Dark theme support
