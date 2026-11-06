using System.Windows;
using System.Windows.Threading;

namespace MilkillaDropVJ;

public partial class App : Application
{
    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(
            "Error inesperado:\n\n" + e.Exception.Message,
            "Milkilla Drop VJ",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }
}
