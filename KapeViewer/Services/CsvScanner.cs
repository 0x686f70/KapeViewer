using KapeViewer.Models;
using System.Collections.ObjectModel;
using System.IO;

namespace KapeViewer.Services;

/// <summary>
/// Scans directories and groups CSV files by first-level subfolder.
/// </summary>
public class CsvScanner
{
    /// <summary>
    /// Scans a folder recursively for CSV files and groups them by first-level subfolder.
    /// </summary>
    /// <param name="rootPath">The root case folder path to scan</param>
    /// <returns>List of GroupNode objects with populated Files collections</returns>
    public List<GroupNode> ScanFolder(string rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
        {
            return new List<GroupNode>();
        }

        var groups = new Dictionary<string, GroupNode>(StringComparer.OrdinalIgnoreCase);

        // Recursively find all CSV files
        var csvFiles = Directory.EnumerateFiles(rootPath, "*.csv", SearchOption.AllDirectories);

        foreach (var filePath in csvFiles)
        {
            var fileInfo = new FileInfo(filePath);
            var relativePath = Path.GetRelativePath(rootPath, filePath);
            
            // Determine group name from first-level subfolder
            string groupName = "Other";
            var pathSegments = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            
            if (pathSegments.Length > 1)
            {
                // File is in a subfolder, use first-level folder name as group
                groupName = pathSegments[0];
            }

            // Create group if it doesn't exist
            if (!groups.ContainsKey(groupName))
            {
                groups[groupName] = new GroupNode
                {
                    Name = groupName,
                    Files = new ObservableCollection<CsvFileItem>()
                };
            }

            // Add file to group
            groups[groupName].Files.Add(new CsvFileItem
            {
                FileName = fileInfo.Name,
                FullPath = filePath,
                GroupName = groupName,
                FileSize = fileInfo.Length
            });
        }

        // Sort groups alphabetically and return as list
        return groups.Values.OrderBy(g => g.Name).ToList();
    }

    /// <summary>
    /// Re-scans the current folder and updates existing groups.
    /// </summary>
    /// <param name="rootPath">The root case folder path to re-scan</param>
    /// <param name="existingGroups">Existing groups to update</param>
    public void RefreshScan(string rootPath, List<GroupNode> existingGroups)
    {
        var newGroups = ScanFolder(rootPath);
        
        existingGroups.Clear();
        foreach (var group in newGroups)
        {
            existingGroups.Add(group);
        }
    }
}
