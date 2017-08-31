using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace GroundStationApplication
{
    public partial class Form1 : Form
    {
        const int historyLength = 670;
        float[] historyX = new float[historyLength];
        float[] historyY = new float[historyLength];
        float[] historyZ = new float[historyLength];
        int nextWrite = 0;
        Graphics bmgAccx;
        Bitmap bmAccx;
        Graphics bmgAccy;
        Bitmap bmAccy;
        Graphics bmgAccz;
        Bitmap bmAccz;
        private TableLayoutPanel tableLayoutPanel1;
        private PictureBox accx;
        private PictureBox accy;
        private PictureBox accz;

        public Form1()
        {
            InitializeComponent();
        }

        private void AddValue(float x, float y, float z)
        {
            historyX[nextWrite] = x;
            historyY[nextWrite] = y;
            historyZ[nextWrite] = z;
            nextWrite = (nextWrite + 1) % historyLength;
        }

        private void accx_Paint(object sender, PaintEventArgs e)
        {
            if (bmAccx == null)
            {                
                bmAccx = new Bitmap(this.accx.Size.Width, this.accx.Size.Height);
                bmgAccx = Graphics.FromImage(bmAccx);
                bmgAccx.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                bmgAccx.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.GammaCorrected;
                bmgAccx.Clear(Color.White);
            }
            Render(bmgAccx, historyX);
            e.Graphics.DrawImage(bmAccx, 0, 0);
        }

        private void accy_Paint(object sender, PaintEventArgs e)
        {
            if (bmAccy == null)
            {
                bmAccy = new Bitmap(this.accy.Size.Width, this.accy.Size.Height);
                bmgAccy = Graphics.FromImage(bmAccy);
                bmgAccy.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                bmgAccy.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.GammaCorrected;
                bmgAccy.Clear(Color.White);
            }
            Render(bmgAccy, historyY);
            e.Graphics.DrawImage(bmAccy, 0, 0);
        }

        private void accz_Paint(object sender, PaintEventArgs e)
        {
            if (bmAccz == null)
            {
                bmAccz = new Bitmap(this.accz.Size.Width, this.accz.Size.Height);
                bmgAccz = Graphics.FromImage(bmAccz);
                bmgAccz.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                bmgAccz.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.GammaCorrected;
                bmgAccz.Clear(Color.White);
            }
            Render(bmgAccz, historyZ);
            e.Graphics.DrawImage(bmAccz, 0, 0);
        }

        private void Render(Graphics bmg, float[] history)
        {
            bmg.Clear(Color.White);
            float y0 = 0;
            float xz0 = 0;
            int x0 = 0;
            bool drawBlue = true;
            for (int i = 0; i < historyLength; i++)
            {
                float y = history[(nextWrite + i) % historyLength];
                float zl = this.accx.Size.Height / 2;
                int x = i;
                if (i != 0)
                {
                    // draw a line
                    bmg.DrawLine(Pens.Black, x0, y0, x, y);
                    if ((x % 10) == 0)
                    {
                        if (true == drawBlue)
                        {
                            bmg.DrawLine(Pens.Blue, xz0, zl, x, zl);
                            drawBlue = false;
                        }
                        else
                        {
                            bmg.DrawLine(Pens.White, xz0, zl, x, zl);
                            drawBlue = true;
                        }
                        xz0 = x;
                    }
                }
                y0 = y;
                x0 = x;
            }
        }

        public void AddData(float accx, float accy, float accz)
        {
            float accxScaled = ((accx / 1100.0f) * (this.accx.Size.Height / 2)) + (this.accx.Size.Height / 2);
            float accyScaled = ((accy / 1100.0f) * (this.accy.Size.Height / 2)) + (this.accy.Size.Height / 2);
            float acczScaled = ((accz / 1100.0f) * (this.accz.Size.Height / 2)) + (this.accz.Size.Height / 2);
            AddValue(accxScaled, accyScaled, acczScaled);
            this.accx.Invalidate();
            this.accy.Invalidate();
            this.accz.Invalidate();
        }

        private void InitializeComponent()
        {
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.accx = new System.Windows.Forms.PictureBox();
            this.accy = new System.Windows.Forms.PictureBox();
            this.accz = new System.Windows.Forms.PictureBox();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.accx)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.accy)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.accz)).BeginInit();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Controls.Add(this.accx, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.accy, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.accz, 0, 2);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 4;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(708, 452);
            this.tableLayoutPanel1.TabIndex = 0;
            this.tableLayoutPanel1.Paint += new System.Windows.Forms.PaintEventHandler(this.tableLayoutPanel1_Paint);
            // 
            // accx
            // 
            this.accx.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.accx.Dock = System.Windows.Forms.DockStyle.Fill;
            this.accx.Location = new System.Drawing.Point(3, 3);
            this.accx.Name = "accx";
            this.accx.Size = new System.Drawing.Size(348, 129);
            this.accx.TabIndex = 0;
            this.accx.TabStop = false;
            this.accx.Click += new System.EventHandler(this.accz_Click);
            this.accx.Paint += new System.Windows.Forms.PaintEventHandler(this.accx_Paint);
            // 
            // accy
            // 
            this.accy.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.accy.Dock = System.Windows.Forms.DockStyle.Fill;
            this.accy.Location = new System.Drawing.Point(3, 138);
            this.accy.Name = "accy";
            this.accy.Size = new System.Drawing.Size(348, 129);
            this.accy.TabIndex = 1;
            this.accy.TabStop = false;
            this.accy.Paint += new System.Windows.Forms.PaintEventHandler(this.accy_Paint);
            // 
            // accz
            // 
            this.accz.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.accz.Dock = System.Windows.Forms.DockStyle.Fill;
            this.accz.Location = new System.Drawing.Point(3, 273);
            this.accz.Name = "accz";
            this.accz.Size = new System.Drawing.Size(348, 129);
            this.accz.TabIndex = 2;
            this.accz.TabStop = false;
            this.accz.Paint += new System.Windows.Forms.PaintEventHandler(this.accz_Paint);
            // 
            // Form1
            // 
            this.ClientSize = new System.Drawing.Size(708, 452);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "Form1";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.Form1_Load);
            this.tableLayoutPanel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.accx)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.accy)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.accz)).EndInit();
            this.ResumeLayout(false);

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // set initial values
            for (int i = 0; i < historyLength; i++)
            {

                historyX[i] = this.accx.Size.Height / 2;
                historyY[i] = this.accy.Size.Height / 2;
                historyZ[i] = this.accz.Size.Height / 2;
            }
        }

        private void tableLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void accz_Click(object sender, EventArgs e)
        {

        }
    }
}