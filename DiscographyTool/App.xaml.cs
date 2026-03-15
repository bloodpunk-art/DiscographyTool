using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace DiscographyTool
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Исключения UI-потока (WPF)
            this.DispatcherUnhandledException += OnDispatcherUnhandledException;

            // Исключения фоновых потоков
            AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            HandleException(e.Exception);
            e.Handled = true;
            Shutdown();
        }

        private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                HandleException(ex);
            }
            else
            {
                WriteLog("Unknown unhandled exception");
            }
        }

        private void HandleException(Exception ex)
        {
            WriteLog(ex.ToString());

            MessageBox.Show(
                "Произошла ошибка в работе программы.\n\n" +
                "Подробности записаны в файл error.log",
                "DiscographyTool",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }

        private void WriteLog(string text)
        {
            try
            {
                File.AppendAllText(
                    "error.log",
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]\n{text}\n\n"
                );
            }
            catch
            {
                // Ничего не делаем, чтобы не вызвать повторное падение
            }
        }
    }
}
