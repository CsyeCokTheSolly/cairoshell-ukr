﻿using CairoDesktop.Common.Logging;
using CairoDesktop.Configuration;
using CairoDesktop.Interop;
using CairoDesktop.SupportingClasses;
using CairoDesktop.WindowsTasks;
using CairoDesktop.WindowsTray;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace CairoDesktop
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        public static new App Current
        {
            get
            {
                return System.Windows.Application.Current as App;
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (Settings.Instance.ForceSoftwareRendering)
                RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
        }

        protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
        {
            base.OnSessionEnding(e);

            GracefullyExit();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            GracefullyExit();
        }

        private void GracefullyExit()
        {
            WindowManager.ResetWorkArea();
            AppBarHelper.ShowWindowsTaskbar();

            DisposeSingletons();

            CairoLogger.Instance.Debug("App: Graceful exit complete");
        }

        private void DisposeSingletons()
        {
            Shell.DisposeIml();
            FullScreenHelper.Instance.Dispose();
            NotificationArea.Instance.Dispose();
            // UpdateManager.Instance.Dispose();
            WindowManager.Instance.Dispose();
            Tasks.Instance.Dispose();
        }

        private static bool errorVisible;

        private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            string version = fvi.FileVersion;

            string inner = "";
            if (e.Exception.InnerException != null)
                inner = "\r\n\r\nInner exception:\r\nMessage: " + e.Exception.InnerException.Message + "\r\nTarget Site: " + e.Exception.InnerException.TargetSite + "\r\n\r\n" + e.Exception.InnerException.StackTrace;

            string msg = "Would you like to restart Cairo?\r\n\r\nPlease submit a bug report with a screenshot of this error. Thanks! \r\nMessage: " + e.Exception.Message + "\r\nTarget Site: " + e.Exception.TargetSite + "\r\nVersion: " + version + "\r\n\r\n" + e.Exception.StackTrace + inner;

            CairoLogger.Instance.Error(msg, e.Exception);

            string dMsg;

            if (msg.Length > 1000)
                dMsg = msg.Substring(0, 999) + "...";
            else
                dMsg = msg;

            try
            {
                if (!errorVisible)
                {
                    errorVisible = true;

                    // Automatically restart for known render thread failure messages.
                    if (e.Exception.Message.StartsWith("UCEERR_RENDERTHREADFAILURE"))
                    {
                        CairoDesktop.Startup.RestartCairo();
                        Environment.FailFast("Automatically restarted Cairo due to a render thread failure.");
                    }
                    else
                    {
                        if (MessageBox.Show(dMsg, "Cairo Desktop Error", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                        {
                            // it's like getting a morning coffee.
                            CairoDesktop.Startup.RestartCairo();
                            Environment.FailFast("User restarted Cairo due to an exception.");
                        }
                    }

                    errorVisible = false;
                }
            }
            catch
            {
                // If this fails we're probably up the creek. Abandon ship!
                CairoDesktop.Startup.ExitCairo();
            }

            e.Handled = true;
        }


        /// <summary>
        /// Compatibility System.Windows.Forms.Application.DoEvents
        /// </summary>
        public static void DoEvents()
        {
            DispatcherFrame frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,
                new DispatcherOperationCallback(ExitFrame), frame);
            Dispatcher.PushFrame(frame);
        }

        private static object ExitFrame(object f)
        {
            ((DispatcherFrame)f).Continue = false;
            return null;
        }

        /// <summary>
        /// Compatibility System.Windows.Forms.Application
        /// </summary>
        public static string StartupPath { get { return Path.GetDirectoryName((Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly()).Location); } }

        public static string ProductName { get { return (Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly()).GetName().Name; } }

        public static Version ProductVersion { get { return (Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly()).GetName().Version; } }

        public static string CairoApplicationDataFolder { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Cairo_Development_Team"); } }

        public static string LogsFolder { get { return Path.Combine(CairoApplicationDataFolder, "Logs"); } }
    }
}