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
using System.Windows.Controls;
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
                    OnPropertyChanged( nameof( Card1ChannelCount ) );
                }
            }
        }
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

        public bool canOpenCardChannel = false;
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
            set
            {
                if ( buttonStates0 != value )
                {
                    buttonStates0 = value;
                    OnPropertyChanged( nameof( ButtonStates0 ) );
                }
            }
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
        #region Card1OpenChannel
        public ICommand OpenCard1Channel1Command
        {
            get; set;
        }
        public ICommand OpenCard1Channel2Command
        {
            get; set;
        }
        public ICommand OpenCard1Channel3Command
        {
            get; set;
        }
        public ICommand OpenCard1Channel4Command
        {
            get; set;
        }
        public ICommand OpenCard1Channel5Command
        {
            get; set;
        }
        public ICommand OpenCard1Channel6Command
        {
            get; set;
        }
        public ICommand OpenCard1Channel7Command
        {
            get; set;
        }
        public ICommand OpenCard1Channel8Command
        {
            get; set;
        }
        #endregion Card1OpenChannel
        #region Card2OpenChannel
        public ICommand OpenCard2Channel1Command
        {
            get; set;
        }
        public ICommand OpenCard2Channel2Command
        {
            get; set;
        }
        public ICommand OpenCard2Channel3Command
        {
            get; set;
        }
        public ICommand OpenCard2Channel4Command
        {
            get; set;
        }
        public ICommand OpenCard2Channel5Command
        {
            get; set;
        }
        public ICommand OpenCard2Channel6Command
        {
            get; set;
        }
        public ICommand OpenCard2Channel7Command
        {
            get; set;
        }
        public ICommand OpenCard2Channel8Command
        {
            get; set;
        }
        #endregion Card2OpenChannel
        #region Card3OpenChannel
        public ICommand OpenCard3Channel1Command
        {
            get; set;
        }
        public ICommand OpenCard3Channel2Command
        {
            get; set;
        }
        public ICommand OpenCard3Channel3Command
        {
            get; set;
        }
        public ICommand OpenCard3Channel4Command
        {
            get; set;
        }
        public ICommand OpenCard3Channel5Command
        {
            get; set;
        }
        public ICommand OpenCard3Channel6Command
        {
            get; set;
        }
        public ICommand OpenCard3Channel7Command
        {
            get; set;
        }
        public ICommand OpenCard3Channel8Command
        {
            get; set;
        }
        #endregion Card1OpenChannel
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
            ButtonStates0 [0] = Brushes.DarkRed;
            ButtonStates0 [1] = Brushes.DarkRed;
            ButtonStates0 [2] = Brushes.DarkRed;
            ButtonStates0 [3] = Brushes.DarkRed;
            ButtonStates0 [4] = Brushes.DarkRed;
            ButtonStates0 [5] = Brushes.DarkRed;
            ButtonStates0 [6] = Brushes.DarkRed;
            ButtonStates0 [7] = Brushes.DarkRed;
            IsSerialPaneEnabled = false;
            CanvasBackground0 = Brushes.DarkGray;
            CanvasBackground1 = Brushes.DarkGray;
            CanvasBackground2 = Brushes.DarkGray;
            serialPortToBeOpenedDetails = new ObservableCollection<object>( new object [10] );
            MessageList = new ObservableCollection<MessageModel>();
            SerialCommunicationSelectedCommand = new RelayCommand(SerialCommunicationSelected);
            OpenSerialPortCommand = new RelayCommand(OpenSerialPortConnection);
            StartBlinkingCommand = new RelayCommand(StartBlinkingConnected);
            // Card1 open channel
            OpenCard1Channel1Command = new RelayCommand( OpenCard1Channel1 );
            OpenCard1Channel2Command = new RelayCommand( OpenCard1Channel2 );
            OpenCard1Channel3Command = new RelayCommand( OpenCard1Channel3 );
            OpenCard1Channel4Command = new RelayCommand( OpenCard1Channel4 );
            OpenCard1Channel5Command = new RelayCommand( OpenCard1Channel5 );
            OpenCard1Channel6Command = new RelayCommand( OpenCard1Channel6 );
            OpenCard1Channel7Command = new RelayCommand( OpenCard1Channel7 );
            OpenCard1Channel8Command = new RelayCommand( OpenCard1Channel8 );
            // Card2 open channel
            OpenCard2Channel1Command = new RelayCommand( OpenCard2Channel1 );
            OpenCard2Channel2Command = new RelayCommand( OpenCard2Channel2 );
            OpenCard2Channel3Command = new RelayCommand( OpenCard2Channel3 );
            OpenCard2Channel4Command = new RelayCommand( OpenCard2Channel4 );
            OpenCard2Channel5Command = new RelayCommand( OpenCard2Channel5 );
            OpenCard2Channel6Command = new RelayCommand( OpenCard2Channel6 );
            OpenCard2Channel7Command = new RelayCommand( OpenCard2Channel7 );
            OpenCard2Channel8Command = new RelayCommand( OpenCard2Channel8 );
            // Card3 open channel
            OpenCard3Channel1Command = new RelayCommand( OpenCard3Channel1 );
            OpenCard3Channel2Command = new RelayCommand( OpenCard3Channel2 );
            OpenCard3Channel3Command = new RelayCommand( OpenCard3Channel3 );
            OpenCard3Channel4Command = new RelayCommand( OpenCard3Channel4 );
            OpenCard3Channel5Command = new RelayCommand( OpenCard3Channel5 );
            OpenCard3Channel6Command = new RelayCommand( OpenCard3Channel6 );
            OpenCard3Channel7Command = new RelayCommand( OpenCard3Channel7 );
            OpenCard3Channel8Command = new RelayCommand( OpenCard3Channel8 );
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
            canOpenCardChannel = true;
        }

        public void SendOpenCardChannel ()
        {
            if ( ButtonStates0 [0] == Brushes.DarkRed )
                ButtonStates0 [0] = Brushes.LimeGreen;
            else
                ButtonStates0 [0] = Brushes.DarkRed;
        }

        #region OpenCardChannel
        private void OpenCard1Channel1 ()
        {
            if ( ButtonStates0 [0] == buttonStateBrushList [0] )
            {
                scModel.SendChannelOpenRequest( 0, 1 );
                ButtonStates0 [0] = buttonStateBrushList [1];
            }
            else if ( ButtonStates0 [0] == buttonStateBrushList [1] )
            {
                scModel.SendChannelCloseRequest( 0, 1 );
                ButtonStates0 [0] = buttonStateBrushList [0];
            }
        }
        private void OpenCard1Channel2 ()
        {
            if ( ButtonStates0 [1] == buttonStateBrushList [0] )
            {
                scModel.SendChannelOpenRequest( 0, 2 );
                ButtonStates0 [1] = buttonStateBrushList [1];
            }
            if ( ButtonStates0 [1] == buttonStateBrushList [1] )
            {
                scModel.SendChannelCloseRequest( 0, 2 );
                ButtonStates0 [1] = buttonStateBrushList [0];
            }
        }
        private void OpenCard1Channel3 ()
        {
            if ( ButtonStates0 [2] == buttonStateBrushList [0] )
            {
                scModel.SendChannelOpenRequest( 0, 3 );
                ButtonStates0 [2] = buttonStateBrushList [1];
            }
            if ( ButtonStates0 [2] == buttonStateBrushList [1] )
            {
                scModel.SendChannelCloseRequest( 0, 3 );
                ButtonStates0 [2] = buttonStateBrushList [0];
            }
        }
        private void OpenCard1Channel4 ()
        {
            if ( ButtonStates0 [3] == buttonStateBrushList [0] )
            {
                scModel.SendChannelOpenRequest( 0, 4 );
                ButtonStates0 [3] = buttonStateBrushList [1];
            }
            if ( ButtonStates0 [3] == buttonStateBrushList [1] )
            {
                scModel.SendChannelCloseRequest( 0, 4 );
                ButtonStates0 [3] = buttonStateBrushList [0];
            }
        }
        private void OpenCard1Channel5 ()
        {
            if ( ButtonStates0 [4] == buttonStateBrushList [0] )
            {
                scModel.SendChannelOpenRequest( 0, 5 );
                ButtonStates0 [4] = buttonStateBrushList [1];
            }
            if ( ButtonStates0 [4] == buttonStateBrushList [1] )
            {
                scModel.SendChannelCloseRequest( 0, 5 );
                ButtonStates0 [4] = buttonStateBrushList [0];
            }
        }
        private void OpenCard1Channel6 ()
        {
            if ( ButtonStates0 [5] == buttonStateBrushList [0] )
            {
                scModel.SendChannelOpenRequest( 0, 6 );
                ButtonStates0 [5] = buttonStateBrushList [1];
            }
            if ( ButtonStates0 [5] == buttonStateBrushList [1] )
            {
                scModel.SendChannelCloseRequest( 0, 6 );
                ButtonStates0 [5] = buttonStateBrushList [0];
            }
        }
        private void OpenCard1Channel7 ()
        {
            if ( ButtonStates0 [6] == buttonStateBrushList [0] )
            {
                scModel.SendChannelOpenRequest( 0, 7 );
                ButtonStates0 [6] = buttonStateBrushList [1];
            }
            if ( ButtonStates0 [6] == buttonStateBrushList [1] )
            {
                scModel.SendChannelCloseRequest( 0, 7 );
                ButtonStates0 [6] = buttonStateBrushList [0];
            }
        }
        private void OpenCard1Channel8 ()
        {
            if ( ButtonStates0 [7] == buttonStateBrushList [0] )
            {
                scModel.SendChannelOpenRequest( 0, 8 );
                ButtonStates0 [7] = buttonStateBrushList [1];
            }
            if ( ButtonStates0 [7] == buttonStateBrushList [1] )
            {
                scModel.SendChannelCloseRequest( 0, 8 );
                ButtonStates0 [7] = buttonStateBrushList [0];
            }
        }
        private void OpenCard2Channel1 ()
        {
            if ( ButtonStates1 [0] == buttonStateBrushList [0] )
            {
                scModel.SendChannelOpenRequest( 1, 1 );
                ButtonStates1 [0] = buttonStateBrushList [1];
            }
            if ( ButtonStates1 [0] == buttonStateBrushList [1] )
            {
                scModel.SendChannelCloseRequest( 1, 1 );
                ButtonStates1 [0] = buttonStateBrushList [0];
            }
        }
        private void OpenCard2Channel2 ()
        {
            if ( ButtonStates1 [1] == buttonStateBrushList [0] )
            {
                scModel.SendChannelOpenRequest( 1, 2 );
                ButtonStates1 [1] = buttonStateBrushList [1];
            }
            if ( ButtonStates1 [1] == buttonStateBrushList [1] )
            {
                scModel.SendChannelCloseRequest( 1, 2 );
                ButtonStates1 [1] = buttonStateBrushList [0];
            }
        }
        private void OpenCard2Channel3 ()
        {
            if ( ButtonStates1 [2] == buttonStateBrushList [0] )
            {
                scModel.SendChannelOpenRequest( 1, 3 );
                ButtonStates1 [2] = buttonStateBrushList [1];
            }
            if ( ButtonStates1 [2] == buttonStateBrushList [1] )
            {
                scModel.SendChannelCloseRequest( 1, 3 );
                ButtonStates1 [2] = buttonStateBrushList [0];
            }
        }
        private void OpenCard2Channel4 ()
        {
            if ( ButtonStates1 [3] == buttonStateBrushList [0] )
            {
                scModel.SendChannelOpenRequest( 1, 4 );
                ButtonStates1 [3] = buttonStateBrushList [1];
            }
            if ( ButtonStates1 [3] == buttonStateBrushList [1] )
            {
                scModel.SendChannelCloseRequest( 1, 4 );
                ButtonStates1 [3] = buttonStateBrushList [0];
            }
        }
        private void OpenCard2Channel5 ()
        {
            if ( ButtonStates1 [4] == buttonStateBrushList [0] )
            {
                scModel.SendChannelOpenRequest( 1, 5 );
                ButtonStates1 [4] = buttonStateBrushList [1];
            }
            if ( ButtonStates1 [4] == buttonStateBrushList [1] )
            {
                scModel.SendChannelCloseRequest( 1, 5 );
                ButtonStates1 [4] = buttonStateBrushList [0];
            }
        }
        private void OpenCard2Channel6 ()
        {
            if ( ButtonStates1 [5] == buttonStateBrushList [0] )
            {
                scModel.SendChannelOpenRequest( 1, 6 );
                ButtonStates1 [5] = buttonStateBrushList [1];
            }
            if ( ButtonStates1 [5] == buttonStateBrushList [1] )
            {
                scModel.SendChannelCloseRequest( 1, 6 );
                ButtonStates1 [5] = buttonStateBrushList [0];
            }
        }
        private void OpenCard2Channel7 ()
        {
            if ( ButtonStates1 [6] == buttonStateBrushList [0] )
            {
                scModel.SendChannelOpenRequest( 1, 7 );
                ButtonStates1 [6] = buttonStateBrushList [1];
            }
            if ( ButtonStates1 [6] == buttonStateBrushList [1] )
            {
                scModel.SendChannelCloseRequest( 1, 7 );
                ButtonStates1 [6] = buttonStateBrushList [0];
            }
        }
        private void OpenCard2Channel8 ()
        {
            if ( ButtonStates1 [7] == buttonStateBrushList [0] )
            {
                scModel.SendChannelOpenRequest( 1, 8 );
                ButtonStates1 [7] = buttonStateBrushList [1];
            }
            if ( ButtonStates1 [7] == buttonStateBrushList [1] )
            {
                scModel.SendChannelCloseRequest( 1, 8 );
                ButtonStates1 [7] = buttonStateBrushList [0];
            }
        }
        private void OpenCard3Channel1 ()
        {
            if ( ButtonStates2 [0] == buttonStateBrushList [0] )
            {
                scModel.SendChannelOpenRequest( 2, 1 );
                ButtonStates2 [0] = buttonStateBrushList [1];
            }
            if ( ButtonStates2 [0] == buttonStateBrushList [1] )
            {
                scModel.SendChannelCloseRequest( 2, 1 );
                ButtonStates2 [0] = buttonStateBrushList [0];
            }
        }
        private void OpenCard3Channel2 ()
        {
            if ( ButtonStates2 [1] == buttonStateBrushList [0] )
            {
                scModel.SendChannelOpenRequest( 2, 2 );
                ButtonStates2 [1] = buttonStateBrushList [1];
            }
            if ( ButtonStates2 [1] == buttonStateBrushList [1] )
            {
                scModel.SendChannelCloseRequest( 2, 2 );
                ButtonStates2 [1] = buttonStateBrushList [0];
            }
        }
        private void OpenCard3Channel3 ()
        {
            if ( ButtonStates2 [2] == buttonStateBrushList [0] )
            {
                scModel.SendChannelOpenRequest( 2, 3 );
                ButtonStates2 [2] = buttonStateBrushList [1];
            }
            if ( ButtonStates2 [2] == buttonStateBrushList [1] )
            {
                scModel.SendChannelCloseRequest( 2, 3 );
                ButtonStates2 [2] = buttonStateBrushList [0];
            }
        }
        private void OpenCard3Channel4 ()
        {
            if ( ButtonStates2 [3] == buttonStateBrushList [0] )
            {
                scModel.SendChannelOpenRequest( 2, 4 );
                ButtonStates2 [3] = buttonStateBrushList [1];
            }
            if ( ButtonStates2 [3] == buttonStateBrushList [1] )
            {
                scModel.SendChannelCloseRequest( 2, 4 );
                ButtonStates2 [3] = buttonStateBrushList [0];
            }
        }
        private void OpenCard3Channel5 ()
        {
            if ( ButtonStates2 [4] == buttonStateBrushList [0] )
            {
                scModel.SendChannelOpenRequest( 2, 5 );
                ButtonStates2 [4] = buttonStateBrushList [1];
            }
            if ( ButtonStates2 [4] == buttonStateBrushList [1] )
            {
                scModel.SendChannelCloseRequest( 2, 5 );
                ButtonStates2 [4] = buttonStateBrushList [0];
            }
        }
        private void OpenCard3Channel6 ()
        {
            if ( ButtonStates2 [5] == buttonStateBrushList [0] )
            {
                scModel.SendChannelOpenRequest( 2, 6 );
                ButtonStates2 [5] = buttonStateBrushList [1];
            }
            if ( ButtonStates2 [5] == buttonStateBrushList [1] )
            {
                scModel.SendChannelCloseRequest( 2, 6 );
                ButtonStates2 [5] = buttonStateBrushList [0];
            }
        }
        private void OpenCard3Channel7 ()
        {
            if ( ButtonStates2 [6] == buttonStateBrushList [0] )
            {
                scModel.SendChannelOpenRequest( 2, 7 );
                ButtonStates2 [6] = buttonStateBrushList [1];
            }
            if ( ButtonStates2 [6] == buttonStateBrushList [1] )
            {
                scModel.SendChannelCloseRequest( 2, 7 );
                ButtonStates2 [6] = buttonStateBrushList [0];
            }
        }
        private void OpenCard3Channel8 ()
        {
            if ( ButtonStates2 [7] == buttonStateBrushList [0] )
            {
                scModel.SendChannelOpenRequest( 2, 8 );
                ButtonStates2 [7] = buttonStateBrushList [1];
            }
            if ( ButtonStates2 [7] == buttonStateBrushList [1] )
            {
                scModel.SendChannelCloseRequest( 2, 8 );
                ButtonStates2 [7] = buttonStateBrushList [0];
            }
        }
        #endregion OpenCardChannel
        #endregion Methods.SerialPort

        #region Methods.UI

        private void ChangeButtonState ( int cardIndex, int channelIndex, Brush brushToBeApplied)
        {
            Trace.WriteLine($"ChangeButtonState cardIndex: {cardIndex}, channelIndex: {channelIndex}, brushToBeApplied: {brushToBeApplied.ToString()}");
            switch ( cardIndex )
            {
                case 0:
                    buttonStates0 [channelIndex] = brushToBeApplied;
                    break;
                case 1:
                    buttonStates1 [channelIndex] = brushToBeApplied;
                    break;
                case 2:
                    buttonStates2 [channelIndex] = brushToBeApplied;
                    break;
                default:
                    break;
            }
        }

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
                errorStatusList0 [i] = errorStateBrushList [2];
                errorStatusList01 [i] = errorStateBrushList [2];
                errorStatusList1 [i] = errorStateBrushList [0];
                errorStatusList2 [i] = errorStateBrushList [1];
            }
        }
        private void PaintError1Brushes ()
        {
            for ( int i = 0; i < 8; i++ )
            {
                //errorStatusList01 [i] = errorStateBrushList [1];
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
                App.Current.Dispatcher.Invoke( () => CheckForUIUpdates( "ErrorReceived" ,model.MessageDateTime, model.MessageToShow ) );
            }
            else if ( e.PropertyName == "Card1ChannelCount" )
            {
                this.Card1ChannelCount = scModel.Card1ChannelCount;
                App.Current.Dispatcher.Invoke( () => CheckForUIUpdates( "Card1ChannelChanged", DateTime.Now, Card1ChannelCount.ToString() ) );
            }
        }

        public void CheckForUIUpdates (string updateType,DateTime dataTime, string data)
        {
            if ( updateType == "Card1ChannelChanged" )
            {
                int countOpen = Convert.ToInt32( data );
                int countClose = 8 - countOpen;
                for ( int i = 0; i < countOpen; i++ )
                {
                    ErrorStatusList0 [i] = errorStateBrushList [1];
                    ErrorStatusList01 [i] = errorStateBrushList [1];
                }
                int closedOnes = 7;
                for ( int i = countClose; i > 0 ; i-- )
                {
                    ErrorStatusList0 [closedOnes] = errorStateBrushList [2];
                    ErrorStatusList01 [closedOnes] = errorStateBrushList [2];
                    closedOnes--;
                }
            }
            else if ( updateType == "ErrorReceived" )
            {
                Trace.WriteLine( "Error Code solving" );
                if ( data == "hata000" && dataTime != DateTime.Now )
                {
                    errorStatusList0 [0] = errorStateBrushList [0];
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
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged ( string propertyName )
        {
            PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( propertyName ) );
        }
    }
}
