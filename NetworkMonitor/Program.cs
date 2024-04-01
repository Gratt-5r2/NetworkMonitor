namespace NetworkMonitor
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            TrayIcon trayIcon = new TrayIcon();
            Application.Run();
        }
    }
}