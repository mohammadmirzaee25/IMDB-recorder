using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;
using System.Drawing;
using System.IO;
using System.Net.Http;

namespace My_IMDB
{
    public partial class Form1 : Form
    {
        // Replace the single API key with a list of keys
        private static readonly string[] OMDB_API_KEYS = new string[]
        {
    "a9e37134",  // Your original key
    "406ab9a4",  // Your new key 1
    "dd8596be",  // Your new key 2
    "ccf8fcc7"   // Your new key 3
        };
        private bool SwitchToNextApiKey()
        {
            if (currentApiKeyIndex < OMDB_API_KEYS.Length - 1)
            {
                currentApiKeyIndex++;
                MessageBox.Show($"Switching to backup API key {currentApiKeyIndex + 1} of {OMDB_API_KEYS.Length}.\n\nYour search will be retried automatically.",
                    "API Key Rotation", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return true;
            }
            else
            {
                MessageBox.Show("All API keys have reached their daily limits!\n\nPlease wait 24 hours or get new keys.",
                    "All Keys Exhausted", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private static int currentApiKeyIndex = 0;
        private static string OMDB_API_KEY => OMDB_API_KEYS[currentApiKeyIndex];
        private static readonly HttpClient client = new HttpClient();

        // Add these flags
        private bool isShowingMyMovies = false;
        private bool isSearchMode = true;
    
        private bool isSearchingMovies = true; // true = movies, false = series
        public Form1()
        {
            InitializeComponent();

            // Make sure DataGridView starts empty
            dataGridView1.DataSource = null;

            // Fix selection behavior
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView1.MultiSelect = false;

            // Make sure clicking doesn't unselect
            dataGridView1.CellMouseDown += DataGridView1_CellMouseDown;

            // Allow selecting by clicking any cell
            dataGridView1.CellClick += DataGridView1_CellClick;

            // Allow Enter key to search
            txtsearch.KeyPress += Txtsearch_KeyPress;

            this.FormClosing += (s, e) => ClearSearchCache();

        }

        private void rbmovies_CheckedChanged(object sender, EventArgs e)
        {
            if (rbmovies.Checked)
            {
                // Only change mode if the state actually changed
                if (isSearchingMovies != true)
                {
                    isSearchingMovies = true;

                    // If we're showing my movies, filter the database view
                    if (isShowingMyMovies)
                    {
                        FilterMyMoviesByType("movie");
                    }
                    else
                    {
                        // We're in search mode
                        MessageBox.Show("Search mode: MOVIES only", "Mode Changed",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);

                        // Clear current results
                        dataGridView1.DataSource = null;
                        pictureBox1.Image = null;
                        txtsearch.Clear();
                        txtsearch.Focus();

                        // Reset any view state
                        if (!isShowingMyMovies)
                        {
                            addbtn.Text = "Add";
                            addbtn.Enabled = true;
                        }
                    }
                }
            }
        }

        private void rbseiries_CheckedChanged(object sender, EventArgs e)
        {
            if (rbseries.Checked)
            {
                // Only change mode if the state actually changed
                if (isSearchingMovies != false)
                {
                    isSearchingMovies = false;

                    // If we're showing my movies, filter the database view
                    if (isShowingMyMovies)
                    {
                        FilterMyMoviesByType("series");
                    }
                    else
                    {
                        // We're in search mode
                        MessageBox.Show("Search mode: TV SERIES only", "Mode Changed",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);

                        // Clear current results
                        dataGridView1.DataSource = null;
                        pictureBox1.Image = null;
                        txtsearch.Clear();
                        txtsearch.Focus();

                        // Reset any view state
                        if (!isShowingMyMovies)
                        {
                            addbtn.Text = "Add";
                            addbtn.Enabled = true;
                        }
                    }
                }
            }
        }

        private void DataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            // Show summary when selection changes (arrow keys, etc.)
            ShowSummaryForSelectedMovie();
        }

        private void FilterMyMoviesByType(string type)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(GetConnectionString()))
                {
                    conn.Open();

                    // Check if table exists
                    string checkTable = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'IMDB'";
                    SqlCommand checkCmd = new SqlCommand(checkTable, conn);
                    int tableExists = (int)checkCmd.ExecuteScalar();

                    if (tableExists == 0)
                    {
                        MessageBox.Show("IMDB table not found in database!", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    // Get filtered movies/series from IMDB table based on type
                    string query = "SELECT name, year, imdbrating, myrating, genre, type, summary FROM IMDB WHERE type = @type ORDER BY name";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@type", type);

                    SqlDataReader reader = cmd.ExecuteReader();

                    var myMovies = new List<MovieInfo>();

                    while (reader.Read())
                    {
                        string typeValue = reader["type"]?.ToString();
                        if (string.IsNullOrEmpty(typeValue)) typeValue = "movie";

                        myMovies.Add(new MovieInfo
                        {
                            Name = reader["name"].ToString(),
                            Year = reader["year"].ToString(),
                            Genre = reader["genre"].ToString(),
                            Score = reader["imdbrating"].ToString(),
                            MyRating = reader["myrating"].ToString(),
                            Type = typeValue,
                            Summary = reader["summary"]?.ToString() ?? "No summary available",  // ADD THIS
                            SearchRelevance = 0
                        });
                    }

                    reader.Close();

                    if (myMovies.Count > 0)
                    {
                        dataGridView1.DataSource = myMovies;
                        ConfigureMyMoviesGridView();

                        // Ensure we're in the correct mode
                        isShowingMyMovies = true;
                        addbtn.Text = "Edit Rating";
                        addbtn.Enabled = true;

                        string displayType = type == "movie" ? "Movies" : "TV Series";
                        MessageBox.Show($"Found {myMovies.Count} {displayType} in your collection!",
                            "Filtered View", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        string displayType = type == "movie" ? "movies" : "TV series";
                        DialogResult result = MessageBox.Show($"No {displayType} found in your database.\n\nWould you like to see all items?",
                            "No Results", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                        if (result == DialogResult.Yes)
                        {
                            // Show all items
                            ShowMyMoviesFromDatabase();
                        }
                        else
                        {
                            // Keep the filtered view (empty)
                            dataGridView1.DataSource = null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error filtering database: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Add this new event to prevent unselecting
        private void DataGridView1_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                dataGridView1.ClearSelection();
                dataGridView1.Rows[e.RowIndex].Selected = true;

                if (dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex] != null)
                {
                    dataGridView1.CurrentCell = dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex];
                }
            }
        }

        private string GetConnectionString()
        {
            string databasePath;

            // Check if running in Visual Studio (debug mode)
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // Development mode - use the project folder (not bin\Debug)
                string projectPath = Path.GetFullPath(Path.Combine(Application.StartupPath, @"..\.."));
                databasePath = Path.Combine(projectPath, "Database1.mdf");

                // If database doesn't exist in project folder, check bin\Debug
                if (!File.Exists(databasePath))
                {
                    databasePath = Path.Combine(Application.StartupPath, "Database1.mdf");
                }
            }
            else
            {
                // Production mode - same folder as EXE
                databasePath = Path.Combine(Application.StartupPath, "Database1.mdf");
            }

            // Show error if database not found
            if (!File.Exists(databasePath))
            {
                MessageBox.Show($"Database file not found at:\n{databasePath}\n\nPlease make sure Database1.mdf exists.",
                    "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }

            return $@"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename={databasePath};Integrated Security=True";
        }

        private void btnmymovies_Click(object sender, EventArgs e)
        {
            // Show all movies from your database
            ShowAllSavedMovies();
        }

        private void ShowAllSavedMovies()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(GetConnectionString()))
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand("SELECT name, year, imdbrating, myrating, genre FROM IMDB", conn);
                    SqlDataReader reader = cmd.ExecuteReader();

                    var movies = new List<MovieInfo>();

                    while (reader.Read())
                    {
                        movies.Add(new MovieInfo
                        {
                            Name = reader["name"].ToString(),
                            Year = reader["year"].ToString(),
                            Genre = reader["genre"].ToString(),
                            Score = reader["imdbrating"].ToString(),
                            // Create a property for MyRating if needed
                        });
                    }

                    if (movies.Count > 0)
                    {
                        dataGridView1.DataSource = movies;
                        ConfigureDataGridView();
                        MessageBox.Show($"Found {movies.Count} movie(s) in your database!",
                            "My Movies", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("No movies found in your database.",
                            "My Movies", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        dataGridView1.DataSource = null;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading saved movies: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void btnsearch_Click(object sender, EventArgs e)
        {
            // Check if search box is empty
            if (string.IsNullOrWhiteSpace(txtsearch.Text))
            {
                MessageBox.Show("Please enter a movie title to search.", "Information",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (isShowingMyMovies)
            {
                // We're showing my movies - search within the database
                SearchInMyMovies(txtsearch.Text.Trim());
            }
            else
            {
                // We're in online search mode - use API
                await SearchMoviesAsync(txtsearch.Text.Trim());
            }
        }

        private void SearchInMyMovies(string searchTerm)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(GetConnectionString()))
                {
                    conn.Open();

                    // Split search term into words for better matching
                    string[] searchWords = searchTerm.ToLower().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    // Build a more flexible search query
                    string query = @"SELECT name, year, imdbrating, myrating, genre, type 
                   FROM IMDB 
                   WHERE LOWER(name) LIKE @search1";

                    // Add additional conditions for each word
                    for (int i = 0; i < searchWords.Length; i++)
                    {
                        query += $" OR LOWER(name) LIKE @search{i + 2}";
                    }

                    query += " ORDER BY name";

                    SqlCommand cmd = new SqlCommand(query, conn);

                    // Add parameters for each search pattern
                    cmd.Parameters.AddWithValue("@search1", "%" + searchTerm.ToLower() + "%");

                    for (int i = 0; i < searchWords.Length; i++)
                    {
                        cmd.Parameters.AddWithValue($"@search{i + 2}", "%" + searchWords[i] + "%");
                    }

                    SqlDataReader reader = cmd.ExecuteReader();

                    var searchResults = new List<MovieInfo>();

                    while (reader.Read())
                    {
                        string typeValue = reader["type"]?.ToString();
                        if (string.IsNullOrEmpty(typeValue)) typeValue = "movie";

                        searchResults.Add(new MovieInfo
                        {
                            Name = reader["name"].ToString(),
                            Year = reader["year"].ToString(),
                            Genre = reader["genre"].ToString(),
                            Score = reader["imdbrating"].ToString(),
                            MyRating = reader["myrating"].ToString(),
                            Type = typeValue,
                            SearchRelevance = 0
                        });
                    }

                    reader.Close();

                    if (searchResults.Count > 0)
                    {
                        // Remove duplicates (same name)
                        var uniqueResults = searchResults
                            .GroupBy(m => m.Name)
                            .Select(g => g.First())
                            .ToList();

                        dataGridView1.DataSource = uniqueResults;
                        ConfigureMyMoviesGridView();
                        MessageBox.Show($"Found {uniqueResults.Count} item(s) matching '{searchTerm}' in your collection!",
                            "Search Results", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show($"No items found matching '{searchTerm}' in your database.\n\nTip: Try searching with partial words!",
                            "Not Found", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        ShowMyMoviesFromDatabase();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error searching database: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SwitchToSearchMode()
        {
            isShowingMyMovies = false;
            isSearchMode = true;
            addbtn.Enabled = true;
            btnmymovies.BackColor = System.Drawing.Color.Black;
            btnmymovies.Text = "My Movies And Series";
            dataGridView1.DataSource = null;
        }

        private void Txtsearch_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Press Enter to trigger search
            if (e.KeyChar == (char)Keys.Enter)
            {
                btnsearch_Click(sender, e);
                e.Handled = true;
            }
        }

        private void DataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            // When any cell is clicked, select the entire row
            if (e.RowIndex >= 0)
            {
                // Clear any existing selection
                dataGridView1.ClearSelection();

                // Select the entire row
                dataGridView1.Rows[e.RowIndex].Selected = true;

                // Also set the current cell to prevent unselecting
                dataGridView1.CurrentCell = dataGridView1.Rows[e.RowIndex].Cells[0];

                ShowSummaryForSelectedMovie();

            }
        }

        // Add this dictionary at the top of your Form1 class to cache search results
        private Dictionary<string, List<MovieInfo>> searchCache = new Dictionary<string, List<MovieInfo>>();

        private async Task SearchMoviesAsync(string searchTerm)
        {
            // Track retry attempts
            int maxRetries = OMDB_API_KEYS.Length;
            int retryCount = 0;
            bool success = false;

            while (!success && retryCount < maxRetries)
            {
                try
                {
                    this.Cursor = Cursors.WaitCursor;
                    btnsearch.Enabled = false;
                    dataGridView1.DataSource = null;

                    string searchType = isSearchingMovies ? "movie" : "series";
                    string resultType = isSearchingMovies ? "movies" : "TV series";
                    string cacheKey = $"{searchTerm.ToLower()}_{searchType}_{currentApiKeyIndex}"; // Include key index in cache

                    // Check cache first (avoid unnecessary API calls for same search)
                    if (searchCache.ContainsKey(cacheKey))
                    {
                        var cachedResults = searchCache[cacheKey];
                        if (cachedResults.Count > 0)
                        {
                            dataGridView1.DataSource = cachedResults;
                            ConfigureDataGridView();
                            MessageBox.Show($"Found {cachedResults.Count} {resultType} matching '{searchTerm}' (from cache).\n\nUsing API Key: {currentApiKeyIndex + 1}",
                                "Search Results", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }
                    }

                    // Search with exact type and limit
                    string encodedSearch = Uri.EscapeDataString(searchTerm);
                    string url = $"https://www.omdbapi.com/?apikey={OMDB_API_KEY}&s={encodedSearch}&type={searchType}";

                    string jsonResponse = await client.GetStringAsync(url);
                    JObject data = JObject.Parse(jsonResponse);

                    // Handle API errors
                    if (data["Response"]?.ToString() == "False")
                    {
                        string errorMsg = data["Error"]?.ToString();

                        if (errorMsg == "Request limit reached!" || errorMsg == "Daily limit exceeded!")
                        {
                            // Switch to next API key
                            if (SwitchToNextApiKey())
                            {
                                retryCount++;
                                continue; // Retry with new key
                            }
                            else
                            {
                                return; // No more keys available
                            }
                        }
                        else if (errorMsg == "Invalid API key!")
                        {
                            // Switch to next API key
                            if (SwitchToNextApiKey())
                            {
                                retryCount++;
                                continue; // Retry with new key
                            }
                            else
                            {
                                return;
                            }
                        }
                        else if (errorMsg == "Movie not found!")
                        {
                            MessageBox.Show($"No {searchType}s found matching '{searchTerm}'. Try a different search term.",
                                "Not Found", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }
                        else
                        {
                            MessageBox.Show($"API Error: {errorMsg}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }

                    var searchResults = data["Search"] as JArray;

                    if (searchResults == null || searchResults.Count == 0)
                    {
                        MessageBox.Show($"No {searchType}s found matching '{searchTerm}'. Try different keywords.",
                            "Not Found", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    // Limit to 10 results
                    int maxResults = Math.Min(searchResults.Count, 10);
                    var allResults = new List<MovieInfo>();

                    // Show progress
                    var progressForm = new Form
                    {
                        Text = "Loading Movie Details",
                        Size = new System.Drawing.Size(350, 100),
                        FormBorderStyle = FormBorderStyle.FixedDialog,
                        StartPosition = FormStartPosition.CenterParent,
                        ControlBox = false
                    };

                    var progressBar = new ProgressBar
                    {
                        Minimum = 0,
                        Maximum = maxResults,
                        Location = new System.Drawing.Point(12, 30),
                        Size = new System.Drawing.Size(310, 23)
                    };

                    var progressLabel = new Label
                    {
                        Text = "Loading movie details...",
                        Location = new System.Drawing.Point(12, 10),
                        Size = new System.Drawing.Size(310, 20)
                    };

                    progressForm.Controls.Add(progressBar);
                    progressForm.Controls.Add(progressLabel);

                    progressForm.Show(this);
                    Application.DoEvents();

                    for (int i = 0; i < maxResults; i++)
                    {
                        var result = searchResults[i];
                        string imdbID = result["imdbID"]?.ToString();
                        string title = result["Title"]?.ToString() ?? "";
                        string year = result["Year"]?.ToString() ?? "";

                        if (!string.IsNullOrEmpty(imdbID))
                        {
                            progressLabel.Text = $"Loading: {title} ({i + 1}/{maxResults})";
                            progressBar.Value = i + 1;
                            Application.DoEvents();

                            string detailUrl = $"https://www.omdbapi.com/?apikey={OMDB_API_KEY}&i={imdbID}";
                            string detailResponse = await client.GetStringAsync(detailUrl);
                            JObject detailData = JObject.Parse(detailResponse);

                            string imdbScore = detailData["imdbRating"]?.ToString() ?? "N/A";
                            allResults.Add(new MovieInfo
                            {
                                Name = title,
                                Year = year,
                                Genre = detailData["Genre"]?.ToString() ?? "N/A",
                                Score = imdbScore,
                                MyRating = "Not rated",
                                SearchRelevance = CalculateRelevance(title, searchTerm),
                                Type = searchType,
                                Summary = detailData["Plot"]?.ToString() ?? "No summary available"  // ADD THIS LINE
                            });

                            // Small delay to be respectful to the API
                            await Task.Delay(50);
                        }
                    }

                    progressForm.Close();
                    progressForm.Dispose();

                    // Sort by relevance (best matches first)
                    var sortedResults = allResults
                        .OrderByDescending(m => m.SearchRelevance)
                        .ThenBy(m => m.Name)
                        .ToList();

                    // Cache the results for this search
                    searchCache[cacheKey] = sortedResults;

                    // Display results
                    dataGridView1.DataSource = sortedResults;
                    ConfigureDataGridView();

                    MessageBox.Show($"Found {sortedResults.Count} {resultType} matching '{searchTerm}'.\n\nResults are sorted by relevance.\n\nUsing API Key: {currentApiKeyIndex + 1} of {OMDB_API_KEYS.Length}",
                        "Search Results", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    success = true; // Mark as successful
                }
                catch (HttpRequestException ex)
                {
                    MessageBox.Show($"Network error: {ex.Message}\n\nPlease check your internet connection.",
                        "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    dataGridView1.DataSource = null;
                    return; // Don't retry on network errors
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    dataGridView1.DataSource = null;
                    return;
                }
                finally
                {
                    this.Cursor = Cursors.Default;
                    btnsearch.Enabled = true;
                }
            }

            if (!success)
            {
                MessageBox.Show("All API keys have been exhausted. Please try again tomorrow or get new keys.",
                    "Search Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void ClearSearchCache()
        {
            searchCache.Clear();
        }

        private int CalculateRelevance(string title, string searchTerm)
        {
            // Calculate how relevant the result is to the search
            title = title.ToLower();
            searchTerm = searchTerm.ToLower();

            int relevance = 0;

            // Exact match (highest relevance)
            if (title == searchTerm)
                relevance += 100;

            // Starts with search term
            if (title.StartsWith(searchTerm))
                relevance += 50;

            // Contains search term as whole word
            if (title.Split(' ').Contains(searchTerm))
                relevance += 30;

            // Contains search term
            if (title.Contains(searchTerm))
                relevance += 10;

            return relevance;
        }

        private void ConfigureDataGridView()
        {
            if (dataGridView1.Columns.Count > 0)
            {
                // Hide the SearchRelevance column (it's for internal use only)
                if (dataGridView1.Columns["SearchRelevance"] != null)
                {
                    dataGridView1.Columns["SearchRelevance"].Visible = false;
                }

                // ADD THIS - Hide the Summary column
                if (dataGridView1.Columns["Summary"] != null)
                {
                    dataGridView1.Columns["Summary"].Visible = false;
                }

                // Set friendly column headers
                dataGridView1.Columns["Name"].HeaderText = "Movie Name";
                dataGridView1.Columns["Year"].HeaderText = "Release Year";
                dataGridView1.Columns["Genre"].HeaderText = "Genre";
                dataGridView1.Columns["Score"].HeaderText = "IMDb Score";

                // Auto-size columns
                dataGridView1.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);

                // Make DataGridView read-only
                dataGridView1.ReadOnly = true;
            }
        }

        private async void addbtn_Click(object sender, EventArgs e)
        {
            if (isShowingMyMovies)
            {
                // EDIT MODE - Update rating for existing movie
                EditMovieRating();
            }
            else
            {
                // ADD MODE - Add new movie from search results
                AddNewMovie();
            }
        }

        private void EditMovieRating()
        {
            // Check if a movie is selected
            if (dataGridView1.SelectedRows.Count == 0 && dataGridView1.SelectedCells.Count == 0)
            {
                MessageBox.Show("Please select a movie first by clicking on any cell.", "No Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            MovieInfo selectedMovie = null;

            // Get selected movie from either selected row or selected cell
            if (dataGridView1.SelectedRows.Count > 0)
            {
                selectedMovie = (MovieInfo)dataGridView1.SelectedRows[0].DataBoundItem;
            }
            else if (dataGridView1.SelectedCells.Count > 0)
            {
                int rowIndex = dataGridView1.SelectedCells[0].RowIndex;
                selectedMovie = (MovieInfo)dataGridView1.Rows[rowIndex].DataBoundItem;
            }

            if (selectedMovie == null)
            {
                MessageBox.Show("Please select a valid movie.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Ask for new rating
            string newRating = AskForRating(selectedMovie.Name, selectedMovie.MyRating);

            if (string.IsNullOrEmpty(newRating))
            {
                return; // User cancelled
            }

            // Update rating in database
            UpdateMovieRating(selectedMovie.Name, newRating);
        }

        private void AddNewMovie()
        {
            // Check if a movie is selected
            if (dataGridView1.SelectedRows.Count == 0 && dataGridView1.SelectedCells.Count == 0)
            {
                MessageBox.Show("Please select a movie first by clicking on any cell.", "No Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            MovieInfo selectedMovie = null;

            // Get selected movie from either selected row or selected cell
            if (dataGridView1.SelectedRows.Count > 0)
            {
                selectedMovie = (MovieInfo)dataGridView1.SelectedRows[0].DataBoundItem;
            }
            else if (dataGridView1.SelectedCells.Count > 0)
            {
                int rowIndex = dataGridView1.SelectedCells[0].RowIndex;
                selectedMovie = (MovieInfo)dataGridView1.Rows[rowIndex].DataBoundItem;
            }

            if (selectedMovie == null)
            {
                MessageBox.Show("Please select a valid movie.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Ask for user rating
            string userRating = AskForRating(selectedMovie.Name, null);

            if (string.IsNullOrEmpty(userRating))
            {
                return; // User cancelled
            }

            // Add to database
            AddMovieToDatabase(selectedMovie, userRating);
        }

        private string AskForRating(string movieName, string currentRating = null)
        {
            // Create a simple input dialog
            Form ratingDialog = new Form();
            Label label = new Label();
            TextBox textBox = new TextBox();
            Button okButton = new Button();
            Button cancelButton = new Button();
            string rating = null;

            ratingDialog.Text = currentRating == null ? $"Rate: {movieName}" : $"Update Rating: {movieName}";
            ratingDialog.Size = new System.Drawing.Size(300, 150);
            ratingDialog.FormBorderStyle = FormBorderStyle.FixedDialog;
            ratingDialog.StartPosition = FormStartPosition.CenterParent;
            ratingDialog.MaximizeBox = false;
            ratingDialog.MinimizeBox = false;

            if (currentRating == null)
            {
                label.Text = "Enter your rating (1-10):";
            }
            else
            {
                label.Text = $"Current rating: {currentRating}/10\nEnter new rating (1-10):";
                label.Size = new System.Drawing.Size(260, 40);
                ratingDialog.Size = new System.Drawing.Size(300, 170);
            }
            label.Location = new System.Drawing.Point(12, 15);

            textBox.Location = new System.Drawing.Point(12, 65);
            textBox.Size = new System.Drawing.Size(260, 20);
            if (currentRating != null)
            {
                textBox.Text = currentRating;
            }

            okButton.Text = "OK";
            okButton.Location = new System.Drawing.Point(100, 95);
            okButton.Click += (sender, e) => {
                if (ValidateRating(textBox.Text))
                {
                    rating = textBox.Text;
                    ratingDialog.DialogResult = DialogResult.OK;
                    ratingDialog.Close();
                }
                else
                {
                    MessageBox.Show("Please enter a valid rating between 1 and 10.",
                        "Invalid Rating", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };

            cancelButton.Text = "Cancel";
            cancelButton.Location = new System.Drawing.Point(180, 95);
            cancelButton.Click += (sender, e) => {
                ratingDialog.DialogResult = DialogResult.Cancel;
                ratingDialog.Close();
            };

            ratingDialog.Controls.Add(label);
            ratingDialog.Controls.Add(textBox);
            ratingDialog.Controls.Add(okButton);
            ratingDialog.Controls.Add(cancelButton);

            ratingDialog.Shown += (s, e) => textBox.Focus();

            if (ratingDialog.ShowDialog() == DialogResult.OK)
            {
                return rating;
            }

            return null;
        }

        private void UpdateMovieRating(string movieName, string newRating)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(GetConnectionString()))
                {
                    conn.Open();

                    string query = "UPDATE IMDB SET myrating = @rating WHERE name = @name";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@rating", newRating);
                        cmd.Parameters.AddWithValue("@name", movieName);

                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show($"Rating updated for '{movieName}'!\n\nNew rating: {newRating}/10",
                                "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                            // Refresh the grid to show updated rating
                            ShowMyMoviesFromDatabase();
                        }
                        else
                        {
                            MessageBox.Show("Failed to update rating.",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating rating: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private bool ValidateRating(string rating)
        {
            if (double.TryParse(rating, out double value))
            {
                return value >= 1 && value <= 10;
            }
            return false;
        }

        private void AddMovieToDatabase(MovieInfo movie, string userRating)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(GetConnectionString()))
                {
                    conn.Open();

                    // First check if movie/series already exists in database
                    string checkQuery = "SELECT COUNT(*) FROM IMDB WHERE name = @name";
                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@name", movie.Name);
                        int exists = (int)checkCmd.ExecuteScalar();

                        if (exists > 0)
                        {
                            MessageBox.Show($"'{movie.Name}' already exists in your watched list!\n\nYou can edit your rating by clicking 'Edit Score' when viewing your collection.",
                                "Already Exists", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }
                    }

                    // FIX: Ensure type is properly set
                    string movieType = movie.Type;

                    // If Type is null or empty, determine it from the current search mode
                    if (string.IsNullOrEmpty(movieType))
                    {
                        movieType = isSearchingMovies ? "movie" : "series";
                    }

                    // If it's still null (shouldn't happen), default to "movie"
                    if (string.IsNullOrEmpty(movieType))
                    {
                        movieType = "movie";
                    }

                    // Get summary (default if null)
                    string summary = string.IsNullOrEmpty(movie.Summary) ? "No summary available" : movie.Summary;

                    // If not exists, add the movie/series (INCLUDING SUMMARY)
                    string query = @"INSERT INTO IMDB (name, year, imdbrating, myrating, genre, type, summary) 
                   VALUES (@name, @year, @imdbrating, @myrating, @genre, @type, @summary)";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@name", movie.Name);
                        cmd.Parameters.AddWithValue("@year", movie.Year);
                        cmd.Parameters.AddWithValue("@imdbrating", movie.Score);
                        cmd.Parameters.AddWithValue("@myrating", userRating);
                        cmd.Parameters.AddWithValue("@genre", movie.Genre);
                        cmd.Parameters.AddWithValue("@type", movieType);
                        cmd.Parameters.AddWithValue("@summary", summary);  // ADD THIS LINE

                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show($"'{movie.Name}' added to your database successfully!\n\nYour rating: {userRating}/10\nType: {movieType}",
                                "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            MessageBox.Show("Failed to add to database.",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Database Error: {ex.Message}\n\nMake sure your database exists and table 'IMDB' is created.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnmymovies_Click_1(object sender, EventArgs e)
        {
            // Toggle between showing my movies and search mode
            if (isShowingMyMovies)
            {
                // Currently showing my movies - switch to API search mode
                GoBackToSearchMode();
            }
            else
            {
                // Currently in search mode - switch to show my movies
                // Clear the DataGridView
                dataGridView1.DataSource = null;

                // Show my movies from database based on current radio button selection
                if (rbmovies.Checked)
                {
                    // Show only movies
                    FilterMyMoviesByType("movie");
                    // Don't change the radio button state
                }
                else if (rbseries.Checked)
                {
                    // Show only series
                    FilterMyMoviesByType("series");
                }
                else
                {
                    // If no radio button is checked, show all
                    ShowMyMoviesFromDatabase();
                }

                // Change mode flags
                isShowingMyMovies = true;
                isSearchMode = false;

                // Change button appearance
                btnmymovies.BackColor = System.Drawing.Color.Green;
                btnmymovies.Text = "Click to Search Online Titles";

                // Set add button text for edit mode
                addbtn.Text = "Edit Rating";
                addbtn.Enabled = true;
            }
        }

        private void ShowMyMoviesFromDatabase()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(GetConnectionString()))
                {
                    conn.Open();

                    // Check if table exists
                    string checkTable = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'IMDB'";
                    SqlCommand checkCmd = new SqlCommand(checkTable, conn);
                    int tableExists = (int)checkCmd.ExecuteScalar();

                    if (tableExists == 0)
                    {
                        MessageBox.Show("IMDB table not found in database!", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        GoBackToSearchMode();
                        return;
                    }

                    // Get ALL movies/series from IMDB table (no type filter)
                    SqlCommand cmd = new SqlCommand("SELECT name, year, imdbrating, myrating, genre, type, summary FROM IMDB ORDER BY name", conn);
                    SqlDataReader reader = cmd.ExecuteReader();

                    var myMovies = new List<MovieInfo>();

                    while (reader.Read())
                    {
                        string typeValue = reader["type"]?.ToString();
                        if (string.IsNullOrEmpty(typeValue)) typeValue = "movie";
                        // When adding to myMovies, include Summary
                        myMovies.Add(new MovieInfo
                        {
                            Name = reader["name"].ToString(),
                            Year = reader["year"].ToString(),
                            Genre = reader["genre"].ToString(),
                            Score = reader["imdbrating"].ToString(),
                            MyRating = reader["myrating"].ToString(),
                            Type = typeValue,
                            Summary = reader["summary"]?.ToString() ?? "No summary available",  // ADD THIS
                            SearchRelevance = 0
                        });
                    }

                    reader.Close();

                    if (myMovies.Count > 0)
                    {
                        dataGridView1.DataSource = myMovies;
                        ConfigureMyMoviesGridView();

                        // Change add button to edit button
                        addbtn.Text = "Edit Rating";
                        addbtn.Enabled = true;

                        // Count movies and series
                        int movieCount = myMovies.Count(m => m.Type == "movie");
                        int seriesCount = myMovies.Count(m => m.Type == "series");

                        MessageBox.Show($"Found {myMovies.Count} item(s) in your collection!\n\nMovies: {movieCount}\nTV Series: {seriesCount}",
                            "My Collection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("No items found in your database.\n\nClick 'Search' to find and add movies or series.",
                            "My Collection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        dataGridView1.DataSource = null;
                        GoBackToSearchMode();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading from database: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                dataGridView1.DataSource = null;
                GoBackToSearchMode();
            }
        }

        // Add this new method to go back to search mode
        private void GoBackToSearchMode()
        {
            isShowingMyMovies = false;
            isSearchMode = true;
            addbtn.Enabled = true;
            addbtn.Text = "Add";
            btnmymovies.BackColor = System.Drawing.Color.Black;
            btnmymovies.Text = "My Movies And Series";
            dataGridView1.DataSource = null;

            // Clear any filters and show appropriate message based on selected type
            if (rbmovies.Checked)
            {
                MessageBox.Show("Switched to ONLINE SEARCH mode - searching for MOVIES",
                    "Mode Changed", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else if (rbseries.Checked)
            {
                MessageBox.Show("Switched to ONLINE SEARCH mode - searching for TV SERIES",
                    "Mode Changed", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ConfigureMyMoviesGridView()
        {
            if (dataGridView1.Columns.Count > 0)
            {
                // Hide the SearchRelevance column
                if (dataGridView1.Columns["SearchRelevance"] != null)
                {
                    dataGridView1.Columns["SearchRelevance"].Visible = false;
                }

                // ADD THIS - Hide the Summary column (fixes your issue)
                if (dataGridView1.Columns["Summary"] != null)
                {
                    dataGridView1.Columns["Summary"].Visible = false;
                }

                // Set friendly column headers
                if (dataGridView1.Columns["Name"] != null)
                    dataGridView1.Columns["Name"].HeaderText = "Movie Name";
                if (dataGridView1.Columns["Year"] != null)
                    dataGridView1.Columns["Year"].HeaderText = "Release Year";
                if (dataGridView1.Columns["Genre"] != null)
                    dataGridView1.Columns["Genre"].HeaderText = "Genre";
                if (dataGridView1.Columns["Score"] != null)
                    dataGridView1.Columns["Score"].HeaderText = "IMDb Score";
                if (dataGridView1.Columns["MyRating"] != null)
                    dataGridView1.Columns["MyRating"].HeaderText = "My Rating";
                if (dataGridView1.Columns["Type"] != null)
                    dataGridView1.Columns["Type"].HeaderText = "Type";

                // Auto-size columns
                dataGridView1.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);

                // Make DataGridView read-only
                dataGridView1.ReadOnly = true;
            }
        }
        private async Task LoadMoviePoster(string movieName, string movieYear)
        {
            int maxRetries = OMDB_API_KEYS.Length;
            int retryCount = 0;
            bool success = false;

            while (!success && retryCount < maxRetries)
            {
                try
                {
                    string encodedTitle = Uri.EscapeDataString(movieName);
                    string url = $"https://www.omdbapi.com/?apikey={OMDB_API_KEY}&t={encodedTitle}&plot=short";

                    using (var httpClient = new HttpClient())
                    {
                        string jsonResponse = await httpClient.GetStringAsync(url);
                        JObject data = JObject.Parse(jsonResponse);

                        if (data["Response"]?.ToString() == "True")
                        {
                            string posterUrl = data["Poster"]?.ToString();

                            if (!string.IsNullOrEmpty(posterUrl) && posterUrl != "N/A")
                            {
                                using (var posterClient = new HttpClient())
                                {
                                    byte[] imageData = await posterClient.GetByteArrayAsync(posterUrl);
                                    using (var ms = new System.IO.MemoryStream(imageData))
                                    {
                                        Image posterImage = Image.FromStream(ms);
                                        pictureBox1.Image = posterImage;
                                        pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
                                        success = true;
                                    }
                                }
                            }
                            else
                            {
                                pictureBox1.Image = null;
                                MessageBox.Show($"No poster found for '{movieName}'.",
                                    "Poster Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                success = true; // Not an API error, just no poster
                            }
                        }
                        else
                        {
                            string errorMsg = data["Error"]?.ToString();
                            if (errorMsg == "Request limit reached!" || errorMsg == "Invalid API key!")
                            {
                                if (SwitchToNextApiKey())
                                {
                                    retryCount++;
                                    continue;
                                }
                                else
                                {
                                    pictureBox1.Image = null;
                                    MessageBox.Show($"No poster found for '{movieName}'.", "Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                    return;
                                }
                            }
                            else
                            {
                                pictureBox1.Image = null;
                                MessageBox.Show($"Movie '{movieName}' not found.", "Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                success = true;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading poster: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    pictureBox1.Image = null;
                    return;
                }
            }
        }

        private async void btnshowposter_Click(object sender, EventArgs e)
        {
            // Check if a movie is selected
            if (dataGridView1.SelectedRows.Count == 0 && dataGridView1.SelectedCells.Count == 0)
            {
                MessageBox.Show("Please select a movie first by clicking on any cell.", "No Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            MovieInfo selectedMovie = null;

            // Get selected movie from either selected row or selected cell
            if (dataGridView1.SelectedRows.Count > 0)
            {
                selectedMovie = (MovieInfo)dataGridView1.SelectedRows[0].DataBoundItem;
            }
            else if (dataGridView1.SelectedCells.Count > 0)
            {
                int rowIndex = dataGridView1.SelectedCells[0].RowIndex;
                selectedMovie = (MovieInfo)dataGridView1.Rows[rowIndex].DataBoundItem;
            }

            if (selectedMovie == null)
            {
                MessageBox.Show("Please select a valid movie.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Show loading indicator
            pictureBox1.Image = null;
            this.Cursor = Cursors.WaitCursor;
            btnshowposter.Enabled = false;
            btnshowposter.Text = "Loading Poster...";

            // Load the poster
            await LoadMoviePoster(selectedMovie.Name, selectedMovie.Year);

            // Reset button
            this.Cursor = Cursors.Default;
            btnshowposter.Enabled = true;
            btnshowposter.Text = "Show Poster";
        }
        private void btnsummary_Click(object sender, EventArgs e)
        {
            // Check if a movie is selected
            if (dataGridView1.SelectedRows.Count == 0 && dataGridView1.SelectedCells.Count == 0)
            {
                MessageBox.Show("Please select a movie or series first by clicking on any cell.", "No Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            MovieInfo selectedMovie = null;

            // Get selected movie from either selected row or selected cell
            if (dataGridView1.SelectedRows.Count > 0)
            {
                selectedMovie = (MovieInfo)dataGridView1.SelectedRows[0].DataBoundItem;
            }
            else if (dataGridView1.SelectedCells.Count > 0)
            {
                int rowIndex = dataGridView1.SelectedCells[0].RowIndex;
                selectedMovie = (MovieInfo)dataGridView1.Rows[rowIndex].DataBoundItem;
            }

            if (selectedMovie == null)
            {
                MessageBox.Show("Please select a valid movie or series.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Get the summary (from the selected movie object)
            string summary = selectedMovie.Summary ?? "No summary available for this title.";

            // Display the summary in pictureBox1
            DisplaySummaryInPictureBox(selectedMovie.Name, summary);
        }

        private void DisplaySummaryInPictureBox(string title, string summary)
        {
            try
            {
                // Create a bitmap to draw the text on
                Bitmap bmp = new Bitmap(pictureBox1.Width, pictureBox1.Height);

                using (Graphics g = Graphics.FromImage(bmp))
                {
                    // Clear with dark background
                    g.Clear(Color.FromArgb(30, 30, 30));

                    // Set up font and formatting
                    using (Font titleFont = new Font("Arial", 14, FontStyle.Bold))
                    using (Font summaryFont = new Font("Arial", 10, FontStyle.Regular))
                    {
                        // Calculate title position
                        SizeF titleSize = g.MeasureString(title, titleFont);
                        float titleX = (bmp.Width - titleSize.Width) / 2;
                        float titleY = 15;

                        // Draw a separator line
                        Pen linePen = new Pen(Color.Gold, 2);

                        // Draw title
                        using (SolidBrush titleBrush = new SolidBrush(Color.Gold))
                        {
                            g.DrawString(title, titleFont, titleBrush, titleX, titleY);
                        }

                        // Draw separator line
                        float lineY = titleY + titleSize.Height + 5;
                        g.DrawLine(linePen, 50, lineY, bmp.Width - 50, lineY);

                        // Draw summary with word wrap
                        RectangleF summaryRect = new RectangleF(15, lineY + 15, bmp.Width - 30, bmp.Height - (lineY + 25));
                        using (SolidBrush summaryBrush = new SolidBrush(Color.White))
                        {
                            g.DrawString(summary, summaryFont, summaryBrush, summaryRect);
                        }
                    }
                }

                // Display the image
                pictureBox1.Image = bmp;
                pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error displaying summary: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void ShowSummaryForSelectedMovie()
        {
            // Check if a movie is selected
            if (dataGridView1.SelectedRows.Count == 0 && dataGridView1.SelectedCells.Count == 0)
            {
                return; // No selection, do nothing
            }

            MovieInfo selectedMovie = null;

            // Get selected movie from either selected row or selected cell
            if (dataGridView1.SelectedRows.Count > 0)
            {
                selectedMovie = (MovieInfo)dataGridView1.SelectedRows[0].DataBoundItem;
            }
            else if (dataGridView1.SelectedCells.Count > 0)
            {
                int rowIndex = dataGridView1.SelectedCells[0].RowIndex;
                if (rowIndex >= 0 && rowIndex < dataGridView1.Rows.Count)
                {
                    selectedMovie = (MovieInfo)dataGridView1.Rows[rowIndex].DataBoundItem;
                }
            }

            if (selectedMovie != null)
            {
                // Get the summary
                string summary = selectedMovie.Summary ?? "No summary available for this title.";

                // Display the summary in pictureBox1
                DisplaySummaryInPictureBox(selectedMovie.Name, summary);
            }
        }

        private void dataGridView1_CurrentCellChanged(object sender, EventArgs e)
        {
            ShowSummaryForSelectedMovie();
        }
    }
    //http://www.omdbapi.com/?apikey=a9e37134&s=batman&type=movie
    // Movie information class
    // Movie information class
    // Movie information class
    public class MovieInfo
    {
        public string Name { get; set; }
        public string Year { get; set; }
        public string Genre { get; set; }
        public string Score { get; set; }
        public string MyRating { get; set; }
        public int SearchRelevance { get; set; }
        public string Type { get; set; }
        public string Summary { get; set; }  // ADD THIS LINE
    }

}