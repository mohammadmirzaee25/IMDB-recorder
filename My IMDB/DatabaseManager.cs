using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace My_IMDB
{
    /// <summary>
    /// Manages all database operations for the IMDB application
    /// </summary>
    public class DatabaseManager
    {
        private string connectionString;

        public DatabaseManager()
        {
            connectionString = GetConnectionString();
        }

        /// <summary>
        /// Gets the database connection string based on execution environment
        /// </summary>
        private string GetConnectionString()
        {
            string databasePath;

            if (System.Diagnostics.Debugger.IsAttached)
            {
                string projectPath = Path.GetFullPath(Path.Combine(Application.StartupPath, @"..\.."));
                databasePath = Path.Combine(projectPath, "Database1.mdf");

                if (!File.Exists(databasePath))
                {
                    databasePath = Path.Combine(Application.StartupPath, "Database1.mdf");
                }
            }
            else
            {
                databasePath = Path.Combine(Application.StartupPath, "Database1.mdf");
            }

            if (!File.Exists(databasePath))
            {
                MessageBox.Show($"Database file not found at:\n{databasePath}", "Database Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }

            return $@"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename={databasePath};Integrated Security=True";
        }

        /// <summary>
        /// Retrieves all saved movies/series from database
        /// </summary>
        public List<MovieInfo> GetAllItems()
        {
            var items = new List<MovieInfo>();

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand(
                        "SELECT name, year, imdbrating, myrating, genre, type, summary FROM IMDB ORDER BY name", conn);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string typeValue = reader["type"]?.ToString() ?? "movie";
                            items.Add(new MovieInfo
                            {
                                Name = reader["name"].ToString(),
                                Year = reader["year"].ToString(),
                                Genre = reader["genre"].ToString(),
                                Score = reader["imdbrating"].ToString(),
                                MyRating = reader["myrating"].ToString(),
                                Type = typeValue,
                                Summary = reader["summary"]?.ToString() ?? "No summary available"
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading from database: {ex.Message}", "Database Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return items;
        }

        /// <summary>
        /// Gets items filtered by type (movie or series)
        /// </summary>
        public List<MovieInfo> GetItemsByType(string type)
        {
            var items = new List<MovieInfo>();

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand(
                        "SELECT name, year, imdbrating, myrating, genre, type, summary FROM IMDB WHERE type = @type ORDER BY name", conn);
                    cmd.Parameters.AddWithValue("@type", type);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            items.Add(new MovieInfo
                            {
                                Name = reader["name"].ToString(),
                                Year = reader["year"].ToString(),
                                Genre = reader["genre"].ToString(),
                                Score = reader["imdbrating"].ToString(),
                                MyRating = reader["myrating"].ToString(),
                                Type = type,
                                Summary = reader["summary"]?.ToString() ?? "No summary available"
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error filtering database: {ex.Message}", "Database Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return items;
        }

        /// <summary>
        /// Searches for items in the database by name
        /// </summary>
        public List<MovieInfo> SearchItems(string searchTerm)
        {
            var results = new List<MovieInfo>();

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"SELECT name, year, imdbrating, myrating, genre, type, summary 
                                   FROM IMDB WHERE LOWER(name) LIKE @search ORDER BY name";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@search", "%" + searchTerm.ToLower() + "%");

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(new MovieInfo
                            {
                                Name = reader["name"].ToString(),
                                Year = reader["year"].ToString(),
                                Genre = reader["genre"].ToString(),
                                Score = reader["imdbrating"].ToString(),
                                MyRating = reader["myrating"].ToString(),
                                Type = reader["type"]?.ToString() ?? "movie",
                                Summary = reader["summary"]?.ToString() ?? "No summary available"
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error searching database: {ex.Message}", "Database Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return results.DistinctBy(m => m.Name).ToList();
        }

        /// <summary>
        /// Adds a new movie/series to the database
        /// </summary>
        public bool AddItem(MovieInfo movie, string userRating, bool isSearchingMovies)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // Check if already exists
                    SqlCommand checkCmd = new SqlCommand("SELECT COUNT(*) FROM IMDB WHERE name = @name", conn);
                    checkCmd.Parameters.AddWithValue("@name", movie.Name);
                    int exists = (int)checkCmd.ExecuteScalar();

                    if (exists > 0)
                    {
                        MessageBox.Show($"'{movie.Name}' already exists in your collection!", "Duplicate",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return false;
                    }

                    string movieType = string.IsNullOrEmpty(movie.Type) ? (isSearchingMovies ? "movie" : "series") : movie.Type;
                    string summary = string.IsNullOrEmpty(movie.Summary) ? "No summary available" : movie.Summary;

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
                        cmd.Parameters.AddWithValue("@summary", summary);

                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show($"'{movie.Name}' added successfully!\nYour rating: {userRating}/10",
                                "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Database Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return false;
        }

        /// <summary>
        /// Updates the user rating for a movie/series
        /// </summary>
        public bool UpdateRating(string movieName, string newRating)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
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
                            MessageBox.Show($"Rating updated for '{movieName}'!\nNew rating: {newRating}/10",
                                "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating rating: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return false;
        }

        /// <summary>
        /// Checks if the IMDB table exists in the database
        /// </summary>
        public bool TableExists()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand(
                        "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'IMDB'", conn);
                    return (int)cmd.ExecuteScalar() > 0;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}

// Extension method for LINQ DistinctBy
public static class EnumerableExtensions
{
    public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
    {
        var seenKeys = new HashSet<TKey>();
        foreach (var element in source)
        {
            if (seenKeys.Add(keySelector(element)))
            {
                yield return element;
            }
        }
    }
}