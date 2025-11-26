using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KapeViewer.Models;
using System.Windows;

namespace KapeViewer.ViewModels
{
    public partial class MainViewModel : BaseViewModel
    {
        [ObservableProperty]
        private FileTreeViewModel _fileTree;

        [ObservableProperty]
        private TimelineViewModel _timeline;

        [ObservableProperty]
        private TableViewModel _table;

        [ObservableProperty]
        private int _selectedTabIndex;

        [ObservableProperty]
        private bool _isBusy;

        public MainViewModel()
        {
            _fileTree = new FileTreeViewModel();
            _timeline = new TimelineViewModel();
            _table = new TableViewModel();

            // Wire up events
            _fileTree.FileSelected += OnFileSelected;
        }

        private void OnFileSelected(object? sender, CsvFileItem e)
        {
            Table.LoadFile(e.FullPath);
            SelectedTabIndex = 0; // Switch to Table view
        }

        [RelayCommand]
        private async Task BuildTimeline()
        {
            if (FileTree.Groups.Count == 0)
            {
                MessageBox.Show("Please open a case folder first.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            IsBusy = true;
            try
            {
                var allFiles = FileTree.Groups.SelectMany(g => g.Files).ToList();
                await Timeline.BuildTimelineAsync(allFiles);
                SelectedTabIndex = 1; // Switch to Timeline view
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void Exit()
        {
            Application.Current.Shutdown();
        }
    }
}
