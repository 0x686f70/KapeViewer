# Tasks

## Phase 1: Architecture Refactoring (MVVM)
- [x] **Task 1.1**: Install CommunityToolkit.Mvvm package.
- [x] **Task 1.2**: Create ViewModels folder and BaseViewModel class.
- [x] **Task 1.3**: Implement FileTreeViewModel (Move scanning logic from MainWindow).
- [x] **Task 1.4**: Implement TimelineViewModel (Move timeline building/filtering logic).
- [x] **Task 1.5**: Implement MainViewModel (Orchestrator) and bind to MainWindow.
- [x] **Task 1.6**: Clean up MainWindow.xaml.cs (Remove logic, keep only UI events if strictly necessary).
- [x] **Task 1.7**: Verify Build & Run.

## Phase 2: Performance & Scalability (SQLite Backend)
**Goal**: Support millions of events using Disk-based storage.
- [x] **Task 2.1**: Install Microsoft.Data.Sqlite package.
- [x] **Task 2.2**: Create DatabaseService (Handle connection, init DB, create tables).
- [x] **Task 2.3**: Modify TimelineBuilder to insert data into SQLite (Batch insert).
- [x] **Task 2.4**: Create VirtualizingTimelineProvider (Fetch data on demand for UI).
- [x] **Task 2.5**: Update TimelineViewModel to use the virtualization provider.
- [x] **Task 2.6**: Verify performance with large datasets.

## Phase 3: Modern UI & UX
**Goal**: Create a premium, dark-themed, responsive UI.
- [ ] **Task 3.1**: Install MahApps.Metro or MaterialDesignInXaml.
- [x] **Task 3.2**: Apply Dark Theme and modern styles to `MainWindow`.
- [x] **Task 3.3**: Style DataGrid and ListView (Custom headers, row colors).
- [x] **Task 3.4**: Add loading indicators and better progress bars.
- [ ] **Task 3.5**: Implement "Glassmorphism" effects (optional, if time permits).

## Phase 4: Advanced DFIR Features
**Goal**: Add filtering, searching, and export capabilities.
- [x] **Task 4.1**: Implement Advanced Filtering (AND/OR logic).
- [x] **Task 4.2**: Add "Global Search" across all columns.
- [x] **Task 4.3**: Implement "Export to CSV/Excel" for filtered results.
- [x] **Task 4.4**: Add "Case Summary" dashboard (Charts/Graphs).
