using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO.Ports;
using System.Linq;
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

        #region Properties
        /// <summary>
        /// Declaring variables needed to create a serial connection
        /// </summary>
        private System.Timers.Timer errorInspectorTimer;
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

        /// <summary>
        /// serialPort.DataReceived event has subscribed this in constructor
        /// it waits until serialPort.ReadExisting().ToString() then
        /// waits for ProcessReceivedDataAsync( hexData )
        /// </summary>
        /// <returns></returns>
        private async Task OnDataReceivedAsync ()
        {
            Trace.WriteLine( ">>>>>>>>>>>>>>>>>DATA RECEIVED" );
            int hexData = await Task.Run( () => selectedSerialPort.ReadByte() );
            //string hexData = await Task.Run( () => selectedSerialPort.ReadExisting().ToString() );
            Trace.WriteLine( ">>>>>>>>>>>>>>>>>Enter : " + hexData );
            await ProcessReceivedDataAsync( hexData );
        }

        /// <summary>
        /// This method is called by OnDataReceivedAsync with hexData and 
        /// filters it by checking message starting from 
        /// "0x02"(beggining of the mesagge) - "0x03"(ending of the message)
        /// it will then assigns the resulted data to completeHexMessage 
        /// finnaly DataReceived?.Invoke( completeHexMessage ) will notify
        /// ServiceCommunicationViewModel
        /// </summary>
        /// <param name="hexData"> data read from serial port in OnDataReceivedAsync </param>
        /// /// <returns></returns>
        private async Task ProcessReceivedDataAsync ( int hexData )
        {

            int id = selectedSerialPort.ReadByte();
            Trace.WriteLine( ">>>>>>>>>>>>>>>>>Creating id " + id );
            int msgCode = selectedSerialPort.ReadByte();
            Trace.WriteLine( ">>>>>>>>>>>>>>>>>Creating msgCode " + msgCode );
            int dataLengthLocal = selectedSerialPort.ReadByte();
            int tempData = selectedSerialPort.ReadByte();
            if ( 48 <= dataLengthLocal && dataLengthLocal <= 57 ) // between 0-9
            {
                dataLengthLocal = ( dataLengthLocal - 48 ) * 16;
                Trace.WriteLine( ">>>>>>>>>>>>>>>>>Creating dataLengthLocal " + dataLengthLocal );
            }
            else if ( 65 <= dataLengthLocal && dataLengthLocal <= 70 ) // between A-F
            {
                dataLengthLocal = ( dataLengthLocal - 55 ) * 16;
                Trace.WriteLine( ">>>>>>>>>>>>>>>>>Creating dataLengthLocal " + dataLengthLocal );
            }
            if ( 48 <= tempData && tempData <= 57 ) // between 0-9
            {
                tempData = ( tempData - 48 );
                Trace.WriteLine( ">>>>>>>>>>>>>>>>>Creating tempData " + tempData );
            }
            else if ( 65 <= tempData && tempData <= 70 ) // between A-F
            {
                tempData = ( tempData - 55 );
                Trace.WriteLine( ">>>>>>>>>>>>>>>>>Creating tempData " + tempData );
            }
            dataLengthLocal += tempData;
            Trace.WriteLine( ">>>>>>>>>>>>>>>>>Creating Final DataLength " + dataLengthLocal );
            int checkSum;
            dataReceiverBuffer.Clear();
            for ( int i = 0; i < dataLengthLocal; i++ )
            {
                Trace.WriteLine( ">>>>>>>>>>>>>>>>>Creating message" );
                dataReceiverBuffer.Append( " " + selectedSerialPort.ReadByte().ToString() );
                Trace.WriteLine( ">>>>>>>>>>>>>>>>>Creating message : " + dataReceiverBuffer.ToString() );
            }
            SerialDataReceived = dataReceiverBuffer.ToString();
            Trace.WriteLine( ">>>>>>>>>>>>>>>>>Message Created" );
            Trace.WriteLine( $"Message Created {SerialDataReceived}" );
            checkSum = selectedSerialPort.ReadByte();
            //checkSum += selectedSerialPort.ReadByte();
            Trace.WriteLine( $"Card ID: {id} | Message Code: {msgCode} | Length: {dataLengthLocal} | Check Sum Received: {checkSum}" );
            Trace.WriteLine( ">>>>>>>>>>>>>>>>>End of Cycle" );
            if ( selectedSerialPort.ReadByte() != 3 )
            {
                Trace.WriteLine( "Where is my closing line" );
            }
            selectedSerialPort.DiscardInBuffer();
            SendChannelInspection();
        }

        /// <summary>
        ///  TO-DO : When physical tests begin check the form of received data
        ///  Dont Forget to uncomment "//string convertedData = ConvertHexToString( completeHexMessage );" in ProcessReceivedDataAsync
        /// </summary>
        /// <param name="hexData"></param>
        /// <returns></returns>
        private string ConvertHexToString ( string hexData )
        {
            try
            {
                byte [] bytes = new byte [hexData.Length / 2];
                for ( int i = 0; i < hexData.Length; i += 2 )
                {
                    bytes [i / 2] = Convert.ToByte( hexData.Substring( i, 2 ), 16 );
                }
                return Encoding.UTF8.GetString( bytes );
            }
            catch ( Exception ex )
            {
                return $"Error converting hex to string: {ex.Message}";
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
                selectedSerialPort.Write( new byte [] { 0x02, 0x39, 0xC9, 0x30, 0x30, 0x00, 0xF0, 0x03 }, 0, 8 );

            }
        }
        // Card1 channel open
        private void SendOpenCommandTo1 (int channelNumber)
        {
            if ( channelNumber == 1 && selectedSerialPort.IsOpen )
            {
                selectedSerialPort.Write( new byte [] { 0x02, 0x39, 0xD4, 0x30, 0x34, 0xFE, 0x01, 0x14, 0x03 }, 0, 8 );
            }
            else if ( channelNumber == 2 && selectedSerialPort.IsOpen )
            {
                selectedSerialPort.Write( new byte [] { 0x02, 0x39, 0xD4, 0x30, 0x34, 0xFD, 0x02, 0x14, 0x03 }, 0, 8 );
            }
            else if ( channelNumber == 3 && selectedSerialPort.IsOpen )
            {
                selectedSerialPort.Write( new byte [] { 0x02, 0x39, 0xD4, 0x30, 0x34, 0xFB, 0x04, 0x14, 0x03 }, 0, 8 );
            }
            else if ( channelNumber == 4 && selectedSerialPort.IsOpen )
            {
                selectedSerialPort.Write( new byte [] { 0x02, 0x39, 0xD4, 0x30, 0x34, 0xF7, 0x08, 0x14, 0x03 }, 0, 8 );
            }
            else if ( channelNumber == 5 && selectedSerialPort.IsOpen )
            {
                selectedSerialPort.Write( new byte [] { 0x02, 0x39, 0xD4, 0x30, 0x34, 0xEF, 0x10, 0x14, 0x03 }, 0, 8 );
            }
            else if ( channelNumber == 6 && selectedSerialPort.IsOpen )
            {
                selectedSerialPort.Write( new byte [] { 0x02, 0x39, 0xD4, 0x30, 0x34, 0xDF, 0x20, 0x14, 0x03 }, 0, 8 );
            }
            else if ( channelNumber == 7 && selectedSerialPort.IsOpen )
            {
                selectedSerialPort.Write( new byte [] { 0x02, 0x39, 0xD4, 0x30, 0x34, 0xBF, 0x40, 0x14, 0x03 }, 0, 8 );
            }
            else if ( channelNumber == 8 && selectedSerialPort.IsOpen )
            {
                selectedSerialPort.Write( new byte [] { 0x02, 0x39, 0xD4, 0x30, 0x34, 0x7F, 0x80, 0x14, 0x03 }, 0, 8 );
            }
        }
        // Card2 channel open
        private void SendOpenCommandTo2 ( int channelNumber )
        {
            if ( channelNumber == 1 && selectedSerialPort.IsOpen )
            {
                selectedSerialPort.Write( new byte [] { 0x02, 0x39, 0xD5, 0x30, 0x34, 0xFE, 0x01, 0x15, 0x03 }, 0, 8 );
            }
            else if ( channelNumber == 2 && selectedSerialPort.IsOpen )
            {
                selectedSerialPort.Write( new byte [] { 0x02, 0x39, 0xD5, 0x30, 0x34, 0xFD, 0x02, 0x15, 0x03 }, 0, 8 );
            }
            else if ( channelNumber == 3 && selectedSerialPort.IsOpen )
            {
                selectedSerialPort.Write( new byte [] { 0x02, 0x39, 0xD5, 0x30, 0x34, 0xFB, 0x04, 0x15, 0x03 }, 0, 8 );
            }
            else if ( channelNumber == 4 && selectedSerialPort.IsOpen )
            {
                selectedSerialPort.Write( new byte [] { 0x02, 0x39, 0xD5, 0x30, 0x34, 0xF7, 0x08, 0x15, 0x03 }, 0, 8 );
            }
            else if ( channelNumber == 5 && selectedSerialPort.IsOpen )
            {
                selectedSerialPort.Write( new byte [] { 0x02, 0x39, 0xD5, 0x30, 0x34, 0xEF, 0x10, 0x15, 0x03 }, 0, 8 );
            }
            else if ( channelNumber == 6 && selectedSerialPort.IsOpen )
            {
                selectedSerialPort.Write( new byte [] { 0x02, 0x39, 0xD5, 0x30, 0x34, 0xDF, 0x20, 0x15, 0x03 }, 0, 8 );
            }
            else if ( channelNumber == 7 && selectedSerialPort.IsOpen )
            {
                selectedSerialPort.Write( new byte [] { 0x02, 0x39, 0xD5, 0x30, 0x34, 0xBF, 0x40, 0x15, 0x03 }, 0, 8 );
            }
            else if ( channelNumber == 8 && selectedSerialPort.IsOpen )
            {
                selectedSerialPort.Write( new byte [] { 0x02, 0x39, 0xD5, 0x30, 0x34, 0x7F, 0x80, 0x15, 0x03 }, 0, 8 );
            }
        }

        private void SendOpenCommandTo3 ( int channelNumber )
        {
            if ( channelNumber == 1 && selectedSerialPort.IsOpen )
            {
                selectedSerialPort.Write( new byte [] { 0x02, 0x39, 0xD6, 0x30, 0x34, 0xFE, 0x01, 0x16, 0x03 }, 0, 8 );
            }
            else if ( channelNumber == 2 && selectedSerialPort.IsOpen )
            {
                selectedSerialPort.Write( new byte [] { 0x02, 0x39, 0xD6, 0x30, 0x34, 0xFD, 0x02, 0x16, 0x03 }, 0, 8 );
            }
            else if ( channelNumber == 3 && selectedSerialPort.IsOpen )
            {
                selectedSerialPort.Write( new byte [] { 0x02, 0x39, 0xD6, 0x30, 0x34, 0xFB, 0x04, 0x16, 0x03 }, 0, 8 );
            }
            else if ( channelNumber == 4 && selectedSerialPort.IsOpen )
            {
                selectedSerialPort.Write( new byte [] { 0x02, 0x39, 0xD6, 0x30, 0x34, 0xF7, 0x08, 0x16, 0x03 }, 0, 8 );
            }
            else if ( channelNumber == 5 && selectedSerialPort.IsOpen )
            {
                selectedSerialPort.Write( new byte [] { 0x02, 0x39, 0xD6, 0x30, 0x34, 0xEF, 0x10, 0x16, 0x03 }, 0, 8 );
            }
            else if ( channelNumber == 6 && selectedSerialPort.IsOpen )
            {
                selectedSerialPort.Write( new byte [] { 0x02, 0x39, 0xD6, 0x30, 0x34, 0xDF, 0x20, 0x16, 0x03 }, 0, 8 );
            }
            else if ( channelNumber == 7 && selectedSerialPort.IsOpen )
            {
                selectedSerialPort.Write( new byte [] { 0x02, 0x39, 0xD6, 0x30, 0x34, 0xBF, 0x40, 0x16, 0x03 }, 0, 8 );
            }
            else if ( channelNumber == 8 && selectedSerialPort.IsOpen )
            {
                selectedSerialPort.Write( new byte [] { 0x02, 0x39, 0xD6, 0x30, 0x34, 0x7F, 0x80, 0x16, 0x03 }, 0, 8 );
            }
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

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged ( string propertyName )
        {
            Trace.WriteLine( $"*** Property Changed : {propertyName}" );
            PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( propertyName ) );
        }

    }
}
