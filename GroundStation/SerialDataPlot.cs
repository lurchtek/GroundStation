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
        public enum TUserInput
        {
            KP,
            KI,
            KD,
            MOTSPEED,
            DRONESTATE,
            KALMAN_Q,
            KALMAN_R,
            KALMAN_P,
            PITCHREF,
            MESSEL
        };
        public delegate void UserInputFloatDelegate(float valToSend, TUserInput ID);
        public delegate void UserInputUInt32Delegate(UInt32 valToSend, TUserInput ID);
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
        private TextBox pidkp_textBox;
        private TextBox pidki_textBox;
        private TextBox pidkd_textBox;
        private Button kpsend_button;
        private Button kisend_button;
        private Button kdsend_button;
        private TextBox setspeed_textBox;
        private Button setspeed_button;
        private PictureBox accz;
        private ComboBox state_ComboBox;
        private Button setstate_button;
        public UserInputFloatDelegate UserSentFloatValue;
        private TextBox set_kalmanQ_textBox;
        private TextBox set_kalmanR_textBox;
        private TextBox set_kalmanP_textBox;
        private Button set_kalmanQ_button;
        private Button set_kalmanR_button;
        private Button set_kalmanP_button;
        private TextBox pitch_textBox;
        private Button pitchsend_button;
        private Button messel_button;
        private ComboBox messel_comboBox;
        public UserInputUInt32Delegate UserSentUInt32Value;

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
            float accxScaled;
            float accyScaled;
            float acczScaled;

            if(1100.0 < accx)
            {
                accxScaled = 1100.0f;
            }
            else if(-1100.0 > accx)
            {
                accxScaled = -1100.0f;
            }
            else
            {
                accxScaled = accx;
            }

            if (1100.0 < accy)
            {
                accyScaled = 1100.0f;
            }
            else if (-1100.0 > accy)
            {
                accyScaled = -1100.0f;
            }
            else
            {
                accyScaled = accy;
            }

            if (1100.0 < accz)
            {
                acczScaled = 1100.0f;
            }
            else if (-1100.0 > accz)
            {
                acczScaled = -1100.0f;
            }
            else
            {
                acczScaled = accz;
            }

            accxScaled = ((accxScaled / 1100.0f) * (this.accx.Size.Height / 2)) + (this.accx.Size.Height / 2);
            accyScaled = ((accyScaled / 1100.0f) * (this.accy.Size.Height / 2)) + (this.accy.Size.Height / 2);
            acczScaled = ((acczScaled / 1100.0f) * (this.accz.Size.Height / 2)) + (this.accz.Size.Height / 2);
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
            this.pidkp_textBox = new System.Windows.Forms.TextBox();
            this.kpsend_button = new System.Windows.Forms.Button();
            this.pidki_textBox = new System.Windows.Forms.TextBox();
            this.pidkd_textBox = new System.Windows.Forms.TextBox();
            this.kisend_button = new System.Windows.Forms.Button();
            this.kdsend_button = new System.Windows.Forms.Button();
            this.setspeed_textBox = new System.Windows.Forms.TextBox();
            this.setspeed_button = new System.Windows.Forms.Button();
            this.state_ComboBox = new System.Windows.Forms.ComboBox();
            this.setstate_button = new System.Windows.Forms.Button();
            this.set_kalmanQ_textBox = new System.Windows.Forms.TextBox();
            this.set_kalmanR_textBox = new System.Windows.Forms.TextBox();
            this.set_kalmanP_textBox = new System.Windows.Forms.TextBox();
            this.set_kalmanQ_button = new System.Windows.Forms.Button();
            this.set_kalmanR_button = new System.Windows.Forms.Button();
            this.set_kalmanP_button = new System.Windows.Forms.Button();
            this.pitch_textBox = new System.Windows.Forms.TextBox();
            this.pitchsend_button = new System.Windows.Forms.Button();
            this.messel_button = new System.Windows.Forms.Button();
            this.messel_comboBox = new System.Windows.Forms.ComboBox();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.accx)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.accy)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.accz)).BeginInit();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Controls.Add(this.accx, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.accy, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.accz, 0, 2);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Left;
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
            this.accx.Size = new System.Drawing.Size(702, 129);
            this.accx.TabIndex = 0;
            this.accx.TabStop = false;
            this.accx.Paint += new System.Windows.Forms.PaintEventHandler(this.accx_Paint);
            // 
            // accy
            // 
            this.accy.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.accy.Dock = System.Windows.Forms.DockStyle.Fill;
            this.accy.Location = new System.Drawing.Point(3, 138);
            this.accy.Name = "accy";
            this.accy.Size = new System.Drawing.Size(702, 129);
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
            this.accz.Size = new System.Drawing.Size(702, 129);
            this.accz.TabIndex = 2;
            this.accz.TabStop = false;
            this.accz.Paint += new System.Windows.Forms.PaintEventHandler(this.accz_Paint);
            // 
            // pidkp_textBox
            // 
            this.pidkp_textBox.Location = new System.Drawing.Point(714, 12);
            this.pidkp_textBox.Name = "pidkp_textBox";
            this.pidkp_textBox.Size = new System.Drawing.Size(100, 20);
            this.pidkp_textBox.TabIndex = 0;
            // 
            // kpsend_button
            // 
            this.kpsend_button.Location = new System.Drawing.Point(820, 12);
            this.kpsend_button.Name = "kpsend_button";
            this.kpsend_button.Size = new System.Drawing.Size(75, 20);
            this.kpsend_button.TabIndex = 1;
            this.kpsend_button.Text = "Kp_Send";
            this.kpsend_button.UseVisualStyleBackColor = true;
            this.kpsend_button.Click += new System.EventHandler(this.kpsend_button_Click);
            // 
            // pidki_textBox
            // 
            this.pidki_textBox.Location = new System.Drawing.Point(714, 38);
            this.pidki_textBox.Name = "pidki_textBox";
            this.pidki_textBox.Size = new System.Drawing.Size(100, 20);
            this.pidki_textBox.TabIndex = 2;
            // 
            // pidkd_textBox
            // 
            this.pidkd_textBox.Location = new System.Drawing.Point(714, 64);
            this.pidkd_textBox.Name = "pidkd_textBox";
            this.pidkd_textBox.Size = new System.Drawing.Size(100, 20);
            this.pidkd_textBox.TabIndex = 3;
            // 
            // kisend_button
            // 
            this.kisend_button.Location = new System.Drawing.Point(820, 38);
            this.kisend_button.Name = "kisend_button";
            this.kisend_button.Size = new System.Drawing.Size(75, 20);
            this.kisend_button.TabIndex = 4;
            this.kisend_button.Text = "Ki_Send";
            this.kisend_button.UseVisualStyleBackColor = true;
            this.kisend_button.Click += new System.EventHandler(this.kisend_button_Click);
            // 
            // kdsend_button
            // 
            this.kdsend_button.Location = new System.Drawing.Point(820, 64);
            this.kdsend_button.Name = "kdsend_button";
            this.kdsend_button.Size = new System.Drawing.Size(75, 20);
            this.kdsend_button.TabIndex = 5;
            this.kdsend_button.Text = "Kd_Send";
            this.kdsend_button.UseVisualStyleBackColor = true;
            this.kdsend_button.Click += new System.EventHandler(this.kdsend_button_Click);
            // 
            // setspeed_textBox
            // 
            this.setspeed_textBox.Location = new System.Drawing.Point(714, 247);
            this.setspeed_textBox.Name = "setspeed_textBox";
            this.setspeed_textBox.Size = new System.Drawing.Size(100, 20);
            this.setspeed_textBox.TabIndex = 6;
            // 
            // setspeed_button
            // 
            this.setspeed_button.Location = new System.Drawing.Point(820, 247);
            this.setspeed_button.Name = "setspeed_button";
            this.setspeed_button.Size = new System.Drawing.Size(75, 20);
            this.setspeed_button.TabIndex = 7;
            this.setspeed_button.Text = "SetSpeed";
            this.setspeed_button.UseVisualStyleBackColor = true;
            this.setspeed_button.Click += new System.EventHandler(this.setspeed_button_Click);
            // 
            // state_ComboBox
            // 
            this.state_ComboBox.FormattingEnabled = true;
            this.state_ComboBox.Items.AddRange(new object[] {
            "Balance",
            "Running",
            "Test",
            "Idle"});
            this.state_ComboBox.Location = new System.Drawing.Point(714, 283);
            this.state_ComboBox.Name = "state_ComboBox";
            this.state_ComboBox.Size = new System.Drawing.Size(100, 21);
            this.state_ComboBox.TabIndex = 8;
            // 
            // setstate_button
            // 
            this.setstate_button.Location = new System.Drawing.Point(820, 284);
            this.setstate_button.Name = "setstate_button";
            this.setstate_button.Size = new System.Drawing.Size(75, 20);
            this.setstate_button.TabIndex = 9;
            this.setstate_button.Text = "SetState";
            this.setstate_button.UseVisualStyleBackColor = true;
            this.setstate_button.Click += new System.EventHandler(this.setstate_button_Click);
            // 
            // set_kalmanQ_textBox
            // 
            this.set_kalmanQ_textBox.Location = new System.Drawing.Point(714, 325);
            this.set_kalmanQ_textBox.Name = "set_kalmanQ_textBox";
            this.set_kalmanQ_textBox.Size = new System.Drawing.Size(100, 20);
            this.set_kalmanQ_textBox.TabIndex = 10;
            // 
            // set_kalmanR_textBox
            // 
            this.set_kalmanR_textBox.Location = new System.Drawing.Point(714, 352);
            this.set_kalmanR_textBox.Name = "set_kalmanR_textBox";
            this.set_kalmanR_textBox.Size = new System.Drawing.Size(100, 20);
            this.set_kalmanR_textBox.TabIndex = 11;
            // 
            // set_kalmanP_textBox
            // 
            this.set_kalmanP_textBox.Location = new System.Drawing.Point(714, 378);
            this.set_kalmanP_textBox.Name = "set_kalmanP_textBox";
            this.set_kalmanP_textBox.Size = new System.Drawing.Size(100, 20);
            this.set_kalmanP_textBox.TabIndex = 12;
            // 
            // set_kalmanQ_button
            // 
            this.set_kalmanQ_button.Location = new System.Drawing.Point(820, 325);
            this.set_kalmanQ_button.Name = "set_kalmanQ_button";
            this.set_kalmanQ_button.Size = new System.Drawing.Size(80, 20);
            this.set_kalmanQ_button.TabIndex = 13;
            this.set_kalmanQ_button.Text = "SetKalmanQ";
            this.set_kalmanQ_button.UseVisualStyleBackColor = true;
            this.set_kalmanQ_button.Click += new System.EventHandler(this.set_kalmanQ_button_Click);
            // 
            // set_kalmanR_button
            // 
            this.set_kalmanR_button.Location = new System.Drawing.Point(820, 352);
            this.set_kalmanR_button.Name = "set_kalmanR_button";
            this.set_kalmanR_button.Size = new System.Drawing.Size(80, 20);
            this.set_kalmanR_button.TabIndex = 14;
            this.set_kalmanR_button.Text = "SetKalmanR";
            this.set_kalmanR_button.UseVisualStyleBackColor = true;
            this.set_kalmanR_button.Click += new System.EventHandler(this.set_kalmanR_button_Click);
            // 
            // set_kalmanP_button
            // 
            this.set_kalmanP_button.Location = new System.Drawing.Point(820, 378);
            this.set_kalmanP_button.Name = "set_kalmanP_button";
            this.set_kalmanP_button.Size = new System.Drawing.Size(80, 20);
            this.set_kalmanP_button.TabIndex = 15;
            this.set_kalmanP_button.Text = "SetKalmanP";
            this.set_kalmanP_button.UseVisualStyleBackColor = true;
            this.set_kalmanP_button.Click += new System.EventHandler(this.set_kalmanP_button_Click);
            // 
            // pitch_textBox
            // 
            this.pitch_textBox.Location = new System.Drawing.Point(714, 121);
            this.pitch_textBox.Name = "pitch_textBox";
            this.pitch_textBox.Size = new System.Drawing.Size(100, 20);
            this.pitch_textBox.TabIndex = 16;
            // 
            // pitchsend_button
            // 
            this.pitchsend_button.Location = new System.Drawing.Point(820, 121);
            this.pitchsend_button.Name = "pitchsend_button";
            this.pitchsend_button.Size = new System.Drawing.Size(75, 20);
            this.pitchsend_button.TabIndex = 17;
            this.pitchsend_button.Text = "Pitch_Send";
            this.pitchsend_button.UseVisualStyleBackColor = true;
            this.pitchsend_button.Click += new System.EventHandler(this.pitchsend_button_Click);
            // 
            // messel_button
            // 
            this.messel_button.Location = new System.Drawing.Point(820, 168);
            this.messel_button.Name = "messel_button";
            this.messel_button.Size = new System.Drawing.Size(75, 20);
            this.messel_button.TabIndex = 19;
            this.messel_button.Text = "SelMessage";
            this.messel_button.UseVisualStyleBackColor = true;
            this.messel_button.Click += new System.EventHandler(this.messel_button_Click);
            // 
            // messel_comboBox
            // 
            this.messel_comboBox.FormattingEnabled = true;
            this.messel_comboBox.Items.AddRange(new object[] {
            "AccelMes",
            "GyroMes",
            "MagnMes",
            "EulerMes",
            "MixedMes"});
            this.messel_comboBox.Location = new System.Drawing.Point(714, 168);
            this.messel_comboBox.Name = "messel_comboBox";
            this.messel_comboBox.Size = new System.Drawing.Size(100, 21);
            this.messel_comboBox.TabIndex = 20;
            // 
            // Form1
            // 
            this.AutoScroll = true;
            this.ClientSize = new System.Drawing.Size(913, 452);
            this.Controls.Add(this.messel_comboBox);
            this.Controls.Add(this.messel_button);
            this.Controls.Add(this.pitchsend_button);
            this.Controls.Add(this.pitch_textBox);
            this.Controls.Add(this.set_kalmanP_button);
            this.Controls.Add(this.set_kalmanR_button);
            this.Controls.Add(this.set_kalmanQ_button);
            this.Controls.Add(this.set_kalmanP_textBox);
            this.Controls.Add(this.set_kalmanR_textBox);
            this.Controls.Add(this.set_kalmanQ_textBox);
            this.Controls.Add(this.setstate_button);
            this.Controls.Add(this.state_ComboBox);
            this.Controls.Add(this.setspeed_button);
            this.Controls.Add(this.setspeed_textBox);
            this.Controls.Add(this.kdsend_button);
            this.Controls.Add(this.kisend_button);
            this.Controls.Add(this.kpsend_button);
            this.Controls.Add(this.pidkd_textBox);
            this.Controls.Add(this.pidki_textBox);
            this.Controls.Add(this.pidkp_textBox);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "Form1";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.Form1_Load);
            this.tableLayoutPanel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.accx)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.accy)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.accz)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

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

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void kpsend_button_Click(object sender, EventArgs e)
        {
            float val = Convert.ToSingle(this.pidkp_textBox.Text);
            UserSentFloatValue(val, TUserInput.KP);
        }

        private void kisend_button_Click(object sender, EventArgs e)
        {
            float val = Convert.ToSingle(this.pidki_textBox.Text);
            UserSentFloatValue(val, TUserInput.KI);
        }

        private void kdsend_button_Click(object sender, EventArgs e)
        {
            float val = Convert.ToSingle(this.pidkd_textBox.Text);
            UserSentFloatValue(val, TUserInput.KD);
        }

        private void setspeed_button_Click(object sender, EventArgs e)
        {
            UInt32 val = Convert.ToUInt32(this.setspeed_textBox.Text);
            UserSentUInt32Value(val, TUserInput.MOTSPEED);
        }

        private void setstate_button_Click(object sender, EventArgs e)
        {
            string boxValue = Convert.ToString(this.state_ComboBox.SelectedItem);
            
            //constants sent should match the state enumerator in drone SW
            if("Balance" == boxValue)
            {
                UserSentUInt32Value(3, TUserInput.DRONESTATE);
            }
            else if("Running" == boxValue)
            {
                UserSentUInt32Value(4, TUserInput.DRONESTATE);
            }
            else if ("Test" == boxValue)
            {
                UserSentUInt32Value(5, TUserInput.DRONESTATE);
            }
            else if ("Idle" == boxValue)
            {
                UserSentUInt32Value(6, TUserInput.DRONESTATE);
            }
            else { }
        }

        private void set_kalmanQ_button_Click(object sender, EventArgs e)
        {
            float val = Convert.ToSingle(this.set_kalmanQ_textBox.Text);
            UserSentFloatValue(val, TUserInput.KALMAN_Q);
        }

        private void set_kalmanR_button_Click(object sender, EventArgs e)
        {
            float val = Convert.ToSingle(this.set_kalmanR_textBox.Text);
            UserSentFloatValue(val, TUserInput.KALMAN_R);
        }

        private void set_kalmanP_button_Click(object sender, EventArgs e)
        {
            float val = Convert.ToSingle(this.set_kalmanP_textBox.Text);
            UserSentFloatValue(val, TUserInput.KALMAN_P);
        }

        private void pitchsend_button_Click(object sender, EventArgs e)
        {
            float val = Convert.ToSingle(this.pitch_textBox.Text);
            UserSentFloatValue(val, TUserInput.PITCHREF);
        }

        private void messel_button_Click(object sender, EventArgs e)
        {
            string boxValue = Convert.ToString(this.messel_comboBox.SelectedItem);

            if ("AccelMes" == boxValue)
            {
                UserSentUInt32Value(0xF0, TUserInput.MESSEL);
            }
            else if ("GyroMes" == boxValue)
            {
                UserSentUInt32Value(0xF1, TUserInput.MESSEL);
            }
            else if ("MagnMes" == boxValue)
            {
                UserSentUInt32Value(0xF2, TUserInput.MESSEL);
            }
            else if ("EulerMes" == boxValue)
            {
                UserSentUInt32Value(0xF3, TUserInput.MESSEL);
            }
            else if ("MixedMes" == boxValue)
            {
                UserSentUInt32Value(0xF4, TUserInput.MESSEL);
            }
            else { }
        }
    }
}