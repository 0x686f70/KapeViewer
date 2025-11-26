# KAPE Viewer

A Windows desktop application for Digital Forensics and Incident Response (DFIR) engineers to efficiently browse and analyze KAPE/!EZParser CSV outputs.

## Features

- **Browse CSV Files**: Organized by artifact groups (ProgramExecution, EventLogs, etc.)
- **Table View**: View individual CSV files with sorting, filtering, and column management
- **Global Timeline**: Merge all CSV files into a single chronological timeline
- **Advanced Filtering**: Filter by time range, source group, and text search
- **Time Zone Support**: Toggle between UTC and local time display
- **Export**: Export filtered data to CSV format
- **Clipboard**: Copy selected rows to clipboard
- **Performance**: Handles large CSV files (100K+ rows) efficiently using streaming and virtualization

## System Requirements

- **Operating System**: Windows 10 or later (x64)
- **.NET Runtime**: .NET 8.0 (included in self-contained builds)
- **Memory**: 4 GB RAM minimum, 8 GB recommended for large datasets
- **Disk Space**: 100 MB for application, additional space for case data

## Installation

### Option 1: Self-Contained Executable (Recommended)

Download the pre-built `KapeViewer.exe` from the releases page. No .NET runtime installation required.

### Option 2: Build from Source

1. Install [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
2. Clone this repository
3. Build the application:

```bash
dotnet build
```

4. Run the application:

```bash
dotnet run --project KapeViewer/KapeViewer.csproj
```

### Option 3: Publish Self-Contained

Create a self-contained single-file executable:

```bash
dotnet publish KapeViewer/KapeViewer.csproj -c Release -r win-x64 -p:PublishSingleFile=true --self-contained true
```

The executable will be located at:
```
KapeViewer/bin/Release/net8.0-windows/win-x64/publish/KapeViewer.exe
```

**Note**: The self-contained executable is approximately 147 MB as it includes the entire .NET 8.0 runtime.

## Usage

### Opening a Case Folder

1. Click **File > Open Case** or press **Ctrl+O**
2. Select the root folder containing your KAPE parsed CSV outputs
3. The application will scan and group CSV files by their subfolder names

### Browsing CSV Files

- The left panel shows a tree view of discovered groups and files
- Click any CSV file to view its contents in the table view
- Use the column headers to sort data
- Right-click column headers to reorder columns

### Building a Global Timeline

1. Click **Tools > Build Global Timeline** or press **Ctrl+T**
2. Wait for the progress dialog to complete (can be canceled)
3. The timeline tab will display all events sorted chronologically
4. Events include timestamp, source file, and description

### Filtering Data

The toolbar provides several filtering options:

- **From/To Date**: Filter events within a specific time range
- **Source Filter**: Show only events from a specific artifact group
- **Quick Search**: Search across all visible columns (press **Ctrl+F** to focus)
- **UTC/Local Toggle**: Switch between UTC and local time display

All filters apply to both table and timeline views.

### Column Management

1. Click **View > Columns** to open the column visibility dialog
2. Check or uncheck columns to show or hide them
3. Click **View > Auto-size Columns** to fit column widths to content

### Exporting Data

- **Export Current Table**: File > Export Current Table or **Ctrl+E**
  - Exports the currently displayed table with active filters applied
  - Only visible columns are included
- **Export Timeline**: File > Export Timeline
  - Exports the filtered timeline events
  - Includes timestamp, source, group, and description

### Copying Data

1. Select one or more rows in the table or timeline
2. Click **Tools > Copy Selected Rows** or press **Ctrl+C**
3. Paste into Excel, text editor, or other applications

### Refreshing Data

- Click **File > Refresh** or press **F5** to re-scan the current case folder
- Useful when new CSV files are added to the case folder

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| **Ctrl+O** | Open Case folder |
| **F5** | Refresh current case |
| **Ctrl+T** | Build Global Timeline |
| **Ctrl+E** | Export Current Table |
| **Ctrl+C** | Copy Selected Rows |
| **Ctrl+F** | Focus Quick Search |

## Application Layout

```
┌─────────────────────────────────────────────────────────┐
│  Menu Bar: File, Tools, View, Help                      │
├─────────────────────────────────────────────────────────┤
│  Toolbar: Filters (From/To, Source, Search) & Actions   │
├──────────────┬──────────────────────────────────────────┤
│  TreeView    │  TabControl                              │
│  ┌─────────┐ │  ┌────────────┬──────────────────────┐  │
│  │ Groups  │ │  │ Table Tab  │ Timeline Tab         │  │
│  │ └─Files │ │  │ (DataGrid) │ (ListView)           │  │
│  └─────────┘ │  └────────────┴──────────────────────┘  │
├──────────────┴──────────────────────────────────────────┤
│  Status Bar: Case Path | View | Row Count               │
└─────────────────────────────────────────────────────────┘
```

## Performance Tips

- **Large CSV Files**: The application uses streaming and virtualization to handle files with 100K+ rows efficiently
- **Timeline Building**: Building a timeline from 50+ CSV files may take 10-30 seconds depending on file sizes
- **Filtering**: Filters are applied efficiently using DataView for tables and LINQ for timelines
- **Memory Usage**: The application is designed to minimize memory usage, but very large datasets (1M+ rows) may require 8+ GB RAM

## Troubleshooting

### CSV File Not Loading

- Ensure the file is a valid CSV with headers
- Check that the file is not locked by another application
- Malformed CSV files are skipped with an error message

### Timeline Missing Events

- The application auto-detects time columns using common patterns (timecreated, timestamp, eventtime, etc.)
- If a CSV file doesn't have a recognized time column, it will be skipped
- Rows with unparseable timestamps are skipped

### Application Crashes or Freezes

- Ensure you have sufficient RAM for large datasets
- Try filtering data to reduce the number of visible rows
- Check Windows Event Viewer for error details

### Export or Copy Not Working

- Ensure you have write permissions to the export location
- Check that the clipboard is not locked by another application

## Sample Data

The repository includes sample KAPE output in the `SampleData/parsed` folder for testing purposes.

## Technology Stack

- **.NET 8.0** (net8.0-windows)
- **WPF** (Windows Presentation Foundation)
- **CsvHelper** (v33.1.0) for CSV parsing
- **Ookii.Dialogs.Wpf** (v5.0.1) for native folder dialogs

## License

[Add your license information here]

## Contributing

[Add contribution guidelines here]

## Support

For issues, questions, or feature requests, please [open an issue](https://github.com/yourusername/KapeViewer/issues).

## Acknowledgments

Built for the DFIR community to streamline analysis of KAPE forensic artifacts.
