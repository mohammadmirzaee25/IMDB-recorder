using System;
using System.Collections.Generic;
using System.Linq;        // ADD THIS LINE
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;

namespace My_IMDB
{
    /// <summary>
    /// Manages all OMDB API interactions with automatic key rotation
    /// </summary>
    public class ApiManager
    {
        private static readonly string[] OMDB_API_KEYS = new string[]
        {
            "ADD Your API her from Omdb site"
        };

        private static int currentApiKeyIndex = 0;
        private static string CurrentApiKey => OMDB_API_KEYS[currentApiKeyIndex];
        private static readonly HttpClient client = new HttpClient();
        private Dictionary<string, List<MovieInfo>> searchCache = new Dictionary<string, List<MovieInfo>>();

        /// <summary>
        /// Searches for movies or TV series using the OMDB API
        /// </summary>
        public async Task<List<MovieInfo>> SearchAsync(string searchTerm, bool searchMovies)
        {
            int maxRetries = OMDB_API_KEYS.Length;
            int retryCount = 0;

            while (retryCount < maxRetries)
            {
                try
                {
                    string searchType = searchMovies ? "movie" : "series";
                    string cacheKey = $"{searchTerm.ToLower()}_{searchType}_{currentApiKeyIndex}";

                    // Check cache first
                    if (searchCache.ContainsKey(cacheKey))
                    {
                        return searchCache[cacheKey];
                    }

                    string encodedSearch = Uri.EscapeDataString(searchTerm);
                    string url = $"https://www.omdbapi.com/?apikey={CurrentApiKey}&s={encodedSearch}&type={searchType}";

                    string jsonResponse = await client.GetStringAsync(url);
                    JObject data = JObject.Parse(jsonResponse);

                    // Handle API errors
                    if (data["Response"]?.ToString() == "False")
                    {
                        string errorMsg = data["Error"]?.ToString();

                        if (errorMsg == "Request limit reached!" || errorMsg == "Daily limit exceeded!" || errorMsg == "Invalid API key!")
                        {
                            if (SwitchToNextApiKey())
                            {
                                retryCount++;
                                continue;
                            }
                            return null;
                        }
                        return null;
                    }

                    var results = await ProcessSearchResults(data, searchTerm, searchType);
                    searchCache[cacheKey] = results;
                    return results;
                }
                catch (Exception)
                {
                    return null;
                }
            }

            MessageBox.Show("All API keys have been exhausted. Please try again tomorrow.",
                "API Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return null;
        }

        /// <summary>
        /// Gets poster URL and additional details for a movie
        /// </summary>
        public async Task<string> GetPosterUrlAsync(string movieName)
        {
            int maxRetries = OMDB_API_KEYS.Length;
            int retryCount = 0;

            while (retryCount < maxRetries)
            {
                try
                {
                    string encodedTitle = Uri.EscapeDataString(movieName);
                    string url = $"https://www.omdbapi.com/?apikey={CurrentApiKey}&t={encodedTitle}";

                    string jsonResponse = await client.GetStringAsync(url);
                    JObject data = JObject.Parse(jsonResponse);

                    if (data["Response"]?.ToString() == "True")
                    {
                        string posterUrl = data["Poster"]?.ToString();
                        if (!string.IsNullOrEmpty(posterUrl) && posterUrl != "N/A")
                        {
                            return posterUrl;
                        }
                        return null;
                    }

                    string errorMsg = data["Error"]?.ToString();
                    if (errorMsg == "Request limit reached!" || errorMsg == "Invalid API key!")
                    {
                        if (SwitchToNextApiKey())
                        {
                            retryCount++;
                            continue;
                        }
                        return null;
                    }
                    return null;
                }
                catch
                {
                    return null;
                }
            }
            return null;
        }

        /// <summary>
        /// Processes search results and fetches detailed information
        /// </summary>
        private async Task<List<MovieInfo>> ProcessSearchResults(JObject data, string searchTerm, string searchType)
        {
            var searchResults = data["Search"] as JArray;
            if (searchResults == null || searchResults.Count == 0) return new List<MovieInfo>();

            int maxResults = Math.Min(searchResults.Count, 10);
            var allResults = new List<MovieInfo>();

            for (int i = 0; i < maxResults; i++)
            {
                var result = searchResults[i];
                string imdbID = result["imdbID"]?.ToString();
                string title = result["Title"]?.ToString() ?? "";
                string year = result["Year"]?.ToString() ?? "";

                if (!string.IsNullOrEmpty(imdbID))
                {
                    string detailUrl = $"https://www.omdbapi.com/?apikey={CurrentApiKey}&i={imdbID}";
                    string detailResponse = await client.GetStringAsync(detailUrl);
                    JObject detailData = JObject.Parse(detailResponse);

                    string imdbScore = detailData["imdbRating"]?.ToString() ?? "N/A";

                    if (imdbScore != "N/A")
                    {
                        allResults.Add(new MovieInfo
                        {
                            Name = title,
                            Year = year,
                            Genre = detailData["Genre"]?.ToString() ?? "N/A",
                            Score = imdbScore,
                            MyRating = "Not rated",
                            SearchRelevance = CalculateRelevance(title, searchTerm),
                            Type = searchType,
                            Summary = detailData["Plot"]?.ToString() ?? "No summary available"
                        });
                    }
                    await Task.Delay(50);
                }
            }

            return allResults.OrderByDescending(m => m.SearchRelevance).ThenBy(m => m.Name).ToList();
        }

        /// <summary>
        /// Calculates relevance score for search results
        /// </summary>
        private int CalculateRelevance(string title, string searchTerm)
        {
            title = title.ToLower();
            searchTerm = searchTerm.ToLower();

            int relevance = 0;
            if (title == searchTerm) relevance += 100;
            if (title.StartsWith(searchTerm)) relevance += 50;
            if (title.Split(' ').Contains(searchTerm)) relevance += 30;
            if (title.Contains(searchTerm)) relevance += 10;

            return relevance;
        }

        /// <summary>
        /// Switches to the next available API key
        /// </summary>
        private bool SwitchToNextApiKey()
        {
            if (currentApiKeyIndex < OMDB_API_KEYS.Length - 1)
            {
                currentApiKeyIndex++;
                MessageBox.Show($"Switching to backup API key {currentApiKeyIndex + 1} of {OMDB_API_KEYS.Length}.",
                    "API Key Rotation", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Clears the search cache
        /// </summary>
        public void ClearCache()
        {
            searchCache.Clear();
        }
    }
}
