using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.AccessControl;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Media;

namespace Arti.MVVM.Model
{
    public class SerialCommunicationModel : INotifyPropertyChanged 
    {

        Queue<Stream> readStreams = new Queue<Stream>();
        Stream readStream;

        private int card1ChannelCount;

        public int Card1ChannelCount
        {
            get
            {
                return card1ChannelCount;
            }
            set
            {
                if ( card1ChannelCount != value )
                {
                    card1ChannelCount = value;
                    OnPropertyChanged(nameof( Card1ChannelCount ) );
                }
            }
        }


        //ManualResetEvent readComplete = new ManualResetEvent( false );
        #region Properties
        /// <summary>
        /// Declaring variables needed to create a serial connection
        /// </summary>
        private System.Timers.Timer errorInspectorTimer; // To send error checks periodically
        private System.Timers.Timer card1InspectorTimer;
        private System.Timers.Timer card2InspectorTimer;
        private System.Timers.Timer card3InspectorTimer;
        private bool isInspectorOnWork;
        private string deviceID; // Unique ID for communication handshake
        private readonly SerialPort selectedSerialPort;
        // For collecting data from serial port
        private StringBuilder dataReceiverBuffer;
        private bool isCommunicationActive; // For the logic implemented in ProcessReceivedDataAsync
        private string selectedPortName;
        private readonly int [] baudRateList = { 110, 300, 600, 1200, 2400, 4800, 9600, 14400, 19200, 38400, 57600, 115200, 128000, 256000 };
        private int selectedBaudRate;
        private readonly Parity [] parityList = { Parity.Even, Parity.None, Parity.Odd, Parity.Mark, Parity.Space };
        private Parity selectedParity;
        private readonly StopBits [] stopBitList = { System.IO.Ports.StopBits.None, System.IO.Ports.StopBits.One, System.IO.Ports.StopBits.OnePointFive, System.IO.Ports.StopBits.Two };
        private StopBits selectedStopBit;
        private readonly Handshake [] handShakeList = { Handshake.None, Handshake.XOnXOff, Handshake.RequestToSend, Handshake.RequestToSendXOnXOff };
        private Handshake selectedHandShake;
        private int selectedReadTimeOut;
        private int selectedWriteTimeOut;
        private string serialDataReceived;
        public string SelectedPortName
        {
            get => selectedPortName;
            set => selectedPortName = value;
        }

        public SerialPort SelectedSerialPort => selectedSerialPort;

        public bool IsCommunicationActive
        {
            get => isCommunicationActive;
            set => isCommunicationActive = value;
        }
        public string DeviceID
        {
            get => deviceID;
            set => deviceID = value;
        }
        public int SelectedReadTimeOut
        {
            get => selectedReadTimeOut;
            set => selectedReadTimeOut = value;
        }
        public int SelectedWriteTimeOut
        {
            get => selectedWriteTimeOut;
            set => selectedWriteTimeOut = value;
        }

        public int [] BaudRateList => baudRateList;

        public Parity [] ParityList => parityList;

        public StopBits [] StopBitList => stopBitList;

        public Handshake [] HandShakeList => handShakeList;

        public int SelectedBaudRate
        {
            get => selectedBaudRate;
            set => selectedBaudRate = value;
        }
        public Parity SelectedParity
        {
            get => selectedParity;
            set => selectedParity = value;
        }
        public StopBits SelectedStopBit
        {
            get => selectedStopBit;
            set => selectedStopBit = value;
        }
        public Handshake SelectedHandShake
        {
            get => selectedHandShake;
            set => selectedHandShake = value;
        }

        public string SerialDataReceived
        {
            get => serialDataReceived;
            set
            {
                serialDataReceived = value;
                OnPropertyChanged(nameof(SerialDataReceived));
            }
        }
        public StringBuilder DataReceiverBuffer
        {
            get => dataReceiverBuffer;
            set => dataReceiverBuffer = value;
        }
        #endregion

        #region Constructor

        public SerialCommunicationModel (
            string deviceIDO, string portName, int baudRate,
            Parity parityC, StopBits stopBitsC,
            Handshake handShakeC, int readTimeOut, int writeTimeOut
            )
        {
            Card1ChannelCount = 0;
            selectedBaudRate = baudRate;
            selectedSerialPort = new SerialPort( portName, selectedBaudRate );
            deviceID = deviceIDO;
            selectedParity = parityC;
            selectedSerialPort.Parity = selectedParity;
            selectedStopBit = stopBitsC;
            selectedSerialPort.StopBits = selectedStopBit;
            selectedHandShake = handShakeC;
            selectedSerialPort.Handshake = SelectedHandShake;
            selectedReadTimeOut = readTimeOut;
            selectedSerialPort.ReadTimeout = selectedReadTimeOut;
            selectedWriteTimeOut = writeTimeOut;
            selectedSerialPort.WriteTimeout = selectedWriteTimeOut;
            dataReceiverBuffer = new StringBuilder();
            selectedSerialPort.DataReceived += async ( sender, e ) => await OnDataReceivedAsync();
            selectedSerialPort.Open();
            //SendChannelInspection();
            //StartInspecting();
            IsCommunicationActive = false;
            Trace.WriteLine($"Serialport opened : deviceID :{DeviceID} || Comport :{SelectedPortName} || BaudRate :{SelectedBaudRate} || Parity :{SelectedParity}" +
                $" || Stopbit :{SelectedStopBit} || HandShake :{SelectedHandShake} || READTO :{SelectedReadTimeOut} || WRITETO :{SelectedWriteTimeOut}" );
        }
        #endregion

        #region Methods

        // Opens the serial port
        public void Open () => selectedSerialPort.Open();

        // Closes the serial port
        public void Close () => selectedSerialPort.Close();

        /// <summary>
        /// Sends data to listener
        /// </summary>
        /// <param name="data"> data to be send </param>
        public void SendData ( string data )
        {
            if ( selectedSerialPort.IsOpen )
            {
                selectedSerialPort.WriteLine( data );
            }
        }

        private async Task WhatToDoWithData (byte msgCode,byte [] dataToBeChecked)
        {
            if ( msgCode == 0xCA ) // Channel info response 6Byte each cards channel will be sent as 2 bytes
            {
                Card1ChannelCount = CalculateDataLength( dataToBeChecked [0], dataToBeChecked [1] );
            }
            else if ( msgCode == 0xCB ) // Error inspection response for each card  bit0(short circuit),bit1 (over current), bit2(voltage error), bit3-7 reserved => byte0-15 card1, 16-31 card2, 31-47 card3 
            {

            }
            else if ( msgCode == 0xCE ) // Card 1 is open/closed 2Byte lsb 1open 0 closed
            {

            }
            else if ( msgCode == 0xCF ) // Card 2 is open/closed 2Byte lsb 1open 0 closed
            {

            }
            else if ( msgCode == 0xD0 ) // Card 3 is open/closed 2Byte lsb 1open 0 closed
            {

            }
            else
            {
                SerialDataReceived = "UNEXPECTED RESPONSE";
            }

        }

        /// <summary>
        /// serialPort.DataReceived event has subscribed this in constructor
        /// it waits until serialPort.ReadExisting().ToString() then
        /// waits for ProcessReceivedDataAsync( hexData )
        /// </summary>
        /// <returns></returns>
        private async Task OnDataReceivedAsync ()
        {
            // Look BaseStream.ReadBytes() vs SerialPort.ReadBytes()
            readStream = new MemoryStream();
            readStream = selectedSerialPort.BaseStream;
            readStreams.Enqueue(readStream);
            await SplitDataIntoMeanings();
        }
        /// <summary>
        /// Decodes the received msg into meaningfull parts
        /// </summary>
        /// <returns></returns>
        private async Task SplitDataIntoMeanings ()
        {
            Stream current = readStreams.Dequeue();
            byte stx = 0x02;
            byte etx = 0x03;
            byte msgID;
            byte msgCode;
            byte dataLengthMSB;
            byte dataLengthLSB;
            int dataLength;
            byte [] data;
            byte checkSumMSB;
            byte checkSumLSB;
            byte [] checkSum;
            if ( (byte)current.ReadByte() == stx )
            {
                msgID = (byte)current.ReadByte();
                msgCode = (byte)current.ReadByte();
                dataLengthMSB = (byte)current.ReadByte();
                dataLengthLSB = (byte)current.ReadByte();
                dataLength = CalculateDataLength(dataLengthMSB, dataLengthLSB);
                data = new byte [dataLength];
                for ( int i = 0; i < dataLength; i++ )
                {
                    data [i] = (byte)current.ReadByte();
                }
                await WhatToDoWithData(msgCode,data);
                checkSumMSB = (byte)current.ReadByte();
                checkSumLSB = (byte)current.ReadByte();
                if ( (byte)current.ReadByte() == etx )
                {
                    SerialDataReceived = CreateMessage( msgID, msgCode, data, checkSumMSB, checkSumLSB );
                }
                else
                {
                    Trace.WriteLine("Etx not received");
                    current.Flush();
                }
            }
            else
            {
                Trace.WriteLine("Stx not received");
                current.Flush();
            }
        }

        /// <summary>
        /// To change Incoming data
        /// Incoming data 
        /// 1 byte stx(0x02) - 1byte ID - 1byte messagecode - 2byte dataLength - dataLength byte data - checksum - 0x03
        /// </summary>
        ///
        private async Task PrepareResponse (string msg)
        {

        }

        private async Task SendChannelInspection () // start of the conversation
        {
            if ( selectedSerialPort.IsOpen )
            {
                selectedSerialPort.BaseStream.Write( new byte [] { 0x02, 0x39, 0xC9, 0x30, 0x30, 0x46, 0x30, 0x03 }, 0, 8 );
            }
        }

        public void SendChannelOpenRequest (int cardIndex, int channelIndex)
        {
            byte [] dataToBeSent = CalculateOpenCommandToBeSent(cardIndex, channelIndex);
            selectedSerialPort.BaseStream.Write(dataToBeSent);
        }

        public void SendChannelCloseRequest ( int cardIndex, int channelIndex )
        {
            byte [] dataToBeSent = CalculateCloseCommandToBeSent( cardIndex, channelIndex );
            selectedSerialPort.BaseStream.Write( dataToBeSent );
        }

        private void SendCardErrorInspection ()
        {
            if ( selectedSerialPort.IsOpen )
            {
                selectedSerialPort.Write( new byte [] { 0x02, 0x39, 0xCB, 0x33, 0x30, 0x00, 0xF3, 0x03 }, 0, 8 );
            }
        }

        private void SendCard1OpenOrClosed ()
        {
            if ( selectedSerialPort.IsOpen )
            {
                selectedSerialPort.Write( new byte [] { 0x02, 0x39, 0xCE, 0x30, 0x30, 0x00, 0xF5, 0x03 }, 0, 8 );
            }
        }

        private void SendCard2OpenOrClosed ()
        {
            if ( selectedSerialPort.IsOpen )
            {
                selectedSerialPort.Write( new byte [] { 0x02, 0x39, 0xCF, 0x30, 0x30, 0x00, 0xF4, 0x03 }, 0, 8 );
            }
        }

        private void SendCard3OpenOrClosed ()
        {
            if ( selectedSerialPort.IsOpen )
            {
                selectedSerialPort.Write( new byte [] { 0x02, 0x39, 0xD0, 0x30, 0x30, 0x00, 0xEB, 0x03 }, 0, 8 );
            }
        }

        private void StartInspecting ()
        {

            Trace.WriteLine( "Start Inspecting" );
            if ( isInspectorOnWork )
                return;

            isInspectorOnWork = true;
            errorInspectorTimer = new System.Timers.Timer( 500 ); // Set the interval to 500ms
            card1InspectorTimer = new System.Timers.Timer( 100 ); // Set the interval to 500ms
            card2InspectorTimer = new System.Timers.Timer( 100 ); // Set the interval to 500ms
            card3InspectorTimer = new System.Timers.Timer( 100 ); // Set the interval to 500ms
            errorInspectorTimer.Elapsed += OnErrorInspectorTimerElapsed;
            card1InspectorTimer.Elapsed += OnCard1InspectorTimerElapsed;
            card2InspectorTimer.Elapsed += OnCard2InspectorTimerElapsed;
            card3InspectorTimer.Elapsed += OnCard3InspectorTimerElapsed;
            errorInspectorTimer.Start();
        }
        private async void OnErrorInspectorTimerElapsed ( object sender, ElapsedEventArgs e )
        {
            SendCardErrorInspection();
        }
        private async void OnCard1InspectorTimerElapsed ( object sender, ElapsedEventArgs e )
        {
            SendCard1OpenOrClosed();
        }
        private async void OnCard2InspectorTimerElapsed ( object sender, ElapsedEventArgs e )
        {
            SendCard2OpenOrClosed();
        }
        private async void OnCard3InspectorTimerElapsed ( object sender, ElapsedEventArgs e )
        {
            SendCard3OpenOrClosed();
        }

        #region DataManupulation

        private string CreateMessage (byte id, byte msgCode, byte [] dataReceived, byte checkSumMsb, byte checkSumLsb)
        {
            string result = "";
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(ByteToString( id ) );
            stringBuilder.Append( ByteToString( msgCode ) );
            for ( int i = 0; i < dataReceived.Length; i++ )
            {
                stringBuilder.Append( ByteToString( dataReceived [i] ));
            }
            stringBuilder.Append(ByteToString(checkSumMsb));
            stringBuilder.Append(ByteToString(checkSumLsb));
            result = stringBuilder.ToString();
            return result;
        }
        /// <summary>
        /// takes byte as input and converts it to 0x00 format
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        private string ByteToString ( byte x )
        {
            return x.ToString( "X2" );
        }
        /// <summary>
        /// Splits given byte to two bytes
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        byte [] SplitByteToTwo ( byte x )
        {
            return Encoding.ASCII.GetBytes( ByteToString( x ) );
        }
        /// <summary>
        /// takes byte string as input and converts it to integer value
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        private int StringToInt ( string x )
        {
            return int.Parse( x, System.Globalization.NumberStyles.HexNumber );
        }
        /// <summary>
        /// calculates the data length with msb and lsb
        /// </summary>
        /// <param name="lengthMSB"></param>
        /// <param name="lengthLSB"></param>
        /// <returns></returns>
        private int CalculateDataLength (byte lengthMSB, byte lengthLSB)
        {
            int result = -1;
            // Calculates msb
            byte x = (byte)StringToInt( ByteToString(lengthMSB) );
            if ( 48 <= x && x <= 57 )
            {
                result = ( x - 48 ) * 16;
            }
            else if ( 65 <= x && x <= 70 )
            {
                result = ( x - 55 ) * 16;
            }
            else
            {
                result = x;
            }
            // Calculates lsb
            byte x2 = (byte)StringToInt( ByteToString(lengthLSB) );
            if ( 48 <= x2 && x2 <= 57 )
            {
                result += ( x2 - 48 );
            }
            else if ( 65 <= x2 && x2 <= 70 )
            {
                result += ( x2 - 55 );
            }
            else
            {
                result += x2;
            }
            return result;
        }
        /// <summary>
        /// Calculates the checkSum of given array of bytes 
        /// </summary>
        /// <param name="checkSumToBeCalculated"></param>
        /// <returns> [0] as msb [1] as lsb </returns>
        private byte [] CalculateCheckSum ( byte [] checkSumToBeCalculated)
        {
            byte tempResult = 0x00;
            foreach ( byte x in checkSumToBeCalculated )
            {
                tempResult ^= x;
            }
            return SplitByteToTwo( tempResult );
        }
        /// <summary>
        /// Takes Card index and channel index as input then calculates the byte array to be sent
        /// </summary>
        /// <param name="cardIndex"></param>
        /// <param name="channelIndex"></param>
        /// <returns> return checksum as byte[] = { msb[0], lsb[1]} </returns>
        private byte [] CalculateOpenCommandToBeSent ( int cardIndex, int channelIndex )
        {
            byte toBegin = 0b00000001;
            byte [] result = new byte [12]; // to be returned
            byte [] checkSumCalc = new byte [9]; // to calculate checksum
            byte [] checkSumTemp = new byte [2]; // to send checksum Method
                                                 // Generic properties
            byte stx = 0x02;
            byte etx = 0x03;
            byte id = 0x39;
            byte msgCode = 0x00;
            // Data part
            byte dataLengthMSB = 0x30;
            byte dataLengthLSB = 0x34;
            byte dataByte1MSB = 0xFF;
            byte dataByte1LSB = 0x00;
            byte dataByte0MSB = 0xFF;
            byte dataByte0LSB = 0x00;
            // Check sum
            byte checkSumMSB;
            byte checkSumLSB;

            if ( cardIndex == 0 )
            {
                msgCode = 0xD4;
            }
            else if ( cardIndex == 1 )
            {
                msgCode = 0xD5;
            }
            else if ( cardIndex == 2 )
            {
                msgCode = 0xD6;
            }

            if ( channelIndex == 1 )
            {
                byte temp1 = toBegin;
                byte temp0 = (byte)( ~temp1 );
                byte [] split1 = SplitByteToTwo( temp1 );
                byte [] split0 = SplitByteToTwo( temp0 );
                dataByte1MSB = split1 [0];
                dataByte1LSB = split1 [1];
                dataByte0MSB = split0 [0];
                dataByte0LSB = split0 [1];
            }
            else if ( channelIndex == 2 )
            {
                byte temp1 = (byte)( toBegin << 1 );
                byte temp0 = (byte)( ~temp1 );
                byte [] split1 = SplitByteToTwo( temp1 );
                byte [] split0 = SplitByteToTwo( temp0 );
                dataByte1MSB = split1 [0];
                dataByte1LSB = split1 [1];
                dataByte0MSB = split0 [0];
                dataByte0LSB = split0 [1];
            }
            else if ( channelIndex == 3 )
            {
                byte temp1 = (byte)( toBegin << 2 );
                byte temp0 = (byte)( ~temp1 );
                byte [] split1 = SplitByteToTwo( temp1 );
                byte [] split0 = SplitByteToTwo( temp0 );
                dataByte1MSB = split1 [0];
                dataByte1LSB = split1 [1];
                dataByte0MSB = split0 [0];
                dataByte0LSB = split0 [1];
            }
            else if ( channelIndex == 4 )
            {
                byte temp1 = (byte)( toBegin << 3 );
                byte temp0 = (byte)( ~temp1 );
                byte [] split1 = SplitByteToTwo( temp1 );
                byte [] split0 = SplitByteToTwo( temp0 );
                dataByte1MSB = split1 [0];
                dataByte1LSB = split1 [1];
                dataByte0MSB = split0 [0];
                dataByte0LSB = split0 [1];
            }
            else if ( channelIndex == 5 )
            {
                byte temp1 = (byte)( toBegin << 4 );
                byte temp0 = (byte)( ~temp1 );
                byte [] split1 = SplitByteToTwo( temp1 );
                byte [] split0 = SplitByteToTwo( temp0 );
                dataByte1MSB = split1 [0];
                dataByte1LSB = split1 [1];
                dataByte0MSB = split0 [0];
                dataByte0LSB = split0 [1];
            }
            else if ( channelIndex == 6 )
            {
                byte temp1 = (byte)( toBegin << 5 );
                byte temp0 = (byte)( ~temp1 );
                byte [] split1 = SplitByteToTwo( temp1 );
                byte [] split0 = SplitByteToTwo( temp0 );
                dataByte1MSB = split1 [0];
                dataByte1LSB = split1 [1];
                dataByte0MSB = split0 [0];
                dataByte0LSB = split0 [1];
            }
            else if ( channelIndex == 7 )
            {
                byte temp1 = (byte)( toBegin << 6 );
                byte temp0 = (byte)( ~temp1 );
                byte [] split1 = SplitByteToTwo( temp1 );
                byte [] split0 = SplitByteToTwo( temp0 );
                dataByte1MSB = split1 [0];
                dataByte1LSB = split1 [1];
                dataByte0MSB = split0 [0];
                dataByte0LSB = split0 [1];
            }
            else if ( channelIndex == 8 )
            {
                byte temp1 = (byte)( toBegin << 7 );
                byte temp0 = (byte)( ~temp1 );
                byte [] split1 = SplitByteToTwo( temp1 );
                byte [] split0 = SplitByteToTwo( temp0 );
                dataByte1MSB = split1 [0];
                dataByte1LSB = split1 [1];
                dataByte0MSB = split0 [0];
                dataByte0LSB = split0 [1];
            }

            checkSumCalc [0] = stx;
            checkSumCalc [1] = id;
            checkSumCalc [2] = msgCode;
            checkSumCalc [3] = dataLengthMSB;
            checkSumCalc [4] = dataLengthLSB;
            checkSumCalc [5] = dataByte1MSB;
            checkSumCalc [6] = dataByte1LSB;
            checkSumCalc [7] = dataByte0MSB;
            checkSumCalc [8] = dataByte0LSB;
            checkSumTemp = CalculateCheckSum( checkSumCalc );
            checkSumMSB = checkSumTemp [0];
            checkSumLSB = checkSumTemp [1];

            result [0] = stx;
            result [1] = id;
            result [2] = msgCode;
            result [3] = dataLengthMSB;
            result [4] = dataLengthLSB;
            result [5] = dataByte1MSB;
            result [6] = dataByte1LSB;
            result [7] = dataByte0MSB;
            result [8] = dataByte0LSB;
            result [9] = checkSumMSB;
            result [10] = checkSumLSB;
            result [11] = etx;
            return result;
        }
        /// <summary>
        /// Takes Card index and channel index as input then calculates the byte array to be sent
        /// </summary>
        /// <param name="cardIndex"></param>
        /// <param name="channelIndex"></param>
        /// <returns> return checksum as byte[] = { msb[0], lsb[1]} </returns>
        private byte [] CalculateCloseCommandToBeSent (int cardIndex, int channelIndex)
        {
            byte toBegin = 0b00000001;
            byte [] result = new byte [12]; // to be returned
            byte [] checkSumCalc = new byte [9]; // to calculate checksum
            byte [] checkSumTemp = new byte [2]; // to send checksum Method
                                                 // Generic properties
            byte stx = 0x02;
            byte etx = 0x03;
            byte id = 0x39;
            byte msgCode = 0x00;
            // Data part
            byte dataLengthMSB = 0x30;
            byte dataLengthLSB = 0x34;
            byte dataByte1MSB = 0xFF;
            byte dataByte1LSB = 0x00;
            byte dataByte0MSB = 0xFF;
            byte dataByte0LSB = 0x00;
            // Check sum
            byte checkSumMSB;
            byte checkSumLSB;

            if ( cardIndex == 0 )
            {
                msgCode = 0xD7;
            }
            else if ( cardIndex == 1 )
            {
                msgCode = 0xD8;
            }
            else if ( cardIndex == 2 )
            {
                msgCode = 0xD9;
            }

            if ( channelIndex == 1 )
            {
                byte temp1 = toBegin;
                byte temp0 = (byte)( ~temp1 );
                byte [] split1 = SplitByteToTwo( temp1 );
                byte [] split0 = SplitByteToTwo( temp0 );
                dataByte1MSB = split1 [0];
                dataByte1LSB = split1 [1];
                dataByte0MSB = split0 [0];
                dataByte0LSB = split0 [1];
            }
            else if ( channelIndex == 2 )
            {
                byte temp1 = (byte)( toBegin << 1 );
                byte temp0 = (byte)( ~temp1 );
                byte [] split1 = SplitByteToTwo( temp1 );
                byte [] split0 = SplitByteToTwo( temp0 );
                dataByte1MSB = split1 [0];
                dataByte1LSB = split1 [1];
                dataByte0MSB = split0 [0];
                dataByte0LSB = split0 [1];
            }
            else if ( channelIndex == 3 )
            {
                byte temp1 = (byte)( toBegin << 2 );
                byte temp0 = (byte)( ~temp1 );
                byte [] split1 = SplitByteToTwo( temp1 );
                byte [] split0 = SplitByteToTwo( temp0 );
                dataByte1MSB = split1 [0];
                dataByte1LSB = split1 [1];
                dataByte0MSB = split0 [0];
                dataByte0LSB = split0 [1];
            }
            else if ( channelIndex == 4 )
            {
                byte temp1 = (byte)( toBegin << 3 );
                byte temp0 = (byte)( ~temp1 );
                byte [] split1 = SplitByteToTwo( temp1 );
                byte [] split0 = SplitByteToTwo( temp0 );
                dataByte1MSB = split1 [0];
                dataByte1LSB = split1 [1];
                dataByte0MSB = split0 [0];
                dataByte0LSB = split0 [1];
            }
            else if ( channelIndex == 5 )
            {
                byte temp1 = (byte)( toBegin << 4 );
                byte temp0 = (byte)( ~temp1 );
                byte [] split1 = SplitByteToTwo( temp1 );
                byte [] split0 = SplitByteToTwo( temp0 );
                dataByte1MSB = split1 [0];
                dataByte1LSB = split1 [1];
                dataByte0MSB = split0 [0];
                dataByte0LSB = split0 [1];
            }
            else if ( channelIndex == 6 )
            {
                byte temp1 = (byte)( toBegin << 5 );
                byte temp0 = (byte)( ~temp1 );
                byte [] split1 = SplitByteToTwo( temp1 );
                byte [] split0 = SplitByteToTwo( temp0 );
                dataByte1MSB = split1 [0];
                dataByte1LSB = split1 [1];
                dataByte0MSB = split0 [0];
                dataByte0LSB = split0 [1];
            }
            else if ( channelIndex == 7 )
            {
                byte temp1 = (byte)( toBegin << 6 );
                byte temp0 = (byte)( ~temp1 );
                byte [] split1 = SplitByteToTwo( temp1 );
                byte [] split0 = SplitByteToTwo( temp0 );
                dataByte1MSB = split1 [0];
                dataByte1LSB = split1 [1];
                dataByte0MSB = split0 [0];
                dataByte0LSB = split0 [1];
            }
            else if ( channelIndex == 8 )
            {
                byte temp1 = (byte)( toBegin << 7 );
                byte temp0 = (byte)( ~temp1 );
                byte [] split1 = SplitByteToTwo( temp1 );
                byte [] split0 = SplitByteToTwo( temp0 );
                dataByte1MSB = split1 [0];
                dataByte1LSB = split1 [1];
                dataByte0MSB = split0 [0];
                dataByte0LSB = split0 [1];
            }

            checkSumCalc [0] = stx;
            checkSumCalc [1] = id;
            checkSumCalc [2] = msgCode;
            checkSumCalc [3] = dataLengthMSB;
            checkSumCalc [4] = dataLengthLSB;
            checkSumCalc [5] = dataByte1MSB;
            checkSumCalc [6] = dataByte1LSB;
            checkSumCalc [7] = dataByte0MSB;
            checkSumCalc [8] = dataByte0LSB;
            checkSumTemp = CalculateCheckSum( checkSumCalc );
            checkSumMSB = checkSumTemp [0];
            checkSumLSB = checkSumTemp [1];

            result [0] = stx;
            result [1] = id;
            result [2] = msgCode;
            result [3] = dataLengthMSB;
            result [4] = dataLengthLSB;
            result [5] = dataByte1MSB;
            result [6] = dataByte1LSB;
            result [7] = dataByte0MSB;
            result [8] = dataByte0LSB;
            result [9] = checkSumMSB;
            result [10] = checkSumLSB;
            result [11] = etx;
            return result;
        }
        #endregion DataManupulation


        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged ( string propertyName )
        {
            Trace.WriteLine( $"*** Property Changed : {propertyName}" );
            PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( propertyName ) );
        }

    }
}
