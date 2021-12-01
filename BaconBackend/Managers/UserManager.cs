using Pancetta.DataObjects;
using Pancetta.Helpers;
using Reddit.AuthTokenRetriever;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Web.Http;

namespace Pancetta.Managers
{
    // Indicates what action happened.
    public enum UserCallbackAction
    {
        Added,
        Updated,
        Removed
    }

    /// <summary>
    /// The event args for the OnUserUpdated event.
    /// </summary>
    public class OnUserUpdatedArgs : EventArgs
    {
        public UserCallbackAction Action;
    }

    public class UserManager
    {
        private OAuthToken AccessTokenData
        {
            get
            {
                if (m_accessTokenData == null)
                {
                    if (m_baconMan.SettingsMan.RoamingSettings.ContainsKey("AuthManager.AccessToken"))
                    {
                        m_accessTokenData = m_baconMan.SettingsMan.ReadFromRoamingSettings<OAuthToken>("AuthManager.AccessToken");
                    }
                    else
                    {
                        m_accessTokenData = null;
                    }
                }
                return m_accessTokenData;
            }
            set
            {
                m_accessTokenData = value;

                // #todo remove
                if (m_accessTokenData != null && String.IsNullOrWhiteSpace(m_accessTokenData.RefreshToken))
                {
                    Debugger.Break();
                }

                m_baconMan.SettingsMan.WriteToRoamingSettings<OAuthToken>("AuthManager.AccessToken", m_accessTokenData);
            }
        }

        private OAuthToken m_accessTokenData = null;

        /// <summary>
        /// Fired when the user is updated, added, or removed
        /// </summary>
        public event EventHandler<OnUserUpdatedArgs> OnUserUpdated
        {
            add { m_onUserUpdated.Add(value); }
            remove { m_onUserUpdated.Remove(value); }
        }
        SmartWeakEvent<EventHandler<OnUserUpdatedArgs>> m_onUserUpdated = new SmartWeakEvent<EventHandler<OnUserUpdatedArgs>>();

        /// <summary>
        /// This class is used to represent a sign in request. 
        /// </summary>
        public class SignInResult
        {
            public bool WasSuccess = false;
            public bool WasErrorNetwork = false;
            public bool WasUserCanceled = false;
            public string Message = "";
        }

        private BaconManager m_baconMan;
        private AuthTokenRetrieverLib m_authLib;

        private String appId = "VS8MYKTvFrbzoI8Zc7fPaw";
        private String appSecret = "u1tSBXHqHi_iDo2-gyNatqhNNQ5X2w";

        private ManualResetEvent m_tokenRefreshEvent = new ManualResetEvent(false);

        public UserManager(BaconManager baconMan)
        {
            m_baconMan = baconMan;
            m_authLib = new AuthTokenRetrieverLib(appId, 8080, appSecret: appSecret);
            m_authLib.AuthSuccess += authSuccess;
        }

        private void authSuccess(object sender, Reddit.AuthTokenRetriever.EventArgs.AuthSuccessEventArgs e)
        {
            m_accessTokenData = e.AccessToken;
            m_tokenRefreshEvent.Set();
        }

        /// <summary>
        /// Returns the current user's access token
        /// </summary>
        public async Task<string> GetAccessToken()
        {
            var shouldRefresh = false;

            if (AccessTokenData != null)
            {
                TimeSpan timeRemaining = AccessTokenData.ExpiresAt - DateTime.Now;

                // If it is already expired or will do so soon wait on the refresh before using it.
                if (timeRemaining.TotalSeconds < 30)
                {
                    shouldRefresh = true;
                }
            }
            else
            {
                shouldRefresh = true;
            }

            if (shouldRefresh)
            {
                m_authLib.AwaitCallback(true);
                await OpenBrowser(m_authLib.AuthURL());

                m_tokenRefreshEvent.WaitOne();
                m_tokenRefreshEvent.Reset();
                m_authLib.StopListening();
            }

            return AccessTokenData.AccessToken;
        }

        public static async Task OpenBrowser(string authUrl = "about:blank")
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                await Windows.System.Launcher.LaunchUriAsync(new Uri(authUrl));
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // For OSX run a separate command to open the web browser as found in https://brockallen.com/2016/09/24/process-start-for-urls-on-net-core/
                Process.Start("open", authUrl);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // Similar to OSX, Linux can (and usually does) use xdg for this task.
                Process.Start("xdg-open", authUrl);
            }
        }

        public async Task<SignInResult> SignInNewUser()
        {
            // Start the process by trying to auth a new user
            //SignInResult result = await m_authMan.AuthNewUser();
            await GetAccessToken();

            // Check the result
            //if(!result.WasSuccess)
            //{
            //    return result;
            //}

            // Try to get the new user
            var result = await InternalUpdateUser();

            // Return the result
            return result;
        }

        public bool UpdateUser(bool forceUpdate = false)
        {
            TimeSpan timeSinceLateUpdate = DateTime.Now - LastUpdate;
            if(timeSinceLateUpdate.TotalHours < 24 && !forceUpdate)
            {
                return false;
            }

            // Kick of a new task to update the user.
            new Task(async ()=> { await InternalUpdateUser(); }).Start();

            return true;
        }

        public void SignOut()
        {
            // Rest the current user object
            CurrentUser = null;
            LastUpdate = new DateTime(0);

            // Fire the user changed callback
            FireOnUserUpdated(UserCallbackAction.Removed);
        }

        private async Task<SignInResult> InternalUpdateUser()
        {
            try
            {
                // Record if we had a user
                string lastUserName = CurrentUser == null ? "" : CurrentUser.Name;

                // Make the web call
                IHttpContent resonse = await m_baconMan.NetworkMan.MakeRedditGetRequest("/api/v1/me/.json");

                // Parse the user
                User user = await m_baconMan.NetworkMan.DeseralizeObject<User>(resonse);

                // Set the new user
                CurrentUser = user;

                // Tell our listeners, but ignore any errors from them
                FireOnUserUpdated(lastUserName == CurrentUser.Name ? UserCallbackAction.Updated : UserCallbackAction.Added);
            }
            catch (Exception e)
            {
                m_baconMan.MessageMan.DebugDia("Failed to parse user", e);
                return new SignInResult()
                {
                    Message = "Failed to parse user"
                };
            }

            return new SignInResult()
            {
                WasSuccess = true
            };
        }

        public void UpdateUnReadMessageCount(int unreadMessages)
        {
            if (CurrentUser != null)
            {
                // Update
                CurrentUser.HasMail = unreadMessages != 0;
                CurrentUser.InboxCount = unreadMessages;
                // Force a save
                CurrentUser = CurrentUser;
                // Fire the callback
                FireOnUserUpdated(UserCallbackAction.Updated);
            }
        }

        private void FireOnUserUpdated(UserCallbackAction action)
        {
            // Tell our listeners, but ignore any errors from them
            try
            {
                m_onUserUpdated.Raise(this, new OnUserUpdatedArgs() { Action = action });
            }
            catch (Exception ex)
            {
                m_baconMan.MessageMan.DebugDia("Failed to notify user listener of update", ex);
            }
        }

        /// <summary>
        /// Returns is a current user exists
        /// </summary>
        public bool IsUserSignedIn
        {
            get
            {
                return AccessTokenData != null;
            }
        }

        /// <summary>
        /// Holds the current user information
        /// </summary>
        public User CurrentUser
        {
            get
            {
                if (m_currentUser == null)
                {
                    if (m_baconMan.SettingsMan.RoamingSettings.ContainsKey("UserManager.CurrentUser"))
                    {
                        m_currentUser = m_baconMan.SettingsMan.ReadFromRoamingSettings<User>("UserManager.CurrentUser");
                    }
                    else
                    {
                        m_currentUser = null;
                    }
                }
                return m_currentUser;
            }
            private set
            {
                m_currentUser = value;
                m_baconMan.SettingsMan.WriteToRoamingSettings<User>("UserManager.CurrentUser", m_currentUser);
            }
        }
        private User m_currentUser = null;

        /// <summary>
        /// The last time the user was updated
        /// </summary>
        private DateTime LastUpdate
        {
            get
            {
                if (m_lastUpdated.Equals(new DateTime(0)))
                {
                    if (m_baconMan.SettingsMan.LocalSettings.ContainsKey("UserManager.LastUpdate"))
                    {
                        m_lastUpdated = m_baconMan.SettingsMan.ReadFromLocalSettings<DateTime>("UserManager.LastUpdate");
                    }
                }
                return m_lastUpdated;
            }
            set
            {
                m_lastUpdated = value;
                m_baconMan.SettingsMan.WriteToLocalSettings<DateTime>("UserManager.LastUpdate", m_lastUpdated);
            }
        }
        private DateTime m_lastUpdated = new DateTime(0);
    }
}
