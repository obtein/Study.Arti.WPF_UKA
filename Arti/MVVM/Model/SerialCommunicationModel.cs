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

        #region CardAndChannels
        /// <summary>
        /// For tracinkg error conditions
        /// </summary>
        private int card1Channel1IsOk;

        public int Card1Channel1IsOk
        {
            get
            {
                return card1Channel1IsOk;
            }
            set
            {
                if ( card1Channel1IsOk != value )
                {
                    card1Channel1IsOk = value;
                    OnPropertyChanged( nameof( Card1Channel1IsOk ) );
                }
            }
        }

        private int card1Channel2IsOk;

        public int Card1Channel2IsOk
        {
            get
            {
                return card1Channel2IsOk;
            }
            set
            {
                if ( card1Channel2IsOk != value )
                {
                    card1Channel2IsOk = value;
                    OnPropertyChanged( nameof( Card1Channel2IsOk ) );
                }
            }
        }

        private int card1Channel3IsOk;

        public int Card1Channel3IsOk
        {
            get
            {
                return card1Channel3IsOk;
            }
            set
            {
                if ( card1Channel3IsOk != value )
                {
                    card1Channel3IsOk = value;
                    OnPropertyChanged( nameof( Card1Channel3IsOk ) );
                }
            }
        }

        private int card1Channel4IsOk;

        public int Card1Channel4IsOk
        {
            get
            {
                return card1Channel4IsOk;
            }
            set
            {
                if ( card1Channel4IsOk != value )
                {
                    card1Channel4IsOk = value;
                    OnPropertyChanged( nameof( Card1Channel4IsOk ) );
                }
            }
        }

        private int card1Channel5IsOk;

        public int Card1Channel5IsOk
        {
            get
            {
                return card1Channel5IsOk;
            }
            set
            {
                if ( card1Channel5IsOk != value )
                {
                    card1Channel5IsOk = value;
                    OnPropertyChanged( nameof( Card1Channel5IsOk ) );
                }
            }
        }

        private int card1Channel6IsOk;

        public int Card1Channel6IsOk
        {
            get
            {
                return card1Channel6IsOk;
            }
            set
            {
                if ( card1Channel6IsOk != value )
                {
                    card1Channel6IsOk = value;
                    OnPropertyChanged( nameof( Card1Channel6IsOk ) );
                }
            }
        }

        private int card1Channel7IsOk;

        public int Card1Channel7IsOk
        {
            get
            {
                return card1Channel7IsOk;
            }
            set
            {
                if ( card1Channel7IsOk != value )
                {
                    card1Channel7IsOk = value;
                    OnPropertyChanged( nameof( Card1Channel7IsOk ) );
                }
            }
        }

        private int card1Channel8IsOk;

        public int Card1Channel8IsOk
        {
            get
            {
                return card1Channel8IsOk;
            }
            set
            {
                if ( card1Channel8IsOk != value )
                {
                    card1Channel8IsOk = value;
                    OnPropertyChanged( nameof( Card1Channel8IsOk ) );
                }
            }
        }

        private int card2Channel1IsOk;

        public int Card2Channel1IsOk
        {
            get
            {
                return card2Channel1IsOk;
            }
            set
            {
                if ( card2Channel1IsOk != value )
                {
                    card2Channel1IsOk = value;
                    OnPropertyChanged( nameof( Card2Channel1IsOk ) );
                }
            }
        }

        private int card2Channel2IsOk;

        public int Card2Channel2IsOk
        {
            get
            {
                return card2Channel2IsOk;
            }
            set
            {
                if ( card2Channel2IsOk != value )
                {
                    card2Channel2IsOk = value;
                    OnPropertyChanged( nameof( Card2Channel2IsOk ) );
                }
            }
        }

        private int card2Channel3IsOk;

        public int Card2Channel3IsOk
        {
            get
            {
                return card2Channel3IsOk;
            }
            set
            {
                if ( card2Channel3IsOk != value )
                {
                    card2Channel3IsOk = value;
                    OnPropertyChanged( nameof( Card2Channel3IsOk ) );
                }
            }
        }

        private int card2Channel4IsOk;

        public int Card2Channel4IsOk
        {
            get
            {
                return card2Channel4IsOk;
            }
            set
            {
                if ( card2Channel4IsOk != value )
                {
                    card2Channel4IsOk = value;
                    OnPropertyChanged( nameof( Card2Channel4IsOk ) );
                }
            }
        }

        private int card2Channel5IsOk;

        public int Card2Channel5IsOk
        {
            get
            {
                return card2Channel5IsOk;
            }
            set
            {
                if ( card2Channel5IsOk != value )
                {
                    card2Channel5IsOk = value;
                    OnPropertyChanged( nameof( Card2Channel5IsOk ) );
                }
            }
        }

        private int card2Channel6IsOk;

        public int Card2Channel6IsOk
        {
            get
            {
                return card2Channel6IsOk;
            }
            set
            {
                if ( card2Channel6IsOk != value )
                {
                    card2Channel6IsOk = value;
                    OnPropertyChanged( nameof( Card2Channel6IsOk ) );
                }
            }
        }

        private int card2Channel7IsOk;

        public int Card2Channel7IsOk
        {
            get
            {
                return card2Channel7IsOk;
            }
            set
            {
                if ( card2Channel7IsOk != value )
                {
                    card2Channel7IsOk = value;
                    OnPropertyChanged( nameof( Card2Channel7IsOk ) );
                }
            }
        }

        private int card2Channel8IsOk;

        public int Card2Channel8IsOk
        {
            get
            {
                return card2Channel8IsOk;
            }
            set
            {
                if ( card2Channel8IsOk != value )
                {
                    card2Channel8IsOk = value;
                    OnPropertyChanged( nameof( Card2Channel8IsOk ) );
                }
            }
        }

        private int card3Channel1IsOk;

        public int Card3Channel1IsOk
        {
            get
            {
                return card3Channel1IsOk;
            }
            set
            {
                if ( card3Channel1IsOk != value )
                {
                    card3Channel1IsOk = value;
                    OnPropertyChanged( nameof( Card3Channel1IsOk ) );
                }
            }
        }

        private int card3Channel2IsOk;

        public int Card3Channel2IsOk
        {
            get
            {
                return card3Channel2IsOk;
            }
            set
            {
                if ( card3Channel2IsOk != value )
                {
                    card3Channel2IsOk = value;
                    OnPropertyChanged( nameof( Card3Channel2IsOk ) );
                }
            }
        }

        private int card3Channel3IsOk;

        public int Card3Channel3IsOk
        {
            get
            {
                return card3Channel3IsOk;
            }
            set
            {
                if ( card3Channel3IsOk != value )
                {
                    card3Channel3IsOk = value;
                    OnPropertyChanged( nameof( Card3Channel3IsOk ) );
                }
            }
        }

        private int card3Channel4IsOk;

        public int Card3Channel4IsOk
        {
            get
            {
                return card3Channel4IsOk;
            }
            set
            {
                if ( card3Channel4IsOk != value )
                {
                    card3Channel4IsOk = value;
                    OnPropertyChanged( nameof( Card3Channel4IsOk ) );
                }
            }
        }

        private int card3Channel5IsOk;

        public int Card3Channel5IsOk
        {
            get
            {
                return card3Channel5IsOk;
            }
            set
            {
                if ( card3Channel5IsOk != value )
                {
                    card3Channel5IsOk = value;
                    OnPropertyChanged( nameof( Card3Channel5IsOk ) );
                }
            }
        }

        private int card3Channel6IsOk;

        public int Card3Channel6IsOk
        {
            get
            {
                return card3Channel6IsOk;
            }
            set
            {
                if ( card3Channel6IsOk != value )
                {
                    card3Channel6IsOk = value;
                    OnPropertyChanged( nameof( Card3Channel6IsOk ) );
                }
            }
        }

        private int card3Channel7IsOk;

        public int Card3Channel7IsOk
        {
            get
            {
                return card3Channel7IsOk;
            }
            set
            {
                if ( card3Channel7IsOk != value )
                {
                    card3Channel7IsOk = value;
                    OnPropertyChanged( nameof( Card3Channel7IsOk ) );
                }
            }
        }

        private int card3Channel8IsOk;

        public int Card3Channel8IsOk
        {
            get
            {
                return card3Channel8IsOk;
            }
            set
            {
                if ( card3Channel8IsOk != value )
                {
                    card3Channel8IsOk = value;
                    OnPropertyChanged( nameof( Card3Channel8IsOk ) );
                }
            }
        }

        /// <summary>
        /// For tracking whether communication is active or not
        /// </summary>
        private bool card1IsActive;

        public bool Card1IsActive
        {
            get
            {
                return card1IsActive;
            }
            set
            {
                if ( card1IsActive != value )
                {
                    card1IsActive = value;
                    OnPropertyChanged( nameof( Card1IsActive ) );
                }
            }
        }
        private bool card1Channel1IsActive;

        public bool Card1Channel1IsActive
        {
            get
            {
                return card1Channel1IsActive;
            }
            set
            {
                if ( card1Channel1IsActive != value )
                {
                    card1Channel1IsActive = value;
                    OnPropertyChanged( nameof( Card1Channel1IsActive ) );
                }
            }
        }
        private bool card1Channel2IsActive;

        public bool Card1Channel2IsActive
        {
            get
            {
                return card1Channel2IsActive;
            }
            set
            {
                if ( card1Channel2IsActive != value )
                {
                    card1Channel2IsActive = value;
                    OnPropertyChanged( nameof( Card1Channel2IsActive ) );
                }
            }
        }
        private bool card1Channel3IsActive;

        public bool Card1Channel3IsActive
        {
            get
            {
                return card1Channel3IsActive;
            }
            set
            {
                if ( card1Channel3IsActive != value )
                {
                    card1Channel3IsActive = value;
                    OnPropertyChanged( nameof( Card1Channel3IsActive ) );
                }
            }
        }
        private bool card1Channel4IsActive;

        public bool Card1Channel4IsActive
        {
            get
            {
                return card1Channel4IsActive;
            }
            set
            {
                if ( card1Channel4IsActive != value )
                {
                    card1Channel4IsActive = value;
                    OnPropertyChanged( nameof( Card1Channel4IsActive ) );
                }
            }
        }
        private bool card1Channel5IsActive;

        public bool Card1Channel5IsActive
        {
            get
            {
                return card1Channel5IsActive;
            }
            set
            {
                if ( card1Channel5IsActive != value )
                {
                    card1Channel5IsActive = value;
                    OnPropertyChanged( nameof( Card1Channel5IsActive ) );
                }
            }
        }
        private bool card1Channel6IsActive;

        public bool Card1Channel6IsActive
        {
            get
            {
                return card1Channel6IsActive;
            }
            set
            {
                if ( card1Channel6IsActive != value )
                {
                    card1Channel6IsActive = value;
                    OnPropertyChanged( nameof( Card1Channel6IsActive ) );
                }
            }
        }
        private bool card1Channel7IsActive;

        public bool Card1Channel7IsActive
        {
            get
            {
                return card1Channel7IsActive;
            }
            set
            {
                if ( card1Channel7IsActive != value )
                {
                    card1Channel7IsActive = value;
                    OnPropertyChanged( nameof( Card1Channel7IsActive ) );
                }
            }
        }
        private bool card1Channel8IsActive;

        public bool Card1Channel8IsActive
        {
            get
            {
                return card1Channel8IsActive;
            }
            set
            {
                if ( card1Channel8IsActive != value )
                {
                    card1Channel8IsActive = value;
                    OnPropertyChanged( nameof( Card1Channel8IsActive ) );
                }
            }
        }
        private bool card2IsActive;

        public bool Card2IsActive
        {
            get
            {
                return card2IsActive;
            }
            set
            {
                if ( card2IsActive != value )
                {
                    card2IsActive = value;
                    OnPropertyChanged( nameof( Card2IsActive ) );
                }
            }
        }
        private bool card2Channel1IsActive;

        public bool Card2Channel1IsActive
        {
            get
            {
                return card2Channel1IsActive;
            }
            set
            {
                if ( card2Channel1IsActive != value )
                {
                    card2Channel1IsActive = value;
                    OnPropertyChanged( nameof( Card2Channel1IsActive ) );
                }
            }
        }
        private bool card2Channel2IsActive;

        public bool Card2Channel2IsActive
        {
            get
            {
                return card2Channel2IsActive;
            }
            set
            {
                if ( card2Channel2IsActive != value )
                {
                    card2Channel2IsActive = value;
                    OnPropertyChanged( nameof( Card2Channel2IsActive ) );
                }
            }
        }
        private bool card2Channel3IsActive;

        public bool Card2Channel3IsActive
        {
            get
            {
                return card2Channel3IsActive;
            }
            set
            {
                if ( card2Channel3IsActive != value )
                {
                    card2Channel3IsActive = value;
                    OnPropertyChanged( nameof( Card2Channel3IsActive ) );
                }
            }
        }
        private bool card2Channel4IsActive;

        public bool Card2Channel4IsActive
        {
            get
            {
                return card2Channel4IsActive;
            }
            set
            {
                if ( card2Channel4IsActive != value )
                {
                    card2Channel4IsActive = value;
                    OnPropertyChanged( nameof( Card2Channel4IsActive ) );
                }
            }
        }
        private bool card2Channel5IsActive;

        public bool Card2Channel5IsActive
        {
            get
            {
                return card2Channel5IsActive;
            }
            set
            {
                if ( card2Channel5IsActive != value )
                {
                    card2Channel5IsActive = value;
                    OnPropertyChanged( nameof( Card2Channel5IsActive ) );
                }
            }
        }
        private bool card2Channel6IsActive;

        public bool Card2Channel6IsActive
        {
            get
            {
                return card2Channel6IsActive;
            }
            set
            {
                if ( card2Channel6IsActive != value )
                {
                    card2Channel6IsActive = value;
                    OnPropertyChanged( nameof( Card2Channel6IsActive ) );
                }
            }
        }
        private bool card2Channel7IsActive;

        public bool Card2Channel7IsActive
        {
            get
            {
                return card2Channel7IsActive;
            }
            set
            {
                if ( card2Channel7IsActive != value )
                {
                    card2Channel7IsActive = value;
                    OnPropertyChanged( nameof( Card2Channel7IsActive ) );
                }
            }
        }
        private bool card2Channel8IsActive;

        public bool Card2Channel8IsActive
        {
            get
            {
                return card2Channel8IsActive;
            }
            set
            {
                if ( card2Channel8IsActive != value )
                {
                    card2Channel8IsActive = value;
                    OnPropertyChanged( nameof( Card2Channel8IsActive ) );
                }
            }
        }
        private bool card3IsActive;

        public bool Card3IsActive
        {
            get
            {
                return card3IsActive;
            }
            set
            {
                if ( card3IsActive != value )
                {
                    card3IsActive = value;
                    OnPropertyChanged( nameof( Card3IsActive ) );
                }
            }
        }
        private bool card3Channel1IsActive;

        public bool Card3Channel1IsActive
        {
            get
            {
                return card3Channel1IsActive;
            }
            set
            {
                if ( card3Channel1IsActive != value )
                {
                    card3Channel1IsActive = value;
                    OnPropertyChanged( nameof( Card3Channel1IsActive ) );
                }
            }
        }
        private bool card3Channel2IsActive;

        public bool Card3Channel2IsActive
        {
            get
            {
                return card3Channel2IsActive;
            }
            set
            {
                if ( card3Channel2IsActive != value )
                {
                    card3Channel2IsActive = value;
                    OnPropertyChanged( nameof( Card3Channel2IsActive ) );
                }
            }
        }
        private bool card3Channel3IsActive;

        public bool Card3Channel3IsActive
        {
            get
            {
                return card3Channel3IsActive;
            }
            set
            {
                if ( card3Channel3IsActive != value )
                {
                    card3Channel3IsActive = value;
                    OnPropertyChanged( nameof( Card3Channel3IsActive ) );
                }
            }
        }
        private bool card3Channel4IsActive;

        public bool Card3Channel4IsActive
        {
            get
            {
                return card3Channel4IsActive;
            }
            set
            {
                if ( card3Channel4IsActive != value )
                {
                    card3Channel4IsActive = value;
                    OnPropertyChanged( nameof( Card3Channel4IsActive ) );
                }
            }
        }
        private bool card3Channel5IsActive;

        public bool Card3Channel5IsActive
        {
            get
            {
                return card3Channel5IsActive;
            }
            set
            {
                if ( card3Channel5IsActive != value )
                {
                    card3Channel5IsActive = value;
                    OnPropertyChanged( nameof( Card3Channel5IsActive ) );
                }
            }
        }
        private bool card3Channel6IsActive;

        public bool Card3Channel6IsActive
        {
            get
            {
                return card3Channel6IsActive;
            }
            set
            {
                if ( card3Channel6IsActive != value )
                {
                    card3Channel6IsActive = value;
                    OnPropertyChanged( nameof( Card3Channel6IsActive ) );
                }
            }
        }
        private bool card3Channel7IsActive;

        public bool Card3Channel7IsActive
        {
            get
            {
                return card3Channel7IsActive;
            }
            set
            {
                if ( card3Channel7IsActive != value )
                {
                    card3Channel7IsActive = value;
                    OnPropertyChanged( nameof( Card3Channel7IsActive ) );
                }
            }
        }
        private bool card3Channel8IsActive;

        public bool Card3Channel8IsActive
        {
            get
            {
                return card3Channel8IsActive;
            }
            set
            {
                if ( card3Channel8IsActive != value )
                {
                    card3Channel8IsActive = value;
                    OnPropertyChanged( nameof( Card3Channel8IsActive ) );
                }
            }
        }

        /// <summary>
        ///  For tracking cards channel count
        /// </summary>
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

        private int card2ChannelCount;

        public int Card2ChannelCount
        {
            get
            {
                return card2ChannelCount;
            }
            set
            {
                if ( card2ChannelCount != value )
                {
                    card2ChannelCount = value;
                    OnPropertyChanged( nameof( Card2ChannelCount ) );
                }
            }
        }

        private int card3ChannelCount;

        public int Card3ChannelCount
        {
            get
            {
                return card3ChannelCount;
            }
            set
            {
                if ( card3ChannelCount != value )
                {
                    card3ChannelCount = value;
                    OnPropertyChanged( nameof( Card3ChannelCount ) );
                }
            }
        }

        /// <summary>
        /// For tracking card voltages and channel currents
        /// </summary>

        private int card1Voltage;

        public int Card1Voltage
        {
            get
            {
                return card1Voltage;
            }
            set
            {
                if ( card1Voltage != value )
                {
                    card1Voltage = value;
                    OnPropertyChanged( nameof( Card1Voltage ) );
                }
            }
        }

        private int card1Channel1Current;

        public int Card1Channel1Current
        {
            get
            {
                return card1Channel1Current;
            }
            set
            {
                if ( card1Channel1Current != value )
                {
                    card1Channel1Current = value;
                    OnPropertyChanged( nameof( Card1Channel1Current ) );
                }
            }
        }

        private int card1Channel2Current;

        public int Card1Channel2Current
        {
            get
            {
                return card1Channel2Current;
            }
            set
            {
                if ( card1Channel2Current != value )
                {
                    card1Channel2Current = value;
                    OnPropertyChanged( nameof( Card1Channel2Current ) );
                }
            }
        }

        private int card1Channel3Current;

        public int Card1Channel3Current
        {
            get
            {
                return card1Channel3Current;
            }
            set
            {
                if ( card1Channel3Current != value )
                {
                    card1Channel3Current = value;
                    OnPropertyChanged( nameof( Card1Channel3Current ) );
                }
            }
        }

        private int card1Channel4Current;

        public int Card1Channel4Current
        {
            get
            {
                return card1Channel4Current;
            }
            set
            {
                if ( card1Channel4Current != value )
                {
                    card1Channel4Current = value;
                    OnPropertyChanged( nameof( Card1Channel4Current ) );
                }
            }
        }

        private int card1Channel5Current;

        public int Card1Channel5Current
        {
            get
            {
                return card1Channel5Current;
            }
            set
            {
                if ( card1Channel5Current != value )
                {
                    card1Channel5Current = value;
                    OnPropertyChanged( nameof( Card1Channel5Current ) );
                }
            }
        }

        private int card1Channel6Current;

        public int Card1Channel6Current
        {
            get
            {
                return card1Channel6Current;
            }
            set
            {
                if ( card1Channel6Current != value )
                {
                    card1Channel6Current = value;
                    OnPropertyChanged( nameof( Card1Channel6Current ) );
                }
            }
        }

        private int card1Channel7Current;

        public int Card1Channel7Current
        {
            get
            {
                return card1Channel7Current;
            }
            set
            {
                if ( card1Channel7Current != value )
                {
                    card1Channel7Current = value;
                    OnPropertyChanged( nameof( Card1Channel7Current ) );
                }
            }
        }

        private int card1Channel8Current;

        public int Card1Channel8Current
        {
            get
            {
                return card1Channel8Current;
            }
            set
            {
                if ( card1Channel8Current != value )
                {
                    card1Channel8Current = value;
                    OnPropertyChanged( nameof( Card1Channel8Current ) );
                }
            }
        }

        private int card2Voltage;

        public int Card2Voltage
        {
            get
            {
                return card2Voltage;
            }
            set
            {
                if ( card2Voltage != value )
                {
                    card2Voltage = value;
                    OnPropertyChanged( nameof( Card2Voltage ) );
                }
            }
        }

        private int card2Channel1Current;

        public int Card2Channel1Current
        {
            get
            {
                return card2Channel1Current;
            }
            set
            {
                if ( card2Channel1Current != value )
                {
                    card2Channel1Current = value;
                    OnPropertyChanged( nameof( Card2Channel1Current ) );
                }
            }
        }

        private int card2Channel2Current;

        public int Card2Channel2Current
        {
            get
            {
                return card2Channel2Current;
            }
            set
            {
                if ( card2Channel2Current != value )
                {
                    card2Channel2Current = value;
                    OnPropertyChanged( nameof( Card2Channel2Current ) );
                }
            }
        }

        private int card2Channel3Current;

        public int Card2Channel3Current
        {
            get
            {
                return card2Channel3Current;
            }
            set
            {
                if ( card2Channel3Current != value )
                {
                    card2Channel3Current = value;
                    OnPropertyChanged( nameof( Card2Channel3Current ) );
                }
            }
        }

        private int card2Channel4Current;

        public int Card2Channel4Current
        {
            get
            {
                return card2Channel4Current;
            }
            set
            {
                if ( card2Channel4Current != value )
                {
                    card2Channel4Current = value;
                    OnPropertyChanged( nameof( Card2Channel4Current ) );
                }
            }
        }

        private int card2Channel5Current;

        public int Card2Channel5Current
        {
            get
            {
                return card2Channel5Current;
            }
            set
            {
                if ( card2Channel5Current != value )
                {
                    card2Channel5Current = value;
                    OnPropertyChanged( nameof( Card2Channel5Current ) );
                }
            }
        }

        private int card2Channel6Current;

        public int Card2Channel6Current
        {
            get
            {
                return card2Channel6Current;
            }
            set
            {
                if ( card2Channel6Current != value )
                {
                    card2Channel6Current = value;
                    OnPropertyChanged( nameof( Card2Channel6Current ) );
                }
            }
        }

        private int card2Channel7Current;

        public int Card2Channel7Current
        {
            get
            {
                return card2Channel7Current;
            }
            set
            {
                if ( card2Channel7Current != value )
                {
                    card2Channel7Current = value;
                    OnPropertyChanged( nameof( Card2Channel7Current ) );
                }
            }
        }

        private int card2Channel8Current;

        public int Card2Channel8Current
        {
            get
            {
                return card2Channel8Current;
            }
            set
            {
                if ( card2Channel8Current != value )
                {
                    card2Channel8Current = value;
                    OnPropertyChanged( nameof( Card2Channel8Current ) );
                }
            }
        }

        private int card3Voltage;

        public int Card3Voltage
        {
            get
            {
                return card3Voltage;
            }
            set
            {
                if ( card3Voltage != value )
                {
                    card3Voltage = value;
                    OnPropertyChanged( nameof( Card3Voltage ) );
                }
            }
        }

        private int card3Channel1Current;

        public int Card3Channel1Current
        {
            get
            {
                return card3Channel1Current;
            }
            set
            {
                if ( card3Channel1Current != value )
                {
                    card3Channel1Current = value;
                    OnPropertyChanged( nameof( Card3Channel1Current ) );
                }
            }
        }

        private int card3Channel2Current;

        public int Card3Channel2Current
        {
            get
            {
                return card3Channel2Current;
            }
            set
            {
                if ( card3Channel2Current != value )
                {
                    card3Channel2Current = value;
                    OnPropertyChanged( nameof( Card3Channel2Current ) );
                }
            }
        }

        private int card3Channel3Current;

        public int Card3Channel3Current
        {
            get
            {
                return card3Channel3Current;
            }
            set
            {
                if ( card3Channel3Current != value )
                {
                    card3Channel3Current = value;
                    OnPropertyChanged( nameof( Card3Channel3Current ) );
                }
            }
        }

        private int card3Channel4Current;

        public int Card3Channel4Current
        {
            get
            {
                return card3Channel4Current;
            }
            set
            {
                if ( card3Channel4Current != value )
                {
                    card3Channel4Current = value;
                    OnPropertyChanged( nameof( Card3Channel4Current ) );
                }
            }
        }

        private int card3Channel5Current;

        public int Card3Channel5Current
        {
            get
            {
                return card3Channel5Current;
            }
            set
            {
                if ( card3Channel5Current != value )
                {
                    card3Channel5Current = value;
                    OnPropertyChanged( nameof( Card3Channel5Current ) );
                }
            }
        }

        private int card3Channel6Current;

        public int Card3Channel6Current
        {
            get
            {
                return card3Channel6Current;
            }
            set
            {
                if ( card3Channel6Current != value )
                {
                    card3Channel6Current = value;
                    OnPropertyChanged( nameof( Card3Channel6Current ) );
                }
            }
        }

        private int card3Channel7Current;

        public int Card3Channel7Current
        {
            get
            {
                return card3Channel7Current;
            }
            set
            {
                if ( card3Channel7Current != value )
                {
                    card3Channel7Current = value;
                    OnPropertyChanged( nameof( Card3Channel7Current ) );
                }
            }
        }

        private int card3Channel8Current;

        public int Card3Channel8Current
        {
            get
            {
                return card3Channel8Current;
            }
            set
            {
                if ( card3Channel8Current != value )
                {
                    card3Channel8Current = value;
                    OnPropertyChanged( nameof( Card3Channel8Current ) );
                }
            }
        }
        #endregion

        #endregion

        #region Constructor

        public SerialCommunicationModel (
            string deviceIDO, string portName, int baudRate,
            Parity parityC, StopBits stopBitsC,
            Handshake handShakeC, int readTimeOut, int writeTimeOut
            )
        {
            Card1ChannelCount = 0;
            Card2ChannelCount = 0;
            Card3ChannelCount = 0;
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
        /// 1 - 0xC9 is for Channel information response
        ///     response is 0x02 0x39 0xC9 0x30 0x36 dataToBeChecked checkSum 0x03
        ///     dataToBeChecked consist of 6 bytes and each 2 byte represents 1 card
        ///     first two bytes are for card1
        ///     second two are for card2
        ///     third are for card3
        ///     those values show the channel amount on each card
        /// 2 - 0xCA is for Analog information response
        ///     response is 0x02 0x39 0xCA 0x36 0x43 dataToBeChecked checksum 0x03
        ///     dataToBeChecked consist of 108 bytes, 36 for each card
        ///     each part of the data i.e 0-35 card1, 36-71 card2, 72-107 card3 will be divided 
        ///     as first 4 bytes are for Card Voltage, second 4 are for channel1 current
        ///     third 4 are for channel2 current and so on
        /// 3 - 0xCB is for Card Error Inspection response
        ///     response is 0x02 0x39 0xCB 0x33 0x30 dataToBeChecked checkSum 0x03
        ///     dataToBeChecked consist of 48 bytes, 16 for each card
        ///     each part of the data i.e. 0-15 card1, 16-31 card2, 31-47 card3 will be divided
        ///     as bit0 represents ShortCircuit, bit1 represents OverCurrent, bi2 represents VoltageError
        ///     bit3-7 are reserved.
        ///     ******NOTE : if there is no communication data will be (128)!!! 
        /// </summary>
        /// <param name="msgCode"></param>
        /// <param name="dataToBeChecked"></param>
        /// <returns></returns>
        private async Task WhatToDoWithData (byte msgCode,byte [] dataToBeChecked)
        {
            if ( msgCode == 0xC9 ) // Channel info response 6Byte each cards channel will be sent as 2 bytes
            {
                await Task.Run( () => Card1ChannelCount = CalculateDataLength( dataToBeChecked [0], dataToBeChecked [1] ) );
                await Task.Run( () => Card2ChannelCount = CalculateDataLength( dataToBeChecked [2], dataToBeChecked [3] ) );
                await Task.Run( () => Card3ChannelCount = CalculateDataLength( dataToBeChecked [4], dataToBeChecked [5] ) );
            }
            else if ( msgCode == 0xCA ) // Analog info response for each card first 4 byte is card voltage second 4 byte channel1 current third 4 byte channel2 current and so on
            {
                // Card 1
                Card1Voltage = Calculate4byteInput( dataToBeChecked [0], dataToBeChecked [1], dataToBeChecked [2], dataToBeChecked [3] );
                Card1Channel1Current = Calculate4byteInput( dataToBeChecked [4], dataToBeChecked [5], dataToBeChecked [6], dataToBeChecked [7] );
                Card1Channel2Current = Calculate4byteInput( dataToBeChecked [8], dataToBeChecked [9], dataToBeChecked [10], dataToBeChecked [11] );
                Card1Channel3Current = Calculate4byteInput( dataToBeChecked [12], dataToBeChecked [13], dataToBeChecked [14], dataToBeChecked [15] );
                Card1Channel4Current = Calculate4byteInput( dataToBeChecked [16], dataToBeChecked [17], dataToBeChecked [18], dataToBeChecked [19] );
                Card1Channel5Current = Calculate4byteInput( dataToBeChecked [20], dataToBeChecked [21], dataToBeChecked [22], dataToBeChecked [23] );
                Card1Channel6Current = Calculate4byteInput( dataToBeChecked [24], dataToBeChecked [25], dataToBeChecked [26], dataToBeChecked [27] );
                Card1Channel7Current = Calculate4byteInput( dataToBeChecked [28], dataToBeChecked [29], dataToBeChecked [30], dataToBeChecked [31] );
                Card1Channel8Current = Calculate4byteInput( dataToBeChecked [32], dataToBeChecked [33], dataToBeChecked [34], dataToBeChecked [35] );
                // Card 2
                Card2Voltage = Calculate4byteInput( dataToBeChecked [36], dataToBeChecked [37], dataToBeChecked [38], dataToBeChecked [39] );
                Card2Channel1Current = Calculate4byteInput( dataToBeChecked [40], dataToBeChecked [41], dataToBeChecked [42], dataToBeChecked [43] );
                Card2Channel2Current = Calculate4byteInput( dataToBeChecked [44], dataToBeChecked [45], dataToBeChecked [46], dataToBeChecked [47] );
                Card2Channel3Current = Calculate4byteInput( dataToBeChecked [48], dataToBeChecked [49], dataToBeChecked [50], dataToBeChecked [51] );
                Card2Channel4Current = Calculate4byteInput( dataToBeChecked [52], dataToBeChecked [53], dataToBeChecked [54], dataToBeChecked [55] );
                Card2Channel5Current = Calculate4byteInput( dataToBeChecked [56], dataToBeChecked [57], dataToBeChecked [58], dataToBeChecked [59] );
                Card2Channel6Current = Calculate4byteInput( dataToBeChecked [60], dataToBeChecked [61], dataToBeChecked [62], dataToBeChecked [63] );
                Card2Channel7Current = Calculate4byteInput( dataToBeChecked [64], dataToBeChecked [65], dataToBeChecked [66], dataToBeChecked [67] );
                Card2Channel8Current = Calculate4byteInput( dataToBeChecked [68], dataToBeChecked [69], dataToBeChecked [70], dataToBeChecked [71] );
                // Card 3
                Card3Voltage = Calculate4byteInput( dataToBeChecked [72], dataToBeChecked [73], dataToBeChecked [74], dataToBeChecked [75] );
                Card3Channel1Current = Calculate4byteInput( dataToBeChecked [76], dataToBeChecked [77], dataToBeChecked [78], dataToBeChecked [79] );
                Card3Channel2Current = Calculate4byteInput( dataToBeChecked [80], dataToBeChecked [81], dataToBeChecked [82], dataToBeChecked [83] );
                Card3Channel3Current = Calculate4byteInput( dataToBeChecked [84], dataToBeChecked [85], dataToBeChecked [86], dataToBeChecked [87] );
                Card3Channel4Current = Calculate4byteInput( dataToBeChecked [88], dataToBeChecked [89], dataToBeChecked [90], dataToBeChecked [91] );
                Card3Channel5Current = Calculate4byteInput( dataToBeChecked [92], dataToBeChecked [93], dataToBeChecked [94], dataToBeChecked [95] );
                Card3Channel6Current = Calculate4byteInput( dataToBeChecked [96], dataToBeChecked [97], dataToBeChecked [98], dataToBeChecked [99] );
                Card3Channel7Current = Calculate4byteInput( dataToBeChecked [100], dataToBeChecked [101], dataToBeChecked [102], dataToBeChecked [103] );
                Card3Channel8Current = Calculate4byteInput( dataToBeChecked [104], dataToBeChecked [105], dataToBeChecked [106], dataToBeChecked [107] );
            }
            else if ( msgCode == 0xCB ) // Error inspection response for each card  bit0(short circuit),bit1 (over current), bit2(voltage error), bit3-7 reserved => byte0-15 card1, 16-31 card2, 31-47 card3 
            {
                byte shortCircuit = 0b00000001; // 1
                byte overCurrent = 0b00000010; // 2
                byte voltageError = 0b00000100; // 4
                /* 1 = shortCircuit 
                 * 2- overCurrent 
                 * 3- shortCircuit + overCurrent
                 * 4- voltageError
                 * 5- shortCircuit + voltageError 
                 * 6- overCurrent + voltageError
                 * 7- shortCircuit + overCurrent + voltageError
                 * 128- No communication 
                 */
                Card1Channel1IsActive = dataToBeChecked [0] != 128 && dataToBeChecked [1] != 128;
                Card1Channel1IsOk = CalculateDataLength( dataToBeChecked [0], dataToBeChecked [1] );
                Card1Channel2IsActive = dataToBeChecked [2] != 128 && dataToBeChecked [3] != 128;
                Card1Channel2IsOk = CalculateDataLength( dataToBeChecked [2], dataToBeChecked [3] );
                Card1Channel3IsActive = dataToBeChecked [4] != 128 && dataToBeChecked [5] != 128;
                Card1Channel3IsOk = CalculateDataLength( dataToBeChecked [4], dataToBeChecked [5] );
                Card1Channel4IsActive = dataToBeChecked [6] != 128 && dataToBeChecked [7] != 128;
                Card1Channel4IsOk = CalculateDataLength( dataToBeChecked [6], dataToBeChecked [7] );
                Card1Channel5IsActive = dataToBeChecked [8] != 128 && dataToBeChecked [9] != 128;
                Card1Channel5IsOk = CalculateDataLength( dataToBeChecked [8], dataToBeChecked [9] );
                Card1Channel6IsActive = dataToBeChecked [10] != 128 && dataToBeChecked [11] != 128;
                Card1Channel6IsOk = CalculateDataLength( dataToBeChecked [10], dataToBeChecked [11] );
                Card1Channel7IsActive = dataToBeChecked [12] != 128 && dataToBeChecked [13] != 128;
                Card1Channel7IsOk = CalculateDataLength( dataToBeChecked [12], dataToBeChecked [13] );
                Card1Channel8IsActive = dataToBeChecked [14] != 128 && dataToBeChecked [15] != 128;
                Card1Channel8IsOk = CalculateDataLength( dataToBeChecked [14], dataToBeChecked [15] );
                Card2Channel1IsActive = dataToBeChecked [16] != 128 && dataToBeChecked [17] != 128;
                Card2Channel1IsOk = CalculateDataLength( dataToBeChecked [16], dataToBeChecked [17] );
                Card2Channel2IsActive = dataToBeChecked [18] != 128 && dataToBeChecked [19] != 128;
                Card2Channel2IsOk = CalculateDataLength( dataToBeChecked [18], dataToBeChecked [19] );
                Card2Channel3IsActive = dataToBeChecked [20] != 128 && dataToBeChecked [21] != 128;
                Card2Channel3IsOk = CalculateDataLength( dataToBeChecked [20], dataToBeChecked [21] );
                Card2Channel4IsActive = dataToBeChecked [22] != 128 && dataToBeChecked [23] != 128;
                Card2Channel4IsOk = CalculateDataLength( dataToBeChecked [22], dataToBeChecked [23] );
                Card2Channel5IsActive = dataToBeChecked [24] != 128 && dataToBeChecked [25] != 128;
                Card2Channel5IsOk = CalculateDataLength( dataToBeChecked [24], dataToBeChecked [25] );
                Card2Channel6IsActive = dataToBeChecked [26] != 128 && dataToBeChecked [27] != 128;
                Card2Channel6IsOk = CalculateDataLength( dataToBeChecked [26], dataToBeChecked [27] );
                Card2Channel7IsActive = dataToBeChecked [28] != 128 && dataToBeChecked [29] != 128;
                Card2Channel7IsOk = CalculateDataLength( dataToBeChecked [28], dataToBeChecked [29] );
                Card2Channel8IsActive = dataToBeChecked [30] != 128 && dataToBeChecked [31] != 128;
                Card2Channel8IsOk = CalculateDataLength( dataToBeChecked [30], dataToBeChecked [31] );
                Card3Channel1IsActive = dataToBeChecked [32] != 128 && dataToBeChecked [33] != 128;
                Card3Channel1IsOk = CalculateDataLength( dataToBeChecked [32], dataToBeChecked [33] );
                Card3Channel2IsActive = dataToBeChecked [34] != 128 && dataToBeChecked [35] != 128;
                Card3Channel2IsOk = CalculateDataLength( dataToBeChecked [34], dataToBeChecked [35] );
                Card3Channel3IsActive = dataToBeChecked [36] != 128 && dataToBeChecked [37] != 128;
                Card3Channel3IsOk = CalculateDataLength( dataToBeChecked [36], dataToBeChecked [37] );
                Card3Channel4IsActive = dataToBeChecked [38] != 128 && dataToBeChecked [39] != 128;
                Card3Channel4IsOk = CalculateDataLength( dataToBeChecked [38], dataToBeChecked [39] );
                Card3Channel5IsActive = dataToBeChecked [40] != 128 && dataToBeChecked [41] != 128;
                Card3Channel5IsOk = CalculateDataLength( dataToBeChecked [40], dataToBeChecked [41] );
                Card3Channel6IsActive = dataToBeChecked [42] != 128 && dataToBeChecked [43] != 128;
                Card3Channel6IsOk = CalculateDataLength( dataToBeChecked [42], dataToBeChecked [43] );
                Card3Channel7IsActive = dataToBeChecked [44] != 128 && dataToBeChecked [45] != 128;
                Card3Channel7IsOk = CalculateDataLength( dataToBeChecked [44], dataToBeChecked [45] );
                Card3Channel8IsActive = dataToBeChecked [46] != 128 && dataToBeChecked [47] != 128;
                Card3Channel8IsOk = CalculateDataLength( dataToBeChecked [46], dataToBeChecked [47] );
            }
            else if ( msgCode == 0xCE ) // Card 1 is open/closed 2Byte lsb 1open 0 closed
            {
                Card1IsActive = dataToBeChecked [1] == 1;
            }
            else if ( msgCode == 0xCF ) // Card 2 is open/closed 2Byte lsb 1open 0 closed
            {
                Card2IsActive = dataToBeChecked [1] == 1;
            }
            else if ( msgCode == 0xD0 ) // Card 3 is open/closed 2Byte lsb 1open 0 closed
            {
                Card3IsActive = dataToBeChecked [1] == 1;
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
        /// Calculates 4 byte addition with the help of CalculateDataLength
        /// </summary>
        /// <param name="firstMSB"></param>
        /// <param name="firstLSB"></param>
        /// <param name="secondMSB"></param>
        /// <param name="secondLSB"></param>
        /// <returns></returns>
        private int Calculate4byteInput (byte firstMSB, byte firstLSB, byte secondMSB, byte secondLSB)
        {
            return CalculateDataLength(firstMSB,firstLSB) + CalculateDataLength(secondMSB,secondLSB);
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
