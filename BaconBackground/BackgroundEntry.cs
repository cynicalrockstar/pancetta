using Pancetta;
using Pancetta.Helpers;
using Pancetta.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;

namespace Pancetta.Windows.Background
{
    public sealed class BackgroundEntry : IBackgroundTask
    {
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            // Setup the ref counted deferral
            RefCountedDeferral refDeferral = new RefCountedDeferral(taskInstance.GetDeferral(), OnDeferralCleanup);

            // Add a ref so everyone in this call is protected
            refDeferral.AddRef();

            // Fire off the update
            await BackgroundManager.Instance.RunUpdate(refDeferral);

            // After this returns the deferral will call complete unless someone else took a ref on it.
            refDeferral.ReleaseRef();
        }

        /// <summary>
        /// Fired just before the deferral is about to complete. We need to flush the settings here.
        /// </summary>
        public void OnDeferralCleanup()
        {
            // We need to flush the settings here. We need to block this function
            // until the settings are flushed.
            using (AutoResetEvent are = new AutoResetEvent(false))
            {
                Task.Run(async () =>
                {
                    // Flush out the local settings
                    await SettingsManager.Instance.FlushLocalSettings();
                    are.Set();
                });
                are.WaitOne();
            }
        }
    }
}
