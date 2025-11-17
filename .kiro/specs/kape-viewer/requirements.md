# Requirements Document

## Introduction

KAPE Viewer is a Windows desktop application designed for Digital Forensics and Incident Response (DFIR) engineers to efficiently browse and analyze KAPE/!EZParser CSV outputs. The application provides table-based viewing of individual CSV files and a merged timeline view across all artifacts, eliminating the need to manually open each CSV file in Excel. The application targets .NET 8.0 and uses WPF for the user interface.

## Glossary

- **KAPE Viewer**: The WPF desktop application system being developed
- **CSV Scanner**: The component responsible for discovering and grouping CSV files
- **Timeline Builder**: The component that merges multiple CSV files into a chronological timeline
- **Table Loader**: The component that loads CSV data into DataTable format
- **Case Folder**: The root directory containing parsed KAPE output with CSV files
- **Group Node**: A top-level subfolder category (e.g., ProgramExecution, EventLogs)
- **Timeline Event**: A single merged event entry containing timestamp, source, and description
- **Toolbar**: The top control panel containing filters and action buttons
- **UTC/Local Toggle**: A control to switch between UTC and local time display

## Requirements

### Requirement 1: Project Setup and Configuration

**User Story:** As a DFIR engineer, I want the application to be built on .NET 8.0 with proper SDK-style project configuration, so that I can deploy and run it on modern Windows systems.

#### Acceptance Criteria

1. THE KAPE Viewer SHALL use .NET 8.0 SDK-style project format with OutputType set to WinExe
2. THE KAPE Viewer SHALL enable WPF, Nullable reference types, and ImplicitUsings in the project configuration
3. THE KAPE Viewer SHALL include CsvHelper NuGet package for CSV parsing operations
4. THE KAPE Viewer SHALL include Ookii.Dialogs.Wpf NuGet package for folder selection dialogs
5. THE KAPE Viewer SHALL support self-contained publishing with PublishSingleFile option for win-x64 runtime
6. THE KAPE Viewer SHALL demonstrate all features using SampleData/parsed folder during verification
7. THE KAPE Viewer deliverable SHALL include the exact dotnet publish command for win-x64 self-contained single-file output

### Requirement 2: Case Folder Selection and CSV Discovery

**User Story:** As a DFIR engineer, I want to select a case folder and have the application automatically discover all CSV files, so that I can quickly access all available artifacts.

#### Acceptance Criteria

1. WHEN the user clicks the Open Case button, THE KAPE Viewer SHALL display a VistaFolderBrowserDialog to select a folder
2. WHEN a folder is selected, THE KAPE Viewer SHALL recursively scan for all files with .csv extension
3. THE KAPE Viewer SHALL group discovered CSV files by their first-level subfolder name
4. WHERE a CSV file has no first-level subfolder, THE KAPE Viewer SHALL assign it to an "Other" group
5. THE KAPE Viewer SHALL display the grouped CSV files in a TreeView control with expandable group nodes

### Requirement 3: Individual CSV Table Display

**User Story:** As a DFIR engineer, I want to view individual CSV files as tables with sorting and filtering capabilities, so that I can analyze specific artifact data efficiently.

#### Acceptance Criteria

1. WHEN the user selects a CSV file in the TreeView, THE KAPE Viewer SHALL load the file content into a DataGrid
2. THE KAPE Viewer SHALL use CsvHelper with CsvDataReader to stream CSV data into a DataTable without loading entire file content into memory
3. THE KAPE Viewer SHALL enable DataGrid virtualization for performance with large datasets
4. THE KAPE Viewer SHALL configure the DataGrid with AutoGenerateColumns enabled and ReadOnly mode
5. THE KAPE Viewer SHALL support column sorting and reordering through standard DataGrid functionality

### Requirement 4: Timeline Explorer-Style Menu and Toolbar

**User Story:** As a DFIR engineer, I want a menu bar and toolbar with controls similar to Timeline Explorer, so that I can use familiar filtering and navigation patterns.

#### Acceptance Criteria

1. THE KAPE Viewer SHALL display a menu bar at the top of the window with File, Tools, Tabs, View, and Help menus
2. THE KAPE Viewer File menu SHALL include "Open Case" menu item to select a case folder
3. THE KAPE Viewer File menu SHALL include "Refresh" menu item to re-scan the current case folder
4. THE KAPE Viewer File menu SHALL include "Export Current Table" menu item to export visible data to CSV
5. THE KAPE Viewer File menu SHALL include "Exit" menu item to close the application
6. THE KAPE Viewer Tools menu SHALL include "Build Global Timeline" menu item to merge all CSV files
7. THE KAPE Viewer Tools menu SHALL include "Copy Selected Rows" menu item to copy data to clipboard
8. THE KAPE Viewer View menu SHALL include "Columns" menu item to show or hide table columns
9. THE KAPE Viewer View menu SHALL include "Auto-size Columns" menu item to fit column widths
10. THE KAPE Viewer View menu SHALL include "UTC/Local Time" toggle menu item to switch time display modes
11. THE KAPE Viewer SHALL display a toolbar below the menu bar containing filter controls
12. THE KAPE Viewer toolbar SHALL include From and To DateTimePicker controls for time range filtering
13. THE KAPE Viewer toolbar SHALL include a Source filter ComboBox populated with "All" and discovered group names
14. THE KAPE Viewer toolbar SHALL include a Quick Search textbox for text-based filtering
15. THE KAPE Viewer toolbar SHALL include a row count label displaying the current number of visible rows

### Requirement 5: Global Timeline Merge

**User Story:** As a DFIR engineer, I want to merge all CSV files into a single chronological timeline, so that I can analyze events across different artifact types in temporal order.

#### Acceptance Criteria

1. WHEN the user clicks Build Global Timeline, THE KAPE Viewer SHALL process all CSV files in the case folder
2. THE KAPE Viewer SHALL auto-detect time columns using case-insensitive matching against these patterns: timecreated, timestamp, eventtime, lastwritetime, created, modified, accessed
3. WHERE a CSV row contains a valid time column, THE KAPE Viewer SHALL create a Timeline Event with Timestamp, Source filename, and Description
4. THE KAPE Viewer SHALL generate the Description field by concatenating up to six non-time columns in format "Col=Value"
5. THE KAPE Viewer SHALL sort all Timeline Events in ascending chronological order by Timestamp
6. THE KAPE Viewer SHALL display the merged timeline in a ListView with three columns: Timestamp, Source, and Description
7. IF a CSV row cannot be parsed for a valid date, THE KAPE Viewer SHALL skip that row without crashing

### Requirement 6: Column Visibility and Clipboard Operations

**User Story:** As a DFIR engineer, I want to control which columns are visible and copy selected data to clipboard, so that I can focus on relevant fields and share data with other tools.

#### Acceptance Criteria

1. WHEN the user clicks the Columns button, THE KAPE Viewer SHALL display a dialog listing all available columns with checkboxes
2. WHEN the user toggles column visibility, THE KAPE Viewer SHALL immediately show or hide the selected columns in the DataGrid
3. THE KAPE Viewer SHALL apply column visibility settings to exported CSV files
4. WHEN the user clicks the Copy button, THE KAPE Viewer SHALL copy all selected rows to the clipboard in CSV format
5. THE KAPE Viewer SHALL include column headers in the clipboard CSV output
6. WHEN the user clicks Auto-size columns, THE KAPE Viewer SHALL adjust all visible column widths to fit their content
7. THE KAPE Viewer SHALL perform auto-sizing based on currently visible rows and columns without causing noticeable lag on large datasets

### Requirement 7: Toolbar Filter Application

**User Story:** As a DFIR engineer, I want toolbar filters to apply to both table and timeline views, so that I can narrow down data to relevant time ranges and sources.

#### Acceptance Criteria

1. WHEN the user sets From or To datetime values, THE KAPE Viewer SHALL filter displayed rows to only show events within the specified time range
2. WHEN the user toggles UTC/Local mode, THE KAPE Viewer SHALL convert and display all timestamps in the selected timezone
3. WHEN the user selects a Source filter value, THE KAPE Viewer SHALL display only rows from CSV files in the selected group
4. WHEN the user enters text in Quick Search, THE KAPE Viewer SHALL filter rows where any visible column contains or starts with the search text
5. THE KAPE Viewer SHALL apply all active filters simultaneously to both Table and Timeline views
6. WHEN filters change, THE KAPE Viewer SHALL update the row count label to reflect the filtered result count

### Requirement 8: Data Export Functionality

**User Story:** As a DFIR engineer, I want to export the currently displayed table to CSV format, so that I can share filtered results or perform additional analysis in other tools.

#### Acceptance Criteria

1. WHEN the user clicks Export Current Table, THE KAPE Viewer SHALL prompt for a save file location
2. THE KAPE Viewer SHALL export all currently visible rows and columns to a CSV file
3. THE KAPE Viewer SHALL apply active filters to the exported data
4. THE KAPE Viewer SHALL generate a valid CSV file that opens correctly in Excel and text editors
5. THE KAPE Viewer SHALL preserve column headers in the exported CSV file

### Requirement 9: Status Bar Information Display

**User Story:** As a DFIR engineer, I want to see case information and data counts in a status bar, so that I can quickly understand the current application state.

#### Acceptance Criteria

1. THE KAPE Viewer SHALL display a status bar at the bottom of the window
2. THE KAPE Viewer SHALL show the current case folder path in the status bar
3. THE KAPE Viewer SHALL show the active view name (Table or Timeline) in the status bar
4. THE KAPE Viewer SHALL show the current row or event count in the status bar
5. WHEN filters are applied, THE KAPE Viewer SHALL update the status bar counts to reflect filtered results

### Requirement 10: Git Version Control Integration

**User Story:** As a developer, I want each development milestone to be committed to Git with conventional commit messages, so that the project history is well-documented and traceable.

#### Acceptance Criteria

1. IF the repository is not initialized, THE KAPE Viewer development process SHALL initialize Git and configure the main branch before the first push
2. WHEN a development task is completed and build errors are fixed, THE KAPE Viewer development process SHALL create a Git commit
3. THE KAPE Viewer development process SHALL use Conventional Commits format with prefixes: feat, fix, or chore
4. THE KAPE Viewer development process SHALL push commits to the origin main branch
5. THE KAPE Viewer development process SHALL provide exact Git command sequences for each milestone

### Requirement 11: Application Layout and Navigation

**User Story:** As a DFIR engineer, I want an intuitive layout with tree navigation and tabbed views, so that I can efficiently switch between individual files and the merged timeline.

#### Acceptance Criteria

1. THE KAPE Viewer SHALL use a DockPanel layout with toolbar at top and status bar at bottom
2. THE KAPE Viewer SHALL display a two-column Grid in the central area
3. THE KAPE Viewer SHALL show a TreeView in the left column for case folder navigation
4. THE KAPE Viewer SHALL show a TabControl in the right column with two tabs: Table and Timeline
5. THE KAPE Viewer SHALL switch to the Table tab when a CSV file is selected in the TreeView
6. THE KAPE Viewer SHALL switch to the Timeline tab when Build Global Timeline is executed
