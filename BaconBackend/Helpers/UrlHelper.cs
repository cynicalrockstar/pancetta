using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Pancetta.Common.Helpers
{
    public static class UrlHelper
    {
        public static string GetYoutubeEmbedLink(String url)
        {
            var id = TryGetYoutubeId(url);
            if (id == null)
                return null;

            return $"http://www.youtube.com/embed/{id}";
        }

        /// <summary>
        /// Attempts to get a youtube id from a url.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string TryGetYoutubeId(String url)
        {
            if (String.IsNullOrWhiteSpace(url))
            {
                return null;
            }

            // Try to find the ID
            string youtubeVideoId = String.Empty;
            string urlLower = url.ToLower();
            if (urlLower.Contains("youtube.com"))
            {
                // Check for an attribution link
                int attribution = urlLower.IndexOf("attribution_link?");
                if (attribution != -1)
                {
                    // We need to parse out the video id
                    // looks like this attribution_link?a=bhvqtDGQD6s&amp;u=%2Fwatch%3Fv%3DrK0D1ehO7CA%26feature%3Dshare
                    int uIndex = urlLower.IndexOf("u=", attribution);
                    string encodedUrl = url.Substring(uIndex + 2);
                    string decodedUrl = WebUtility.UrlDecode(encodedUrl);
                    urlLower = decodedUrl.ToLower();
                    // At this point urlLower should be something like "v=jfkldfjl&feature=share"
                }

                int beginId = urlLower.IndexOf("v=");
                int endId = urlLower.IndexOf("&", beginId);
                if (beginId != -1)
                {
                    if (endId == -1)
                    {
                        endId = urlLower.Length;
                    }
                    // Important! Since this might be case sensitive use the original url!
                    beginId += 2;
                    youtubeVideoId = url.Substring(beginId, endId - beginId);
                }
            }
            else if (urlLower.Contains("youtu.be"))
            {
                int domain = urlLower.IndexOf("youtu.be");
                int beginId = urlLower.IndexOf("/", domain);
                int endId = urlLower.IndexOf("?", beginId);
                // If we can't find a ? search for a &
                if (endId == -1)
                {
                    endId = urlLower.IndexOf("&", beginId);
                }

                if (beginId != -1)
                {
                    if (endId == -1)
                    {
                        endId = urlLower.Length;
                    }
                    // Important! Since this might be case sensitive use the original url!
                    beginId++;
                    youtubeVideoId = url.Substring(beginId, endId - beginId);
                }
            }

            return youtubeVideoId;
        }
    }
}
