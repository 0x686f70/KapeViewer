using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KapeViewer.Models;
using KapeViewer.Services;
using System.Collections.ObjectModel;

namespace KapeViewer.ViewModels
{
    public partial class DashboardViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private readonly FilterCriteria _filterCriteria;

        [ObservableProperty]
        private ObservableCollection<KeyValuePair<string, int>> _groupCounts = new();

        [ObservableProperty]
        private int _totalEvents;

        public DashboardViewModel(DatabaseService databaseService, FilterCriteria filterCriteria)
        {
            _databaseService = databaseService;
            _filterCriteria = filterCriteria;
        }

        [RelayCommand]
        public void Refresh()
        {
            TotalEvents = _databaseService.GetEventCount(_filterCriteria);
            
            var counts = _databaseService.GetEventCountsByGroup(_filterCriteria);
            GroupCounts = new ObservableCollection<KeyValuePair<string, int>>(counts);
        }
    }
}
