using CheckHost.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace CheckHost
{
    class Program : Form
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            Application.Run(new Program());
        }

        private NotifyIcon trayIcon;
        private ContextMenu trayMenu;
        private System.Timers.Timer timer;
        private StreamWriter m_log;

        private long TimeUp = 0;
        private long TimeDown = 0;
        private long TotalTimeUp = 0;
        private long TotalTimeDown = 0;

        public Program()
        {
            try
            {
                // Create a simple tray menu with only one item.
                trayMenu = new ContextMenu();
                trayMenu.MenuItems.Add(Resources.Exit, OnExit);
                trayMenu.MenuItems.Add(Resources.OpenFile, OnOpenFile);
                trayMenu.MenuItems.Add(Resources.About, OnAbout);

                // Create a tray icon. In this example we use a
                // standard system icon for simplicity, but you
                // can of course use your own custom icon too.
                trayIcon = new NotifyIcon();
                trayIcon.Text = Resources.WIP;
                trayIcon.Icon = Resources.network;

                // Add menu to tray icon and show it.
                trayIcon.ContextMenu = trayMenu;
                trayIcon.Visible = true;

                // Check every n seconds
                timer = new System.Timers.Timer(Settings.Default.CheckSecondsInterval * 1000);
                timer.Elapsed += timer_Elapsed;
                timer.Enabled = true;

                // init result file
                WriteLogHead();

                timer_Elapsed(this, null);
            }
            catch (Exception ex)
            {
                ProcessExp(ex);
            }
        }

        private void OnAbout(object sender, EventArgs e)
        {
            string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();


            MessageBox.Show(String.Format(Resources.Gossip, 
                                          Settings.Default.Host, 
                                          Settings.Default.CheckSecondsInterval,
                                          Path.GetFullPath(Settings.Default.OutFile),
                                          version), "CheckHost, by Vincent Tollu");
        }

        private void OnOpenFile(object sender, EventArgs e)
        {
            Process.Start(Settings.Default.OutFile);
        }

        private void WriteLogHead()
        {
            m_log = new System.IO.StreamWriter(Settings.Default.OutFile, true);
            FileInfo f = new FileInfo(Settings.Default.OutFile);

            if (f.Length < 1)
            {
                m_log.WriteLine("\"{0}\";\"{1}\";\"{2}\";\"{3}\";\"{4}\";\"{5}\";\"{6}\"", Resources.ColumnDate, Resources.ColumnHeure, Resources.ColumnResult, Resources.ColumnPercentUp, Resources.ColumnDay, Resources.ColumnHours, Resources.ColumnMins);
            }
            m_log.Write(System.DateTime.Now.ToShortDateString() + ";" +
                             System.DateTime.Now.ToLongTimeString() + ";");
            m_log.Write(Resources.StartMessage + ";");
            m_log.WriteLine(0 + ";" + 0 + ";" + 0 + ";" + 0);
            m_log.Flush();
        }

        private void WriteLogResults(bool IsSuccess, long Time, long TotalTimeUp, long TotalTimeDown)
        {
            TimeSpan ts = TimeSpan.FromSeconds(TotalTimeUp + TotalTimeDown);
            double percent = ((TotalTimeUp + TotalTimeDown) > 0) ? (double)(100 * TotalTimeUp / (TotalTimeUp + TotalTimeDown)) : 100;

            m_log.Write(System.DateTime.Now.ToShortDateString() + ";" +
                             System.DateTime.Now.ToLongTimeString() + ";");
            if (IsSuccess == true) m_log.Write(Resources.OK + ";");
            else m_log.Write(Resources.NoCon + ";");
            m_log.WriteLine(percent + ";" + ts.Days + ";" + ts.Hours + ";" + ts.Minutes);
            m_log.Flush();
        }

        private void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                timer.Enabled = false;

                if (Check(Settings.Default.Host))
                {
                    trayIcon.Icon = Resources.networkOK;
                    trayIcon.Text = String.Format(Resources.Connected, Settings.Default.Host, GetTimeString());

                    TimeDown = 0;

                    // Do not write OK logs too often, like once per hour
                    if ((TimeUp / Settings.Default.CheckSecondsInterval) % 360 == 0)
                        WriteLogResults(true, TimeUp, TotalTimeUp, TotalTimeDown);

                    TimeUp += Settings.Default.CheckSecondsInterval;
                    TotalTimeUp += Settings.Default.CheckSecondsInterval;
                }
                else
                {
                    trayIcon.Icon = Resources.networkK0;
                    trayIcon.Text = String.Format(Resources.DisConnected, Settings.Default.Host, GetTimeString());

                    TimeUp = 0;

                    WriteLogResults(false, TimeDown, TotalTimeUp, TotalTimeDown);

                    TimeDown += Settings.Default.CheckSecondsInterval;
                    TotalTimeDown += Settings.Default.CheckSecondsInterval;
                }
            }
            catch (Exception ex)
            {
                ProcessExp(ex);
            }
            finally
            {
                timer.Enabled = true;
            }
        }

        private string GetTimeString()
        {
            long value = TimeUp > 0 ? TimeUp : TimeDown;

            TimeSpan ts = TimeSpan.FromSeconds(value);
            return ts.ToString();
        }

        public bool Check(string stHost)
        {
            try
            {
                System.Net.IPHostEntry he = System.Net.Dns.GetHostEntry(stHost);
                return true;
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Loads and hide
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            try
            {
                Visible = false; // Hide form window.
                ShowInTaskbar = false; // Remove from taskbar.

                base.OnLoad(e);
            }
            catch (Exception ex)
            {
                ProcessExp(ex);
            }
        }

        private static void ProcessExp(Exception ex)
        {
            MessageBox.Show(string.Format(Resources.Error, ex.Message));
        }

        private void OnExit(object sender, EventArgs e)
        {
            Application.Exit();
        }

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                // Release the icon resource.
                trayIcon.Dispose();
            }

            base.Dispose(isDisposing);
        }
    }
}
