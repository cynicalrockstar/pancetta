﻿using Pancetta.DataObjects;
using Pancetta.Helpers;
using System.Collections.Generic;
using System.Net;

namespace Pancetta.Collectors
{
    public class SearchSubredditCollector : Collector<Subreddit>
    {
        public SearchSubredditCollector(string searchTerm) :
            base("SearchSubredditCollector")
        {
            // Encode the query
            searchTerm = WebUtility.UrlEncode(searchTerm);

            // Set up the list helper
            InitListHelper("/search.json", false, false, $"q=" + searchTerm + "&restrict_sr=&sort=relevance&t=all&type=sr&count=50");
        }

        /// <summary>
        /// Fired when the subreddits should be formatted.
        /// </summary>
        /// <param name="subreddits"></param>
        protected override void ApplyCommonFormatting(ref List<Subreddit> subreddits)
        {
            foreach (Subreddit subreddit in subreddits)
            {
                // Do some simple formatting
                subreddit.PublicDescription =subreddit.PublicDescription.Trim();

                // Do a quick and dirty replace for double new lines. This doesn't work for triple or tabs.
                subreddit.PublicDescription = subreddit.PublicDescription.Replace("\n\n", "\n");
            }
        }

        protected override List<Subreddit> ParseElementList(List<Element<Subreddit>> elements)
        {
            // Converts the elements into a list.
            List<Subreddit> subreddits = new List<Subreddit>();
            foreach (Element<Subreddit> element in elements)
            {
                subreddits.Add(element.Data);
            }
            return subreddits;
        }
    }
}
