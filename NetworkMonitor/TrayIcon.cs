using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NetworkMonitor
{
    internal class TrayIcon
    {
        private NotifyIcon trayIcon;
        private System.Windows.Forms.Timer updateTimer;
        private PerformanceCounter downloadCounter;
        private PerformanceCounter uploadCounter;


        public TrayIcon()
        {
            InitializeTrayIcon();
            InitializeTimer();
            UpdateNetworkStatistics();
        }

        private void InitializeTrayIcon()
        {
            trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Text = "TrayApp",
                Visible = true
            };

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Выход", null, OnExit);
            trayIcon.ContextMenuStrip = contextMenu;
        }

        private void InitializeTimer()
        {
            updateTimer = new System.Windows.Forms.Timer
            {
                Interval = 1000
            };

            updateTimer.Tick += UpdateTimer_Tick;
            updateTimer.Start();
        }

        private string FormatSpeed(float speed)
        {
            if (speed < 1024)
            {
                return $"{speed:F2} Б/с";
            }
            else if (speed < 1024 * 1024)
            {
                return $"{speed / 1024:F2} КБ/с";
            }
            else
            {
                return $"{speed / (1024 * 1024):F2} МБ/с";
            }
        }

        private string FormatMBPS(float speed)
        {
            float mbps = speed * 0.00000763f;
            return $"{mbps:F2} МБит/с";
        }


        private const int IconSize = 32;
        private Bitmap IconBitmap = new Bitmap(IconSize, IconSize);

        private Icon GenerateSpeedIcon(float downloadSpeed, float uploadSpeed)
        {
            Graphics graphics = Graphics.FromImage(IconBitmap);
            Icon icon = null;
            try {
                graphics.Clear(Color.Black);

                float downloadPercentage = downloadSpeed / 10485760f;
                int downloadWidth = (int)(downloadPercentage * (float)IconSize);
                graphics.FillRectangle(Brushes.DeepSkyBlue, 0, 0, downloadWidth, IconSize / 2);

                float uploadPercentage = uploadSpeed / 10485760f;
                int uploadWidth = (int)(uploadPercentage * IconSize / 2);
                graphics.FillRectangle(Brushes.Orange, 0, IconSize / 2, uploadWidth, IconSize);

                icon = Icon.FromHandle(IconBitmap.GetHicon());
            }
            catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
            
            graphics.Dispose();

            return icon;
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            UpdateNetworkStatistics();
        }

        private void OnExit(object sender, EventArgs e)
        {
            trayIcon.Visible = false;
            Application.Exit();
        }


        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = CharSet.Auto)]
        extern static bool DestroyIcon(IntPtr handle);

        private long previousBytesReceived;
        private long previousBytesSent;
        DateTime previousTime = DateTime.Now;

        private void UpdateNetworkStatistics()
        {
            long receivedSpeed = 0;
            long sentSpeed = 0;
            foreach (NetworkInterface networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (networkInterface.OperationalStatus == OperationalStatus.Up &&
                    networkInterface.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                    networkInterface.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                {
                    IPv4InterfaceStatistics interfaceStats = networkInterface.GetIPv4Statistics();

                    long bytesReceived = interfaceStats.BytesReceived;
                    long bytesSent = interfaceStats.BytesSent;

                    receivedSpeed += bytesReceived - previousBytesReceived;
                    sentSpeed += bytesSent - previousBytesSent;

                    previousBytesReceived += receivedSpeed;
                    previousBytesSent += sentSpeed;
                }
            }

            DateTime now = DateTime.Now;
            float elapsedTime = (float)(now - previousTime).TotalSeconds;
            previousTime = now;

            string formattedDownloadSpeed = FormatSpeed(receivedSpeed / elapsedTime);
            string formattedDownloadMBPS = FormatMBPS(receivedSpeed / elapsedTime);
            string formattedUploadSpeed = FormatSpeed(sentSpeed / elapsedTime);
            string formattedUploadMBPS = FormatMBPS(sentSpeed / elapsedTime);

            string lineDownload = $"↓:  {formattedDownloadSpeed}   \t↓:  {formattedDownloadMBPS}";
            string lineUpload   = $"↑:  {formattedUploadSpeed}   \t↑:  {formattedUploadMBPS}";
            string updatedText  = $"{lineDownload}\n\n{lineUpload}";

            trayIcon.Text = updatedText;

            var icon = GenerateSpeedIcon(receivedSpeed, sentSpeed);
            if (icon != null)
            {
                var oldIcon = trayIcon.Icon;
                trayIcon.Icon = icon;
                DestroyIcon(oldIcon.Handle);
            }
        }
    }
}
