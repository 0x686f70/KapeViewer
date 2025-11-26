using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KapeViewer.Models;
using KapeViewer.Services;
using System.Collections;
using System.IO;
using System.Windows;

namespace KapeViewer.ViewModels
{
    public partial class TimelineViewModel : BaseViewModel
    {
        private readonly TimelineBuilder _timelineBuilder = new();
        private readonly DatabaseService _databaseService;

        [ObservableProperty]
        private IList _filteredEvents;

        [ObservableProperty]
        private FilterCriteria _filterCriteria = new();

        [ObservableProperty]
        private bool _isUtcMode = true;

        [ObservableProperty]
        private string _statusMessage = "Ready";

        [ObservableProperty]
        private int _eventCount;

        public TimelineViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            _filteredEvents = new VirtualizingTimelineCollection(_databaseService, _filterCriteria);
        }

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
                // Clear existing data
                // Note: DatabaseService currently creates a new DB on init. 
                // Ideally we should clear tables or recreate service, but for now we append or assume new session.
                // Since DatabaseService is created once per ViewModel, we might want to clear it.
                // But DatabaseService doesn't have Clear method yet. 
                // Let's assume for this phase we just append or the user restarts app for new case.
                // Actually, let's add a Clear method to DatabaseService later if needed.
                
                await _timelineBuilder.BuildTimelineAsync(files, _databaseService, progress, cts.Token);
                
                ApplyFilters();
                
                int count = _databaseService.GetEventCount(_filterCriteria);
                StatusMessage = $"Timeline built: {count:N0} events";
                MessageBox.Show($"Timeline built successfully!\nTotal events: {count:N0}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
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
            if (FilteredEvents is VirtualizingTimelineCollection vtc)
            {
                vtc.Refresh();
                EventCount = vtc.Count;
            }
        }

        [RelayCommand]
        private void ToggleUtcMode()
        {
            IsUtcMode = !IsUtcMode;
            if (FilteredEvents is VirtualizingTimelineCollection vtc)
            {
                vtc.IsUtcMode = IsUtcMode;
            }
            else
            {
                ApplyFilters();
            }
        }

        [RelayCommand]
        private void ExportTimeline()
        {
            if (EventCount == 0)
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
            // Stream from DB to CSV to avoid loading all into memory
            using var writer = new StreamWriter(filePath);
            using var csv = new CsvHelper.CsvWriter(writer, System.Globalization.CultureInfo.InvariantCulture);

            csv.WriteField("Timestamp");
            csv.WriteField("Source");
            csv.WriteField("GroupName");
            csv.WriteField("Description");
            csv.NextRecord();

            // Fetch in chunks
            int offset = 0;
            int limit = 1000;
            while (true)
            {
                var events = _databaseService.GetEvents(_filterCriteria, offset, limit);
                if (events.Count == 0) break;

                foreach (var evt in events)
                {
                    var timestamp = IsUtcMode ? DateTime.SpecifyKind(evt.Timestamp, DateTimeKind.Utc) : evt.Timestamp.ToLocalTime();
                    csv.WriteField(timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                    csv.WriteField(evt.Source);
                    csv.WriteField(evt.GroupName);
                    csv.WriteField(evt.Description);
                    csv.NextRecord();
                }

                offset += limit;
            }
        }
        

    }
}
