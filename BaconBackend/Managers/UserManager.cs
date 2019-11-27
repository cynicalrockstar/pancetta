using Pancetta.DataObjects;
using Pancetta.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        private static UserManager _instance = null;
        public static UserManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new UserManager();

                return _instance;
            }
        }

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

        private UserManager()
        {
        }

        /// <summary>
        /// Returns the current user's access token
        /// </summary>
        public async Task<string> GetAccessToken()
        {
            return await AuthManager.Instance.GetAccessToken();
        }

        public async Task<SignInResult> SignInNewUser()
        {
            // Start the process by trying to auth a new user
            SignInResult result = await AuthManager.Instance.AuthNewUser();

            // Check the result
            if(!result.WasSuccess)
            {
                return result;
            }

            // Try to get the new user
            result = await InternalUpdateUser();

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
            // Remove the auth
            AuthManager.Instance.DeleteCurrentAuth();

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
                IHttpContent resonse = await NetworkManager.Instance.MakeRedditGetRequest("/api/v1/me/.json");

                // Parse the user
                User user = await NetworkManager.Instance.DeseralizeObject<User>(resonse);

                // Set the new user
                CurrentUser = user;

                // Tell our listeners, but ignore any errors from them
                FireOnUserUpdated(lastUserName == CurrentUser.Name ? UserCallbackAction.Updated : UserCallbackAction.Added);
            }
            catch (Exception e)
            {
                MessageManager.Instance.DebugDia("Failed to parse user", e);
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
                MessageManager.Instance.DebugDia("Failed to notify user listener of update", ex);
            }
        }

        /// <summary>
        /// Returns is a current user exists
        /// </summary>
        public bool IsUserSignedIn
        {
            get
            {
                return AuthManager.Instance.IsUserSignedIn;
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
                    if (SettingsManager.Instance.RoamingSettings.ContainsKey("UserManager.CurrentUser"))
                    {
                        m_currentUser = SettingsManager.Instance.ReadFromRoamingSettings<User>("UserManager.CurrentUser");
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
                SettingsManager.Instance.WriteToRoamingSettings<User>("UserManager.CurrentUser", m_currentUser);
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
                    if (SettingsManager.Instance.LocalSettings.ContainsKey("UserManager.LastUpdate"))
                    {
                        m_lastUpdated = SettingsManager.Instance.ReadFromLocalSettings<DateTime>("UserManager.LastUpdate");
                    }
                }
                return m_lastUpdated;
            }
            set
            {
                m_lastUpdated = value;
                SettingsManager.Instance.WriteToLocalSettings<DateTime>("UserManager.LastUpdate", m_lastUpdated);
            }
        }
        private DateTime m_lastUpdated = new DateTime(0);
    }
}
