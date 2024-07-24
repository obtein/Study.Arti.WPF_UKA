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

        #region To test easily

        #endregion

        #region Variables

        #region Variables.SerialCommunication
        /// <summary>
        /// Serialport to be opened thruough user selections will have these variables
        /// 0 = deviceId (string) 
        /// 1 = portName (string)
        /// 2 = BaudRate          | 5 = HandShake       (int)
        /// 3 = Parity            | 6 = ReadTimeOut     (int)
        /// 4 = StopBit           | 7 = WriteTimeOut    (int)
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

        /// <summary>
        /// Card channel controllers green for communication red for not communication
        /// Brush Picker 0 - not communicating , 1 - communicating, 2- not connected
        /// </summary>
        private Brush [] buttonStateBrushList = { Brushes.DarkRed, Brushes.LimeGreen, Brushes.Transparent };
        private ObservableCollection<Brush> buttonStates0;
        public ObservableCollection<Brush> ButtonStates0
        {
            get => buttonStates0;
            set => buttonStates0 = value;
        }
        private ObservableCollection<Brush> buttonStates1;
        public ObservableCollection<Brush> ButtonStates1
        {
            get => buttonStates1;
            set => buttonStates1 = value;
        }
        private ObservableCollection<Brush> buttonStates2;
        public ObservableCollection<Brush> ButtonStates2
        {
            get => buttonStates2;
            set => buttonStates2 = value;
        }
        /// <summary>
        /// For handling 2 errors for each channel
        /// </summary>
        private Brush [] errorStateBrushList = { Brushes.Red, Brushes.SpringGreen, Brushes.Transparent };
        // For card0 err0
        private ObservableCollection<Brush> errorStatusList0;
        public ObservableCollection<Brush> ErrorStatusList0
        {
            get
            {
                return errorStatusList0;
            }
            set
            {
                errorStatusList0 = value;
            }
        }
        // For card0 err1
        private ObservableCollection<Brush> errorStatusList01;
        public ObservableCollection<Brush> ErrorStatusList01
        {
            get
            {
                return errorStatusList01;
            }
            set
            {
                errorStatusList01 = value;
            }
        }
        // For card1 err0
        private ObservableCollection<Brush> errorStatusList1;
        public ObservableCollection<Brush> ErrorStatusList1
        {
            get
            {
                return errorStatusList1;
            }
            set
            {
                errorStatusList1 = value;
            }
        }
        // For card1 err1
        private ObservableCollection<Brush> errorStatusList11;
        public ObservableCollection<Brush> ErrorStatusList11
        {
            get
            {
                return errorStatusList11;
            }
            set
            {
                errorStatusList11 = value;
            }
        }
        // For card2 err0
        private ObservableCollection<Brush> errorStatusList2;
        public ObservableCollection<Brush> ErrorStatusList2
        {
            get
            {
                return errorStatusList2;
            }
            set
            {
                errorStatusList2 = value;
            }
        }
        // For card2 err1
        private ObservableCollection<Brush> errorStatusList21;
        public ObservableCollection<Brush> ErrorStatusList21
        {
            get
            {
                return errorStatusList21;
            }
            set
            {
                errorStatusList21 = value;
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
            errorStatusList0 = new ObservableCollection<Brush>( new Brush [8] );
            errorStatusList1 = new ObservableCollection<Brush>( new Brush [8] );
            errorStatusList2 = new ObservableCollection<Brush>( new Brush [8] );
            errorStatusList01 = new ObservableCollection<Brush>( new Brush [8] );
            errorStatusList11 = new ObservableCollection<Brush>( new Brush [8] );
            errorStatusList21 = new ObservableCollection<Brush>( new Brush [8] );
            PaintError0Brushes();
            PaintError1Brushes();
            Trace.WriteLine( "ViewModelConstructor" );
            buttonStates0 = new ObservableCollection<Brush>( new Brush [8] );
            buttonStates1 = new ObservableCollection<Brush>( new Brush [8] );
            buttonStates2 = new ObservableCollection<Brush>( new Brush [8] );
            PaintButtonBrushes();
            IsSerialPaneEnabled = false;
            CanvasBackground0 = Brushes.DarkGray;
            CanvasBackground1 = Brushes.DarkGray;
            CanvasBackground2 = Brushes.DarkGray;
            serialPortToBeOpenedDetails = new ObservableCollection<object>( new object [10] );
            MessageList = new ObservableCollection<MessageModel>();
            SerialCommunicationSelectedCommand = new RelayCommand(SerialCommunicationSelected);
            OpenSerialPortCommand = new RelayCommand(OpenSerialPortConnection);
            StartBlinkingCommand = new RelayCommand(StartBlinkingConnected);
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

        private void PaintButtonBrushes ()
        {
            for ( int i = 0; i < 8; i++ )
            {
                buttonStates0 [i] = buttonStateBrushList [0];
                buttonStates1 [i] = buttonStateBrushList [0];
                buttonStates2 [i] = buttonStateBrushList [1];
            }
        }
        private void PaintError0Brushes ()
        {
            for ( int i = 0; i < 8; i++ )
            {
                errorStatusList0 [i] = errorStateBrushList [0];
                errorStatusList1 [i] = errorStateBrushList [0];
                errorStatusList2 [i] = errorStateBrushList [1];
            }
        }
        private void PaintError1Brushes ()
        {
            for ( int i = 0; i < 8; i++ )
            {
                errorStatusList01 [i] = errorStateBrushList [1];
                errorStatusList11 [i] = errorStateBrushList [0];
                errorStatusList21 [i] = errorStateBrushList [1];
            }
        }

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
                App.Current.Dispatcher.Invoke( () => CheckForUIUpdates(model.MessageDateTime, model.MessageToShow));
            }
        }

        public void CheckForUIUpdates (DateTime dataTime, string data)
        {
            Trace.WriteLine("Error Code solving");
            if ( data == "hata000"  && dataTime != DateTime.Now)
            {
                errorStatusList0 [0] = errorStateBrushList[0];
                errorStatusList01 [0] = errorStateBrushList [0];
            }
            else if ( data == "hata001" )
            {
                errorStatusList0 [0] = errorStateBrushList [0];
                errorStatusList01 [0] = errorStateBrushList [1];
            }
            else if ( data == "hata010" )
            {
                errorStatusList0 [0] = errorStateBrushList [1];
                errorStatusList01 [0] = errorStateBrushList [0];
            }
            else if ( data == "hata011" )
            {
                errorStatusList0 [0] = errorStateBrushList [1];
                errorStatusList01 [0] = errorStateBrushList [1];
            }
            else if ( data == "hata100" )
            {
                errorStatusList0 [1] = errorStateBrushList [0];
                errorStatusList01 [1] = errorStateBrushList [0];
            }
            else if ( data == "hata101" )
            {
                errorStatusList0 [1] = errorStateBrushList [0];
                errorStatusList01 [1] = errorStateBrushList [1];
            }
            else if ( data == "hata110" )
            {
                errorStatusList0 [1] = errorStateBrushList [1];
                errorStatusList01 [1] = errorStateBrushList [0];
            }
            else if ( data == "hata111" )
            {
                errorStatusList0 [1] = errorStateBrushList [1];
                errorStatusList01 [1] = errorStateBrushList [1];
            }
            else if ( data == "hata200" )
            {
                errorStatusList0 [2] = errorStateBrushList [0];
                errorStatusList01 [2] = errorStateBrushList [0];
            }
            else if ( data == "hata201" )
            {
                errorStatusList0 [2] = errorStateBrushList [0];
                errorStatusList01 [2] = errorStateBrushList [1];
            }
            else if ( data == "hata210" )
            {
                errorStatusList0 [2] = errorStateBrushList [1];
                errorStatusList01 [2] = errorStateBrushList [0];
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged ( string propertyName )
        {
            PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( propertyName ) );
        }
    }
}
