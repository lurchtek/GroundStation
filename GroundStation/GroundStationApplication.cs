using System;
using System.IO;
using System.IO.Ports;
using System.Windows.Forms;
using System.Text;

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

        const int AccelSensorData_MesId = 0xF0;
        const int GyroSensorData_MesId = 0xF1;
        const int MagnSensorData_MesId = 0xF2;
        const int EulerAnglesData_MesId = 0xF3;
        const int MixedData_MesId = 0xF4;
        const int StartByte = 0x7E;
        const int EndByte = 0x81;
        const int LenOfDataMes = 16;

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
            //_serialPort.Close();
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
                         (MixedData_MesId       == byteFromBuf)
                       )
                    {
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
                        Console.WriteLine("Wrong data read!");
                    }
                    break;
                case TReadingState.ReadingPayload:
                    while(-1 != byteFromBuf)
                    {
                        buffer[0] = (byte)byteFromBuf;
                        CopyBytesToMesBuffer(buffer, 1);

                        if (LenOfDataMes == bytesReadFromMes)
                        {
                            if (EndByte == mesBuffer[EndByteIdx])
                            {
                                MessageReadCallback( mesBuffer, mesBuffer[CommandIdx]);
                            }
                            else
                            {
                                Console.WriteLine("Wrong Stop Condition!");
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
        const int AccelSensorData_MesId = 0xF0;
        const int GyroSensorData_MesId = 0xF1;
        const int MagnSensorData_MesId = 0xF2;
        const int EulerAnglesData_MesId = 0xF3;
        const int MixedData_MesId = 0xF4;

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
        const Boolean dataLoggingEnabled = false;

        static UInt32 samplesCollected = 0;
        static StringBuilder csv = new StringBuilder();
        static Form1 _dataGraph = new Form1();
        static SerialPortInterface _serialPortInterface = new SerialPortInterface();
        
        public void WritePlotData(float x, float y, float z)
        {
            _dataGraph.AddData(x, y, z);
        }

        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            _serialPortInterface.MessageReadCallback = new SerialPortInterface.MessageReadHandler(InterpretMessage);
            _dataGraph.UserSentFloatValue = new Form1.UserInputFloatDelegate(SendFloatCommandToDrone);
            _dataGraph.UserSentUInt32Value = new Form1.UserInputUInt32Delegate(SendUInt32CommandToDrone);

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
            
            Application.Run(_dataGraph);
            //Console.ReadKey();
            _serialPortInterface.Close();
        }

        unsafe private static void SendFloatCommandToDrone(float value, Form1.TUserInput ID)
        {
            byte[] data = new byte[sizeof(float) + 1];
            data[0] = ((byte*)(&value))[0];
            data[1] = ((byte*)(&value))[1];
            data[2] = ((byte*)(&value))[2];
            data[3] = ((byte*)(&value))[3];
            data[4] = (byte)ID;
            _serialPortInterface.WriteData( data, (sizeof(float) + 1) );
        }

        unsafe private static void SendUInt32CommandToDrone(UInt32 value, Form1.TUserInput ID)
        {
            byte[] data = new byte[sizeof(UInt32) + 1];
            data[0] = ((byte*)(&value))[0];
            data[1] = ((byte*)(&value))[1];
            data[2] = ((byte*)(&value))[2];
            data[3] = ((byte*)(&value))[3];
            data[4] = (byte)ID;
            _serialPortInterface.WriteData(data, (sizeof(UInt32) + 1));
        }

        private static void InterpretMessage(byte[] dataBuffer, byte mesID)
        {
            //int miliseconds = 0;
            //int seconds;
            //int minutes;
            float accx;
            float accy;
            float accz;
            float gyrox;
            float gyroy;
            float gyroz;
            float magnx;
            float magny;
            float magnz;
            float roll;
            float pitch;
            float yaw;
            float signal1;
            float signal2;
            float signal3;
            string first;
            string second;
            string third;

            if (AccelSensorData_MesId == dataBuffer[CommandIdx] )
            {
                //miliseconds = BitConverter.ToUInt16(dataBuffer, TimestampIdx);
                //minutes = dataBuffer[TimestampIdx + 3];
                //seconds = dataBuffer[TimestampIdx + 2];
                accx = BitConverter.ToSingle(dataBuffer, AccxIdx);
                accy = BitConverter.ToSingle(dataBuffer, AccyIdx);
                accz = BitConverter.ToSingle(dataBuffer, AcczIdx);

                _dataGraph.AddData(accx, accy, accz);
            }
            else if (GyroSensorData_MesId == dataBuffer[CommandIdx])
            {
                //miliseconds = BitConverter.ToUInt16(dataBuffer, TimestampIdx);
                //minutes = dataBuffer[TimestampIdx + 3];
                //seconds = dataBuffer[TimestampIdx + 2];
                gyrox = ((BitConverter.ToSingle(dataBuffer, GyroxIdx)) / 32767.0f) * 1100.0f;
                gyroy = ((BitConverter.ToSingle(dataBuffer, GyroyIdx)) / 32767.0f) * 1100.0f;
                gyroz = ((BitConverter.ToSingle(dataBuffer, GyrozIdx)) / 32767.0f) * 1100.0f;

                _dataGraph.AddData(gyrox, gyroy, gyroz);
            }
            else if (MagnSensorData_MesId == dataBuffer[CommandIdx])
            {
                //miliseconds = BitConverter.ToUInt16(dataBuffer, TimestampIdx);
                //minutes = dataBuffer[TimestampIdx + 3];
                //seconds = dataBuffer[TimestampIdx + 2];
                magnx = ((BitConverter.ToSingle(dataBuffer, MagnxIdx)) / 1.3f) * 1100.0f;
                magny = ((BitConverter.ToSingle(dataBuffer, MagnyIdx)) / 1.3f) * 1100.0f;
                magnz = ((BitConverter.ToSingle(dataBuffer, MagnzIdx)) / 1.3f) * 1100.0f;

                _dataGraph.AddData(magnx, magny, magnz);
            }
            else if (EulerAnglesData_MesId == dataBuffer[CommandIdx])
            {
                //miliseconds = BitConverter.ToUInt16(dataBuffer, TimestampIdx);
                //minutes = dataBuffer[TimestampIdx + 3];
                //seconds = dataBuffer[TimestampIdx + 2];
                roll = (BitConverter.ToSingle(dataBuffer, RollIdx)) / 180.0f * 1100.0f;
                pitch = -((BitConverter.ToSingle(dataBuffer, PitchIdx)) / 90.0f * 1100.0f);
                yaw = (BitConverter.ToSingle(dataBuffer, YawIdx)) / 180.0f * 1100.0f;

                _dataGraph.AddData(roll, pitch, yaw);
            }
            else if (MixedData_MesId == dataBuffer[CommandIdx])
            {
                signal1 = BitConverter.ToSingle(dataBuffer, Signal1Idx);
                signal2 = BitConverter.ToSingle(dataBuffer, Signal2Idx);
                signal3 = BitConverter.ToSingle(dataBuffer, Signal3Idx);
                if (true == dataLoggingEnabled)
                {
                    if (1000 > samplesCollected)
                    {
                        first = Convert.ToString(signal1);
                        second = Convert.ToString(signal2);
                        third = Convert.ToString(signal3);
                        samplesCollected++;
                        var newLine = string.Format("{0},{1},{2}", first, second, third);
                        csv.AppendLine(newLine);
                        if (1000 == samplesCollected)
                        {
                            File.WriteAllText("C:/Users/Lurch/GIT/Quad/tools/CollectedDataSamples/newData.csv", csv.ToString());
                        }
                    }
                }

                _dataGraph.AddData(signal1, signal2, signal3);
                //Console.WriteLine(minutes);
                //Console.WriteLine(seconds);
                //Console.WriteLine(accz);
            }
            else
            {
                Console.WriteLine("Wrong data read!");
            }
        }
    }
}