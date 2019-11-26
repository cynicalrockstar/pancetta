using Pancetta.Helpers;
using Pancetta.Managers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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

namespace Pancetta.Windows.HelperControls
{
    public sealed partial class MotdPopUp : UserControl
    {
        /// <summary>
        /// Fired when the Motd box close
        /// </summary>
        public event EventHandler<EventArgs> OnHideComplete
        {
            add { m_onHideComplete.Add(value); }
            remove { m_onHideComplete.Remove(value); }
        }
        SmartWeakEvent<EventHandler<EventArgs>> m_onHideComplete = new SmartWeakEvent<EventHandler<EventArgs>>();

        public MotdPopUp(string title, string markdownContent)
        {
            this.InitializeComponent();

            // Set the title and markdown
            ui_titleText.Text = title;
            ui_markdownText.Markdown = markdownContent;

            // Hide the box
            VisualStateManager.GoToState(this, "HideDialog", false);
        }

        public void ShowPopUp()
        {
            // Show it
            VisualStateManager.GoToState(this, "ShowDialog", true);

            // Sub to back button callbacks
            BaconManager.Instance.OnBackButton += BaconMan_OnBackButton;
        }

        private void Close_OnIconTapped(object sender, EventArgs e)
        {
            // Hide it
            VisualStateManager.GoToState(this, "HideDialog", true);

            // UnSub to back button callbacks
            BaconManager.Instance.OnBackButton -= BaconMan_OnBackButton;
        }

        /// <summary>
        /// Fired when the hide animation is complete
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HideDialog_Completed(object sender, object e)
        {
            m_onHideComplete.Raise(this, new EventArgs());
        }

        /// <summary>
        /// Fired when the back button is pressed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BaconMan_OnBackButton(object sender, Pancetta.OnBackButtonArgs e)
        {
            if(e.IsHandled)
            {
                return;
            }

            // Close the dialog
            Close_OnIconTapped(null, null);

            // Mark as handled.
            e.IsHandled = true;
        }

        private void MarkdownText_OnMarkdownLinkTapped(object sender, UniversalMarkdown.OnMarkdownLinkTappedArgs e)
        {            
            try
            {
                // See if what we have is a reddit link
                RedditContentContainer redditContent = MiscellaneousHelper.TryToFindRedditContentInLink(e.Link);

                if(redditContent != null && redditContent.Type != RedditContentType.Website)
                {
                    // If we are opening a reddit link show the content and hide hide the message.
                    BaconManager.Instance.ShowGlobalContent(redditContent);

                    // Hide the box
                    Close_OnIconTapped(null, null);
                }
                else
                {
                    // If we have a link show it but don't close the message.
                    BaconManager.Instance.ShowGlobalContent(e.Link);
                }                
            }
            catch (Exception ex)
            {
                MessageManager.Instance.DebugDia("MOTDLinkFailedToOpen", ex);
            }
        }
    }
}
