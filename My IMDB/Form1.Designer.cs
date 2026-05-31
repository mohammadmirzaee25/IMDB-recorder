
namespace My_IMDB
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.btnmymovies = new System.Windows.Forms.Button();
            this.txtsearch = new System.Windows.Forms.TextBox();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.btnsearch = new System.Windows.Forms.Button();
            this.addbtn = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 36F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(295, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(245, 55);
            this.label1.TabIndex = 0;
            this.label1.Text = "Welcome!";
            // 
            // btnmymovies
            // 
            this.btnmymovies.BackColor = System.Drawing.Color.Black;
            this.btnmymovies.ForeColor = System.Drawing.Color.Yellow;
            this.btnmymovies.Location = new System.Drawing.Point(21, 367);
            this.btnmymovies.Name = "btnmymovies";
            this.btnmymovies.Size = new System.Drawing.Size(830, 61);
            this.btnmymovies.TabIndex = 2;
            this.btnmymovies.Text = "List of Watched Movies And Series";
            this.btnmymovies.UseVisualStyleBackColor = false;
            this.btnmymovies.Click += new System.EventHandler(this.btnmymovies_Click_1);
            // 
            // txtsearch
            // 
            this.txtsearch.BackColor = System.Drawing.Color.LemonChiffon;
            this.txtsearch.Location = new System.Drawing.Point(21, 99);
            this.txtsearch.Multiline = true;
            this.txtsearch.Name = "txtsearch";
            this.txtsearch.Size = new System.Drawing.Size(303, 38);
            this.txtsearch.TabIndex = 4;
            // 
            // dataGridView1
            // 
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Location = new System.Drawing.Point(21, 144);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.Size = new System.Drawing.Size(533, 217);
            this.dataGridView1.TabIndex = 5;
            // 
            // btnsearch
            // 
            this.btnsearch.BackColor = System.Drawing.Color.Black;
            this.btnsearch.ForeColor = System.Drawing.Color.Yellow;
            this.btnsearch.Location = new System.Drawing.Point(339, 99);
            this.btnsearch.Name = "btnsearch";
            this.btnsearch.Size = new System.Drawing.Size(99, 38);
            this.btnsearch.TabIndex = 6;
            this.btnsearch.Text = "search";
            this.btnsearch.UseVisualStyleBackColor = false;
            this.btnsearch.Click += new System.EventHandler(this.btnsearch_Click);
            // 
            // addbtn
            // 
            this.addbtn.BackColor = System.Drawing.Color.Black;
            this.addbtn.ForeColor = System.Drawing.Color.Yellow;
            this.addbtn.Location = new System.Drawing.Point(455, 99);
            this.addbtn.Name = "addbtn";
            this.addbtn.Size = new System.Drawing.Size(99, 38);
            this.addbtn.TabIndex = 7;
            this.addbtn.Text = "add";
            this.addbtn.UseVisualStyleBackColor = false;
            this.addbtn.Click += new System.EventHandler(this.addbtn_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(128)))));
            this.ClientSize = new System.Drawing.Size(863, 440);
            this.Controls.Add(this.addbtn);
            this.Controls.Add(this.btnsearch);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.txtsearch);
            this.Controls.Add(this.btnmymovies);
            this.Controls.Add(this.label1);
            this.Cursor = System.Windows.Forms.Cursors.Hand;
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ForeColor = System.Drawing.SystemColors.ControlText;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.Margin = new System.Windows.Forms.Padding(5);
            this.Name = "Form1";
            this.Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnmymovies;
        private System.Windows.Forms.TextBox txtsearch;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.Button btnsearch;
        private System.Windows.Forms.Button addbtn;
    }
}

