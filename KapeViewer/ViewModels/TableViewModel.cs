using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KapeViewer.Services;
using System.Data;
using System.IO;
using System.Windows;

namespace KapeViewer.ViewModels
{
    public partial class TableViewModel : BaseViewModel
    {
        private readonly CsvTableLoader _csvTableLoader = new();

        [ObservableProperty]
        private DataTable? _currentTable;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private int _rowCount;

        public void LoadFile(string filePath)
        {
            try
            {
                // In a real app, use a busy indicator service
                CurrentTable = _csvTableLoader.LoadCsv(filePath);
                
                if (CurrentTable != null)
                {
                    RowCount = CurrentTable.Rows.Count;
                    StatusMessage = $"Loaded {Path.GetFileName(filePath)}";
                }
                else
                {
                    StatusMessage = "Failed to load file";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading CSV: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void ExportTable()
        {
            if (CurrentTable == null)
            {
                MessageBox.Show("No table loaded.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                DefaultExt = ".csv",
                FileName = "export.csv"
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
            if (CurrentTable == null) return;

            using var writer = new StreamWriter(filePath);
            using var csv = new CsvHelper.CsvWriter(writer, System.Globalization.CultureInfo.InvariantCulture);

            // Write headers
            foreach (DataColumn column in CurrentTable.Columns)
            {
                csv.WriteField(column.ColumnName);
            }
            csv.NextRecord();

            // Write rows
            foreach (DataRow row in CurrentTable.Rows)
            {
                for (int i = 0; i < CurrentTable.Columns.Count; i++)
                {
                    csv.WriteField(row[i]);
                }
                csv.NextRecord();
            }
        }
    }
}
