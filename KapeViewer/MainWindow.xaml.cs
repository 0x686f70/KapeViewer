using System.Windows;
using System.Windows.Controls;
using KapeViewer.Models;

namespace KapeViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ViewModels.MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new ViewModels.MainViewModel();
            DataContext = _viewModel;
        }

        private void TreeViewItem_Selected(object sender, RoutedEventArgs e)
        {
            // Handle TreeView selection manually since it doesn't support binding SelectedItem
            if (e.OriginalSource is TreeViewItem item && item.DataContext is CsvFileItem csvFile)
            {
                _viewModel.FileTree.SelectedItem = csvFile;
            }
        }
    }
}