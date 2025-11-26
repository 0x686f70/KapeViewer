# Implementation Plan

> **Note:** After completing each task, automatically commit and push changes to Git repository.
> - Command: `git add .` → `git commit -m "Task X: [description]"` → `git push origin main`

- [x] 1. Set up project structure and core models
  - Create KapeViewer.sln solution file
  - Create KapeViewer.csproj with .NET 8.0 SDK-style configuration (OutputType=WinExe, TargetFramework=net8.0-windows, UseWPF=true, Nullable=enable)
  - Add NuGet packages: CsvHelper and Ookii.Dialogs.Wpf
  - Create Models folder with CsvFileItem.cs, GroupNode.cs, TimelineEvent.cs, and FilterCriteria.cs classes
  - Create Services folder with CsvScanner.cs, CsvTableLoader.cs, and TimelineBuilder.cs
  - Create Converters folder with UtcLocalTimeConverter.cs
  - Create basic App.xaml and App.xaml.cs with application entry point
  - Create basic MainWindow.xaml and MainWindow.xaml.cs with empty window
  - Initialize Git repository if not exists (git init, git branch -M main)
  - _Requirements: 1.1, 1.2, 1.3, 1.4, 10.1_

- [x] 2. Implement CSV scanner and file discovery service
  - [x] 2.1 Implement CsvScanner.cs service class
    - Write ScanFolder method to recursively find *.csv files using Directory.EnumerateFiles
    - Implement grouping logic by first-level subfolder name
    - Assign ungrouped files to "Other" category
    - Return List<GroupNode> with populated Files collections
    - Add RefreshScan method for re-scanning current folder
    - _Requirements: 2.2, 2.3, 2.4_

  - [x] 2.2 Add TreeView to MainWindow.xaml for file navigation
    - Add TreeView control in left column (Width=250) with GridSplitter
    - Create HierarchicalDataTemplate for GroupNode → CsvFileItem binding
    - Set TreeView properties: SelectionMode=Single
    - Add SelectedItemChanged event handler
    - _Requirements: 2.5, 11.3_

  - [x] 2.3 Implement Open Case functionality
    - Add File menu with "Open Case" menu item
    - Implement OpenCase_Click handler using VistaFolderBrowserDialog
    - Call CsvScanner.ScanFolder and populate TreeView ItemsSource
    - Store current case path in _currentCasePath field
    - Update status bar with case path
    - _Requirements: 2.1, 4.2_

- [x] 3. Implement table view with CSV loading
  - [x] 3.1 Implement CsvTableLoader.cs service class
    - Write LoadCsv method using CsvHelper's CsvDataReader
    - Stream CSV data into DataTable using DataTable.Load(IDataReader)
    - Handle CSV parsing errors gracefully (try-catch, skip malformed files)
    - Return DataTable for DataGrid binding
    - _Requirements: 3.2_

  - [x] 3.2 Add DataGrid to MainWindow.xaml Table tab
    - Add TabControl in right column with "Table" and "Timeline" tabs
    - Add DataGrid in Table tab with virtualization enabled
    - Set DataGrid properties: AutoGenerateColumns=True, IsReadOnly=True, EnableRowVirtualization=True, EnableColumnVirtualization=True
    - Set SelectionMode=Extended, CanUserSortColumns=True, CanUserReorderColumns=True
    - _Requirements: 3.3, 3.4, 3.5, 11.4_

  - [x] 3.3 Implement TreeView selection handler to load CSV
    - Implement TreeView_SelectedItemChanged event handler
    - Check if selected item is CsvFileItem
    - Call CsvTableLoader.LoadCsv with file path
    - Bind DataTable to DataGrid.ItemsSource
    - Switch to Table tab automatically
    - Update status bar with file name and row count
    - _Requirements: 3.1, 11.5_

- [x] 4. Implement global timeline merge functionality
  - [x] 4.1 Implement TimelineBuilder.cs service class
    - Write BuildTimelineAsync method with IProgress<int> and CancellationToken parameters
    - Implement DetectTimeColumn method with case-insensitive pattern matching (timecreated, timestamp, eventtime, lastwritetime, created, modified, accessed)
    - Implement ParseTimestamp method with multiple format support (DateTimeOffset.TryParse, Unix epoch detection, TryParseExact with common formats)
    - For each CSV file, detect time column, parse rows, create TimelineEvent objects with Timestamp, Source, GroupName, Description
    - Generate Description by concatenating up to 6 non-time columns as "Col=Value"
    - Map GroupName from CsvFileItem to TimelineEvent
    - Skip rows with unparseable dates (no crash)
    - Sort all events by Timestamp ascending
    - Return List<TimelineEvent>
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.7_

  - [x] 4.2 Add Timeline ListView to MainWindow.xaml
    - Add ListView in Timeline tab with GridView
    - Create three columns: Timestamp, Source, Description
    - Enable virtualization: VirtualizingPanel.IsVirtualizing=True, VirtualizationMode=Recycling
    - _Requirements: 5.6, 11.4_

  - [x] 4.3 Implement Build Global Timeline button and progress dialog
    - Add Tools menu with "Build Global Timeline" menu item
    - Add toolbar button for Build Global Timeline
    - Create ProgressDialog.xaml and ProgressDialog.xaml.cs
    - Implement BuildTimeline_Click async event handler
    - Show progress dialog with cancel button
    - Call TimelineBuilder.BuildTimelineAsync with progress reporting
    - Store result in _allTimelineEvents field
    - Bind to Timeline ListView
    - Switch to Timeline tab automatically
    - Update status bar with event count
    - _Requirements: 4.6, 5.1_

- [x] 5. Implement toolbar with filter controls
  - [x] 5.1 Add complete toolbar to MainWindow.xaml
    - Add ToolBarTray below Menu with ToolBar
    - Add Open Case button (folder icon)
    - Add Refresh button
    - Add Build Global Timeline button
    - Add Separator
    - Add From DatePicker with "From:" label
    - Add To DatePicker with "To:" label
    - Add UTC/Local toggle button (checkable)
    - Add Separator
    - Add Source filter ComboBox with "Source:" label
    - Add Quick Search TextBox with "Search:" label and placeholder
    - Add Separator
    - Add Export dropdown button
    - Add Columns button
    - Add Copy button
    - Add Auto-size columns button
    - Add Spacer (HorizontalAlignment=Stretch)
    - Add Row count label (e.g., "Rows: 0")
    - _Requirements: 4.1, 4.11, 4.12, 4.13, 4.14, 4.15_

  - [x] 5.2 Implement FilterCriteria class and filter application logic
    - Create FilterCriteria class with FromDate, ToDate, SourceGroup, SearchText, IsUtcMode properties
    - Add _filterCriteria field to MainWindow
    - Implement ApplyFilters method that calls ApplyTableFilters or ApplyTimelineFilters based on active tab
    - Implement ApplyTableFilters method using DataView.RowFilter for time range and quick search
    - Implement BuildRowFilter method to search across all visible columns
    - Implement ApplyTimelineFilters method using LINQ Where on _allTimelineEvents
    - Wire up filter control change events (DatePicker.SelectedDateChanged, ComboBox.SelectionChanged, TextBox.TextChanged with 300ms debounce)
    - Update row count label after filtering
    - _Requirements: 7.1, 7.3, 7.4, 7.5, 7.6_

  - [x] 5.3 Implement UTC/Local time toggle
    - Add _isUtcMode field to MainWindow
    - Implement ToggleUtcLocal_Click handler to flip _isUtcMode flag
    - Update button label to show current mode ("UTC" or "Local")
    - Create IValueConverter for time column display conversion
    - Apply converter to DataGrid time columns (use DataGridTemplateColumn)
    - Update Timeline ListView binding to show converted timestamps
    - Implement DisplayTime helper method (UTC or ToLocalTime)
    - _Requirements: 4.10, 7.2_

  - [x] 5.4 Populate Source filter ComboBox with group names
    - After case folder scan, populate Source ComboBox with "All" and discovered group names
    - Bind ComboBox.SelectedItem to _filterCriteria.SourceGroup
    - Implement SelectionChanged handler to call ApplyFilters
    - _Requirements: 4.13, 7.3_

- [x] 6. Implement column management and export functionality
  - [x] 6.1 Implement Columns visibility dialog
    - Create ColumnsDialog.xaml window with ListBox of checkboxes
    - Populate ListBox with all DataGrid column names
    - Bind checkbox IsChecked to column visibility state
    - Implement ShowColumnsDialog_Click handler to show dialog
    - Apply column visibility changes to DataGrid.Columns[i].Visibility
    - Store visibility state in _columnVisibility dictionary
    - _Requirements: 6.1, 6.2, 4.10_

  - [x] 6.2 Implement Export Current Table functionality
    - Add File menu item "Export Current Table"
    - Add Export dropdown button in toolbar with "Export Current Table" menu item
    - Implement ExportTable_Click handler with SaveFileDialog
    - Export DataView with current RowFilter applied (respects filters)
    - Export only visible columns (check _columnVisibility)
    - Use CsvHelper to write CSV with headers
    - Show success message or error dialog
    - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5, 4.9_

  - [x] 6.3 Implement Export Timeline functionality
    - Add "Export Timeline" menu item to Export dropdown
    - Implement ExportTimeline_Click handler with SaveFileDialog
    - Export _filteredTimelineEvents (respects filters)
    - Include Timestamp, Source, GroupName, Description columns
    - Apply UTC/Local conversion based on _isUtcMode
    - Use CsvHelper to write CSV with headers
    - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5_

  - [x] 6.4 Implement Copy to Clipboard functionality
    - Add Tools menu item "Copy Selected Rows"
    - Add Copy button in toolbar
    - Implement CopyRows_Click handler
    - Get selected rows from DataGrid or ListView
    - Format as CSV with headers (tab-separated for Excel compatibility)
    - Copy to Clipboard using Clipboard.SetText
    - Show feedback (status bar message or tooltip)
    - _Requirements: 6.4, 6.5, 4.11_

  - [x] 6.5 Implement Auto-size Columns functionality
    - Add View menu item "Auto-size Columns"
    - Add Auto-size button in toolbar
    - Implement AutoSizeColumns_Click handler
    - Iterate through DataGrid.Columns
    - Set column.Width = DataGridLength.Auto for visible columns only
    - Limit measurement to first 500 visible rows for performance
    - _Requirements: 6.6, 6.7, 4.12_

- [x] 7. Complete menu bar and enhance status bar




  - [x] 7.1 Add missing menu items to MainWindow.xaml


    - Add "Refresh" menu item to File menu with F5 shortcut
    - Add "UTC/Local Time" checkable menu item to View menu
    - Add Help menu with "About" menu item
    - Wire up Refresh_Click event handler (already implemented)
    - _Requirements: 4.3, 4.4, 4.5, 4.7, 4.8, 4.10_

  - [x] 7.2 Implement tab change handler for status bar updates


    - Add SelectionChanged event handler to MainTabControl
    - Update StatusBarViewName when tab changes
    - Update StatusBarRowCount to show appropriate count (Rows vs Events)
    - Call UpdateRowCount when switching tabs
    - _Requirements: 9.2, 9.3, 9.5_

- [x] 8. Add keyboard shortcuts and input validation



  - [x] 8.1 Add keyboard shortcuts to MainWindow.xaml


    - Add CommandBindings for Ctrl+O (Open Case)
    - Add CommandBindings for F5 (Refresh)
    - Add CommandBindings for Ctrl+T (Build Timeline)
    - Add CommandBindings for Ctrl+E (Export Table)
    - Add CommandBindings for Ctrl+C (Copy Rows)
    - Add CommandBindings for Ctrl+F (Focus Quick Search)
    - Update InputGestureText on menu items to show shortcuts
    - _Requirements: 11.1_

  - [x] 8.2 Add input validation


    - Validate folder path exists before scanning in OpenCase_Click
    - Validate From date <= To date in DateFilter_Changed
    - Show meaningful error messages for validation failures
    - _Requirements: 2.1, 7.1, 8.1_
-

- [x] 9. Final polish and testing






  - [x] 9.1 Create About dialog

    - Create AboutDialog.xaml with application name, version, and description
    - Create AboutDialog.xaml.cs with simple OK button handler
    - Implement Help > About menu item handler in MainWindow
    - Show dialog with application information (KAPE Viewer, .NET 8.0, purpose)
    - _Requirements: 4.1_




  - [x] 9.2 Test with SampleData/parsed folder

    - Open SampleData/parsed folder
    - Verify all groups appear in TreeView
    - Click each CSV file and verify table loads correctly
    - Build Global Timeline and verify events appear sorted chronologically
    - Apply From/To date filters and verify row count updates
    - Apply Source filter and verify only selected group shows
    - Enter Quick Search text and verify filtering works across columns
    - Toggle UTC/Local and verify timestamps convert
    - Show/hide columns and verify DataGrid updates
    - Export current table and verify CSV opens in Excel
    - Copy selected rows and verify clipboard contains CSV data
    - Auto-size columns and verify widths adjust
    - Test all keyboard shortcuts
    - _Requirements: 1.6, 2.5, 3.1, 5.6, 7.1, 7.2, 7.3, 7.4, 7.5, 7.6, 6.2, 6.3, 6.4, 6.5, 8.4_


  - [x] 9.3 Fix any runtime errors and optimize performance

    - Run application and fix any crashes or exceptions
    - Test with large CSV files (100K+ rows) and verify performance
    - Test timeline build with 50+ CSV files and verify completion time
    - Verify DataGrid virtualization is working (smooth scrolling)
    - Verify memory usage is reasonable (no memory leaks)
    - _Requirements: 3.3_
-

- [x] 10. Documentation and deployment






  - [ ] 10.1 Create README.md with usage instructions
    - Document application purpose and features
    - Document how to open a case folder
    - Document how to use filters and timeline
    - Document keyboard shortcuts
    - Document system requirements (.NET 8.0)
    - Document how to build and run the application
    - _Requirements: 1.7_



  - [ ] 10.2 Test self-contained publish
    - Run: dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true --self-contained true
    - Verify output at bin/Release/net8.0-windows/win-x64/publish/KapeViewer.exe
    - Test executable runs without .NET runtime installed
    - Verify file size is reasonable (~80-100 MB)
    - Document publish command in README.md
    - _Requirements: 1.5, 1.7_
