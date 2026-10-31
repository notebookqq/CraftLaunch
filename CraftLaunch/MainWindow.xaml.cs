using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using CraftLaunch.ViewModels;

namespace CraftLaunch;

public partial class MainWindow : Window
{
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    private const int DWMWA_WINDOW_RADIUS = 33;
    private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
        SourceInitialized += (_, _) => ApplyWindowBackdrop();
        StateChanged += (_, _) =>
            MaximizeButton.Content = WindowState == WindowState.Maximized ? "\uE949" : "\uE922";
    }

    private void ApplyWindowBackdrop()
    {
        // Win11 上尝试启用圆角与云母材质，Win10 会静默忽略
        try
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            var radius = 2;
            DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_RADIUS, ref radius, sizeof(int));
            var backdrop = 2;
            DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdrop, sizeof(int));
        }
        catch
        {
            // 平台不支持时保持纯色主题背景
        }
    }

    private void OnMinimize(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void OnMaximize(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void OnClose(object sender, RoutedEventArgs e) => Close();
}
