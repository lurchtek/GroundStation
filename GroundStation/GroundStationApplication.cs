using System;
using System.IO.Ports;
using System.Windows.Forms;

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

        const int RawSensorData_MesId = 0xF0;
        const int EulerAngles_MesId = 0xF1;
        const int StartByte = 0x7E;
        const int EndByte = 0x81;
        const int LenOfRawSensorDataMes = 43;

        const int StartByteIdx = 0;
        const int CommandIdx = 1;
        const int LenIdx = 2;
        const int EndByteIdx = 42;

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
        private byte[] mesBuffer = new byte[LenOfRawSensorDataMes];

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
                    if (RawSensorData_MesId == byteFromBuf)
                    {
                        buffer[StartByteIdx] = (byte)StartByte;
                        buffer[CommandIdx] = (byte)RawSensorData_MesId;
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

                        if (LenOfRawSensorDataMes == bytesReadFromMes)
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
                        }
                        byteFromBuf = _serialPort.ReadByte();
                    }
                    break;
            }
        }
    
        private int CopyBytesToMesBuffer(byte[] buffer, int bytesRead)
        {
            int index;

            for (index = 0; (index < bytesRead) && (bytesReadFromMes < LenOfRawSensorDataMes); index++, bytesReadFromMes++)
            {
                mesBuffer[bytesReadFromMes] = buffer[index];
            }
            //return remaining bytes
            return (bytesRead - index);
        }
    }

    class GroundStationExecute
    {
        const int RawSensorData_MesId = 0xF0;
        const int EulerAngles_MesId = 0xF1;
        const int LenOfRawSensorDataMes = 43;

        const int CommandIdx = 1;
        const int TimestampIdx = 2;
        const int AccxIdx = 6;
        const int AccyIdx = 10;
        const int AcczIdx = 14;
        const int GyroIdx = 18;
        const int MagnetomIdx = 30;

        static Form1 _dataGraph = new Form1();

        public void WritePlotData(float x, float y, float z)
        {
            _dataGraph.AddData(x, y, z);
        }

        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            
            SerialPortInterface _serialPortInterface = new SerialPortInterface();
            _serialPortInterface.MessageReadCallback = new SerialPortInterface.MessageReadHandler(InterpretMessage);

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

        private static void InterpretMessage(byte[] dataBuffer, byte mesID)
        {
            int miliseconds = 0;
            int seconds;
            int minutes;
            float accx;
            float accy;
            float accz;

            if ( RawSensorData_MesId == dataBuffer[CommandIdx] )
            {
                //Endian
                miliseconds = BitConverter.ToUInt16(dataBuffer, TimestampIdx);
                minutes = dataBuffer[TimestampIdx + 3];
                seconds = dataBuffer[TimestampIdx + 2];
                accx = BitConverter.ToSingle(dataBuffer, AccxIdx);
                accy = BitConverter.ToSingle(dataBuffer, AccyIdx);
                accz = BitConverter.ToSingle(dataBuffer, AcczIdx);

                _dataGraph.AddData(accx, accy, accz);
                Console.WriteLine(minutes);
                Console.WriteLine(seconds);
                Console.WriteLine(accz);
            }
            else
            {
                Console.WriteLine("Wrong data read!");
            }
        }
    }
}