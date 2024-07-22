using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arti.MVVM.Model
{
    public class SerialCommunicationModel
    {

        #region privateVariables
        /// <summary>
        /// Declaring variables needed to create a serial connection
        /// </summary>

        private string deviceID; // Unique ID for communication handshake
        private readonly SerialPort selectedSerialPort;
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
        #endregion

        #region Constructor

        public SerialCommunicationModel (
            string deviceIDO, string portName, int baudRateIndex,
            int parityListIndex, int stopBitsIndex,
            int handShakesIndex, int readTimeOut, int writeTimeOut
            )
        {
            selectedBaudRate = baudRateList [baudRateIndex];
            selectedSerialPort = new SerialPort( portName, selectedBaudRate );
            deviceID = deviceIDO;
            selectedParity = parityList [parityListIndex];
            selectedSerialPort.Parity = selectedParity;
            selectedStopBit = stopBitList [stopBitsIndex];
            selectedSerialPort.StopBits = selectedStopBit;
            selectedHandShake = handShakeList [handShakesIndex];
            selectedSerialPort.Handshake = SelectedHandShake;
            selectedReadTimeOut = readTimeOut;
            selectedSerialPort.ReadTimeout = selectedReadTimeOut;
            selectedWriteTimeOut = writeTimeOut;
            selectedSerialPort.WriteTimeout = selectedWriteTimeOut;
        }
        #endregion

        #region Methods

        // Opens the serial port
        public void Open () => selectedSerialPort.Open();

        // Closes the serial port
        public void Close () => selectedSerialPort.Close();
        #endregion
    }
}
