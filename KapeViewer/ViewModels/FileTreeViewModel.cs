using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KapeViewer.Models;
using KapeViewer.Services;
using Ookii.Dialogs.Wpf;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace KapeViewer.ViewModels
{
    public partial class FileTreeViewModel : BaseViewModel
    {
        private readonly CsvScanner _csvScanner = new();

        [ObservableProperty]
        private ObservableCollection<GroupNode> _groups = new();

        [ObservableProperty]
        private string _currentCasePath = string.Empty;

        [ObservableProperty]
        private object? _selectedItem;

        public event EventHandler<CsvFileItem>? FileSelected;

        [RelayCommand]
        private void OpenCase()
        {
            var dialog = new VistaFolderBrowserDialog
            {
                Description = "Select KAPE case folder",
                UseDescriptionForTitle = true
            };

            if (!string.IsNullOrEmpty(CurrentCasePath))
            {
                dialog.SelectedPath = CurrentCasePath;
            }

            if (dialog.ShowDialog() == true)
            {
                LoadCase(dialog.SelectedPath);
            }
        }

        public void LoadCase(string casePath)
        {
            try
            {
                if (!Directory.Exists(casePath))
                {
                    MessageBox.Show(
                        $"The case folder does not exist:\n{casePath}",
                        "Invalid Folder Path",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                var groups = _csvScanner.ScanFolder(casePath);
                Groups = new ObservableCollection<GroupNode>(groups);
                CurrentCasePath = casePath;

                int totalFiles = groups.Sum(g => g.FileCount);
                if (totalFiles == 0)
                {
                    MessageBox.Show(
                        $"No CSV files found in the selected folder:\n{casePath}\n\n" +
                        "Please select a folder containing KAPE/!EZParser CSV output files.",
                        "No CSV Files Found",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show(
                    $"Access denied to the case folder:\n{casePath}\n\n" +
                    "Please check folder permissions and try again.",
                    "Access Denied",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error loading case folder:\n{ex.Message}\n\n" +
                    $"Folder: {casePath}",
                    "Error Loading Case",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        partial void OnSelectedItemChanged(object? value)
        {
            if (value is CsvFileItem csvFile)
            {
                FileSelected?.Invoke(this, csvFile);
            }
        }
    }
}
