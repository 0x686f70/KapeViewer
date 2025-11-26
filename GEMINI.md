# Project Rules & Guidelines

## Workflow
1.  **Task Breakdown**: All phases must be broken down into small, atomic tasks.
2.  **Version Control**: Perform a `git commit` immediately after completing each atomic task.
    *   Commit messages should be descriptive (e.g., "Refactor: Extract FileTreeViewModel").
3.  **Error Handling**:
    *   Fix errors in place. Do not create temporary files or "backup" files (e.g., `file_v2.cs`) to bypass errors.
    *   Persist in debugging until the code compiles and runs correctly.
    *   Do not leave unused or temporary files in the workspace.

## Code Quality
*   Follow standard C# coding conventions.
*   Ensure the solution builds successfully after every task completion.
*   Clean up any resources or debug code before committing.
