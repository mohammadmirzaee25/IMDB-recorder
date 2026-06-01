using System;
using System.Drawing;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace My_IMDB
{
    /// <summary>
    /// Helper class for displaying images and text in PictureBox
    /// </summary>
    public static class ImageHelper
    {
        /// <summary>
        /// Displays formatted summary text in a PictureBox
        /// </summary>
        public static void DisplaySummaryInPictureBox(PictureBox pictureBox, string title, string summary)
        {
            if (pictureBox == null) return;

            try
            {
                var bmp = new Bitmap(pictureBox.Width, pictureBox.Height);

                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.FromArgb(30, 30, 30));

                    using (Font titleFont = new Font("Arial", 14, FontStyle.Bold))
                    using (Font summaryFont = new Font("Arial", 10, FontStyle.Regular))
                    {
                        SizeF titleSize = g.MeasureString(title, titleFont);
                        float titleX = (bmp.Width - titleSize.Width) / 2;
                        float titleY = 15;

                        using (SolidBrush titleBrush = new SolidBrush(Color.Gold))
                        {
                            g.DrawString(title, titleFont, titleBrush, titleX, titleY);
                        }

                        using (Pen linePen = new Pen(Color.Gold, 2))
                        {
                            float lineY = titleY + titleSize.Height + 5;
                            g.DrawLine(linePen, 50, lineY, bmp.Width - 50, lineY);
                        }

                        var summaryRect = new RectangleF(15, titleY + titleSize.Height + 20,
                            bmp.Width - 30, bmp.Height - (titleY + titleSize.Height + 30));

                        using (SolidBrush summaryBrush = new SolidBrush(Color.White))
                        {
                            g.DrawString(summary, summaryFont, summaryBrush, summaryRect);
                        }
                    }
                }

                pictureBox.Image = bmp;
                pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error displaying summary: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Loads an image from URL into a PictureBox
        /// </summary>
        public static async Task LoadImageFromUrlAsync(PictureBox pictureBox, string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl) || pictureBox == null) return;

            try
            {
                using (var client = new HttpClient())
                {
                    byte[] imageData = await client.GetByteArrayAsync(imageUrl);
                    using (var ms = new System.IO.MemoryStream(imageData))
                    {
                        var image = Image.FromStream(ms);
                        pictureBox.Image = image;
                        pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading image: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}