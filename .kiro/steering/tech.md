# Technology Stack

## Framework & Platform

- **.NET 8.0** (net8.0-windows) with SDK-style project format
- **WPF** (Windows Presentation Foundation) for UI
- **Target Platform**: Windows x64
- **Language**: C# with nullable reference types and implicit usings enabled

## Key Libraries

- **CsvHelper** (v33.1.0): CSV parsing with streaming support via CsvDataReader
- **Ookii.Dialogs.Wpf** (v5.0.1): Native Windows folder browser dialogs

## Architecture Pattern

Model-Service-View architecture:
- **Models**: Data classes (GroupNode, CsvFileItem, TimelineEvent, FilterCriteria)
- **Services**: Business logic (CsvScanner, CsvTableLoader, TimelineBuilder)
- **Views**: WPF XAML + code-behind (MainWindow, ColumnsDialog, ProgressDialog)

## Performance Optimizations

- **Streaming CSV reads**: Use CsvDataReader to avoid loading entire files into memory
- **DataGrid virtualization**: EnableRowVirtualization and EnableColumnVirtualization
- **Async operations**: Timeline building runs asynchronously with progress reporting
- **DataView filtering**: Apply filters on DataView.RowFilter for efficient table filtering

## Common Commands

### Build
```bash
dotnet build
```

### Run
```bash
dotnet run --project KapeViewer/KapeViewer.csproj
```

### Publish (Self-Contained Single File)
```bash
dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true --self-contained true
```
Output: `bin/Release/net8.0-windows/win-x64/publish/KapeViewer.exe`

### Test with Sample Data
Open the `SampleData/parsed` folder in the application to test with real KAPE output.

## Deployment

The application publishes as a self-contained single-file executable (~80-100 MB) that includes the .NET runtime, requiring no separate installation.
