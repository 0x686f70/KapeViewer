using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KapeViewer.Models;
using KapeViewer.Services;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace KapeViewer.ViewModels
{
    public partial class TimelineViewModel : BaseViewModel
    {
        private readonly TimelineBuilder _timelineBuilder = new();
        private List<TimelineEvent> _allEvents = new();

        [ObservableProperty]
        private ObservableCollection<TimelineEvent> _filteredEvents = new();

        [ObservableProperty]
        private FilterCriteria _filterCriteria = new();

        [ObservableProperty]
        private bool _isUtcMode = true;

        [ObservableProperty]
        private string _statusMessage = "Ready";

        [ObservableProperty]
        private int _eventCount;

        public async Task BuildTimelineAsync(List<CsvFileItem> files)
        {
            if (files == null || files.Count == 0)
            {
                MessageBox.Show("No files to process.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var progressDialog = new ProgressDialog
            {
                Owner = Application.Current.MainWindow
            };

            var cts = new CancellationTokenSource();
            progressDialog.SetCancellationTokenSource(cts);

            var progress = new Progress<int>(percentage =>
            {
                progressDialog.UpdateProgress(percentage);
            });

            progressDialog.Show();

            try
            {
                _allEvents = await _timelineBuilder.BuildTimelineAsync(files, progress, cts.Token);
                ApplyFilters();
                
                StatusMessage = $"Timeline built: {_allEvents.Count:N0} events";
                MessageBox.Show($"Timeline built successfully!\nTotal events: {_allEvents.Count:N0}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "Timeline build cancelled";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error building timeline: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                progressDialog.Close();
            }
        }

        [RelayCommand]
        public void ApplyFilters()
        {
            if (_allEvents.Count == 0) return;

            var query = _allEvents.AsEnumerable();

            if (FilterCriteria.FromDate.HasValue)
                query = query.Where(e => e.Timestamp >= FilterCriteria.FromDate.Value);

            if (FilterCriteria.ToDate.HasValue)
                query = query.Where(e => e.Timestamp <= FilterCriteria.ToDate.Value);

            if (FilterCriteria.HasSourceFilter && FilterCriteria.SourceGroup != "All")
                query = query.Where(e => e.GroupName == FilterCriteria.SourceGroup);

            if (FilterCriteria.HasSearchFilter)
            {
                var search = FilterCriteria.SearchText.ToLowerInvariant();
                query = query.Where(e => 
                    e.Source.ToLowerInvariant().Contains(search) ||
                    e.Description.ToLowerInvariant().Contains(search) ||
                    e.Timestamp.ToString().Contains(search));
            }

            var result = query.Select(e => new TimelineEvent
            {
                Timestamp = IsUtcMode ? DateTime.SpecifyKind(e.Timestamp, DateTimeKind.Utc) : e.Timestamp.ToLocalTime(),
                Source = e.Source,
                GroupName = e.GroupName,
                Description = e.Description,
                OriginalTimeString = e.OriginalTimeString
            }).ToList();

            FilteredEvents = new ObservableCollection<TimelineEvent>(result);
            EventCount = result.Count;
        }

        [RelayCommand]
        private void ToggleUtcMode()
        {
            IsUtcMode = !IsUtcMode;
            ApplyFilters();
        }

        [RelayCommand]
        private void ExportTimeline()
        {
            if (FilteredEvents.Count == 0)
            {
                MessageBox.Show("No events to export.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                DefaultExt = ".csv",
                FileName = "timeline_export.csv"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    ExportToCsv(dialog.FileName);
                    MessageBox.Show($"Exported to {dialog.FileName}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Export failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExportToCsv(string filePath)
        {
            using var writer = new StreamWriter(filePath);
            using var csv = new CsvHelper.CsvWriter(writer, System.Globalization.CultureInfo.InvariantCulture);

            csv.WriteField("Timestamp");
            csv.WriteField("Source");
            csv.WriteField("GroupName");
            csv.WriteField("Description");
            csv.NextRecord();

            foreach (var evt in FilteredEvents)
            {
                csv.WriteField(evt.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                csv.WriteField(evt.Source);
                csv.WriteField(evt.GroupName);
                csv.WriteField(evt.Description);
                csv.NextRecord();
            }
        }
    }
}
