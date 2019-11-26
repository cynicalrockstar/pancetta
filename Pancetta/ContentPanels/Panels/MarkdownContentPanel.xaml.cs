﻿using BaconBackend.Collectors;
using Baconit.Interfaces;
using Baconit.Panels;
using Baconit.Panels.FlipView;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using UniversalMarkdown;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Baconit.ContentPanels.Panels
{
    public sealed partial class MarkdownContentPanel : UserControl, IContentPanel
    {
        /// <summary>
        /// Holds a reference to our base.
        /// </summary>
        IContentPanelBaseInternal m_base;

        /// <summary>
        /// The current markdown text box.
        /// </summary>
        MarkdownTextBlock m_markdownBlock;

        FlipViewPostContext m_context;

        public MarkdownContentPanel(IContentPanelBaseInternal panelBase)
        {
            this.InitializeComponent();
            m_base = panelBase;
        }

        /// <summary>
        /// Called by the host when it queries if we can handle a post.
        /// </summary>
        /// <param name="post"></param>
        /// <returns></returns>
        static public bool CanHandlePost(ContentPanelSource source)
        {
            // If it is a self post and it has text it is for us.
            return source.IsSelf && !String.IsNullOrWhiteSpace(source.SelfText);
        }

        #region IContentPanel

        /// <summary>
        /// Indicates how large the panel is in memory.
        /// </summary>
        public PanelMemorySizes PanelMemorySize
        {
            get
            {
                // #todo can we figure this out?
                return PanelMemorySizes.Small;
            }
        }

        /// <summary>
        /// Fired when we should load the content.
        /// </summary>
        /// <param name="source"></param>
        public async void OnPrepareContent()
        {
            // Since some of this can be costly, delay the work load until we aren't animating.
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                m_markdownBlock = new MarkdownTextBlock();
                m_markdownBlock.OnMarkdownLinkTapped += MarkdownBlock_OnMarkdownLinkTapped;
                m_markdownBlock.OnMarkdownReady += MarkdownBox_OnMarkdownReady;
                m_markdownBlock.Markdown = m_base.Source.SelfText;
                m_markdownBlock.Foreground = new SolidColorBrush(Windows.UI.Colors.Black);
                m_markdownBlock.FontSize = 12;
                ui_contentRoot.Children.Add(m_markdownBlock);

                if (m_base.Source.post != null)
                {
                    votesText.Text = m_base.Source.post.Score.ToString();
                    authorText.Text = m_base.Source.post.Author;
                    subredditText.Text = m_base.Source.post.Subreddit;
                    titleText.Text = m_base.Source.post.Title;
                    commentsText.Text = m_base.Source.post.NumComments.ToString() + " comments";
                }

                m_context = m_base.Source.context;
            });
        }

        /// <summary>
        /// Fired when we should destroy our content.
        /// </summary>
        public void OnDestroyContent()
        {
            // Clear the markdown
            if (m_markdownBlock != null)
            {
                m_markdownBlock.OnMarkdownReady -= MarkdownBox_OnMarkdownReady;
                m_markdownBlock.OnMarkdownLinkTapped -= MarkdownBlock_OnMarkdownLinkTapped;
            }
            m_markdownBlock = null;

            // Clear the UI
            ui_contentRoot.Children.Clear();
        }

        /// <summary>
        /// Fired when a new host has been added.
        /// </summary>
        public void OnHostAdded()
        {
            // Ignore for now.
        }

        /// <summary>
        /// Fired when this post becomes visible
        /// </summary>
        public void OnVisibilityChanged(bool isVisible)
        {
            // Ignore for now
        }

        #endregion

        #region Markdown Events

        /// <summary>
        /// Fired when a link is tapped in the markdown.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MarkdownBlock_OnMarkdownLinkTapped(object sender, OnMarkdownLinkTappedArgs e)
        {
            App.BaconMan.ShowGlobalContent(e.Link);
        }

        /// <summary>
        /// Hide the loading text when the markdown is done.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MarkdownBox_OnMarkdownReady(object sender, OnMarkdownReadyArgs e)
        {
            if (e.WasError)
            {
                m_base.FireOnFallbackToBrowser();
                App.BaconMan.TelemetryMan.ReportUnexpectedEvent(this, "FailedToShowMarkdown", e.Exception);
            }
            else
            {
                // Hide loading
                m_base.FireOnLoading(false);
            }
        }

        #endregion

        private void UpButton_Click(object sender, RoutedEventArgs e)
        {
            m_context.Collector.ChangePostVote(m_context.Post, PostVoteAction.UpVote);
        }

        private void DownButton_Click(object sender, RoutedEventArgs e)
        {
            m_context.Collector.ChangePostVote(m_context.Post, PostVoteAction.DownVote);
        }

        private async void WorldButton_Click(object sender, RoutedEventArgs e)
        {
            string url = m_context.Post.Url;
            if (String.IsNullOrWhiteSpace(url))
            {
                url = m_context.Post.Permalink;
            }
            if (!String.IsNullOrWhiteSpace(url))
            {
                await Windows.System.Launcher.LaunchUriAsync(new Uri(url, UriKind.Absolute));
                App.BaconMan.TelemetryMan.ReportEvent(this, "OpenInBrowser");
            }
        }

        private void ShareButton_Click(object sender, RoutedEventArgs e)
        {
            if (!String.IsNullOrWhiteSpace(m_context.Post.Url))
            {
                // Setup the share contract so we can share data
                DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
                dataTransferManager.DataRequested += DataTransferManager_DataRequested;
                DataTransferManager.ShowShareUI();
                App.BaconMan.TelemetryMan.ReportEvent(this, "SharePostTapped");
            }
        }

        private void UserButton_Click(object sender, RoutedEventArgs e)
        {
            Dictionary<string, object> args = new Dictionary<string, object>();
            args.Add(PanelManager.NAV_ARGS_USER_NAME, m_context.Post.Author);
            m_context.Host.Navigate(typeof(UserProfile), m_context.Post.Author, args);
            App.BaconMan.TelemetryMan.ReportEvent(this, "GoToUserFlipView");
        }

        private void SubredditButton_Click(object sender, RoutedEventArgs e)
        {
            Dictionary<string, object> args = new Dictionary<string, object>();
            args.Add(PanelManager.NAV_ARGS_SUBREDDIT_NAME, m_context.Post.Subreddit);
            m_context.Host.Navigate(typeof(SubredditPanel), m_context.Post.Subreddit + SortTypes.Hot + SortTimeTypes.Week, args);
            App.BaconMan.TelemetryMan.ReportEvent(this, "GoToSubredditFlipView");
        }

        private void DataTransferManager_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            if (m_context.Post != null)
            {
                args.Request.Data.Properties.ApplicationName = "Baconit";
                args.Request.Data.Properties.ContentSourceWebLink = new Uri(m_context.Post.Url, UriKind.Absolute);
                args.Request.Data.Properties.Title = "A Reddit Post Shared From Baconit";
                args.Request.Data.Properties.Description = m_context.Post.Title;
                args.Request.Data.SetText($"\r\n\r\n{m_context.Post.Title}\r\n\r\n{m_context.Post.Url}");
                App.BaconMan.TelemetryMan.ReportEvent(this, "PostShared");
            }
            else
            {
                args.Request.FailWithDisplayText("Baconit doesn't have anything to share!");
                App.BaconMan.TelemetryMan.ReportUnexpectedEvent(this, "FailedToShareFilpViewPostNoSharePost");
            }
        }
    }
}
