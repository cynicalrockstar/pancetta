﻿using Pancetta.DataObjects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Pancetta.Managers;

namespace Pancetta.Helpers
{
    /// <summary>
    /// The type of content that a link points toward.
    /// </summary>
    public enum RedditContentType
    {
        /// <summary>
        /// A subreddit.
        /// </summary>
        Subreddit,
        /// <summary>
        /// A post within a subreddit.
        /// </summary>
        Post,
        /// <summary>
        /// A comment on a reddit post.
        /// </summary>
        Comment,
        /// <summary>
        /// A user.
        /// </summary>
        User,
        /// <summary>
        /// A URL linking somewhere other than reddit.
        /// </summary>
        Website
    }

    /// <summary>
    /// Content that a link posted on reddit links to.
    /// </summary>
    public class RedditContentContainer
    {
        /// <summary>
        /// This content's content type.
        /// </summary>
        public RedditContentType Type;
        /// <summary>
        /// The webpage this content links to, if the type is a website.
        /// </summary>
        public string Website;
        /// <summary>
        /// The subreddit this content links to, if the type is a subreddit.
        /// </summary>
        public string Subreddit;
        /// <summary>
        /// The user this content links to, if the type is a user.
        /// </summary>
        public string User;
        /// <summary>
        /// The post this content links to, if the type is a reddit post
        /// </summary>
        public string Post;
        /// <summary>
        /// The comment this content links to, if the type is a reddit comment.
        /// </summary>
        public string Comment;
    }

    /// <summary>
    /// Error type that occurs when posting a new reddit post.
    /// </summary>
    public enum SubmitNewPostErrors
    {
        /// <summary>
        /// No error occurred.
        /// </summary>
        NONE,
        /// <summary>
        /// An unknown error occurred.
        /// </summary>
        UNKNOWN,
        /// <summary>
        /// The request specified an option that doesn't exist.
        /// </summary>
        INVALID_OPTION,
        /// <summary>
        /// A Captcha is required to submit the post, and it was submitted incorrectly.
        /// </summary>
        BAD_CAPTCHA,
        /// <summary>
        /// The URL linked to in the submitted post is invalid.
        /// </summary>
        BAD_URL,
        /// <summary>
        /// The subreddit posted to does not exist.
        /// </summary>
        SUBREDDIT_NOEXIST,
        /// <summary>
        /// The subreddit posted to does not allow the logged in user to make this post.
        /// </summary>
        SUBREDDIT_NOTALLOWED,
        /// <summary>
        /// No subreddit was specified when posting.
        /// </summary>
        SUBREDDIT_REQUIRED,
        /// <summary>
        /// The subreddit posted to does not allow the logged in user to make this self-text post.
        /// </summary>
        NO_SELFS,
        /// <summary>
        /// The subreddit posted to does not allow the logged in user to make this link post.
        /// </summary>
        NO_LINKS,
        /// <summary>
        /// The post attempt timed out, and failed to complete.
        /// </summary>
        IN_TIMEOUT,
        /// <summary>
        /// Reddit disallows the app to post this cpntent, as it has exceeded the reddit API rate limit.
        /// </summary>
        RATELIMIT,
        /// <summary>
        /// The subreddit posted to does not allow links to be posted to the domain this post linked to.
        /// </summary>
        DOMAIN_BANNED,
        /// <summary>
        /// The subreddit posted to does not allow links to be resubmitted, and this post has already been posted.
        /// </summary>
        ALREADY_SUB
    }

    /// <summary>
    /// A response from reddit when a new post is submitted.
    /// </summary>
    public class SubmitNewPostResponse
    {
        /// <summary>
        /// Whether the post was successfully posted.
        /// </summary>
        public bool Success = false;
        /// <summary>
        /// A link to the post that was successfully created, or the empty string if no post was created.
        /// </summary>
        public string NewPostLink = String.Empty;
        /// <summary>
        /// The error type that occurred when posting, or NONE if no error occurred.
        /// </summary>
        public SubmitNewPostErrors RedditError = SubmitNewPostErrors.NONE;
    }

    /// <summary>
    /// Miscellaneous static helper methods.
    /// </summary>
    public class MiscellaneousHelper
    {
        /// <summary>
        /// Called when the user is trying to comment on something.
        /// </summary>
        /// <returns>Returns the json returned or a null string if failed.</returns>
        public static async Task<string> SendRedditComment(BaconManager baconMan, string redditIdCommentingOn, string comment, bool isEdit = false)
        {
            string returnString = null;
            try
            {
                // Build the data to send
                List<KeyValuePair<string, string>> postData = new List<KeyValuePair<string, string>>();
                postData.Add(new KeyValuePair<string, string>("thing_id", redditIdCommentingOn));
                postData.Add(new KeyValuePair<string, string>("text", comment));

                string apiString = isEdit ? "api/editusertext" : "api/comment";

                // Make the call
                returnString = await NetworkManager.Instance.MakeRedditPostRequestAsString(apiString, postData);
            }
            catch (Exception e)
            {
                MessageManager.Instance.DebugDia("failed to send message", e);
            }
            return returnString;
        }      

        /// <summary>
        /// Gets a reddit user.
        /// </summary>
        /// <returns>Returns null if it fails or the user doesn't exist.</returns>
        public static async Task<User> GetRedditUser(BaconManager baconMan, string userName)
        {
            User foundUser = null;
            try
            {
                // Make the call
                string jsonResponse = await NetworkManager.Instance.MakeRedditGetRequestAsString($"user/{userName}/about/.json");

                // Parse the new user
                foundUser = MiscellaneousHelper.ParseOutRedditDataElement<User>(baconMan, jsonResponse);
            }
            catch (Exception e)
            {
                MessageManager.Instance.DebugDia("failed to search for user", e);
            }
            return foundUser;
        }

        /// <summary>
        /// Attempts to delete a post.
        /// </summary>
        /// <param name="baconMan"></param>
        /// <param name="postId"></param>
        /// <returns></returns>
        public static async Task<bool> DeletePost(BaconManager baconMan, string postId)
        {
            string returnString = null;
            try
            {
                // Build the data to send
                List<KeyValuePair<string, string>> postData = new List<KeyValuePair<string, string>>();
                postData.Add(new KeyValuePair<string, string>("id", "t3_"+postId));   

                // Make the call
                returnString = await NetworkManager.Instance.MakeRedditPostRequestAsString("api/del", postData);

                if(returnString.Equals("{}"))
                {
                    return true;
                }
            }
            catch (Exception e)
            {
                MessageManager.Instance.DebugDia("failed to delete post", e);
            }
            return false;
        }

        /// <summary>
        /// Saves, unsaves, hides, or unhides a reddit item.
        /// </summary>
        /// <returns>Returns null if it fails or the user doesn't exist.</returns>
        public static async Task<bool> SaveOrHideRedditItem(BaconManager baconMan, string redditId, bool? save, bool? hide)
        {
            if(!UserManager.Instance.IsUserSignedIn)
            {
                MessageManager.Instance.ShowSigninMessage(save.HasValue ? "save item" : "hide item");
                return false;
            }

            bool wasSuccess = false;
            try
            {
                // Make the data
                List<KeyValuePair<string, string>> data = new List<KeyValuePair<string, string>>();
                data.Add(new KeyValuePair<string, string>("id", redditId));

                string url;
                if (save.HasValue)
                {
                    url = save.Value ? "/api/save" : "/api/unsave";
                }
                else if(hide.HasValue)
                {
                    url = hide.Value ? "/api/hide" : "/api/unhide";
                }
                else
                {
                    return false;
                }

                // Make the call
                string jsonResponse = await NetworkManager.Instance.MakeRedditPostRequestAsString(url, data);

                if(jsonResponse.Contains("{}"))
                {
                    wasSuccess = true;
                }
                else
                {
                    MessageManager.Instance.DebugDia("failed to save or hide item, unknown response");
                }
            }
            catch (Exception e)
            {
                MessageManager.Instance.DebugDia("failed to save or hide item", e);
            }
            return wasSuccess;
        }

        /// <summary>
        /// Submits a new reddit post
        /// </summary>
        public static async Task<SubmitNewPostResponse> SubmitNewPost(BaconManager baconMan, string title, string urlOrText, string subredditDisplayName, bool isSelfText, bool sendRepliesToInbox)
        {
            if (!UserManager.Instance.IsUserSignedIn)
            {
                MessageManager.Instance.ShowSigninMessage("submit a new post");
                return new SubmitNewPostResponse(){ Success = false };
            }

            try
            {
                // Make the data
                List<KeyValuePair<string, string>> data = new List<KeyValuePair<string, string>>();
                data.Add(new KeyValuePair<string, string>("kind", isSelfText ? "self" : "link"));
                data.Add(new KeyValuePair<string, string>("sr", subredditDisplayName));
                data.Add(new KeyValuePair<string, string>("sendreplies", sendRepliesToInbox ? "true" : "false"));
                if (isSelfText)
                {
                    data.Add(new KeyValuePair<string, string>("text", urlOrText));
                }
                else
                {
                    data.Add(new KeyValuePair<string, string>("url", urlOrText));
                }
                data.Add(new KeyValuePair<string, string>("title", title));

                // Make the call
                string jsonResponse = await NetworkManager.Instance.MakeRedditPostRequestAsString("/api/submit/", data);

                // Try to see if we can find the word redirect and if we can find the subreddit url
                string responseLower = jsonResponse.ToLower();
                if (responseLower.Contains("redirect") && responseLower.Contains($"://www.reddit.com/r/{subredditDisplayName}/comments/"))
                {
                    // Success, try to parse out the new post link
                    int startOfLink = responseLower.IndexOf($"://www.reddit.com/r/{subredditDisplayName}/comments/");
                    if(startOfLink == -1)
                    {
                        return new SubmitNewPostResponse() { Success = false };
                    }

                    int endofLink = responseLower.IndexOf('"', startOfLink);
                    if (endofLink == -1)
                    {
                        return new SubmitNewPostResponse() { Success = false };
                    }

                    // Try to get the link
                    string link = "https" + jsonResponse.Substring(startOfLink, endofLink - startOfLink);

                    // Return
                    return new SubmitNewPostResponse() { Success = true, NewPostLink = link};
                }
                else
                {
                    // We have a reddit error. Try to figure out what it is.
                    for(int i = 0; i < Enum.GetNames(typeof(SubmitNewPostErrors)).Length; i++)
                    {
                        string enumName = Enum.GetName(typeof(SubmitNewPostErrors), i).ToLower(); ;
                        if (responseLower.Contains(enumName))
                        {
                            MessageManager.Instance.DebugDia("failed to submit post; error: "+ enumName);
                            return new SubmitNewPostResponse() { Success = false, RedditError = (SubmitNewPostErrors)i};
                        }
                    }

                    MessageManager.Instance.DebugDia("failed to submit post; unknown reddit error");
                    return new SubmitNewPostResponse() { Success = false, RedditError = SubmitNewPostErrors.UNKNOWN };
                }
            }
            catch (Exception e)
            {
                MessageManager.Instance.DebugDia("failed to submit post", e);
                return new SubmitNewPostResponse() { Success = false };
            }
        }

        /// <summary>
        /// Uploads a image file to imgur.
        /// </summary>
        /// <param name="baconMan"></param>
        /// <param name="image"></param>
        /// <returns></returns>
        public static string UploadImageToImgur(BaconManager baconMan, FileRandomAccessStream imageStream)
        {
            //try
            //{
            //    // Make the data
            //    List<KeyValuePair<string, string>> data = new List<KeyValuePair<string, string>>();
            //    data.Add(new KeyValuePair<string, string>("key", "1a507266cc9ac194b56e2700a67185e4"));
            //    data.Add(new KeyValuePair<string, string>("title", "1a507266cc9ac194b56e2700a67185e4"));

            //    // Read the image from the stream and base64 encode it.
            //    Stream str = imageStream.AsStream();
            //    byte[] imageData = new byte[str.Length];
            //    await str.ReadAsync(imageData, 0, (int)str.Length);
            //    data.Add(new KeyValuePair<string, string>("image", WebUtility.UrlEncode(Convert.ToBase64String(imageData))));
            //    string repsonse = await NetworkManager.Instance.MakePostRequest("https://api.imgur.com/2/upload.json", data);
            //}
            //catch (Exception e)
            //{
            //    baconMan.TelemetryMan.ReportUnexpectedEvent("MisHelper", "failed to submit post", e);
            //    MessageManager.Instance.DebugDia("failed to submit post", e);
            //    return new SubmitNewPostResponse() { Success = false };
            //}
            throw new NotImplementedException("This function isn't complete!");
        }


        /// <summary>
        /// Attempts to parse out a reddit object from a generic reddit response
        /// json containing a data object. We can't do a generic DataObject here because
        /// the function handles data blocks that are in various depths in the json.
        /// </summary>
        /// <param name="originalJson"></param>
        /// <returns></returns>
        public static T ParseOutRedditDataElement<T>(BaconManager baconMan, string originalJson)
        {
            // TODO make this async. If I try to Task.Run(()=> the parse the task returns but the
            // await never resumes... idk why.
            // Try to parse out the data object
            int dataPos = originalJson.IndexOf("\"data\":");
            if (dataPos == -1)
            {
                return default(T);
            }
            int dataStartPos = originalJson.IndexOf('{', dataPos + 7);
            if (dataStartPos == -1)
            {
                return default(T);
            }
            // There can be nested { } in the data we want
            // To do this optimally, we will just look for the close manually.
            int dataEndPos = dataStartPos + 1;
            int depth = 0;
            bool isInText = false;
            while (dataEndPos < originalJson.Length)
            {
                // If we find a " make sure we ignore everything.
                if (originalJson[dataEndPos] == '"')
                {
                    if (isInText)
                    {
                        // If we are in a text block look if it is an escape.
                        // If it isn't an escape, end the text block.
                        if (originalJson[dataEndPos - 1] != '\\')
                        {
                            isInText = false;
                        }
                    }
                    else
                    {
                        // We entered text.
                        isInText = true;
                    }
                }

                // If not in a text block, look for {}
                if (!isInText)
                {
                    // If we find an open +1 to depth
                    if (originalJson[dataEndPos] == '{')
                    {
                        depth++;
                    }
                    // If we find and end..
                    else if (originalJson[dataEndPos] == '}')
                    {
                        // If we have no depth we are done.
                        if (depth == 0)
                        {
                            break;
                        }
                        // Otherwise take one off.
                        else
                        {
                            depth--;
                        }
                    }
                }

                dataEndPos++;
            }

            // Make sure we didn't fail.
            if (depth != 0)
            {
                return default(T);
            }

            // Move past the last }
            dataEndPos++;

            string dataBlock = originalJson.Substring(dataStartPos, (dataEndPos - dataStartPos));
            return JsonConvert.DeserializeObject<T>(dataBlock);
        }

        /// <summary>
        /// Attempts to find some reddit content in a link. A subreddit, post or comments.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static RedditContentContainer TryToFindRedditContentInLink(string url)
        {
            string urlLower = url.ToLower();
            RedditContentContainer containter = null;

            // Try to find /r/ or r/ links
            if (urlLower.StartsWith("/r/") || urlLower.StartsWith("r/"))
            {
                // Get the display name
                int subStart = urlLower.IndexOf("r/");
                subStart += 2;

                // Try to find the next / after the subreddit if it exists
                int subEnd = urlLower.IndexOf("/", subStart);
                if(subEnd == -1)
                {
                    subEnd = urlLower.Length;
                }

                // Get the name.
                string displayName = urlLower.Substring(subStart, subEnd - subStart).Trim();

                // Make sure we don't have trailing arguments other than a /, if we do we should handle this as we content.
                string trimedLowerUrl = urlLower.TrimEnd();
                if (trimedLowerUrl.Length - subEnd > 1)
                {
                    // Make a web link for this
                    containter = new RedditContentContainer()
                    {
                        Type = RedditContentType.Website,
                        Website = $"https://reddit.com/{url}"
                    };
                }
                else
                {
                    // We are good, make the subreddit link for this.
                    containter = new RedditContentContainer()
                    {
                        Type = RedditContentType.Subreddit,
                        Subreddit = displayName
                    };
                }
            }
            // Try to find /u/ or u/ links
            if (urlLower.StartsWith("/u/") || urlLower.StartsWith("u/"))
            {
                // Get the display name
                int userStart = urlLower.IndexOf("u/");
                userStart += 2;

                // Try to find the next / after the subreddit if it exists
                int subEnd = urlLower.IndexOf("/", userStart);
                if (subEnd == -1)
                {
                    subEnd = urlLower.Length;
                }

                // Get the name.
                string displayName = urlLower.Substring(userStart, subEnd - userStart).Trim();

                // Make sure we don't have trailing arguments other than a /, if we do we should handle this as we content.
                string trimedLowerUrl = urlLower.TrimEnd();
                if (trimedLowerUrl.Length - subEnd > 1)
                {
                    // Make a web link for this
                    containter = new RedditContentContainer()
                    {
                        Type = RedditContentType.Website,
                        Website = $"https://reddit.com/{url}"
                    };
                }
                else
                {
                    // We are good, make the user link.
                    containter = new RedditContentContainer()
                    {
                        Type = RedditContentType.User,
                        User = displayName
                    };
                }
            }
            // Try to find any other reddit link
            else if(urlLower.Contains("reddit.com/"))
            {
                // Try to find the start of the subreddit
                int startSub = urlLower.IndexOf("r/");
                if(startSub != -1)
                {
                    startSub += 2;

                    // Try to find the end of the subreddit.
                    int endSub = FindNextUrlBreak(urlLower, startSub);

                    if (endSub > startSub)
                    {
                        // We found a subreddit!
                        containter = new RedditContentContainer();
                        containter.Subreddit = urlLower.Substring(startSub, endSub - startSub);
                        containter.Type = RedditContentType.Subreddit;

                        // Special case! If the text after the subreddit is submit or wiki don't do anything special
                        // If we return null we will just open the website.
                        if(urlLower.IndexOf("/submit") == endSub || urlLower.IndexOf("/wiki") == endSub || urlLower.IndexOf("/w/") == endSub)
                        {
                            containter = null;
                            urlLower = "";
                        }

                        // See if we have a post
                        int postStart = urlLower.IndexOf("comments/");
                        if(postStart != -1)
                        {
                            postStart += 9;

                            // Try to find the end
                            int postEnd = FindNextUrlBreak(urlLower, postStart);

                            if(postEnd > postStart)
                            {
                                // We found a post! Build on top of the subreddit
                                containter.Post = urlLower.Substring(postStart, postEnd - postStart);
                                containter.Type = RedditContentType.Post;

                                // Try to find a comment, for there to be a comment this should have a / after it.
                                if(urlLower.Length > postEnd && urlLower[postEnd] == '/')
                                {
                                    postEnd++;
                                    // Now try to find the / after the post title
                                    int commentStart = urlLower.IndexOf('/', postEnd);
                                    if(commentStart != -1)
                                    {
                                        commentStart++;

                                        // Try to find the end of the comment
                                        int commentEnd = FindNextUrlBreak(urlLower, commentStart);

                                        if(commentEnd > commentStart )
                                        {
                                            // We found a comment!
                                            containter.Comment = urlLower.Substring(commentStart, commentEnd - commentStart);
                                            containter.Type = RedditContentType.Comment;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return containter;
        }

        private static int FindNextUrlBreak(string url, int startingPos)
        {
            int nextBreak = startingPos;
            while (url.Length > nextBreak && (Char.IsLetterOrDigit(url[nextBreak]) || url[nextBreak] == '_'))
            {
                nextBreak++;
            }
            return nextBreak;
        }

        /// <summary>
        /// Gets the complementary color of the one given.
        /// </summary>
        /// <param name="source">The color to find the complement of.</param>
        /// <returns>The complement to the input color.</returns>
        public static Color GetComplementaryColor(Color source)
        {
            Color inputColor = source;
            // If RGB values are close to each other by a diff less than 10%, then if RGB values are lighter side,
            // decrease the blue by 50% (eventually it will increase in conversion below), if RBB values are on darker
            // side, decrease yellow by about 50% (it will increase in conversion)
            byte avgColorValue = (byte)((source.R + source.G + source.B) / 3);
            int diff_r = Math.Abs(source.R - avgColorValue);
            int diff_g = Math.Abs(source.G - avgColorValue);
            int diff_b = Math.Abs(source.B - avgColorValue);
            if (diff_r < 20 && diff_g < 20 && diff_b < 20) //The color is a shade of gray
            {
                if (avgColorValue < 123) //color is dark
                {
                    inputColor.B = 220;
                    inputColor.G = 230;
                    inputColor.R = 50;
                }
                else
                {
                    inputColor.R = 255;
                    inputColor.G = 255;
                    inputColor.B = 50;
                }
            }

            RGB rgb = new RGB { R = inputColor.R, G = inputColor.G, B = inputColor.B };
            HSB hsb = ConvertToHSB(rgb);
            hsb.H = hsb.H < 180 ? hsb.H + 180 : hsb.H - 180;
            //hsb.B = isColorDark ? 240 : 50; //Added to create dark on light, and light on dark
            rgb = ConvertToRGB(hsb);
            return new Color { A = 255, R = (byte)rgb.R, G = (byte)rgb.G, B = (byte)rgb.B };
        }

        internal static RGB ConvertToRGB(HSB hsb)
        {
            // By: <a href="http://blogs.msdn.com/b/codefx/archive/2012/02/09/create-a-color-picker-for-windows-phone.aspx" title="MSDN" target="_blank">Yi-Lun Luo</a>
            double chroma = hsb.S * hsb.B;
            double hue2 = hsb.H / 60;
            double x = chroma * (1 - Math.Abs(hue2 % 2 - 1));
            double r1 = 0d;
            double g1 = 0d;
            double b1 = 0d;
            if (hue2 >= 0 && hue2 < 1)
            {
                r1 = chroma;
                g1 = x;
            }
            else if (hue2 >= 1 && hue2 < 2)
            {
                r1 = x;
                g1 = chroma;
            }
            else if (hue2 >= 2 && hue2 < 3)
            {
                g1 = chroma;
                b1 = x;
            }
            else if (hue2 >= 3 && hue2 < 4)
            {
                g1 = x;
                b1 = chroma;
            }
            else if (hue2 >= 4 && hue2 < 5)
            {
                r1 = x;
                b1 = chroma;
            }
            else if (hue2 >= 5 && hue2 <= 6)
            {
                r1 = chroma;
                b1 = x;
            }
            double m = hsb.B - chroma;
            return new RGB()
            {
                R = r1 + m,
                G = g1 + m,
                B = b1 + m
            };
        }
        internal static HSB ConvertToHSB(RGB rgb)
        {
            // By: <a href="http://blogs.msdn.com/b/codefx/archive/2012/02/09/create-a-color-picker-for-windows-phone.aspx" title="MSDN" target="_blank">Yi-Lun Luo</a>
            double r = rgb.R;
            double g = rgb.G;
            double b = rgb.B;

            double max = Max(r, g, b);
            double min = Min(r, g, b);
            double chroma = max - min;
            double hue2 = 0d;
            if (chroma != 0)
            {
                if (max == r)
                {
                    hue2 = (g - b) / chroma;
                }
                else if (max == g)
                {
                    hue2 = (b - r) / chroma + 2;
                }
                else
                {
                    hue2 = (r - g) / chroma + 4;
                }
            }
            double hue = hue2 * 60;
            if (hue < 0)
            {
                hue += 360;
            }
            double brightness = max;
            double saturation = 0;
            if (chroma != 0)
            {
                saturation = chroma / brightness;
            }
            return new HSB()
            {
                H = hue,
                S = saturation,
                B = brightness
            };
        }
        private static double Max(double d1, double d2, double d3)
        {
            if (d1 > d2)
            {
                return Math.Max(d1, d3);
            }
            return Math.Max(d2, d3);
        }
        private static double Min(double d1, double d2, double d3)
        {
            if (d1 < d2)
            {
                return Math.Min(d1, d3);
            }
            return Math.Min(d2, d3);
        }

        internal struct RGB
        {
            internal double R;
            internal double G;
            internal double B;
        }

        internal struct HSB
        {
            internal double H;
            internal double S;
            internal double B;
        }
    }
}
