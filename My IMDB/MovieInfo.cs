using System;

namespace My_IMDB
{
    /// <summary>
    /// Represents a movie or TV series information
    /// </summary>
    public class MovieInfo
    {
        public string Name { get; set; }
        public string Year { get; set; }
        public string Genre { get; set; }
        public string Score { get; set; }
        public string MyRating { get; set; }
        public int SearchRelevance { get; set; }
        public string Type { get; set; }
        public string Summary { get; set; }
    }
}