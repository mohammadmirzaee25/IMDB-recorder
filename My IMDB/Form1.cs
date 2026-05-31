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
        private const string OMDB_API_KEY = "a9e37134";
        private static readonly HttpClient client = new HttpClient();

        // Add these flags
        private bool isShowingMyMovies = false;
        private bool isSearchMode = true;

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

                    // Search for movies matching the search term (case insensitive)
                    string query = @"SELECT name, year, imdbrating, myrating, genre 
                           FROM IMDB 
                           WHERE name LIKE @search 
                           ORDER BY name";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@search", "%" + searchTerm + "%");

                    SqlDataReader reader = cmd.ExecuteReader();

                    var searchResults = new List<MovieInfo>();

                    while (reader.Read())
                    {
                        searchResults.Add(new MovieInfo
                        {
                            Name = reader["name"].ToString(),
                            Year = reader["year"].ToString(),
                            Genre = reader["genre"].ToString(),
                            Score = reader["imdbrating"].ToString(),
                            MyRating = reader["myrating"].ToString(),
                            SearchRelevance = 0
                        });
                    }

                    reader.Close();

                    if (searchResults.Count > 0)
                    {
                        dataGridView1.DataSource = searchResults;
                        ConfigureMyMoviesGridView();
                        MessageBox.Show($"Found {searchResults.Count} movie(s) matching '{searchTerm}' in your collection!",
                            "Search Results", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show($"No movies found matching '{searchTerm}' in your database.",
                            "Not Found", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        // Optionally show all movies again
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
            }
        }

        private async Task SearchMoviesAsync(string searchTerm)
        {
            try
            {
                // Show loading cursor and disable button
                this.Cursor = Cursors.WaitCursor;
                btnsearch.Enabled = false;

                // Clear the DataGridView before searching
                dataGridView1.DataSource = null;

                var allMovies = new List<MovieInfo>();
                int currentPage = 1;
                bool hasMorePages = true;

                // Search through pages until we find all results or run out
                while (hasMorePages && allMovies.Count < 100)
                {
                    string encodedSearch = Uri.EscapeDataString(searchTerm);
                    string url = $"http://www.omdbapi.com/?apikey={OMDB_API_KEY}&s={encodedSearch}&page={currentPage}&type=movie";

                    string jsonResponse = await client.GetStringAsync(url);
                    JObject data = JObject.Parse(jsonResponse);

                    if (data["Response"]?.ToString() == "True")
                    {
                        var searchResults = data["Search"] as JArray;

                        if (searchResults != null && searchResults.Count > 0)
                        {
                            foreach (var result in searchResults)
                            {
                                string imdbID = result["imdbID"]?.ToString();
                                string title = result["Title"]?.ToString() ?? "";
                                string year = result["Year"]?.ToString() ?? "";

                                if (!string.IsNullOrEmpty(imdbID))
                                {
                                    string detailUrl = $"http://www.omdbapi.com/?apikey={OMDB_API_KEY}&i={imdbID}";
                                    string detailResponse = await client.GetStringAsync(detailUrl);
                                    JObject detailData = JObject.Parse(detailResponse);

                                    string imdbScore = detailData["imdbRating"]?.ToString() ?? "N/A";

                                    // ONLY add movies that have a valid score (not N/A)
                                    if (imdbScore != "N/A")
                                    {
                                        allMovies.Add(new MovieInfo
                                        {
                                            Name = title,
                                            Year = year,
                                            Genre = detailData["Genre"]?.ToString() ?? "N/A",
                                            Score = imdbScore,
                                            SearchRelevance = CalculateRelevance(title, searchTerm)
                                        });
                                    }

                                    await Task.Delay(100);
                                }
                            }

                            int totalResults = int.Parse(data["totalResults"]?.ToString() ?? "0");
                            if (currentPage * 10 >= totalResults)
                            {
                                hasMorePages = false;
                            }
                            else
                            {
                                currentPage++;
                            }
                        }
                        else
                        {
                            hasMorePages = false;
                        }
                    }
                    else
                    {
                        hasMorePages = false;
                    }
                }

                // Sort by relevance (most relevant first)
                var sortedMovies = allMovies.OrderByDescending(m => m.SearchRelevance).ThenBy(m => m.Name).ToList();

                // Show results if found
                if (sortedMovies.Count > 0)
                {
                    dataGridView1.DataSource = sortedMovies;
                    ConfigureDataGridView();
                    MessageBox.Show($"Found {sortedMovies.Count} movie(s) matching '{searchTerm}'.",
                        "Search Results", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show($"No movies found matching '{searchTerm}'.",
                        "Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    dataGridView1.DataSource = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}\n\nPlease check your internet connection.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                dataGridView1.DataSource = null;
            }
            finally
            {
                this.Cursor = Cursors.Default;
                btnsearch.Enabled = true;
            }
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

                    // First check if movie already exists in database
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

                    // If not exists, add the movie
                    string query = @"INSERT INTO IMDB (name, year, imdbrating, myrating, genre) 
                           VALUES (@name, @year, @imdbrating, @myrating, @genre)";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@name", movie.Name);
                        cmd.Parameters.AddWithValue("@year", movie.Year);
                        cmd.Parameters.AddWithValue("@imdbrating", movie.Score);
                        cmd.Parameters.AddWithValue("@myrating", userRating);
                        cmd.Parameters.AddWithValue("@genre", movie.Genre);

                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show($"Movie '{movie.Name}' added to your database successfully!\n\nYour rating: {userRating}/10",
                                "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            MessageBox.Show("Failed to add movie to database.",
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

                // Show my movies from database
                ShowMyMoviesFromDatabase();

                // Disable add button (can't add while viewing database)

                // Change mode flags
                isShowingMyMovies = true;
                isSearchMode = false;

                // Change button appearance
                btnmymovies.BackColor = System.Drawing.Color.Green;
                btnmymovies.Text = "Click to Search Online Titles";
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

                    // Get all movies from IMDB table
                    SqlCommand cmd = new SqlCommand("SELECT name, year, imdbrating, myrating, genre FROM IMDB ORDER BY name", conn);
                    SqlDataReader reader = cmd.ExecuteReader();

                    var myMovies = new List<MovieInfo>();

                    while (reader.Read())
                    {
                        myMovies.Add(new MovieInfo
                        {
                            Name = reader["name"].ToString(),
                            Year = reader["year"].ToString(),
                            Genre = reader["genre"].ToString(),
                            Score = reader["imdbrating"].ToString(),
                            MyRating = reader["myrating"].ToString(),
                            SearchRelevance = 0
                        });
                    }

                    reader.Close();

                    if (myMovies.Count > 0)
                    {
                        dataGridView1.DataSource = myMovies;
                        ConfigureMyMoviesGridView();

                        // Change add button to edit button
                        addbtn.Text = "Edit ";
                        addbtn.Enabled = true;  // Enable it for editing


                        MessageBox.Show($"Found {myMovies.Count} movie(s) in your collection!",
                            "My Movies", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("No movies found in your database.\n\nClick 'Search' to find and add movies.",
                            "My Movies", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        dataGridView1.DataSource = null;
                        GoBackToSearchMode();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading movies from database: {ex.Message}",
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
            btnmymovies.Text = "My Movies And Series";
            dataGridView1.DataSource = null;
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

                // Auto-size columns
                dataGridView1.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);

                // Make DataGridView read-only
                dataGridView1.ReadOnly = true;
            }
        }
        private async Task LoadMoviePoster(string movieName, string movieYear)
        {
            try
            {
                string encodedTitle = Uri.EscapeDataString(movieName);
                string url = $"http://www.omdbapi.com/?apikey={OMDB_API_KEY}&t={encodedTitle}&plot=short";

                using (var httpClient = new HttpClient())
                {
                    string jsonResponse = await httpClient.GetStringAsync(url);
                    JObject data = JObject.Parse(jsonResponse);

                    if (data["Response"]?.ToString() == "True")
                    {
                        string posterUrl = data["Poster"]?.ToString();

                        if (!string.IsNullOrEmpty(posterUrl) && posterUrl != "N/A")
                        {
                            // Download the poster image
                            using (var posterClient = new HttpClient())
                            {
                                byte[] imageData = await posterClient.GetByteArrayAsync(posterUrl);
                                using (var ms = new System.IO.MemoryStream(imageData))
                                {
                                    Image posterImage = Image.FromStream(ms);
                                    pictureBox1.Image = posterImage;
                                    pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
                                }
                            }
                        }
                        else
                        {
                            pictureBox1.Image = null;
                            MessageBox.Show($"No poster found for '{movieName}'.",
                                "Poster Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                    else
                    {
                        pictureBox1.Image = null;
                        MessageBox.Show($"Movie '{movieName}' not found.",
                            "Movie Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading poster: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                pictureBox1.Image = null;
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
    }

    // Movie information class
    // Movie information class
    public class MovieInfo
    {
        public string Name { get; set; }
        public string Year { get; set; }
        public string Genre { get; set; }
        public string Score { get; set; }
        public string MyRating { get; set; }  // Add this property
        public int SearchRelevance { get; set; }
    }
}