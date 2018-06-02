using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
//using System.Threading;
using Windows.Gaming.Input;
using System.Runtime.InteropServices;

namespace GroundStationApplication
{
    class AccurateTimer
    {
        private delegate void TimerEventDel(int id, int msg, IntPtr user, int dw1, int dw2);
        private const int TIME_PERIODIC = 1;
        private const int EVENT_TYPE = TIME_PERIODIC;// + 0x100;  // TIME_KILL_SYNCHRONOUS causes a hang ?!
        [DllImport("winmm.dll")]
        private static extern int timeBeginPeriod(int msec);
        [DllImport("winmm.dll")]
        private static extern int timeEndPeriod(int msec);
        [DllImport("winmm.dll")]
        private static extern int timeSetEvent(int delay, int resolution, TimerEventDel handler, IntPtr user, int eventType);
        [DllImport("winmm.dll")]
        private static extern int timeKillEvent(int id);

        Action mAction;
        Form mForm;
        private int mTimerId;
        private TimerEventDel mHandler;  // NOTE: declare at class scope so garbage collector doesn't release it!!!

        public AccurateTimer(Form form, Action action, int delay)
        {
            mAction = action;
            mForm = form;
            timeBeginPeriod(1);
            mHandler = new TimerEventDel(TimerCallback);
            mTimerId = timeSetEvent(delay, 0, mHandler, IntPtr.Zero, EVENT_TYPE);
        }

        public void Stop()
        {
            int err = timeKillEvent(mTimerId);
            timeEndPeriod(1);
            System.Threading.Thread.Sleep(100);// Ensure callbacks are drained
        }

        private void TimerCallback(int id, int msg, IntPtr user, int dw1, int dw2)
        {
            if (mTimerId != 0)

                mForm.BeginInvoke(mAction);
        }
    }

    public partial class Form1 : Form
    {
        public enum TUserInput
        {
            KP,
            KI,
            KD,
            MOTSPEED,
            MOT1SPEED,
            MOT2SPEED,
            MOT3SPEED,
            MOT4SPEED,
            DRONESTATE,
            KALMAN_Q,
            KALMAN_R,
            KALMAN_P,
            PITCHREF,
            MESSEL,
            SELECTPID,
            CONTROLX,
            CONTROLY
        };

        public delegate void UserInputFloatDelegate(float valToSend, TUserInput ID);
        public delegate void UserInputUInt32Delegate(UInt32 valToSend, TUserInput ID);
        public delegate void UserInputInt32Delegate(Int32 valToSend, TUserInput ID);

        const uint SELECTEDPID_NOPID = 0u;
        const uint SELECTEDPID_PITCH = 1u;
        const uint SELECTEDPID_ROLL = 2u;
        const uint SELECTEDPID_YAW = 3u;
        const uint SELECTEDPID_PITCHROT = 4u;
        const uint SELECTEDPID_ROLLROT = 5u;
        const uint SELECTEDPID_YAWROT = 6u;

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
        private NumericUpDown setspeedUpDown;
        private NumericUpDown setMot1UpDown;
        private NumericUpDown setMot2UpDown;
        private NumericUpDown setMot3UpDown;
        private NumericUpDown setMot4UpDown;
        private Label SetSpeed;
        private Label Mot1Speed;
        private Label Mot2Speed;
        private Label Mot3Speed;
        private Label Mot4Speed;
        private ComboBox SelectPid_comboBox;
        public UserInputInt32Delegate UserSentInt32Value;
        Gamepad gamepad;
        //private static System.Threading.Timer gamepadTimer;
        AccurateTimer gamepadTimer;
        private UInt32 lastControlXY = 0;
        private UInt32 ticksCount = 0;

        public Form1()
        {
            InitializeComponent();

            //gamepadTimer = new System.Threading.Timer(Tick, null, 20, 45);
            //Timer gamepadTimer = new Timer();
            //gamepadTimer.Interval = 20;
            //gamepadTimer.Tick += new EventHandler(Tick);
            //gamepadTimer.Start();
        }
        
        //private void Tick(object sender)
        private void Tick()
        {
            if(Gamepad.Gamepads.Count > 0)
            {
                gamepad = Gamepad.Gamepads.Single();
                UInt32 currentSpeed;
                Int16 rightStickXConverted;
                Int16 rightStickYConverted;
                UInt32 controlXY;
                string boxValue = Convert.ToString(this.state_ComboBox.SelectedItem);
                byte[] tempBytes = new byte[2];
                var reading = gamepad.GetCurrentReading();
                var rightStickX = reading.RightThumbstickX;
                var rightStickY = reading.RightThumbstickY;
                var upPad = reading.Buttons.HasFlag(GamepadButtons.DPadUp);
                var downPad = reading.Buttons.HasFlag(GamepadButtons.DPadDown);
                var upStep = reading.Buttons.HasFlag(GamepadButtons.DPadRight);
                var downStep = reading.Buttons.HasFlag(GamepadButtons.DPadLeft);
                var assistedButton = reading.LeftTrigger;
                var manualButton = reading.RightTrigger;
                var panicButton = reading.Buttons.HasFlag(GamepadButtons.RightShoulder);

                currentSpeed = Convert.ToUInt32(this.setspeedUpDown.Value);

                if (true == upPad)
                {
                    if (currentSpeed + 10 <= 2000)
                    {
                        if (InvokeRequired)
                        {
                            this.Invoke(new MethodInvoker(delegate
                            {
                                this.setspeedUpDown.Value = currentSpeed + 10;
                            }));
                        }
                        else
                        {
                            this.setspeedUpDown.Value = currentSpeed + 10;
                        }
                    }
                }
                else if(true == downPad)
                {
                    if (currentSpeed - 10 >= 980)
                    {
                        if (InvokeRequired)
                        {
                            this.Invoke(new MethodInvoker(delegate
                            {
                                this.setspeedUpDown.Value = currentSpeed - 10;
                            }));
                        }
                        else
                        {
                            this.setspeedUpDown.Value = currentSpeed - 10;
                        }
                    }
                }
                else
                {

                }

                if (true == upStep)
                {
                    if (currentSpeed + 40 <= 2000)
                    {
                        if (InvokeRequired)
                        {
                            this.Invoke(new MethodInvoker(delegate
                            {
                                this.setspeedUpDown.Value = currentSpeed + 40;
                            }));
                        }
                        else
                        {
                            this.setspeedUpDown.Value = currentSpeed + 40;
                        }
                    }
                }
                else if (true == downStep)
                {
                    if (currentSpeed - 40 >= 980)
                    {
                        if (InvokeRequired)
                        {
                            this.Invoke(new MethodInvoker(delegate
                            {
                                this.setspeedUpDown.Value = currentSpeed - 40;
                            }));
                        }
                        else
                        {
                            this.setspeedUpDown.Value = currentSpeed - 40;
                        }
                    }
                }
                else
                {

                }

                if (true == panicButton)
                {
                    boxValue = Convert.ToString(this.state_ComboBox.SelectedItem);
                    if (boxValue != "Idle")
                    {
                        if (InvokeRequired)
                        {
                            this.Invoke(new MethodInvoker(delegate
                            {
                                this.state_ComboBox.SelectedItem = "Idle";
                            }));
                        }
                        else
                        {
                            this.state_ComboBox.SelectedItem = "Idle";
                        }
                        UserSentUInt32Value(6, TUserInput.DRONESTATE);
                    }
                }
                else if(0 < manualButton)
                {
                    boxValue = Convert.ToString(this.state_ComboBox.SelectedItem);
                    if (boxValue != "FlyManual")
                    {
                        if (InvokeRequired)
                        {
                            this.Invoke(new MethodInvoker(delegate
                            {
                                this.state_ComboBox.SelectedItem = "FlyManual";
                            }));
                        }
                        else
                        {
                            this.state_ComboBox.SelectedItem = "FlyManual";
                        }
                        UserSentUInt32Value(8, TUserInput.DRONESTATE);
                    }
                }
                else if(0 < assistedButton)
                {
                    boxValue = Convert.ToString(this.state_ComboBox.SelectedItem);
                    if (boxValue != "FlyAssisted")
                    {
                        if (InvokeRequired)
                        {
                            this.Invoke(new MethodInvoker(delegate
                            {
                                this.state_ComboBox.SelectedItem = "FlyAssisted";
                            }));
                        }
                        else
                        {
                            this.state_ComboBox.SelectedItem = "FlyAssisted";
                        }
                        UserSentUInt32Value(9, TUserInput.DRONESTATE);
                    }
                }

                if (InvokeRequired)
                {
                    this.Invoke(new MethodInvoker(delegate
                    {
                        boxValue = Convert.ToString(this.state_ComboBox.SelectedItem);
                        if ((boxValue == "FlyAssisted") || (boxValue == "FlyManual"))
                        {
                            ticksCount++;
                            rightStickX = rightStickX * 30;
                            rightStickY = rightStickY * 30;
                            rightStickXConverted = Convert.ToInt16(rightStickX);
                            rightStickYConverted = Convert.ToInt16(rightStickY);
                            tempBytes = BitConverter.GetBytes(rightStickXConverted);
                            controlXY = (UInt32)BitConverter.ToUInt16(tempBytes, 0);
                            controlXY = controlXY << 16;
                            tempBytes = BitConverter.GetBytes(rightStickYConverted);
                            controlXY = controlXY | (UInt32)BitConverter.ToUInt16(tempBytes, 0);
                            if( (lastControlXY != controlXY) || (5 == ticksCount) )
                            {
                                lastControlXY = controlXY;
                                UserSentUInt32Value(controlXY, TUserInput.CONTROLX);
                                ticksCount = 0;
                                //UserSentInt32Value(rightStickYConverted, TUserInput.CONTROLY);
                            }
                        }
                    }));
                }
                else
                {
                    boxValue = Convert.ToString(this.state_ComboBox.SelectedItem);
                    if ((boxValue == "FlyAssisted") || (boxValue == "FlyManual"))
                    {
                        ticksCount++;
                        rightStickX = rightStickX * 30;
                        rightStickY = rightStickY * 30;
                        rightStickXConverted = Convert.ToInt16(rightStickX);
                        rightStickYConverted = Convert.ToInt16(rightStickY);
                        tempBytes = BitConverter.GetBytes(rightStickXConverted);
                        controlXY = (UInt32)BitConverter.ToUInt16(tempBytes, 0);
                        controlXY = controlXY << 16;
                        tempBytes = BitConverter.GetBytes(rightStickYConverted);
                        controlXY = controlXY | (UInt32)BitConverter.ToUInt16(tempBytes, 0);
                        if ((lastControlXY != controlXY) || (5 == ticksCount))
                        {
                            lastControlXY = controlXY;
                            UserSentUInt32Value(controlXY, TUserInput.CONTROLX);
                            ticksCount = 0;
                            //UserSentInt32Value(rightStickYConverted, TUserInput.CONTROLY);
                        }
                    }
                }
            }           
        }

        private void Form1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (27 == e.KeyChar)
            {
                this.state_ComboBox.SelectedItem = "Idle";
                UserSentUInt32Value(6, TUserInput.DRONESTATE);
            }
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

        public void Refresh_PidData(float pidP, float pidI, float pidD)
        {
            if (InvokeRequired)
            {
                // after we've done all the processing, 
                this.Invoke(new MethodInvoker(delegate {
                    this.pidkp_textBox.Text = Convert.ToString(pidP);
                    this.pidki_textBox.Text = Convert.ToString(pidI);
                    this.pidkd_textBox.Text = Convert.ToString(pidD);
                }));
                return;
            }
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
            this.setspeedUpDown = new System.Windows.Forms.NumericUpDown();
            this.setMot1UpDown = new System.Windows.Forms.NumericUpDown();
            this.setMot2UpDown = new System.Windows.Forms.NumericUpDown();
            this.setMot3UpDown = new System.Windows.Forms.NumericUpDown();
            this.setMot4UpDown = new System.Windows.Forms.NumericUpDown();
            this.SetSpeed = new System.Windows.Forms.Label();
            this.Mot1Speed = new System.Windows.Forms.Label();
            this.Mot2Speed = new System.Windows.Forms.Label();
            this.Mot3Speed = new System.Windows.Forms.Label();
            this.Mot4Speed = new System.Windows.Forms.Label();
            this.SelectPid_comboBox = new System.Windows.Forms.ComboBox();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.accx)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.accy)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.accz)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.setspeedUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.setMot1UpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.setMot2UpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.setMot3UpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.setMot4UpDown)).BeginInit();
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
            this.pidkp_textBox.Location = new System.Drawing.Point(714, 222);
            this.pidkp_textBox.Name = "pidkp_textBox";
            this.pidkp_textBox.Size = new System.Drawing.Size(50, 20);
            this.pidkp_textBox.TabIndex = 0;
            // 
            // kpsend_button
            // 
            this.kpsend_button.Location = new System.Drawing.Point(773, 222);
            this.kpsend_button.Name = "kpsend_button";
            this.kpsend_button.Size = new System.Drawing.Size(75, 20);
            this.kpsend_button.TabIndex = 1;
            this.kpsend_button.Text = "Kp_Send";
            this.kpsend_button.UseVisualStyleBackColor = true;
            this.kpsend_button.Click += new System.EventHandler(this.kpsend_button_Click);
            // 
            // pidki_textBox
            // 
            this.pidki_textBox.Location = new System.Drawing.Point(714, 248);
            this.pidki_textBox.Name = "pidki_textBox";
            this.pidki_textBox.Size = new System.Drawing.Size(50, 20);
            this.pidki_textBox.TabIndex = 2;
            // 
            // pidkd_textBox
            // 
            this.pidkd_textBox.Location = new System.Drawing.Point(714, 274);
            this.pidkd_textBox.Name = "pidkd_textBox";
            this.pidkd_textBox.Size = new System.Drawing.Size(50, 20);
            this.pidkd_textBox.TabIndex = 3;
            // 
            // kisend_button
            // 
            this.kisend_button.Location = new System.Drawing.Point(773, 248);
            this.kisend_button.Name = "kisend_button";
            this.kisend_button.Size = new System.Drawing.Size(75, 20);
            this.kisend_button.TabIndex = 4;
            this.kisend_button.Text = "Ki_Send";
            this.kisend_button.UseVisualStyleBackColor = true;
            this.kisend_button.Click += new System.EventHandler(this.kisend_button_Click);
            // 
            // kdsend_button
            // 
            this.kdsend_button.Location = new System.Drawing.Point(773, 274);
            this.kdsend_button.Name = "kdsend_button";
            this.kdsend_button.Size = new System.Drawing.Size(75, 20);
            this.kdsend_button.TabIndex = 5;
            this.kdsend_button.Text = "Kd_Send";
            this.kdsend_button.UseVisualStyleBackColor = true;
            this.kdsend_button.Click += new System.EventHandler(this.kdsend_button_Click);
            // 
            // state_ComboBox
            // 
            this.state_ComboBox.FormattingEnabled = true;
            this.state_ComboBox.Items.AddRange(new object[] {
            "Balance",
            "Running",
            "Test",
            "Idle",
            "TakeOff",
            "FlyManual",
            "FlyAssisted"});
            this.state_ComboBox.Location = new System.Drawing.Point(714, 30);
            this.state_ComboBox.Name = "state_ComboBox";
            this.state_ComboBox.Size = new System.Drawing.Size(100, 21);
            this.state_ComboBox.TabIndex = 8;
            // 
            // setstate_button
            // 
            this.setstate_button.Location = new System.Drawing.Point(820, 31);
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
            this.set_kalmanQ_textBox.Size = new System.Drawing.Size(50, 20);
            this.set_kalmanQ_textBox.TabIndex = 10;
            // 
            // set_kalmanR_textBox
            // 
            this.set_kalmanR_textBox.Location = new System.Drawing.Point(714, 352);
            this.set_kalmanR_textBox.Name = "set_kalmanR_textBox";
            this.set_kalmanR_textBox.Size = new System.Drawing.Size(50, 20);
            this.set_kalmanR_textBox.TabIndex = 11;
            // 
            // set_kalmanP_textBox
            // 
            this.set_kalmanP_textBox.Location = new System.Drawing.Point(714, 378);
            this.set_kalmanP_textBox.Name = "set_kalmanP_textBox";
            this.set_kalmanP_textBox.Size = new System.Drawing.Size(50, 20);
            this.set_kalmanP_textBox.TabIndex = 12;
            // 
            // set_kalmanQ_button
            // 
            this.set_kalmanQ_button.Location = new System.Drawing.Point(773, 325);
            this.set_kalmanQ_button.Name = "set_kalmanQ_button";
            this.set_kalmanQ_button.Size = new System.Drawing.Size(80, 20);
            this.set_kalmanQ_button.TabIndex = 13;
            this.set_kalmanQ_button.Text = "SetKalmanQ";
            this.set_kalmanQ_button.UseVisualStyleBackColor = true;
            this.set_kalmanQ_button.Click += new System.EventHandler(this.set_kalmanQ_button_Click);
            // 
            // set_kalmanR_button
            // 
            this.set_kalmanR_button.Location = new System.Drawing.Point(773, 352);
            this.set_kalmanR_button.Name = "set_kalmanR_button";
            this.set_kalmanR_button.Size = new System.Drawing.Size(80, 20);
            this.set_kalmanR_button.TabIndex = 14;
            this.set_kalmanR_button.Text = "SetKalmanR";
            this.set_kalmanR_button.UseVisualStyleBackColor = true;
            this.set_kalmanR_button.Click += new System.EventHandler(this.set_kalmanR_button_Click);
            // 
            // set_kalmanP_button
            // 
            this.set_kalmanP_button.Location = new System.Drawing.Point(773, 378);
            this.set_kalmanP_button.Name = "set_kalmanP_button";
            this.set_kalmanP_button.Size = new System.Drawing.Size(80, 20);
            this.set_kalmanP_button.TabIndex = 15;
            this.set_kalmanP_button.Text = "SetKalmanP";
            this.set_kalmanP_button.UseVisualStyleBackColor = true;
            this.set_kalmanP_button.Click += new System.EventHandler(this.set_kalmanP_button_Click);
            // 
            // pitch_textBox
            // 
            this.pitch_textBox.Location = new System.Drawing.Point(714, 300);
            this.pitch_textBox.Name = "pitch_textBox";
            this.pitch_textBox.Size = new System.Drawing.Size(50, 20);
            this.pitch_textBox.TabIndex = 16;
            // 
            // pitchsend_button
            // 
            this.pitchsend_button.Location = new System.Drawing.Point(773, 300);
            this.pitchsend_button.Name = "pitchsend_button";
            this.pitchsend_button.Size = new System.Drawing.Size(75, 20);
            this.pitchsend_button.TabIndex = 17;
            this.pitchsend_button.Text = "Pitch_Send";
            this.pitchsend_button.UseVisualStyleBackColor = true;
            this.pitchsend_button.Click += new System.EventHandler(this.pitchsend_button_Click);
            // 
            // messel_button
            // 
            this.messel_button.Location = new System.Drawing.Point(820, 4);
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
            "NoMes",
            "AccelMes",
            "GyroMes",
            "MagnMes",
            "EulerMes",
            "MixedMes",
            "AttitudeLogMes"});
            this.messel_comboBox.Location = new System.Drawing.Point(714, 3);
            this.messel_comboBox.Name = "messel_comboBox";
            this.messel_comboBox.Size = new System.Drawing.Size(100, 21);
            this.messel_comboBox.TabIndex = 20;
            // 
            // setspeedUpDown
            // 
            this.setspeedUpDown.ImeMode = System.Windows.Forms.ImeMode.On;
            this.setspeedUpDown.Increment = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.setspeedUpDown.Location = new System.Drawing.Point(714, 57);
            this.setspeedUpDown.Maximum = new decimal(new int[] {
            2000,
            0,
            0,
            0});
            this.setspeedUpDown.Minimum = new decimal(new int[] {
            980,
            0,
            0,
            0});
            this.setspeedUpDown.Name = "setspeedUpDown";
            this.setspeedUpDown.Size = new System.Drawing.Size(50, 20);
            this.setspeedUpDown.TabIndex = 29;
            this.setspeedUpDown.Value = new decimal(new int[] {
            980,
            0,
            0,
            0});
            this.setspeedUpDown.ValueChanged += new System.EventHandler(this.setSpeedUpDown_ValueChanged);
            // 
            // setMot1UpDown
            // 
            this.setMot1UpDown.Increment = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.setMot1UpDown.Location = new System.Drawing.Point(714, 83);
            this.setMot1UpDown.Maximum = new decimal(new int[] {
            500,
            0,
            0,
            0});
            this.setMot1UpDown.Minimum = new decimal(new int[] {
            500,
            0,
            0,
            -2147483648});
            this.setMot1UpDown.Name = "setMot1UpDown";
            this.setMot1UpDown.Size = new System.Drawing.Size(50, 20);
            this.setMot1UpDown.TabIndex = 30;
            this.setMot1UpDown.ValueChanged += new System.EventHandler(this.setMot1UpDown_ValueChanged);
            // 
            // setMot2UpDown
            // 
            this.setMot2UpDown.Increment = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.setMot2UpDown.Location = new System.Drawing.Point(714, 109);
            this.setMot2UpDown.Maximum = new decimal(new int[] {
            500,
            0,
            0,
            0});
            this.setMot2UpDown.Minimum = new decimal(new int[] {
            500,
            0,
            0,
            -2147483648});
            this.setMot2UpDown.Name = "setMot2UpDown";
            this.setMot2UpDown.Size = new System.Drawing.Size(50, 20);
            this.setMot2UpDown.TabIndex = 31;
            this.setMot2UpDown.ValueChanged += new System.EventHandler(this.setMot2UpDown_ValueChanged);
            // 
            // setMot3UpDown
            // 
            this.setMot3UpDown.Increment = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.setMot3UpDown.Location = new System.Drawing.Point(714, 135);
            this.setMot3UpDown.Maximum = new decimal(new int[] {
            500,
            0,
            0,
            0});
            this.setMot3UpDown.Minimum = new decimal(new int[] {
            500,
            0,
            0,
            -2147483648});
            this.setMot3UpDown.Name = "setMot3UpDown";
            this.setMot3UpDown.Size = new System.Drawing.Size(50, 20);
            this.setMot3UpDown.TabIndex = 32;
            this.setMot3UpDown.ValueChanged += new System.EventHandler(this.setMot3UpDown_ValueChanged);
            // 
            // setMot4UpDown
            // 
            this.setMot4UpDown.Increment = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.setMot4UpDown.Location = new System.Drawing.Point(714, 161);
            this.setMot4UpDown.Maximum = new decimal(new int[] {
            500,
            0,
            0,
            0});
            this.setMot4UpDown.Minimum = new decimal(new int[] {
            500,
            0,
            0,
            -2147483648});
            this.setMot4UpDown.Name = "setMot4UpDown";
            this.setMot4UpDown.Size = new System.Drawing.Size(50, 20);
            this.setMot4UpDown.TabIndex = 33;
            this.setMot4UpDown.ValueChanged += new System.EventHandler(this.setMot4UpDown_ValueChanged);
            // 
            // SetSpeed
            // 
            this.SetSpeed.AutoSize = true;
            this.SetSpeed.Location = new System.Drawing.Point(770, 57);
            this.SetSpeed.Name = "SetSpeed";
            this.SetSpeed.Size = new System.Drawing.Size(54, 13);
            this.SetSpeed.TabIndex = 34;
            this.SetSpeed.Text = "SetSpeed";
            // 
            // Mot1Speed
            // 
            this.Mot1Speed.AutoSize = true;
            this.Mot1Speed.Location = new System.Drawing.Point(770, 83);
            this.Mot1Speed.Name = "Mot1Speed";
            this.Mot1Speed.Size = new System.Drawing.Size(62, 13);
            this.Mot1Speed.TabIndex = 35;
            this.Mot1Speed.Text = "Mot1Speed";
            // 
            // Mot2Speed
            // 
            this.Mot2Speed.AutoSize = true;
            this.Mot2Speed.Location = new System.Drawing.Point(770, 109);
            this.Mot2Speed.Name = "Mot2Speed";
            this.Mot2Speed.Size = new System.Drawing.Size(62, 13);
            this.Mot2Speed.TabIndex = 36;
            this.Mot2Speed.Text = "Mot2Speed";
            // 
            // Mot3Speed
            // 
            this.Mot3Speed.AutoSize = true;
            this.Mot3Speed.Location = new System.Drawing.Point(770, 135);
            this.Mot3Speed.Name = "Mot3Speed";
            this.Mot3Speed.Size = new System.Drawing.Size(62, 13);
            this.Mot3Speed.TabIndex = 37;
            this.Mot3Speed.Text = "Mot3Speed";
            // 
            // Mot4Speed
            // 
            this.Mot4Speed.AutoSize = true;
            this.Mot4Speed.Location = new System.Drawing.Point(770, 161);
            this.Mot4Speed.Name = "Mot4Speed";
            this.Mot4Speed.Size = new System.Drawing.Size(62, 13);
            this.Mot4Speed.TabIndex = 38;
            this.Mot4Speed.Text = "Mot4Speed";
            // 
            // SelectPid_comboBox
            // 
            this.SelectPid_comboBox.FormattingEnabled = true;
            this.SelectPid_comboBox.Items.AddRange(new object[] {
            "Pitch",
            "Roll",
            "Yaw",
            "PitchRot",
            "RollRot",
            "YawRot"});
            this.SelectPid_comboBox.Location = new System.Drawing.Point(714, 195);
            this.SelectPid_comboBox.Name = "SelectPid_comboBox";
            this.SelectPid_comboBox.Size = new System.Drawing.Size(100, 21);
            this.SelectPid_comboBox.TabIndex = 39;
            this.SelectPid_comboBox.SelectedIndexChanged += new System.EventHandler(this.SelectPid_comboBox_SelectedIndexChanged);
            // 
            // Form1
            // 
            this.AutoScroll = true;
            this.ClientSize = new System.Drawing.Size(913, 452);
            this.Controls.Add(this.SelectPid_comboBox);
            this.Controls.Add(this.Mot4Speed);
            this.Controls.Add(this.Mot3Speed);
            this.Controls.Add(this.Mot2Speed);
            this.Controls.Add(this.Mot1Speed);
            this.Controls.Add(this.SetSpeed);
            this.Controls.Add(this.setMot4UpDown);
            this.Controls.Add(this.setMot3UpDown);
            this.Controls.Add(this.setMot2UpDown);
            this.Controls.Add(this.setMot1UpDown);
            this.Controls.Add(this.setspeedUpDown);
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
            this.Controls.Add(this.kdsend_button);
            this.Controls.Add(this.kisend_button);
            this.Controls.Add(this.kpsend_button);
            this.Controls.Add(this.pidkd_textBox);
            this.Controls.Add(this.pidki_textBox);
            this.Controls.Add(this.pidkp_textBox);
            this.Controls.Add(this.tableLayoutPanel1);
            this.KeyPreview = true;
            this.Name = "Form1";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.Form1_Load);
            this.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.Form1_KeyPress);
            this.tableLayoutPanel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.accx)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.accy)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.accz)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.setspeedUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.setMot1UpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.setMot2UpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.setMot3UpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.setMot4UpDown)).EndInit();
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

            gamepadTimer = new AccurateTimer(this, new Action(Tick), 100);
        }

        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            gamepadTimer.Stop();
        }


        private void tableLayoutPanel1_Paint(object sender, PaintEventArgs e)
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
            else if ("TakeOff" == boxValue)
            {
                UserSentUInt32Value(7, TUserInput.DRONESTATE);
            }
            else if ("FlyManual" == boxValue)
            {
                UserSentUInt32Value(8, TUserInput.DRONESTATE);
            }
            else if ("FlyAssisted" == boxValue)
            {
                UserSentUInt32Value(9, TUserInput.DRONESTATE);
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

            if("NoMes" == boxValue)
            {
                UserSentUInt32Value(0xF0, TUserInput.MESSEL);
            }
            else if ("AccelMes" == boxValue)
            {
                UserSentUInt32Value(0xF1, TUserInput.MESSEL);
            }
            else if ("GyroMes" == boxValue)
            {
                UserSentUInt32Value(0xF2, TUserInput.MESSEL);
            }
            else if ("MagnMes" == boxValue)
            {
                UserSentUInt32Value(0xF3, TUserInput.MESSEL);
            }
            else if ("EulerMes" == boxValue)
            {
                UserSentUInt32Value(0xF4, TUserInput.MESSEL);
            }
            else if ("MixedMes" == boxValue)
            {
                UserSentUInt32Value(0xF5, TUserInput.MESSEL);
            }
            else if ("AttitudeLogMes" == boxValue)
            {
                UserSentUInt32Value(0xF7, TUserInput.MESSEL);
            }
            else { }
        }

        private void setSpeedUpDown_ValueChanged(object sender, EventArgs e)
        {
            UInt32 val = Convert.ToUInt32(this.setspeedUpDown.Value);
            UserSentUInt32Value(val, TUserInput.MOTSPEED);
        }

        private void setMot1UpDown_ValueChanged(object sender, EventArgs e)
        {
            Int32 val = Convert.ToInt32(this.setMot1UpDown.Value);
            UserSentInt32Value(val, TUserInput.MOT1SPEED);
        }

        private void setMot2UpDown_ValueChanged(object sender, EventArgs e)
        {
            Int32 val = Convert.ToInt32(this.setMot2UpDown.Value);
            UserSentInt32Value(val, TUserInput.MOT2SPEED);
        }

        private void setMot3UpDown_ValueChanged(object sender, EventArgs e)
        {
            Int32 val = Convert.ToInt32(this.setMot3UpDown.Value);
            UserSentInt32Value(val, TUserInput.MOT3SPEED);
        }

        private void setMot4UpDown_ValueChanged(object sender, EventArgs e)
        {
            Int32 val = Convert.ToInt32(this.setMot4UpDown.Value);
            UserSentInt32Value(val, TUserInput.MOT4SPEED);
        }

        private void SelectPid_comboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            string boxValue = Convert.ToString(this.SelectPid_comboBox.SelectedItem);

            if ("Pitch" == boxValue)
            {
                UserSentUInt32Value(SELECTEDPID_PITCH, TUserInput.SELECTPID);
            }
            else if ("Roll" == boxValue)
            {
                UserSentUInt32Value(SELECTEDPID_ROLL, TUserInput.SELECTPID);
            }
            else if ("Yaw" == boxValue)
            {
                UserSentUInt32Value(SELECTEDPID_YAW, TUserInput.SELECTPID);
            }
            else if ("PitchRot" == boxValue)
            {
                UserSentUInt32Value(SELECTEDPID_PITCHROT, TUserInput.SELECTPID);
            }
            else if ("RollRot" == boxValue)
            {
                UserSentUInt32Value(SELECTEDPID_ROLLROT, TUserInput.SELECTPID);
            }
            else if ("YawRot" == boxValue)
            {
                UserSentUInt32Value(SELECTEDPID_YAWROT, TUserInput.SELECTPID);
            }
            else { }
        }
    }
}