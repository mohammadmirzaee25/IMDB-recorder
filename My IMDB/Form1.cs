using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Linq;

namespace My_IMDB
{
    public partial class Form1 : Form
    {
        // Managers
        private ApiManager apiManager;
        private DatabaseManager dbManager;

        // State flags
        private bool isShowingMyMovies = false;
        private bool isSearchMode = true;
        private bool isSearchingMovies = true;

        public Form1()
        {
            InitializeComponent();
            InitializeManagers();
            SetupDataGridView();
            RegisterEventHandlers();
        }

        #region Initialization

        private void InitializeManagers()
        {
            apiManager = new ApiManager();
            dbManager = new DatabaseManager();
        }

        private void SetupDataGridView()
        {
            dataGridView1.DataSource = null;
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView1.MultiSelect = false;
        }

        private void RegisterEventHandlers()
        {
            dataGridView1.CellMouseDown += DataGridView1_CellMouseDown;
            dataGridView1.CellClick += DataGridView1_CellClick;
            dataGridView1.SelectionChanged += DataGridView1_SelectionChanged;
            dataGridView1.CurrentCellChanged += dataGridView1_CurrentCellChanged;
            txtsearch.KeyPress += Txtsearch_KeyPress;
            this.FormClosing += (s, e) => apiManager.ClearCache();
        }

        #endregion

        #region UI Event Handlers

        private void btnsearch_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtsearch.Text))
            {
                MessageBox.Show("Please enter a movie title to search.", "Information",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (isShowingMyMovies)
                SearchInMyMovies(txtsearch.Text.Trim());
            else
                SearchOnlineAsync(txtsearch.Text.Trim());
        }

        private void btnmymovies_Click_1(object sender, EventArgs e)
        {
            if (isShowingMyMovies)
            {
                GoBackToSearchMode();
            }
            else
            {
                SwitchToMyMoviesMode();
            }
        }

        private void addbtn_Click(object sender, EventArgs e)
        {
            if (isShowingMyMovies)
                EditMovieRating();
            else
                AddNewMovie();
        }

        private async void btnshowposter_Click(object sender, EventArgs e)
        {
            var selectedMovie = GetSelectedMovie(true);  // true = show message
            if (selectedMovie != null)
            {
                await LoadPosterAsync(selectedMovie.Name);
            }
        }
        private void btnsummary_Click(object sender, EventArgs e)
        {
            var selectedMovie = GetSelectedMovie(true);  // true = show message
            if (selectedMovie != null)
            {
                ImageHelper.DisplaySummaryInPictureBox(pictureBox1, selectedMovie.Name,
                    selectedMovie.Summary ?? "No summary available.");
            }
        }

        private void rbmovies_CheckedChanged(object sender, EventArgs e)
        {
            if (rbmovies.Checked && isSearchingMovies != true)
            {
                isSearchingMovies = true;

                if (isShowingMyMovies)
                {
                    FilterMyMoviesByType("movie");
                }
                else
                {
                    ShowModeChangeMessage("MOVIES");
                    ClearSearchResults();
                }
            }
        }

        private void rbseries_CheckedChanged(object sender, EventArgs e)
        {
            if (rbseries.Checked && isSearchingMovies != false)
            {
                isSearchingMovies = false;

                if (isShowingMyMovies)
                {
                    FilterMyMoviesByType("series");
                }
                else
                {
                    ShowModeChangeMessage("TV SERIES");
                    ClearSearchResults();
                }
            }
        }

        #endregion

        #region DataGridView Event Handlers

        private void DataGridView1_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                dataGridView1.ClearSelection();
                dataGridView1.Rows[e.RowIndex].Selected = true;
                dataGridView1.CurrentCell = dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex];
            }
        }

        private void DataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                dataGridView1.ClearSelection();
                dataGridView1.Rows[e.RowIndex].Selected = true;
                dataGridView1.CurrentCell = dataGridView1.Rows[e.RowIndex].Cells[0];
                ShowSummaryForSelectedMovie();
            }
        }

        private void DataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            ShowSummaryForSelectedMovie();
        }

        private void dataGridView1_CurrentCellChanged(object sender, EventArgs e)
        {
            ShowSummaryForSelectedMovie();
        }

        private void Txtsearch_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                btnsearch_Click(sender, e);
                e.Handled = true;
            }
        }

        #endregion

        #region Search Methods

        private async Task SearchOnlineAsync(string searchTerm)
        {
            var results = await apiManager.SearchAsync(searchTerm, isSearchingMovies);

            if (results != null && results.Count > 0)
            {
                dataGridView1.DataSource = results;
                ConfigureDataGridView();

                string resultType = isSearchingMovies ? "movies" : "TV series";
                MessageBox.Show($"Found {results.Count} {resultType} matching '{searchTerm}'.",
                    "Search Results", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show($"No { (isSearchingMovies ? "movies" : "TV series") } found.",
                    "Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void SearchInMyMovies(string searchTerm)
        {
            var results = dbManager.SearchItems(searchTerm);

            if (results.Count > 0)
            {
                dataGridView1.DataSource = results;
                ConfigureMyMoviesGridView();
                MessageBox.Show($"Found {results.Count} item(s) matching '{searchTerm}'.",
                    "Search Results", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show($"No items found matching '{searchTerm}'.",
                    "Not Found", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        #endregion

        #region Database Operations

        private void SwitchToMyMoviesMode()
        {
            dataGridView1.DataSource = null;

            if (rbmovies.Checked)
                FilterMyMoviesByType("movie");
            else if (rbseries.Checked)
                FilterMyMoviesByType("series");
            else
                ShowMyMoviesFromDatabase();

            isShowingMyMovies = true;
            isSearchMode = false;
            btnmymovies.BackColor = Color.Green;
            btnmymovies.Text = "Click to Search Online Titles";
            addbtn.Text = "Edit Rating";
            addbtn.Enabled = true;
        }

        private void ShowMyMoviesFromDatabase()
        {
            var movies = dbManager.GetAllItems();

            if (movies.Count > 0)
            {
                dataGridView1.DataSource = movies;
                ConfigureMyMoviesGridView();
                addbtn.Text = "Edit Rating";
                addbtn.Enabled = true;

                int movieCount = movies.Count(m => m.Type == "movie");
                int seriesCount = movies.Count(m => m.Type == "series");
                MessageBox.Show($"Found {movies.Count} item(s)!\nMovies: {movieCount}\nTV Series: {seriesCount}",
                    "My Collection", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("No items found in your database.", "My Collection",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                GoBackToSearchMode();
            }
        }

        private void FilterMyMoviesByType(string type)
        {
            if (!dbManager.TableExists())
            {
                MessageBox.Show("IMDB table not found!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var movies = dbManager.GetItemsByType(type);

            if (movies.Count > 0)
            {
                dataGridView1.DataSource = movies;
                ConfigureMyMoviesGridView();
                isShowingMyMovies = true;
                addbtn.Text = "Edit Rating";
                addbtn.Enabled = true;

                string displayType = type == "movie" ? "Movies" : "TV Series";
                MessageBox.Show($"Found {movies.Count} {displayType} in your collection!",
                    "Filtered View", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                var result = MessageBox.Show($"No {type}s found. See all items?", "No Results",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                    ShowMyMoviesFromDatabase();
                else
                    dataGridView1.DataSource = null;
            }
        }

        private void AddNewMovie()
        {
            var selectedMovie = GetSelectedMovie();
            if (selectedMovie == null) return;

            string userRating = AskForRating(selectedMovie.Name, null);
            if (string.IsNullOrEmpty(userRating)) return;

            dbManager.AddItem(selectedMovie, userRating, isSearchingMovies);
        }

        private void EditMovieRating()
        {
            var selectedMovie = GetSelectedMovie();
            if (selectedMovie == null) return;

            string newRating = AskForRating(selectedMovie.Name, selectedMovie.MyRating);
            if (string.IsNullOrEmpty(newRating)) return;

            if (dbManager.UpdateRating(selectedMovie.Name, newRating))
            {
                ShowMyMoviesFromDatabase();
            }
        }

        #endregion

        #region UI Helpers

        private MovieInfo GetSelectedMovie(bool showMessage = true)
        {
            if (dataGridView1.SelectedRows.Count == 0 && dataGridView1.SelectedCells.Count == 0)
            {
                if (showMessage)
                    MessageBox.Show("Please select a movie first.", "No Selection",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            if (dataGridView1.SelectedRows.Count > 0)
                return (MovieInfo)dataGridView1.SelectedRows[0].DataBoundItem;

            int rowIndex = dataGridView1.SelectedCells[0].RowIndex;
            if (rowIndex >= 0 && rowIndex < dataGridView1.Rows.Count)
                return (MovieInfo)dataGridView1.Rows[rowIndex].DataBoundItem;

            return null;
        }

        private void ShowSummaryForSelectedMovie()
        {
            var selectedMovie = GetSelectedMovie(false);  // false = don't show message
            if (selectedMovie != null)
            {
                ImageHelper.DisplaySummaryInPictureBox(pictureBox1, selectedMovie.Name,
                    selectedMovie.Summary ?? "No summary available.");
            }
        }

        private async Task LoadPosterAsync(string movieName)
        {
            pictureBox1.Image = null;
            Cursor = Cursors.WaitCursor;
            btnshowposter.Enabled = false;
            btnshowposter.Text = "Loading Poster...";

            string posterUrl = await apiManager.GetPosterUrlAsync(movieName);

            if (!string.IsNullOrEmpty(posterUrl))
            {
                await ImageHelper.LoadImageFromUrlAsync(pictureBox1, posterUrl);
            }
            else
            {
                MessageBox.Show($"No poster found for '{movieName}'.", "Poster Not Found",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            Cursor = Cursors.Default;
            btnshowposter.Enabled = true;
            btnshowposter.Text = "Show Poster";
        }

        private void GoBackToSearchMode()
        {
            isShowingMyMovies = false;
            isSearchMode = true;
            addbtn.Enabled = true;
            addbtn.Text = "Add";
            btnmymovies.BackColor = Color.Black;
            btnmymovies.Text = "My Movies And Series";
            dataGridView1.DataSource = null;

            ShowModeChangeMessage(isSearchingMovies ? "MOVIES" : "TV SERIES");
        }

        private void ClearSearchResults()
        {
            dataGridView1.DataSource = null;
            pictureBox1.Image = null;
            txtsearch.Clear();
            txtsearch.Focus();
            addbtn.Text = "Add";
            addbtn.Enabled = true;
        }

        private void ShowModeChangeMessage(string mode)
        {
            MessageBox.Show($"Search mode: {mode} only", "Mode Changed",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private string AskForRating(string movieName, string currentRating = null)
        {
            var ratingDialog = new Form();
            var label = new Label();
            var textBox = new TextBox();
            var okButton = new Button();
            var cancelButton = new Button();
            string rating = null;

            ratingDialog.Text = currentRating == null ? $"Rate: {movieName}" : $"Update Rating: {movieName}";
            ratingDialog.Size = new System.Drawing.Size(300, 150);
            ratingDialog.FormBorderStyle = FormBorderStyle.FixedDialog;
            ratingDialog.StartPosition = FormStartPosition.CenterParent;

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
            if (currentRating != null) textBox.Text = currentRating;

            okButton.Text = "OK";
            okButton.Location = new System.Drawing.Point(100, 95);
            okButton.Click += (s, ev) => {
                if (ValidateRating(textBox.Text))
                {
                    rating = textBox.Text;
                    ratingDialog.DialogResult = DialogResult.OK;
                    ratingDialog.Close();
                }
                else
                {
                    MessageBox.Show("Please enter a valid rating between 1 and 10.", "Invalid Rating");
                }
            };

            cancelButton.Text = "Cancel";
            cancelButton.Location = new System.Drawing.Point(180, 95);
            cancelButton.Click += (s, ev) => {
                ratingDialog.DialogResult = DialogResult.Cancel;
                ratingDialog.Close();
            };

            ratingDialog.Controls.AddRange(new Control[] { label, textBox, okButton, cancelButton });
            ratingDialog.Shown += (s, ev) => textBox.Focus();

            return ratingDialog.ShowDialog() == DialogResult.OK ? rating : null;
        }

        private bool ValidateRating(string rating)
        {
            return double.TryParse(rating, out double value) && value >= 1 && value <= 10;
        }

        #endregion

        #region Grid Configuration

        private void ConfigureDataGridView()
        {
            if (dataGridView1.Columns.Count == 0) return;

            HideColumns(new[] { "SearchRelevance", "Summary" });
            SetColumnHeaders(new[] { "Name", "Year", "Genre", "Score" },
                            new[] { "Movie Name", "Release Year", "Genre", "IMDb Score" });

            dataGridView1.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
            dataGridView1.ReadOnly = true;
        }

        private void ConfigureMyMoviesGridView()
        {
            if (dataGridView1.Columns.Count == 0) return;

            HideColumns(new[] { "SearchRelevance", "Summary" });
            SetColumnHeaders(new[] { "Name", "Year", "Genre", "Score", "MyRating", "Type" },
                            new[] { "Movie Name", "Release Year", "Genre", "IMDb Score", "My Rating", "Type" });

            dataGridView1.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
            dataGridView1.ReadOnly = true;
        }

        private void HideColumns(string[] columnNames)
        {
            foreach (var colName in columnNames)
            {
                if (dataGridView1.Columns[colName] != null)
                    dataGridView1.Columns[colName].Visible = false;
            }
        }

        private void SetColumnHeaders(string[] columnNames, string[] headerTexts)
        {
            for (int i = 0; i < columnNames.Length && i < headerTexts.Length; i++)
            {
                if (dataGridView1.Columns[columnNames[i]] != null)
                    dataGridView1.Columns[columnNames[i]].HeaderText = headerTexts[i];
            }
        }

        #endregion
    }
}