# Project Structure

## Solution Organization

```
KapeViewer/
├── KapeViewer.sln          # Visual Studio solution file
├── KapeViewer.csproj       # SDK-style project file
├── App.xaml                # WPF application definition
├── App.xaml.cs
├── AssemblyInfo.cs
├── MainWindow.xaml         # Main application window
├── MainWindow.xaml.cs
├── ColumnsDialog.xaml      # Column visibility dialog
├── ColumnsDialog.xaml.cs
├── ProgressDialog.xaml     # Timeline build progress dialog
├── ProgressDialog.xaml.cs
├── Models/                 # Data models
│   ├── GroupNode.cs
│   ├── CsvFileItem.cs
│   ├── TimelineEvent.cs
│   └── FilterCriteria.cs
├── Services/               # Business logic
│   ├── CsvScanner.cs
│   ├── CsvTableLoader.cs
│   └── TimelineBuilder.cs
└── Converters/             # WPF value converters
    └── UtcLocalTimeConverter.cs
```

## Key Components

### Models
- **GroupNode**: Represents a folder group with collection of CSV files
- **CsvFileItem**: Represents individual CSV file metadata
- **TimelineEvent**: Represents a merged timeline event with timestamp, source, and description
- **FilterCriteria**: Encapsulates filter settings (date range, source, search text)

### Services
- **CsvScanner**: Recursively scans folders and groups CSV files by first-level subfolder
- **CsvTableLoader**: Loads CSV files into DataTable using streaming reads
- **TimelineBuilder**: Merges multiple CSV files into chronological timeline with async support

### Views
- **MainWindow**: Main application with TreeView (left), TabControl (right), toolbar, and status bar
- **ColumnsDialog**: Dialog for showing/hiding table columns
- **ProgressDialog**: Progress dialog with cancellation support for timeline building

## Naming Conventions

- **Classes**: PascalCase (e.g., `CsvScanner`, `TimelineEvent`)
- **Methods**: PascalCase (e.g., `LoadCsv`, `BuildTimelineAsync`)
- **Private fields**: _camelCase with underscore prefix (e.g., `_currentTable`, `_isUtcMode`)
- **Properties**: PascalCase (e.g., `FileName`, `GroupName`)
- **Event handlers**: PascalCase with pattern `ElementName_EventName` (e.g., `OpenCase_Click`)

## File Organization Rules

- Place data models in `Models/` folder
- Place business logic in `Services/` folder
- Place WPF value converters in `Converters/` folder
- Keep dialogs at root level with XAML + code-behind pairs
- Use `.xaml` and `.xaml.cs` pairs for all WPF windows and dialogs

## Data Binding Patterns

- Use `ObservableCollection<T>` for dynamic collections bound to UI
- Bind TreeView to `List<GroupNode>` with HierarchicalDataTemplate
- Bind DataGrid to `DataTable.DefaultView` for efficient filtering
- Bind ListView to `List<TimelineEvent>` for timeline display
- Use DataView.RowFilter for table filtering instead of LINQ

## Error Handling

- Wrap file I/O operations in try-catch blocks
- Show MessageBox for user-facing errors with specific error messages
- Log errors to Debug output for developer diagnostics
- Continue processing remaining files if one file fails (timeline building)
- Never crash on malformed CSV or unparseable timestamps
