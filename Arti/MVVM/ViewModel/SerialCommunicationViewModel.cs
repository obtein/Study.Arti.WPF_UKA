using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Xml.Linq;
using Arti.MVVM.Model;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Arti.MVVM.ViewModel
{
    partial class SerialCommunicationViewModel : INotifyPropertyChanged
    {

        #region Variables

        #region Variables.SerialCommunication
        /// <summary>
        /// Serialport to be opened thruough user selections will have these variables
        /// 0 = deviceId (string) 
        /// 1 = portName (string)
        /// 2 = baudRateIndex     | 5 = handShakeIndex  (int)
        /// 3 = parityListIndex   | 6 = ReadTimeOut     (int)
        /// 4 = stopBitIndex      | 7 = WriteTimeOut    (int)
        /// </summary>
        private ObservableCollection<object> serialPortToBeOpenedDetails;
        public ObservableCollection<object> SerialPortToBeOpenedDetails
        {
            get => serialPortToBeOpenedDetails;
            set => serialPortToBeOpenedDetails = value;
        }
        // SerialCommunicationModel to be created
        SerialCommunicationModel scModel;
        // SerialPort to be used
        SerialPort sp;
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

        private string serialDataReceived;

        public string SerialDataReceived
        {
            get => serialDataReceived;
            set
            {
                if ( serialDataReceived != value )
                {
                    serialDataReceived = value;
                    OnPropertyChanged( nameof( SerialDataReceived ) );
                    Trace.WriteLine( $"+++++++++ Data changed on ViewModel : {SerialDataReceived}" );
                }
            }
        }
        #endregion Variables.SerialCommunication

        #region Variables.View
        private ObservableCollection<MessageModel> messageList;
        public ObservableCollection<MessageModel> MessageList
        {
            get
            {
                return messageList;
            } 
            set 
            {
                messageList = value;
                OnPropertyChanged( nameof( MessageList ) );
            }
        }
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

        private string[] comPortList;

        public string[] ComPortList
        {
            get => comPortList;
            set
            {
                if ( comPortList != value )
                {
                    comPortList = value;
                    OnPropertyChanged( nameof( ComPortList ) );
                }
            }
        }


        private System.Timers.Timer blinkTimer0;
        private bool isBlinking0;
        private System.Timers.Timer blinkTimer1;
        private bool isBlinking1;
        private System.Timers.Timer blinkTimer2;
        private bool isBlinking2;
        private Brush canvasBackground0;
        public Brush CanvasBackground0
        {
            get
            {
                return canvasBackground0;
            }
            set
            {
                if ( canvasBackground0 != value )
                {
                    canvasBackground0 = value;
                    OnPropertyChanged( nameof( CanvasBackground0 ) );
                }
            }
        }
        private Brush canvasBackground1;
        public Brush CanvasBackground1
        {
            get
            {
                return canvasBackground1;
            }
            set
            {
                if ( canvasBackground1 != value )
                {
                    canvasBackground1 = value;
                    OnPropertyChanged( nameof( CanvasBackground1 ) );
                }
            }
        }
        private Brush canvasBackground2;
        public Brush CanvasBackground2
        {
            get
            {
                return canvasBackground2;
            }
            set
            {
                if ( canvasBackground2 != value )
                {
                    canvasBackground2 = value;
                    OnPropertyChanged( nameof( CanvasBackground2 ) );
                }
            }
        }

        private bool isCard0Connected = false;
        private bool isCard1Connected = false;
        private bool isCard2Connected = false;
        private bool isSerialPanelEnabled;

        public bool IsSerialPaneEnabled
        {
            get
            {
                return isSerialPanelEnabled;
            }
            set
            {
                if ( isSerialPanelEnabled != value )
                {
                    isSerialPanelEnabled = value;
                    OnPropertyChanged( nameof( IsSerialPaneEnabled ) );
                }
            }
        }

        #endregion Variables.View

        #region Variables.Commands
        public ICommand SerialCommunicationSelectedCommand
        {
            get; set;
        }
        public ICommand OpenSerialPortCommand
        {
            get; set;
        }
        public ICommand StartBlinkingCommand
        {
            get; set;
        }
        #endregion Variables.Commands


        #endregion Variables

        #region Constructors
        public SerialCommunicationViewModel ()
        {
            Trace.WriteLine( "ViewModelConstructor" );
            IsSerialPaneEnabled = false;
            CanvasBackground0 = Brushes.DarkGray;
            CanvasBackground1 = Brushes.DarkGray;
            CanvasBackground2 = Brushes.DarkGray;
            serialPortToBeOpenedDetails = new ObservableCollection<object>( new object [10] );
            MessageList = new ObservableCollection<MessageModel>();
            SerialCommunicationSelectedCommand = new RelayCommand(SerialCommunicationSelected);
            OpenSerialPortCommand = new RelayCommand(OpenSerialPortConnection);
            StartBlinkingCommand = new RelayCommand(StartBlinkingConnected);
            StartBlinkingConnected();
        }

        #endregion Constructors


        #region Methods

        #region Methods.SerialPort

        public void OpenSerialPortConnection ()
        {
            serialPortToBeOpenedDetails [0] = "Hello";
            int tempBaudRate = Convert.ToInt32( serialPortToBeOpenedDetails [2] );
            int tempReadTimeOUT = Convert.ToInt32( serialPortToBeOpenedDetails [6] );
            int tempWriteTimeOUT = Convert.ToInt32( serialPortToBeOpenedDetails [7] );
            MessageList = new ObservableCollection<MessageModel>();
            scModel = new SerialCommunicationModel( (string)serialPortToBeOpenedDetails [0],
                                                    (string)serialPortToBeOpenedDetails [1],
                                                    tempBaudRate,
                                                    (Parity)serialPortToBeOpenedDetails [3],
                                                    (StopBits)serialPortToBeOpenedDetails [4],
                                                    (Handshake)serialPortToBeOpenedDetails [5],
                                                    tempReadTimeOUT,
                                                    tempWriteTimeOUT );
            scModel.PropertyChanged += new PropertyChangedEventHandler( DataReceived_PropertyChanged );
        }


        #endregion Methods.SerialPort

        #region Methods.UI

        /// <summary>
        /// To mimic blinking effect around device UI when connected
        /// </summary>
        private void StartBlinkingConnected ()
        {

            Trace.WriteLine( "Start Blinking" );
            if ( isBlinking0 )
                return;

            isBlinking0 = true;
            blinkTimer0 = new System.Timers.Timer( 500 ); // Set the interval to 500ms
            blinkTimer0.Elapsed += OnBlinkTimerElapsed;
            blinkTimer0.Start();

            StartBlinkingConnected1();
        }
        private void StartBlinkingConnected1 ()
        {
            Trace.WriteLine( "Start Blinking 1" );
            if ( isBlinking1 )
                return;

            isBlinking1 = true;
            blinkTimer1 = new System.Timers.Timer( 500 ); // Set the interval to 500ms
            blinkTimer1.Elapsed += OnBlinkTimerElapsed1;
            blinkTimer1.Start();
            StartBlinkingConnected2();
        }
        private void StartBlinkingConnected2 ()
        {
            Trace.WriteLine( "Start Blinking 2" );
            if ( isBlinking2 )
                return;

            isBlinking2 = true;
            blinkTimer2 = new System.Timers.Timer( 500 ); // Set the interval to 500ms
            blinkTimer2.Elapsed += OnBlinkTimerElapsed2;
            blinkTimer2.Start();
        }

        private void OnBlinkTimerElapsed ( object sender, ElapsedEventArgs e )
        {
            if ( CanvasBackground0 == Brushes.WhiteSmoke )
            {
                CanvasBackground0 = Brushes.Green; // Change to Green
            }
            else
            {
                CanvasBackground0 = Brushes.WhiteSmoke; // Change back to ForestGreen
            }
        }
        private void OnBlinkTimerElapsed1 ( object sender, ElapsedEventArgs e )
        {
            if ( CanvasBackground1 == Brushes.WhiteSmoke )
            {
                CanvasBackground1 = Brushes.Green; // Change to Green
            }
            else
            {
                CanvasBackground1 = Brushes.WhiteSmoke; // Change back to ForestGreen
            }
        }
        private void OnBlinkTimerElapsed2 ( object sender, ElapsedEventArgs e )
        {
            if ( CanvasBackground2 == Brushes.WhiteSmoke )
            {
                CanvasBackground2 = Brushes.Green; // Change to Green
            }
            else
            {
                CanvasBackground2 = Brushes.WhiteSmoke; // Change back to ForestGreen
            }
        }

        private void SerialCommunicationSelected ()
        {
            IsSerialPaneEnabled = true;
            ComPortList = SerialPort.GetPortNames();
        }

        /// <summary>
        /// 
        /// </summary>
        /// 
        private void MessageReceived (MessageModel model)
        {
            Trace.WriteLine( $"+++++++++ Model Inıt" );
            if ( !MessageList.Any( x => x.MessageDateTime == model.MessageDateTime ) )
            {
                Trace.WriteLine( $"+++++++++ Success" );
                App.Current.Dispatcher.Invoke( () => 
                {
                    ObservableCollection<MessageModel> temp = MessageList;
                    temp.Add(model);
                    MessageList = temp;
                }
                );
                Trace.WriteLine( $"+++++++++ Model Inıt {MessageList [0].MessageDateTime} // {MessageList [0].MessageToShow}" );
            }

        }
        #endregion Methods.UI

        #endregion Methods

        public void DataReceived_PropertyChanged ( object sender, PropertyChangedEventArgs e )
        {
            Trace.WriteLine($"++++++++++++++++++++++ Property Changed on ViewModel : {e.PropertyName}");
            if ( e.PropertyName == "SerialDataReceived" )
            {
                Trace.WriteLine( $"+++++++++ Data will be assigned on ViewModel : {SerialDataReceived}" );
                this.SerialDataReceived = scModel.SerialDataReceived;
                Trace.WriteLine( $"+++++++++ Data has assigned on ViewModel : {SerialDataReceived}" );
                MessageModel model = new MessageModel();
                model.MessageDateTime = DateTime.Now;
                model.MessageToShow = SerialDataReceived;
                Trace.WriteLine( $"+++++++++ Model has created on ViewModel : {model.MessageDateTime} // {model.MessageToShow}" );
                MessageReceived( model );
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged ( string propertyName )
        {
            PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( propertyName ) );
        }
    }
}
