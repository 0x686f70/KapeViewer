using System.Windows;

namespace KapeViewer;

/// <summary>
/// Progress dialog for long-running operations.
/// </summary>
public partial class ProgressDialog : Window
{
    private CancellationTokenSource? _cancellationTokenSource;

    public ProgressDialog()
    {
        InitializeComponent();
    }

    public void SetCancellationTokenSource(CancellationTokenSource cts)
    {
        _cancellationTokenSource = cts;
    }

    public void UpdateProgress(int percentage)
    {
        Dispatcher.Invoke(() =>
        {
            ProgressBar.Value = percentage;
            StatusText.Text = $"Processing CSV files... {percentage}%";
        });
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        _cancellationTokenSource?.Cancel();
        CancelButton.IsEnabled = false;
        StatusText.Text = "Cancelling...";
    }
}
