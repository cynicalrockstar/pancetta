﻿using Pancetta.Windows.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Store;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Pancetta.Windows.Panels.SettingsPanels
{
    public sealed partial class AboutSettings : UserControl, IPanel
    {
        IPanelHost m_host;

        public AboutSettings()
        {
            this.InitializeComponent();
        }

        public void PanelSetup(IPanelHost host, Dictionary<string, object> arguments)
        {
            m_host = host;
        }

        public void OnNavigatingFrom()
        {
            // Pause the snow if going
            ui_letItSnow.AllOfTheSnowIsNowBlackSlushPlsSuspendIt();
        }

        public void OnPanelPulledToTop(Dictionary<string, object> arguments)
        {
            OnNavigatingTo();
        }

        public async void OnNavigatingTo()
        {
            // Set the status bar color and get the size returned. If it is not 0 use that to move the
            // color of the page into the status bar.
            double statusBarHeight = await m_host.SetStatusBar(null, 0);
            ui_contentRoot.Margin = new Thickness(0, -statusBarHeight, 0, 0);
            ui_contentRoot.Padding = new Thickness(0, statusBarHeight, 0, 0);

            Package package = Package.Current;
            PackageId packageId = package.Id;
            PackageVersion version = packageId.Version;
            ui_buildString.Text = $"Build: {version.Major}.{version.Minor}.{version.Build}.{version.Revision}";

            // Resume snow if it was going
            ui_letItSnow.OkNowIWantMoreSnowIfItHasBeenStarted();
        }

        public void OnCleanupPanel()
        {
            // Ignore for now.
        }

        /// <summary>
        /// Fired when the panel should try to reduce memory if possible. This will only be called
        /// while the panel isn't visible.
        /// </summary>
        public void OnReduceMemory()
        {
            // Ignore for now.
        }

        private async void RateAndReview_Tapped(object sender, TappedRoutedEventArgs e)
        {
            await global::Windows.System.Launcher.LaunchUriAsync(new Uri("ms-windows-store:reviewapp?appid=" + CurrentApp.AppId));
        }

        private void Website_Tapped(object sender, TappedRoutedEventArgs e)
        {
            OpenGlobalPresenter("http://baconit.quinndamerell.com/");
        }

        private void ShowSource_Tapped(object sender, TappedRoutedEventArgs e)
        {
            OpenGlobalPresenter("http://github.com/cynicalrockstar/pancetta");
        }

        private void OpenBaconitSub_Tapped(object sender, TappedRoutedEventArgs e)
        {
            //#todo make this open in app when we support it
            OpenGlobalPresenter("http://reddit.com/r/Baconit");
        }

        private void Logo_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // Start the snow
            ui_letItSnow.MakeItSnow();

            // Navigate to developer settings
            m_host.Navigate(typeof(DeveloperSettings), "DeveloperSettings");
        }

        private void OpenGlobalPresenter(string url)
        {
            BaconManager.Instance.ShowGlobalContent(url);
        }
    }
}
