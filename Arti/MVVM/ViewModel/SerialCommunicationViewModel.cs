using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Arti.MVVM.Model;

namespace Arti.MVVM.ViewModel
{
    public class SerialCommunicationViewModel : INotifyPropertyChanged
    {
        #region Variables

        #region SerialCommunication
        /// <summary>
        /// Serialport to be opened thruough user selections will have these variables
        /// 0 = deviceId (string) 
        /// 1 = portName (string)
        /// 2 = baudRateIndex     | 5 = handShakeIndex  (int)
        /// 3 = parityListIndex   | 6 = ReadTimeOut     (int)
        /// 4 = stopBitIndex      | 7 = WriteTimeOut    (int)
        /// </summary>
        List<object> serialPortToBeOpenedDetails = new List<object>();
        // SerialCommunicationModel to be created
        SerialCommunicationModel scModel;
        // SerialPort to be used
        SerialPort sp;
        // For collecting data from serial port
        private StringBuilder dataReceiverBuffer;
        public StringBuilder DataReceiverBuffer
        {
            get => dataReceiverBuffer;
            set => dataReceiverBuffer = value;
        }
        // To check if communication active
        private bool isCommunicationActive;
        public bool IsCommunicationActive
        {
            get
            {
                return isCommunicationActive;
            }
            set
            {
                isCommunicationActive = value;
            }
        }
        #endregion Serial Communication

        #region View
        private int [] baudRateList = { 110, 300, 600, 1200, 2400, 4800, 9600, 14400, 19200, 38400, 57600, 115200, 128000, 256000 };
        public int [] BaudRateList
        {
            get => baudRateList;
        }
        private Parity [] parityList = { Parity.Even, Parity.None, Parity.Odd, Parity.Mark, Parity.Space };
        public Parity [] ParityList
        {
            get => parityList;
        }
        private StopBits [] stopBitList = { System.IO.Ports.StopBits.None, System.IO.Ports.StopBits.One, System.IO.Ports.StopBits.OnePointFive, System.IO.Ports.StopBits.Two };
        public StopBits [] StopBitList
        {
            get => stopBitList;
        }
        private Handshake [] handShakeList = { Handshake.None, Handshake.XOnXOff, Handshake.RequestToSend, Handshake.RequestToSendXOnXOff };
        public Handshake [] HandShakeList
        {
            get => handShakeList;
        }
        private string dataReceivedSerial;
        public string DataReceivedSerial
        {
            get
            {
                return dataReceivedSerial;
            }
            set
            {
                dataReceivedSerial = value;
                PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( nameof( DataReceivedSerial ) ) );
            }
        }

        private int desiredReadTimeOUT;
        public int DesiredReadTimeOUT
        {
            get
            {
                return desiredReadTimeOUT;
            }
            set
            {
                desiredReadTimeOUT = value;
            }
        }

        private int desiredWriteTimeOUT;
        public int DesiredWriteTimeOUT
        {
            get
            {
                return desiredWriteTimeOUT;
            }
            set
            {
                desiredWriteTimeOUT = value;
            }
        }

        #endregion View

        #endregion Variables

        #region Constructors
        public SerialCommunicationViewModel ()
        {
            
        }
        #endregion Constructors


        #region Methods
        #region SerialPort
        public void OpenSerialPortConnection ()
        {
            scModel = new SerialCommunicationModel( (string)serialPortToBeOpenedDetails [0],
                                                    (string)serialPortToBeOpenedDetails [1],
                                                    (int)serialPortToBeOpenedDetails [2],
                                                    (int)serialPortToBeOpenedDetails [3],
                                                    (int)serialPortToBeOpenedDetails [4],
                                                    (int)serialPortToBeOpenedDetails [5],
                                                    (int)serialPortToBeOpenedDetails [6],
                                                    (int)serialPortToBeOpenedDetails [7] );
            sp = scModel.SelectedSerialPort;
            sp.DataReceived += async ( sender, e ) => await OnDataReceivedAsync();
            IsCommunicationActive = false;
        }

        /// <summary>
        /// Sends data to listener
        /// </summary>
        /// <param name="data"> data to be send </param>
        public void SendData ( string data )
        {
            if ( sp.IsOpen )
            {
                sp.WriteLine( data );
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
            string hexData = await Task.Run( () => sp.ReadExisting().ToString() );
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
                    isCommunicationActive = true;
                    dataReceiverBuffer.Clear();
                }

                if ( isCommunicationActive && hexData != "0x02" && hexData != "0x03" )
                {
                    dataReceiverBuffer.Append( hexData );
                }

                if ( hexData == "0x03" && isCommunicationActive )
                {
                    isCommunicationActive = false;
                    DataReceivedSerial = dataReceiverBuffer.ToString();
                    //string convertedData = ConvertHexToString( completeHexMessage );
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
        #endregion SerialPort
        #endregion Methods

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
