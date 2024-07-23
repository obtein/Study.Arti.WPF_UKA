using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arti.MVVM.Model
{
    public class SerialCommunicationModel : INotifyPropertyChanged 
    {
        
        #region privateVariables
        /// <summary>
        /// Declaring variables needed to create a serial connection
        /// </summary>

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
        #endregion

        #region Properties
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
            Trace.WriteLine(">>>>>>>>>>>>>>>>>DATA RECEIVED");
            string hexData = await Task.Run( () => selectedSerialPort.ReadExisting().ToString() );
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
        private async Task ProcessReceivedDataAsync ( string hexData )
        {
            await Task.Run( () =>
            {
                if ( hexData == "0x02" )
                {
                    Trace.WriteLine( ">>>>>>>>>>>>>>>>>Start Reading" );
                    isCommunicationActive = true;
                    dataReceiverBuffer.Clear();
                }

                if ( isCommunicationActive && hexData != "0x02" && hexData != "0x03" )
                {
                    Trace.WriteLine( ">>>>>>>>>>>>>>>>>Creating message" );
                    dataReceiverBuffer.Append( hexData );
                }

                if ( hexData == "0x03" && isCommunicationActive )
                {
                    isCommunicationActive = false;
                    SerialDataReceived = dataReceiverBuffer.ToString();
                    Trace.WriteLine( ">>>>>>>>>>>>>>>>>Message Created" );
                    Trace.WriteLine( $"Message Created {SerialDataReceived}" );
                    Trace.WriteLine( ">>>>>>>>>>>>>>>>>End of Cycle" );
                    isCommunicationActive = false;
                }
            } );
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
        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged ( string propertyName )
        {
            Trace.WriteLine( $"*** Property Changed : {propertyName}" );
            PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( propertyName ) );
        }

    }
}
