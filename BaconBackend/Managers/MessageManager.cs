﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Popups;

namespace Pancetta.Managers
{
#pragma warning disable CS4014
    public class MessageManager
    {
        private static MessageManager _instance = null;
        public static MessageManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new MessageManager();

                return _instance;
            }
        }

        private MessageManager()
        {
        }

        public void ShowMessageSimple(string title, string content)
        {
            ShowMessaage(content, title);
        }

        public async void ShowRedditDownMessage()
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                bool? showStatus = await ShowYesNoMessage("Reddit is Down", "It looks like reddit is down right now. Go outside for a while and try again in a few minutes.", "Check Reddit's Status", "Go Outside");
                if (showStatus.HasValue && showStatus.Value)
                {
                    BaconManager.Instance.ShowGlobalContent("http://www.redditstatus.com/");
                }
            });
        }

        public async void ShowSigninMessage(string toDoWhat)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                // Add the buttons
                bool? response = await ShowYesNoMessage("Login Required", $"You must be logged into to a reddit account to {toDoWhat}. Do you want to login or create a new account now?");

                if(response.HasValue && response.Value)
                {
                    BaconManager.Instance.NavigateToLogin();
                }
            });
        }


        public void DebugDia(string str, Exception ex = null)
        {
            if (UiSettingManager.Instance.Developer_Debug)
            {
                System.Diagnostics.Debug.WriteLine("Error, " + str + " Message: " + (ex == null ? "" : ex.Message));
                ShowMessaage("DebugDia: str " + str + " \n\nMessage: " + (ex == null ? "" : ex.Message) + "\n\nCall Stack:\n"+(ex== null? "" : ex.StackTrace), "DebugDia");
            }
        }

        private async void ShowMessaage(string content, string title)
        {
            // Don't show messages if we are in the background.
            if(BaconManager.Instance.IsBackgroundTask)
            {
                return;
            }

            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                MessageDialog message = new MessageDialog(content, title);
                await message.ShowAsync();
            });
        }

        /// <summary>
        /// Shows a yes no dialog with a message.
        /// MUST BE CALLED FROM THE UI THREAD!
        /// </summary>
        /// <param name="title"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public async Task<bool?> ShowYesNoMessage(string title, string content, string postiveButton = "Yes", string negativeButton = "No")
        {
            // Don't show messages if we are in the background.
            if (BaconManager.Instance.IsBackgroundTask)
            {
                return null;
            }

            bool? response = null;

            // Add the buttons
            MessageDialog message = new MessageDialog(content, title);
            message.Commands.Add(new UICommand(
                postiveButton,
                (IUICommand command)=> {
                    response = true; }));
            message.Commands.Add(new UICommand(
                negativeButton,
                (IUICommand command) => { response = false; }));
            message.DefaultCommandIndex = 0;
            message.CancelCommandIndex = 1;

            // Show the dialog
            await message.ShowAsync();

            return response;
        }
    }
}
