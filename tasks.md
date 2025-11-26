# KapeViewer Upgrade Tasks

## Phase 1: Architecture Refactoring (MVVM)
**Goal**: Decouple business logic from the UI.

- [x] **Task 1.1**: Install `CommunityToolkit.Mvvm` NuGet package.
- [x] **Task 1.2**: Create `ViewModels` folder and `BaseViewModel` class.
- [x] **Task 1.3**: Implement `FileTreeViewModel` (Move scanning logic from MainWindow).
- [x] **Task 1.4**: Implement `TimelineViewModel` (Move timeline building/filtering logic).
- [x] **Task 1.5**: Implement `MainViewModel` (Orchestrator) and bind to `MainWindow`.
- [x] **Task 1.6**: Clean up `MainWindow.xaml.cs` (Remove logic, keep only UI events if strictly necessary).
- [x] **Task 1.7**: Verify Build & Run.

## Phase 2: Performance & Scalability (SQLite Backend)
**Goal**: Support millions of events using Disk-based storage.

- [ ] **Task 2.1**: Install `Microsoft.Data.Sqlite` package.
- [ ] **Task 2.2**: Create `DatabaseService` (Handle connection, init DB, create tables).
- [ ] **Task 2.3**: Modify `TimelineBuilder` to insert data into SQLite (Batch insert).
- [ ] **Task 2.4**: Create `VirtualizingTimelineProvider` (Fetch data on demand for UI).
- [ ] **Task 2.5**: Update `TimelineViewModel` to use the virtualization provider.
- [ ] **Task 2.6**: Verify performance with large datasets.

## Phase 3: Modern UI & UX
**Goal**: Professional look and feel.

- [ ] **Task 3.1**: Install `MahApps.Metro` package.
- [ ] **Task 3.2**: Configure `App.xaml` for Dark/Light theme support.
- [ ] **Task 3.3**: Convert `MainWindow` to `MetroWindow`.
- [ ] **Task 3.4**: Integrate `AvalonDock` (or similar) for flexible layout.
- [ ] **Task 3.5**: Polish UI (Icons, Spacing, Fonts).

## Phase 4: Advanced DFIR Features
**Goal**: Specialized forensic tools.

- [ ] **Task 4.1**: Update SQLite schema for Bookmarks & Notes.
- [ ] **Task 4.2**: Add UI context menu for "Add Bookmark/Note".
- [ ] **Task 4.3**: Implement "Session Save/Load" (JSON serialization of state).
- [ ] **Task 4.4**: Implement Regex Search in `TimelineViewModel`.
