using Pancetta.DataObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Pancetta.Helpers;
using System.Net;

namespace Pancetta.Managers
{
    /// <summary>
    /// Event args for the OnSubredditsUpdated event.
    /// </summary>
    public class OnSubredditsUpdatedArgs : EventArgs
    {
        public List<Subreddit> NewSubreddits;
    }

    public class SubredditManager
    {
        private static SubredditManager _instance = null;
        public static SubredditManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new SubredditManager();
                return _instance;
            }
        }

        /// <summary>
        /// Fired when the subreddit list updates.
        /// </summary>
        public event EventHandler<OnSubredditsUpdatedArgs> OnSubredditsUpdated
        {
            add { m_onSubredditsUpdated.Add(value); }
            remove { m_onSubredditsUpdated.Remove(value); }
        }
        SmartWeakEvent<EventHandler<OnSubredditsUpdatedArgs>> m_onSubredditsUpdated = new SmartWeakEvent<EventHandler<OnSubredditsUpdatedArgs>>();

        //
        // Private Vars
        //
        object objectLock = new object();
        bool m_isUpdateRunning = false;

        /// <summary>
        /// This is used as a temp cache while the app is open to look up any subreddits
        /// the user wanted to look at temporally.
        /// </summary>
        List<Subreddit> m_tempSubredditCache = new List<Subreddit>();

        private SubredditManager()
        {
            UserManager.Instance.OnUserUpdated += UserMan_OnUserUpdated;
        }

        /// <summary>
        /// Called when the reddit subreddit list should be updated.
        /// </summary>
        /// <param name="force">Forces the update</param>
        public bool Update(bool force = false)
        {
            //TimeSpan timeSinceLastUpdate = DateTime.Now - LastUpdate;
            //if (!force && timeSinceLastUpdate.TotalMinutes < 300 && SubredditList.Count > 0)
            //{
            //   return false;
            //}

            lock(objectLock)
            {
                if(m_isUpdateRunning)
                {
                    return false;
                }
                m_isUpdateRunning = true;
            }

            // Kick off an new task
            new Task(async () =>
            {
                try
                {
                    // Get the entire list of subreddits. We will give the helper a super high limit so it
                    // will return all it can find.
                    string baseUrl = UserManager.Instance.IsUserSignedIn ? "/subreddits/mine.json" : "/subreddits/default.json";
                    int maxLimit = UserManager.Instance.IsUserSignedIn ? 99999 : 100;
                    RedditListHelper <Subreddit> listHelper = new RedditListHelper<Subreddit>(baseUrl, NetworkManager.Instance);

                    // Get the list
                    List<Element<Subreddit>> elements = await listHelper.FetchElements(0, maxLimit);

                    // Create a json list from the wrappers.
                    List<Subreddit> subredditList = new List<Subreddit>();
                    foreach(Element<Subreddit> element in elements)
                    {
                        subredditList.Add(element.Data);
                    }

                    // Update the subreddit list
                    HandleSubredditsFromWeb(subredditList);
                    LastUpdate = DateTime.Now;
                }
                catch(Exception e)
                {
                    MessageManager.Instance.DebugDia("Failed to get subreddit list", e);
                }

                // Indicate we aren't running anymore
                m_isUpdateRunning = false;
            }).Start();
            return true;
        }

        public void SetFavorite(string redditId, bool isAdding)
        {
            if(isAdding && FavoriteSubreddits.ContainsKey(redditId))
            {
                // We already have it
                return;
            }

            if(!isAdding && !FavoriteSubreddits.ContainsKey(redditId))
            {
                // We already don't have it
                return;
            }

            if(isAdding)
            {
                // Add it to the map
                FavoriteSubreddits[redditId] = true;
            }
            else
            {
                FavoriteSubreddits.Remove(redditId);
            }

            // Force the settings to save
            SaveSettings();

            SetSubreddits(SubredditList);
        }

        /// <summary>
        /// Returns the given subreddit by display name
        /// </summary>
        /// <param name="subredditDisplayName"></param>
        /// <returns></returns>
        public Subreddit GetSubredditByDisplayName(string subredditDisplayName)
        {
            // Grab a local copy
            List<Subreddit> subreddits = SubredditList;

            // Look for the subreddit
            foreach (Subreddit subreddit in subreddits)
            {
                if (subreddit.DisplayName.Equals(subredditDisplayName))
                {
                    return subreddit;
                }
            }

            // Check the temp cache
            lock (m_tempSubredditCache)
            {
                foreach (Subreddit subreddit in m_tempSubredditCache)
                {
                    if (subreddit.DisplayName.Equals(subredditDisplayName))
                    {
                        return subreddit;
                    }
                }
            }

            // We don't have it.
            return null;
        }

        /// <summary>
        /// Tries to get a subreddit from the web by the display name.
        /// </summary>
        /// <returns>Returns null if the subreddit get fails.</returns>
        public async Task<Subreddit> GetSubredditFromWebByDisplayName(string displayName)
        {
            Subreddit foundSubreddit = null;
            try
            {
                // Make the call
                string jsonResponse = await NetworkManager.Instance.MakeRedditGetRequestAsString($"/r/{displayName}/about/.json");

                // Parse the new subreddit
                foundSubreddit = MiscellaneousHelper.ParseOutRedditDataElement<Subreddit>(BaconManager.Instance, jsonResponse);
            }
            catch (Exception e)
            {
                MessageManager.Instance.DebugDia("failed to get subreddit", e);
            }

            // If we found it add it to the cache.
            if(foundSubreddit != null)
            {
                lock(m_tempSubredditCache)
                {
                    m_tempSubredditCache.Add(foundSubreddit);
                }
            }

            return foundSubreddit;
        }

        /// <summary>
        /// Checks to see if a subreddit is subscribed to by the user or not.
        /// </summary>
        /// <param name="displayName"></param>
        /// <returns></returns>
        public bool IsSubredditSubscribedTo(string displayName)
        {
            // Grab a local copy
            List<Subreddit> subreddits = SubredditList;

            // If it exists in the local subreddit list it is subscribed to.
            foreach (Subreddit subreddit in subreddits)
            {
                if (subreddit.DisplayName.Equals(displayName))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Subscribes or unsubscribes to a subreddit
        /// </summary>
        /// <param name="subredditId"></param>
        /// <param name="subscribe"></param>
        public async Task<bool> ChangeSubscriptionStatus(string subredditId, bool subscribe)
        {
            try
            {
                // Build the data to send
                List<KeyValuePair<string, string>> postData = new List<KeyValuePair<string, string>>();
                postData.Add(new KeyValuePair<string, string>("action", subscribe ? "sub" : "unsub"));
                postData.Add(new KeyValuePair<string, string>("sr", "t5_"+subredditId));

                // Make the call
                string jsonResponse = await NetworkManager.Instance.MakeRedditPostRequestAsString($"/api/subscribe", postData);

                // Validate the response
                if (jsonResponse.Contains("{}"))
                {
                    AddRecentlyChangedSubedSubreddit(subredditId, subscribe);
                    return true;
                }

                // Report the error
                MessageManager.Instance.DebugDia("failed to subscribe / unsub subreddit, reddit returned an expected value");
                return false;
            }
            catch (Exception e)
            {
                MessageManager.Instance.DebugDia("failed to subscribe / unsub subreddit", e);
            }
            return false;
        }

        private void AddRecentlyChangedSubedSubreddit(string subredditId, bool subscribe)
        {
            if (!subscribe)
            {
                // Try to find it in the list
                bool removeSuccess = false;
                List<Subreddit> currentSubs = SubredditList;
                for (int i = 0; i < currentSubs.Count; i++)
                {
                    if (currentSubs[i].Id.Equals(subredditId))
                    {
                        currentSubs.RemoveAt(i);
                        removeSuccess = true;
                        break;
                    }
                }

                // If successful reset the new list
                if (removeSuccess)
                {
                    SetSubreddits(currentSubs);
                }
            }
            else
            {
                // If we just subscribed to a new subreddit...
                // Try to find the subreddit in our temp subreddits.
                Subreddit subreddit = null;
                lock (m_tempSubredditCache)
                {
                    foreach (Subreddit serachSub in m_tempSubredditCache)
                    {
                        if (serachSub.Id.Equals(subredditId))
                        {
                            subreddit = serachSub;
                        }
                    }
                }

                if (subreddit == null)
                {
                    // If we can't find it just force an update.
                    Update(true);
                }
                else
                {
                    // Otherwise add it locally
                    List<Subreddit> currentSubs = SubredditList;
                    currentSubs.Add(subreddit);
                    SetSubreddits(currentSubs);
                }
            }
        }

        /// <summary>
        /// This function handles subreddits that are being parsed from the web. It will add
        /// any subreddits that need to be added
        /// </summary>
        private void HandleSubredditsFromWeb(List<Subreddit> subreddits)
        {
            var anySaved = subreddits.Where(s => s.Id == "saved");
            foreach (var s in anySaved)
                subreddits.Remove(s);

            // Add the defaults
            // #todo figure out what to add here
            Subreddit subreddit = new Subreddit()
            {
                DisplayName = "all",
                Title = "The top of reddit",
                Id = "all",
                IsArtifical = true,
                IsFavorite = true
            };
            subreddits.Add(subreddit);
            subreddit = new Subreddit()
            {
                DisplayName = "frontpage",
                Title = "Your front page",
                Id = "frontpage",
                IsArtifical = true,
                IsFavorite = true
            };
            subreddits.Add(subreddit);
            subreddit = new Subreddit()
            {
                DisplayName = "saved",
                Title = "Your saved posts",
                Id = "saved",
                IsArtifical = true,
                IsFavorite = true
            };
            subreddits.Add(subreddit);

            // Send them on
            SetSubreddits(subreddits);
        }

        /// <summary>
        /// Used to sort and add fix up the subreddits before they are saved.
        /// </summary>
        /// <param name="subreddits"></param>
        private void SetSubreddits(List<Subreddit> subreddits)
        {
            //Order and arrange
            var newSubredditList = new List<Subreddit>();
            var faves = subreddits.Where(s => s.IsFavorite == true).OrderBy(s => s.DisplayName).ToList();
            newSubredditList.AddRange(faves);
            var nonFaves = subreddits.Where(s => s.IsFavorite == false).OrderBy(s => s.DisplayName).ToList();
            newSubredditList.AddRange(nonFaves);

            // Set the list
            SubredditList = newSubredditList;

            // Save the list
            SaveSettings();

            // Fire the callback for listeners
            m_onSubredditsUpdated.Raise(this, new OnSubredditsUpdatedArgs() { NewSubreddits = SubredditList });
        }

        /// <summary>
        /// Fired when the user is updated, we should update the subreddit list.
        /// </summary>
        private void UserMan_OnUserUpdated(object sender, OnUserUpdatedArgs args)
        {
            // Take action on everything but user updated
            if (args.Action != UserCallbackAction.Updated)
            {
                Update(true);
            }
        }

        /// <summary>
        /// The current list of subreddits.
        /// </summary>
        public List<Subreddit> SubredditList
        {
            get
            {
                if(m_subredditList == null)
                {
                    if(SettingsManager.Instance.LocalSettings.ContainsKey("SubredditManager.SubredditList"))
                    {
                        m_subredditList = SettingsManager.Instance.ReadFromLocalSettings<List<Subreddit>>("SubredditManager.SubredditList");
                    }
                    else
                    {
                        m_subredditList = new List<Subreddit>();

                        // We don't need this 100%, but when the app first
                        // opens we won't have the subreddit yet so this prevents
                        // us from grabbing the sub from the internet.
                        Subreddit subreddit = new Subreddit()
                        {
                            DisplayName = "frontpage",
                            Title = "Your front page",
                            Id = "frontpage"
                        };
                        m_subredditList.Add(subreddit);
                    }
                }
                return m_subredditList;
            }
            private set
            {
                m_subredditList = value;
                SettingsManager.Instance.WriteToLocalSettings<List<Subreddit>>("SubredditManager.SubredditList", m_subredditList);
            }
        }
        private List<Subreddit> m_subredditList = null;

        /// <summary>
        /// The last time the subreddit was updated
        /// </summary>
        public DateTime LastUpdate
        {
            get
            {
                if (m_lastUpdated.Equals(new DateTime(0)))
                {
                    if (SettingsManager.Instance.LocalSettings.ContainsKey("SubredditManager.LastUpdate"))
                    {
                        m_lastUpdated = SettingsManager.Instance.ReadFromLocalSettings<DateTime>("SubredditManager.LastUpdate");
                    }
                }
                return m_lastUpdated;
            }
            private set
            {
                m_lastUpdated = value;
                SettingsManager.Instance.WriteToLocalSettings<DateTime>("SubredditManager.LastUpdate", m_lastUpdated);
            }
        }
        private DateTime m_lastUpdated = new DateTime(0);

        private void SaveSettings()
        {
            // For lists we need to set the list again so the setter is called.
            SubredditList = m_subredditList;
            FavoriteSubreddits = m_favoriteSubreddits;
        }

        /// <summary>
        /// A dictionary of all favorite subreddits
        /// </summary>
        private Dictionary<string, bool> FavoriteSubreddits
        {
            get
            {
                if (m_favoriteSubreddits == null)
                {
                    if (SettingsManager.Instance.RoamingSettings.ContainsKey("SubredditManager.FavoriteSubreddits"))
                    {
                        m_favoriteSubreddits = SettingsManager.Instance.ReadFromRoamingSettings<Dictionary<string, bool>>("SubredditManager.FavoriteSubreddits");
                    }
                    else
                    {
                        m_favoriteSubreddits = new Dictionary<string, bool>();
                        // Add the presets
                        m_favoriteSubreddits.Add("frontpage", true);
                        m_favoriteSubreddits.Add("all", true);
                        m_favoriteSubreddits.Add("saved", true);
                    }
                }
                return m_favoriteSubreddits;
            }
            set
            {
                m_favoriteSubreddits = value;
                SettingsManager.Instance.WriteToRoamingSettings<Dictionary<string, bool>>("SubredditManager.FavoriteSubreddits", m_favoriteSubreddits);
            }
        }
        private Dictionary<string, bool> m_favoriteSubreddits = null;
    }
}

