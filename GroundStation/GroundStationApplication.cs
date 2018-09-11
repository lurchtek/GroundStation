using System;
using System.IO;
using System.IO.Ports;
using System.Windows.Forms;
using System.Threading;
using System.Text;
using System.Collections;

namespace GroundStationApplication
{
    /// <summary> 
    /// Interfaces with a serial port. There should only be one instance 
    /// of this class for each serial port to be used. 
    /// </summary> 
    public class SerialPortInterface
    {
        private enum TReadingState
        {
            WaitingForStart,
            CheckValidID,
            ReadingPayload
        };
        public delegate void MessageReadHandler(byte[] dataBuffer, byte mesID);

        const int NoMessage_MesId = 0xF0;
        const int AccelSensorData_MesId = 0xF1;
        const int GyroSensorData_MesId = 0xF2;
        const int MagnSensorData_MesId = 0xF3;
        const int EulerAnglesData_MesId = 0xF4;
        const int MixedData_MesId = 0xF5;
        const int PidData_MesId = 0xF6;
        const int AttitudeLog_MesId = 0xF7;
        const int Empty_MesId = 0xF8;
        public const int StartByte = 0x7E;
        public const int EndByte = 0x81;
        const int LenOfDataMes = 16;
        const int LenOfEmptyMes = 4;

        const int StartByteIdx = 0;
        const int CommandIdx = 1;
        const int LenIdx = 2;
        const int EndByteIdx = 15;

        private SerialPort _serialPort = new SerialPort();
        private int _baudRate = 57600;
        private int _dataBits = 8;
        private Handshake _handshake = Handshake.None;
        private Parity _parity = Parity.None;
        private string _portName = "COM4";
        private StopBits _stopBits = StopBits.One;

        private TReadingState ReadingState = TReadingState.WaitingForStart;
        private int bytesReadFromMes = 0;
        private int lengthOfCurrentMessage = 0;
        //Initialize a buffer to hold the received data 
        private byte[] mesBuffer = new byte[LenOfDataMes];

        public int BaudRate { get { return _baudRate; } set { _baudRate = value; } }
        public int DataBits { get { return _dataBits; } set { _dataBits = value; } }
        public Handshake Handshake { get { return _handshake; } set { _handshake = value; } }
        public Parity Parity { get { return _parity; } set { _parity = value; } }
        public string PortName { get { return _portName; } set { _portName = value; } }


        public bool Open()
        {
            try
            {
                _serialPort.BaudRate = _baudRate;
                _serialPort.DataBits = _dataBits;
                _serialPort.Handshake = _handshake;
                _serialPort.Parity = _parity;
                _serialPort.PortName = _portName;
                _serialPort.StopBits = _stopBits;
                _serialPort.DataReceived += new SerialDataReceivedEventHandler(_serialPort_DataReceived);
                _serialPort.Open();
            }
            catch { return false; }
            return true;
        }

        public void Close()
        {
            _serialPort.Close();
        }

        public void WriteData(byte[] data, UInt16 size)
        {
            _serialPort.Write(data, 0, size);
        }

        public MessageReadHandler MessageReadCallback; 

        private void _serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            byte[] buffer = new byte[2];
            //There is no accurate method for checking how many bytes are read 
            //unless you check the return from the Read method 
            int byteFromBuf = 0;
            byteFromBuf = _serialPort.ReadByte();

            //Console.WriteLine("event handler started");

            switch (ReadingState)
            {
                case TReadingState.WaitingForStart:
                    while (-1 != byteFromBuf)
                    {    
                        if (StartByte == byteFromBuf)
                        {
                            byteFromBuf = _serialPort.ReadByte();
                            ReadingState = TReadingState.CheckValidID;
                            goto case TReadingState.CheckValidID;
                        }

                        byteFromBuf = _serialPort.ReadByte();
                    }
                    break;
                case TReadingState.CheckValidID:
                    if ( (AccelSensorData_MesId == byteFromBuf) ||
                         (GyroSensorData_MesId  == byteFromBuf) ||
                         (MagnSensorData_MesId  == byteFromBuf) ||
                         (EulerAnglesData_MesId == byteFromBuf) ||
                         (MixedData_MesId       == byteFromBuf) ||
                         (PidData_MesId         == byteFromBuf) ||
                         (AttitudeLog_MesId     == byteFromBuf) ||
                         (Empty_MesId           == byteFromBuf)
                       )
                    {
                        if(Empty_MesId == byteFromBuf)
                        {
                            lengthOfCurrentMessage = LenOfEmptyMes;
                        }
                        else
                        {
                            lengthOfCurrentMessage = LenOfDataMes;
                        }
                        buffer[StartByteIdx] = (byte)StartByte;
                        buffer[CommandIdx] = (byte)byteFromBuf;
                        CopyBytesToMesBuffer(buffer, 2);
                        byteFromBuf = _serialPort.ReadByte();
                        //Console.WriteLine("Message reading started");
                        ReadingState = TReadingState.ReadingPayload;
                        goto case TReadingState.ReadingPayload;
                    }
                    else
                    {
                        goto case TReadingState.WaitingForStart;
                        //Console.WriteLine("Wrong data read!");
                    }
                case TReadingState.ReadingPayload:
                    while(-1 != byteFromBuf)
                    {
                        buffer[0] = (byte)byteFromBuf;
                        CopyBytesToMesBuffer(buffer, 1);

                        if (lengthOfCurrentMessage == bytesReadFromMes)
                        {
                            if (EndByte == mesBuffer[lengthOfCurrentMessage - 1])
                            {
                                MessageReadCallback( mesBuffer, mesBuffer[CommandIdx]);
                            }
                            else
                            {
                                //Console.WriteLine("Wrong Stop Condition!");
                            }

                            bytesReadFromMes = 0;
                            ReadingState = TReadingState.WaitingForStart;
                            byteFromBuf = _serialPort.ReadByte();
                            goto case TReadingState.WaitingForStart;
                        }
                        byteFromBuf = _serialPort.ReadByte();
                    }
                    break;
            }
        }
    
        private int CopyBytesToMesBuffer(byte[] buffer, int bytesRead)
        {
            int index;

            for (index = 0; (index < bytesRead) && (bytesReadFromMes < LenOfDataMes); index++, bytesReadFromMes++)
            {
                mesBuffer[bytesReadFromMes] = buffer[index];
            }
            //return remaining bytes
            return (bytesRead - index);
        }
    }

    class GroundStationExecute
    {
        const int NoMessage_MesId = 0xF0;
        const int AccelSensorData_MesId = 0xF1;
        const int GyroSensorData_MesId = 0xF2;
        const int MagnSensorData_MesId = 0xF3;
        const int EulerAnglesData_MesId = 0xF4;
        const int MixedData_MesId = 0xF5;
        const int PidData_MesId = 0xF6;
        const int AttitudeLog_MesId = 0xF7;
        const int Empty_MesId = 0xF8;

        const int CommandIdx = 1;
        const int TimestampIdx = 14;
        const int AccxIdx = 2;
        const int AccyIdx = 6;
        const int AcczIdx = 10;
        const int GyroxIdx = 2;
        const int GyroyIdx = 6;
        const int GyrozIdx = 10;
        const int MagnxIdx = 2;
        const int MagnyIdx = 6;
        const int MagnzIdx = 10;
        const int RollIdx = 2;
        const int PitchIdx = 6;
        const int YawIdx = 10;
        const int Signal1Idx = 2;
        const int Signal2Idx = 6;
        const int Signal3Idx = 10;
        const int PitchRefIdx = 2;
        const int RollRefIdx = 4;
        const int PitchFeedbackIdx = 6;
        const int RollFeedbackIdx = 8;
        const int PitchOutputIdx = 10;
        const int RollOutputIdx = 12;
        const int PidP_Idx = 2;
        const int PidI_Idx = 6;
        const int PidD_Idx = 10;

        const int TxMessageSize = 8;
        const int TxMaxMessages = 10;

        const Boolean dataLoggingEnabled = false;

        static byte[] IdleMes = { (byte)SerialPortInterface.StartByte, (byte)0, (byte)0, (byte)0, (byte)0, (byte)Form1.TUserInput.IDLEMES, (byte)0, (byte)SerialPortInterface.EndByte };
        static byte[] messageBuffer = new byte[TxMessageSize * TxMaxMessages];

        static UInt32 samplesCollected = 0;
        static UInt32 accelRawSamplesCollected = 0;
        static UInt32 accelFiltSamplesCollected = 0;
        static UInt32 eulerSamplesCollected = 0;
        static StringBuilder csv1 = new StringBuilder();
        static StringBuilder csv2 = new StringBuilder();
        static StringBuilder csv3 = new StringBuilder();
        static StringBuilder csv4 = new StringBuilder();
        static StringBuilder csv5 = new StringBuilder();
        static Form1 _dataGraph = new Form1();
        static SerialPortInterface _serialPortInterface = new SerialPortInterface();
        private static Object thisLock = new Object();
        static AccurateTimer transmissionTimer;
        static UInt32 countL = 0;
        static UInt32 idleCounter = 0;

        static Queue messages = new Queue();

        private void WritePlotData(float x, float y, float z)
        {
            _dataGraph.AddData(x, y, z);
        }

        static void Main(string[] args)
        {
            //Application.SetCompatibleTextRenderingDefault(false);
            Application.EnableVisualStyles();

            Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            _serialPortInterface.MessageReadCallback = new SerialPortInterface.MessageReadHandler(InterpretMessage);
            _dataGraph.UserSentFloatValue = new Form1.UserInputFloatDelegate(SendFloatCommandToDrone);
            _dataGraph.UserSentUInt32Value = new Form1.UserInputUInt32Delegate(SendUInt32CommandToDrone);
            _dataGraph.UserSentInt32Value = new Form1.UserInputInt32Delegate(SendInt32CommandToDrone);

            bool result;

            result = _serialPortInterface.Open();

            if (result == false)
            {
                Console.WriteLine("port openning failed!");
            }
            else
            {
                Console.WriteLine("port openning successful!");
            }

            transmissionTimer = new AccurateTimer(_dataGraph, new Action(SendMessages), 10);

            Application.Run(_dataGraph);
            _serialPortInterface.Close();
        }

        static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            MessageBox.Show(e.Exception.Message, "Unhandled Thread Exception");
            // here you can log the exception ...
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            MessageBox.Show((e.ExceptionObject as Exception).Message, "Unhandled UI Exception");
            // here you can log the exception ...
        }

        static void SendMessages()//object sender, EventArgs e)
        {
            int currentMesBufIndex;
            int messageCount = messages.Count;
            int bufferLength = TxMessageSize * messageCount;
            byte[] singleMesBuf = new byte[TxMessageSize];
            byte[] messageBuf;

            idleCounter++;

            if (80 > idleCounter)
            {
                if (0 < bufferLength)
                {
                    messageBuf = new byte[bufferLength];
                    while (0 < messageCount)
                    {
                        currentMesBufIndex = (bufferLength - (messageCount * TxMessageSize));
                        singleMesBuf = (byte[])messages.Dequeue();
                        messageCount = messages.Count;
                        singleMesBuf[6] = (byte)messageCount;
                        Array.ConstrainedCopy(singleMesBuf, 0, messageBuf, currentMesBufIndex, TxMessageSize);
                        _serialPortInterface.WriteData(messageBuf, (UInt16)bufferLength);
                    }
                }
            }
            else
            {
                messageBuf = new byte[TxMessageSize];
                bufferLength = TxMessageSize;
                Array.Copy(IdleMes, messageBuf, bufferLength);
                _serialPortInterface.WriteData(messageBuf, (UInt16)bufferLength);
                idleCounter = 0;
            }

        }

        unsafe private static void SendFloatCommandToDrone(float value, Form1.TUserInput ID)
        {
            byte[] messageBuf = new byte[TxMessageSize];
            lock (GroundStationExecute.thisLock)
            {
                messageBuf[0] = (byte)SerialPortInterface.StartByte;
                messageBuf[1] = ((byte*)(&value))[0];
                messageBuf[2] = ((byte*)(&value))[1];
                messageBuf[3] = ((byte*)(&value))[2];
                messageBuf[4] = ((byte*)(&value))[3];
                messageBuf[5] = (byte)ID;
                messageBuf[7] = (byte)SerialPortInterface.EndByte;
                if (15 > messages.Count)
                {
                    messages.Enqueue(messageBuf);
                }
                else { countL++; }
            }
        }

        unsafe private static void SendUInt32CommandToDrone(UInt32 value, Form1.TUserInput ID)
        {
            byte[] messageBuf = new byte[TxMessageSize];
            lock (GroundStationExecute.thisLock)
            {
                messageBuf[0] = (byte)SerialPortInterface.StartByte;
                messageBuf[1] = ((byte*)(&value))[0];
                messageBuf[2] = ((byte*)(&value))[1];
                messageBuf[3] = ((byte*)(&value))[2];
                messageBuf[4] = ((byte*)(&value))[3];
                messageBuf[5] = (byte)ID;
                messageBuf[7] = (byte)SerialPortInterface.EndByte;
                if (15 > messages.Count)
                {
                    messages.Enqueue(messageBuf);
                }
                else { countL++; }
            }
        }

        unsafe private static void SendInt32CommandToDrone(Int32 value, Form1.TUserInput ID)
        {
            byte[] messageBuf = new byte[TxMessageSize];
            lock (GroundStationExecute.thisLock)
            {
                messageBuf[0] = (byte)SerialPortInterface.StartByte;
                messageBuf[1] = ((byte*)(&value))[0];
                messageBuf[2] = ((byte*)(&value))[1];
                messageBuf[3] = ((byte*)(&value))[2];
                messageBuf[4] = ((byte*)(&value))[3];
                messageBuf[5] = (byte)ID;
                messageBuf[7] = (byte)SerialPortInterface.EndByte;
                if (15 > messages.Count)
                {
                    messages.Enqueue(messageBuf);
                }
                else { countL++; }
            }
        }

        private static void InterpretMessage(byte[] dataBuffer, byte mesID)
        {
            //int miliseconds = 0;
            //int seconds;
            //int minutes;
            float signal1;
            float signal2;
            float signal3;
            Int16 pitchRef;
            Int16 rollRef;
            Int16 pitchFeedback;
            Int16 rollFeedback;
            Int16 pitchOutput;
            Int16 rollOutput;
            string first;
            string second;
            string third;
            string fourth;
            string fifth;
            string sixth;
            string FileName1 = "C:/Users/Lurch/GIT/Quad/tools/CollectedDataSamples/MagnData.csv";
            string FileName2 = "C:/Users/Lurch/GIT/Quad/tools/CollectedDataSamples/AttitudeLog.csv";
            string FileName3 = "C:/Users/Lurch/GIT/Quad/tools/CollectedDataSamples/AccelFiltData.csv";
            string FileName4 = "C:/Users/Lurch/GIT/Quad/tools/CollectedDataSamples/EulerData.csv";
            string FileName5 = "C:/Users/Lurch/GIT/Quad/tools/CollectedDataSamples/AccelRawData.csv";
            string ptest = System.DateTime.Now.ToString("yyyyMMdd_hhmm");

            if (AccelSensorData_MesId == dataBuffer[CommandIdx])
            {
                //miliseconds = BitConverter.ToUInt16(dataBuffer, TimestampIdx);
                //minutes = dataBuffer[TimestampIdx + 3];
                //seconds = dataBuffer[TimestampIdx + 2];
                signal1 = BitConverter.ToSingle(dataBuffer, AccxIdx);
                signal2 = BitConverter.ToSingle(dataBuffer, AccyIdx);
                signal3 = BitConverter.ToSingle(dataBuffer, AcczIdx);

                if (true == dataLoggingEnabled)
                {
                    if (1000 > accelRawSamplesCollected)
                    {
                        first = Convert.ToString(signal1);
                        second = Convert.ToString(signal2);
                        third = Convert.ToString(signal3);
                        accelRawSamplesCollected++;
                        var newLine = string.Format("{0},{1},{2}", first, second, third);
                        csv1.AppendLine(newLine);
                        if (1000 == accelRawSamplesCollected)
                        {
                            FileName5 = FileName5.Replace(".csv", "_" + ptest + ".csv");
                            File.WriteAllText(FileName5, csv1.ToString());
                            csv1.Clear();
                        }
                    }
                }

                _dataGraph.RefreshDisplayedValues(signal1, signal2, signal3);
                _dataGraph.AddData(signal1, signal2, signal3);
            }
            else if (GyroSensorData_MesId == dataBuffer[CommandIdx])
            {
                //miliseconds = BitConverter.ToUInt16(dataBuffer, TimestampIdx);
                //minutes = dataBuffer[TimestampIdx + 3];
                //seconds = dataBuffer[TimestampIdx + 2];
                signal1 = BitConverter.ToSingle(dataBuffer, GyroxIdx);
                signal2 = BitConverter.ToSingle(dataBuffer, GyroyIdx);
                signal3 = BitConverter.ToSingle(dataBuffer, GyrozIdx);

                _dataGraph.RefreshDisplayedValues(signal1, signal2, signal3);

                signal1 = ((signal1) / 32767.0f) * 1100.0f;
                signal2 = ((signal2) / 32767.0f) * 1100.0f;
                signal3 = ((signal3) / 32767.0f) * 1100.0f;

                _dataGraph.AddData(signal1, signal2, signal3);
            }
            else if (MagnSensorData_MesId == dataBuffer[CommandIdx])
            {
                //miliseconds = BitConverter.ToUInt16(dataBuffer, TimestampIdx);
                //minutes = dataBuffer[TimestampIdx + 3];
                //seconds = dataBuffer[TimestampIdx + 2];
                signal1 = BitConverter.ToSingle(dataBuffer, MagnxIdx);
                signal2 = BitConverter.ToSingle(dataBuffer, MagnyIdx);
                signal3 = BitConverter.ToSingle(dataBuffer, MagnzIdx);

                if (true == dataLoggingEnabled)
                {
                    if (3000 > samplesCollected)
                    {
                        first = Convert.ToString(signal1);
                        second = Convert.ToString(signal2);
                        third = Convert.ToString(signal3);
                        samplesCollected++;
                        var newLine = string.Format("{0},{1},{2}", first, second, third);
                        csv2.AppendLine(newLine);
                        if (3000 == samplesCollected)
                        {
                            FileName1 = FileName1.Replace(".csv", "_" + ptest + ".csv");
                            File.WriteAllText(FileName1, csv2.ToString());
                            csv2.Clear();
                        }
                    }
                } 

                _dataGraph.RefreshDisplayedValues(signal1, signal2, signal3);

                signal1 = ((signal1)) * 30000.0f;
                signal2 = ((signal2)) * 30000.0f;
                signal3 = ((signal3)) * 30000.0f;
                _dataGraph.AddData(signal1, signal2, signal3);
            }
            else if (EulerAnglesData_MesId == dataBuffer[CommandIdx])
            {
                //miliseconds = BitConverter.ToUInt16(dataBuffer, TimestampIdx);
                //minutes = dataBuffer[TimestampIdx + 3];
                //seconds = dataBuffer[TimestampIdx + 2];
                signal1 = BitConverter.ToSingle(dataBuffer, RollIdx);
                signal2 = BitConverter.ToSingle(dataBuffer, PitchIdx);
                signal3 = BitConverter.ToSingle(dataBuffer, YawIdx);

                _dataGraph.RefreshDisplayedValues(signal1, signal2, signal3);

                if (true == dataLoggingEnabled)
                {
                    if (1000 > eulerSamplesCollected)
                    {
                        first = Convert.ToString(signal1);
                        second = Convert.ToString(signal2);
                        third = Convert.ToString(signal3);
                        eulerSamplesCollected++;
                        var newLine = string.Format("{0},{1},{2}", first, second, third);
                        csv3.AppendLine(newLine);
                        if (1000 == eulerSamplesCollected)
                        {
                            FileName4 = FileName4.Replace(".csv", "_" + ptest + ".csv");
                            File.WriteAllText(FileName4, csv3.ToString());
                            csv3.Clear();
                        }
                    }
                }

                signal1 = (signal1) / 180.0f * 1100.0f;
                signal2 = -((signal2) / 90.0f * 1100.0f);
                signal3 = (signal3) / 180.0f * 1100.0f;

                _dataGraph.AddData(signal1, signal2, signal3);
            }
            else if (MixedData_MesId == dataBuffer[CommandIdx])
            {
                signal1 = BitConverter.ToSingle(dataBuffer, Signal1Idx);
                signal2 = BitConverter.ToSingle(dataBuffer, Signal2Idx);
                signal3 = BitConverter.ToSingle(dataBuffer, Signal3Idx);

                _dataGraph.RefreshDisplayedValues(signal1, signal2, signal3);

                if (true == dataLoggingEnabled)
                {
                    if (1000 > accelFiltSamplesCollected)
                    {
                        first = Convert.ToString(signal1);
                        second = Convert.ToString(signal2);
                        third = Convert.ToString(signal3);
                        accelFiltSamplesCollected++;
                        var newLine = string.Format("{0},{1},{2}", first, second, third);
                        csv4.AppendLine(newLine);
                        if (1000 == accelFiltSamplesCollected)
                        {
                            FileName3 = FileName3.Replace(".csv", "_" + ptest + ".csv");
                            File.WriteAllText(FileName3, csv4.ToString());
                            csv4.Clear();
                        }
                    }
                }

                _dataGraph.AddData(signal1, signal2, signal3);
            }
            else if (AttitudeLog_MesId == dataBuffer[CommandIdx])
            {
                pitchRef = BitConverter.ToInt16(dataBuffer, PitchRefIdx);
                rollRef = BitConverter.ToInt16(dataBuffer, RollRefIdx);
                pitchFeedback = BitConverter.ToInt16(dataBuffer, PitchFeedbackIdx);
                rollFeedback = BitConverter.ToInt16(dataBuffer, RollFeedbackIdx);
                pitchOutput = BitConverter.ToInt16(dataBuffer, PitchOutputIdx);
                rollOutput = BitConverter.ToInt16(dataBuffer, RollOutputIdx);

                if (2000 > samplesCollected)
                {
                    first = Convert.ToString(pitchRef);
                    second = Convert.ToString(rollRef);
                    third = Convert.ToString(pitchFeedback);
                    fourth = Convert.ToString(rollFeedback);
                    fifth = Convert.ToString(pitchOutput);
                    sixth = Convert.ToString(rollOutput);
                    samplesCollected++;
                    var newLine = string.Format("{0},{1},{2},{3},{4},{5}", first, second, third, fourth, fifth, sixth);
                    csv5.AppendLine(newLine);
                    if (2000 == samplesCollected)
                    {
                        FileName2 = FileName2.Replace(".csv", "_" + ptest + ".csv");
                        File.WriteAllText(FileName2, csv5.ToString());
                        csv5.Clear();
                    }
                }
            }
            else if (PidData_MesId == dataBuffer[CommandIdx])
            {
                signal1 = BitConverter.ToSingle(dataBuffer, PidP_Idx);
                signal2 = BitConverter.ToSingle(dataBuffer, PidI_Idx);
                signal3 = BitConverter.ToSingle(dataBuffer, PidD_Idx);

                _dataGraph.Refresh_PidData(signal1, signal2, signal3);
            }
            else if(Empty_MesId == dataBuffer[CommandIdx])
            {

            }
            else
            {
                Console.WriteLine("Wrong data read!");
            }
        }
    }
}