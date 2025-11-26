using System.Windows.Input;

namespace KapeViewer
{
    /// <summary>
    /// Custom commands for KAPE Viewer application
    /// </summary>
    public static class CustomCommands
    {
        public static readonly RoutedUICommand BuildTimeline = new RoutedUICommand(
            "Build Global Timeline",
            "BuildTimeline",
            typeof(CustomCommands),
            new InputGestureCollection()
            {
                new KeyGesture(Key.T, ModifierKeys.Control)
            });

        public static readonly RoutedUICommand ExportTable = new RoutedUICommand(
            "Export Current Table",
            "ExportTable",
            typeof(CustomCommands),
            new InputGestureCollection()
            {
                new KeyGesture(Key.E, ModifierKeys.Control)
            });
    }
}
