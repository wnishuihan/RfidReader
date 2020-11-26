using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RfidLibrary
{
    public class ProxySocket : IReader
    {
        public Socket CSocket { get; }
        private static bool _isUdpSearchListener;
        private bool _isReceiving;
        private ReceivedValueType _curType;
        private GetOperateResultHandler _getOperateResultHandler;

        public ProxySocket(string ip, int port, AsyncCallback callback)
        {
            CSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var ipAdd = IPAddress.Parse(ip);
            var point = new IPEndPoint(ipAdd, Convert.ToInt32(port));

            try
            {
                CSocket.BeginConnect(point, callback, CSocket);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public ReaderType GetReadType()
        {
            return ReaderType.Socket;
        }

        public event ReaderCommandListenerHandle CommandListener;

        /// <summary>
        /// 搜索设备
        /// </summary>
        /// <param name="deviceHandle"></param>
        public static void SearchingDevices(HandleDevice deviceHandle)
        {
            try
            {
                var port = int.Parse("1500");
                var udpSend = new UdpClient();

                //得到客户机IP  
                var ipep = new IPEndPoint(IPAddress.Parse("255.255.255.255"), port);

                var cmd = new byte[] {0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39,
                0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39,
                0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39,
                0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39};

                udpSend.Send(cmd, cmd.Length, ipep);

                _isUdpSearchListener = true;
                var newThread = new Thread(DevicesSearchingListener) { IsBackground = true };
                newThread.Start(new DeviceSearch() { UdpRecev = udpSend, DeviceHandle = deviceHandle });
            }
            catch (Exception ex)
            {
                throw new Exception("The search appliance abnormal", ex);
            }
        }

        private class DeviceSearch
        {
            public UdpClient UdpRecev { get; set; }
            public HandleDevice DeviceHandle { get; set; }
        }

        private static void DevicesSearchingListener(object obj)
        {
            var deviceSearch = (DeviceSearch)obj;
            var remoteIpep = new IPEndPoint(IPAddress.Parse("255.255.255.255"), 1500);
            var udpReceive = deviceSearch.UdpRecev;
            var deviceHandle = deviceSearch.DeviceHandle;

            try
            {
                while (_isUdpSearchListener)
                {
                    var data = udpReceive.Receive(ref remoteIpep);
                    //完整性判断  
                    if (data.Length != 35) continue;
                    var device = new Device {OriginalBytes = data};
                    deviceHandle.Invoke(device);
                }
            }
            catch (Exception)
            {
                //throw;
            }
            finally
            {
                Thread.CurrentThread.Abort();
                udpReceive.Close();
            }
        }

        /// <summary>
        /// 获得本机IPv4地址
        /// </summary>
        /// <returns></returns>
        public static List<IPAddress> GetIpAddress()
        {

            var ipas = new List<IPAddress>();
            var ips = Dns.GetHostAddresses(Dns.GetHostName());
            if (ips.Length >= 1)
            {
                ipas.AddRange(ips.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork));
            }
            return ipas;
        }

        public bool IsOpenOrConnection()
        {
            return CSocket?.Connected ?? false;
        }

        public bool DisConnection()
        {
            _isUdpSearchListener = false;//停止搜索监听
            _isReceiving = false;
            var result = false;
            try
            {
                if (CSocket != null && CSocket.Connected)
                {
                    CSocket.Close();
                    result = true;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred while closing the socket", ex);//关闭套接字时发生异常
            }
            return result;
        }

        public void GetReadFirmwareVersion(int deviceNo)
        {
            if (CSocket == null || !CSocket.Connected)
            {
                throw new ArgumentNullException("Socket", "Reader object is empty");
            }

            _curType = ReceivedValueType.GetFirmwareVersion;
            //发送数据
            try
            {
                var command = Commands.GetFirmwareVersionCommand((byte)deviceNo);
                SentCommand(command);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred while retrieving firmware version", ex);
            }
            
        }

        public void IdentifySingleTag(int deviceNo)
        {
            if (CSocket == null || !CSocket.Connected)
            {
                throw new ArgumentNullException("Socket", "Reader object is empty");
            }

            _curType = ReceivedValueType.IdentifySingleTag;
            //发送数据
            try
            {
                var command = Commands.GetIdentifySingleTagCommand((byte)deviceNo);
                SentCommand(command);
            }
            catch (Exception ex)
            {
                Console.WriteLine("An exception occurred while reading a single card," + ex.Message);
            }
        }

        public void IdentifyMultiTag(int deviceNo)
        {
            if (CSocket == null || !CSocket.Connected)
            {
                throw new ArgumentNullException("Socket", "Reader object is empty");
            }
            
            _curType = ReceivedValueType.IdentifyMultieTag;
            //发送命令
            try
            {
                var command = Commands.GetIdentifyMultiTagCommand((byte)deviceNo);
                SentCommand(command);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred while reading multi-tag", ex);
            }
        }

        public void StopReadingTag(int deviceNo)
        {
            if (CSocket == null || !CSocket.Connected)
            {
                throw new ArgumentNullException("Socket", "Reader object is empty");
            }

            _curType = ReceivedValueType.StopIdentify;
            //发送数据
            try
            {
                var command = Commands.GetStopReadingCommand((byte) deviceNo);
                SentCommand(command);
            }
            catch (Exception ex)
            {
                throw new Exception("Stop abnormal reader", ex);
            }
        }

        public void GetCommunicationType(int deviceNo)
        {
            if (CSocket == null || !CSocket.Connected)
            {
                throw new ArgumentNullException("Socket", "Reader object is empty");
            }

            _curType = ReceivedValueType.GetCommunicationInterfaceType;
            //发送数据
            try
            {
                var command = Commands.GetCommunicationTypeCommand((byte)deviceNo);
                SentCommand(command);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred while getting the type of communication interface", ex);
            }
        }

        public void GetBaudRate(int deviceNo)
        {
            if (CSocket == null || !CSocket.Connected)
            {
                throw new ArgumentNullException("Socket", "Reader object is empty");
            }

            _curType = ReceivedValueType.GetBaudRate;
            //发送数据
            try
            {
                var command = Commands.GetBaudRateCommand((byte)deviceNo);
                SentCommand(command);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred while getting the baud rate", ex);
            }
        }

        public void GetWorkMode(int deviceNo)
        {
            if (CSocket == null || !CSocket.Connected)
            {
                throw new ArgumentNullException("Socket", "Reader object is empty");
            }

            _curType = ReceivedValueType.GetWorkMode;
            //发送数据
            try
            {
                var command = Commands.GetWorkModeTypeCommand((byte)deviceNo);
                SentCommand(command);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred while getting work mode", ex);
            }
        }

        public void SetWorkMode(int deviceNo, byte mode)
        {
            if (CSocket == null || !CSocket.Connected)
            {
                throw new ArgumentNullException("Socket", "Reader object is empty");
            }

            _curType = ReceivedValueType.SetReaderParams;
            //发送数据
            try
            {
                var command = Commands.SetWorkModeCommand((byte)deviceNo, mode);
                SentCommand(command);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred when setting the operating mode", ex);
            }
        }

        public void GetReadTagTimeInterval(int deviceNo)
        {
            if (CSocket == null || !CSocket.Connected)
            {
                throw new ArgumentNullException("Socket", "Reader object is empty");
            }

            _curType = ReceivedValueType.GetReadTagTimeInterval;
            //发送数据
            try
            {
                var command = Commands.GetReadTagTimeIntervalCommand((byte)deviceNo);
                SentCommand(command);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred when obtaining the reader time interval", ex);
            }
        }

        public void SetReadTagTimeInterval(int deviceNo, byte interval)
        {
            if (CSocket == null || !CSocket.Connected)
            {
                throw new ArgumentNullException("Socket", "Reader object is empty");
            }

            _curType = ReceivedValueType.SetReaderParams;
            //发送数据
            try
            {
                var command = Commands.SetReadTagTimeIntervalCommand((byte)deviceNo, interval);
                SentCommand(command);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred while setting the timing interval", ex);
            }
        }

        public void GetAdjacentDiscriminantTime(int deviceNo)
        {
            if (CSocket == null || !CSocket.Connected)
            {
                throw new ArgumentNullException("Socket", "Reader object is empty");
            }

            _curType = ReceivedValueType.GetAdjacentDiscriminantTime;
            //发送数据
            try
            {
                var command = Commands.GetAdjacentDiscriminantTimeCommand((byte)deviceNo);
                SentCommand(command);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred while obtaining the adjacent discriminating time", ex);
            }
        }

        public void SetAdjacentDiscriminantTime(int deviceNo, byte time)
        {
            if (CSocket == null || !CSocket.Connected)
            {
                throw new ArgumentNullException("Socket", "Reader object is empty");
            }

            _curType = ReceivedValueType.SetReaderParams;
            //发送数据
            try
            {
                var command = Commands.SetAdjacentDiscriminantTimeCommand((byte)deviceNo, time);
                SentCommand(command);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred while setting adjacent to determine the duration of", ex);
            }
        }

        public void GetAdjacentDiscriminant(int deviceNo)
        {
            if (CSocket == null || !CSocket.Connected)
            {
                throw new ArgumentNullException("Socket", "Reader object is empty");
            }

            _curType = ReceivedValueType.GetAdjacentDiscriminant;
            //发送数据
            try
            {
                var command = Commands.GetAdjacentDiscriminantCommand((byte)deviceNo);
                SentCommand(command);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred while getting neighboring discrimination", ex);
            }
        }

        public void SetAdjacentDiscriminant(int deviceNo, byte adjacentDiscriminant)
        {
            if (CSocket == null || !CSocket.Connected)
            {
                throw new ArgumentNullException("Socket", "Reader object is empty");
            }

            _curType = ReceivedValueType.SetReaderParams;
            //发送数据
            try
            {
                var command = Commands.SetAdjacentDiscriminantCommand((byte)deviceNo, adjacentDiscriminant);
                SentCommand(command);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred while setting adjacent sentence", ex);
            }
        }

        public void GetTriggerSwitch(int deviceNo)
        {
            if (CSocket == null || !CSocket.Connected)
            {
                throw new ArgumentNullException("Socket", "Reader object is empty");
            }

            _curType = ReceivedValueType.GetTriggerSwitch;
            //发送数据
            try
            {
                var command = Commands.GetTriggerSwitchCommand((byte)deviceNo);
                SentCommand(command);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred while getting the trigger switch", ex);
            }
        }

        public void SetTriggerSwitch(int deviceNo, byte triggerSwitch)
        {
            if (CSocket == null || !CSocket.Connected)
            {
                throw new ArgumentNullException("Socket", "Reader object is empty");
            }

            _curType = ReceivedValueType.SetReaderParams;
            //发送数据
            try
            {
                var command = Commands.SetTriggerSwitchCommand((byte)deviceNo, triggerSwitch);
                SentCommand(command);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred while setting the trigger switch", ex);
            }
        }

        public void GetTriggerDelay(int deviceNo)
        {
            if (CSocket == null || !CSocket.Connected)
            {
                throw new ArgumentNullException("Socket", "Reader object is empty");
            }

            _curType = ReceivedValueType.GetTriggerDelay;
            //发送数据
            try
            {
                var command = Commands.GetTriggerDelayCommand((byte)deviceNo);
                SentCommand(command);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred while obtaining the delay time", ex);
            }
        }

        public void SetTriggerDelay(int deviceNo, byte delay)
        {
            if (CSocket == null || !CSocket.Connected)
            {
                throw new ArgumentNullException("Socket", "Reader object is empty");
            }

            _curType = ReceivedValueType.SetReaderParams;
            //发送数据
            try
            {
                var command = Commands.SetTriggerDelayCommand((byte)deviceNo, delay);
                SentCommand(command);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred while setting the delay time", ex);
            }
        }

        public void GetDeviceNumber(int deviceNo)
        {
            if (CSocket == null || !CSocket.Connected)
            {
                throw new ArgumentNullException("Socket", "Reader object is empty");
            }

            _curType = ReceivedValueType.GetDeviceNumber;
            //发送数据
            try
            {
                var command = Commands.GetDeviceNumberCommand((byte)deviceNo);
                SentCommand(command);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred while getting the device address", ex);
            }
        }

        public void SetDeviceNumber(int deviceNo, byte number)
        {
            if (CSocket == null || !CSocket.Connected)
            {
                throw new ArgumentNullException("Socket", "Reader object is empty");
            }

            _curType = ReceivedValueType.SetReaderParams;
            //发送数据
            try
            {
                var command = Commands.SetDeviceNumberCommand((byte)deviceNo, number);
                SentCommand(command);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred when setting the device address", ex);
            }
        }

        public void GetTransmitPower(int deviceNo)
        {
            if (CSocket == null || !CSocket.Connected)
            {
                throw new ArgumentNullException("Socket", "Reader object is empty");
            }

            _curType = ReceivedValueType.GetTransmitPower;
            //发送数据
            try
            {
                var command = Commands.GetTransmitPowerCommand((byte)deviceNo);
                SentCommand(command);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred while obtaining transmission power", ex);
            }
        }

        public void SetTransmitPower(int deviceNo, byte power)
        {
            if (CSocket == null || !CSocket.Connected)
            {
                throw new ArgumentNullException("Socket", "Reader object is empty");
            }

            _curType = ReceivedValueType.SetReaderParams;
            //发送数据
            try
            {
                var command = Commands.SetTransmitPowerCommand((byte)deviceNo, power);
                SentCommand(command);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred while setting the transmission power", ex);
            }
        }

        public void GetAntenna(int deviceNo)
        {
            if (CSocket == null || !CSocket.Connected)
            {
                throw new ArgumentNullException("Socket", "Reader object is empty");
            }

            _curType = ReceivedValueType.GetAntenna;
            //发送数据
            try
            {
                var command = Commands.GetAntennaCommand((byte)deviceNo);
                SentCommand(command);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred while obtaining antenna Information", ex);
            }
        }

        public void SetAntenna(int deviceNo, byte antenna)
        {
            if (CSocket == null || !CSocket.Connected)
            {
                throw new ArgumentNullException("Socket", "Reader object is empty");
            }

            _curType = ReceivedValueType.SetReaderParams;
            //发送数据
            try
            {
                var command = Commands.SetAntennaCommand((byte)deviceNo, antenna);
                SentCommand(command);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred when setting up the antenna", ex);
            }
        }

        public void GetReadTagType(int deviceNo)
        {
            if (CSocket == null || !CSocket.Connected)
            {
                throw new ArgumentNullException("Socket", "Reader object is empty");
            }

            _curType = ReceivedValueType.GetReadTagType;
            //发送数据
            try
            {
                var command = Commands.GetReadTagTypeCommand((byte)deviceNo);
                SentCommand(command);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred while obtaining the card reader mode", ex);
            }
        }

        public void SetReadTagType(int deviceNo, byte type)
        {
            if (CSocket == null || !CSocket.Connected)
            {
                throw new ArgumentNullException("Socket", "Reader object is empty");
            }

            _curType = ReceivedValueType.SetReaderParams;
            //发送数据
            try
            {
                var command = Commands.SetReadTagTypeCommand((byte)deviceNo, type);
                SentCommand(command);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred when setting up the reader mode", ex);
            }
        }

        public void SetCommunicationType(int deviceNo, byte type)
        {
            if (CSocket == null || !CSocket.Connected)
            {
                throw new ArgumentNullException("Socket", "Reader object is empty");
            }

            var buffer = new List<byte>(4096);   //默认分配1页内存，并始终限制不允许超过 
            var message = new byte[1024];

            //发送数据
            try
            {
                var command = Commands.SetCommunicationInterfaceCommand((byte)deviceNo, type);
                SentCommand(command);

                //接收数据
                int receivedCount;
                do
                {
                    receivedCount = CSocket.Receive(message);
                    buffer.AddRange(message.Take(receivedCount));

                    //结果处理
                    var result = DataProcessUtils.ProcessSetParams(buffer);
                    _getOperateResultHandler?.Invoke(ReceivedValueType.SetReaderParams, result);
                }
                while (receivedCount > 0 && receivedCount >= message.Length);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred when setting the type of communication interface mode", ex);
            }
        }

        public void SetBaudRate(int deviceNo, byte baudRate)
        {
            if (CSocket == null || !CSocket.Connected)
            {
                throw new ArgumentNullException("Socket", "Reader object is empty");
            }

            _curType = ReceivedValueType.SetReaderParams;
            //发送数据
            try
            {
                var command = Commands.SetBaudRateCommand((byte)deviceNo, baudRate);
                SentCommand(command);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred when setting the baud rate", ex);
            }
        }

        public void GetWiegandParams(int deviceNo)
        {
            if (CSocket == null || !CSocket.Connected)
            {
                throw new ArgumentNullException("Socket", "Reader object is empty");
            }

            _curType = ReceivedValueType.GetWiegandParam;
            //发送数据
            try
            {
                var command = Commands.GetWiegandParamsCommand((byte)deviceNo);
                SentCommand(command);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred while getting Wiegand parameters", ex);
            }
        }

        public void SetWiegandParams(int deviceNo, byte type, byte width, byte period)
        {
            if (CSocket == null || !CSocket.Connected)
            {
                throw new ArgumentNullException("Socket", "Reader object is empty");
            }

            _curType = ReceivedValueType.SetReaderParams;
            //发送数据
            try
            {
                var command = Commands.SetWiegandParamsCommand((byte)deviceNo, new[] { type, width, period });
                SentCommand(command);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred when setting parameters Wiegand", ex);
            }
        }

        public void GetHopping(int deviceNo)
        {
            if (CSocket == null || !CSocket.Connected)
            {
                throw new ArgumentNullException("Socket", "Reader object is empty");
            }

            _curType = ReceivedValueType.GetHopping;
            //发送数据
            try
            {
                var command = Commands.GetHoppingCommand((byte)deviceNo);
                SentCommand(command);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred while getting hopping parameters", ex);
            }
        }

        public void SetHopping(int deviceNo, byte frequency)
        {
            if (CSocket == null || !CSocket.Connected)
            {
                throw new ArgumentNullException("Socket", "Reader object is empty");
            }

            _curType = ReceivedValueType.SetReaderParams;
            //发送数据
            try
            {
                var command = Commands.SetHoppingCommand((byte)deviceNo, frequency);
                SentCommand(command);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred while setting parameters of frequency hopping", ex);
            }
        }

        public void GetBands(int deviceNo)
        {
            if (CSocket == null || !CSocket.Connected)
            {
                throw new ArgumentNullException("Socket", "Reader object is empty");
            }

            _curType = ReceivedValueType.GetBands;
            //发送数据
            try
            {
                var command = Commands.GetBandsCommand((byte)deviceNo);
                SentCommand(command);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred while obtaining frequency", ex);
            }
        }

        public void SetBands(int deviceNo, byte[] bands)
        {
            if (CSocket == null || !CSocket.Connected)
            {
                throw new ArgumentNullException("Socket", "Reader object is empty");
            }

            _curType = ReceivedValueType.SetReaderParams;
            //发送数据
            try
            {
                var command = Commands.SetBandsCommand((byte)deviceNo, bands);
                SentCommand(command);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred while setting frequency", ex);
            }
        }
        
        public void SetBuzzer(int deviceNo, byte buzzerCtrl)
        {
            if (CSocket == null || !CSocket.Connected)
            {
                throw new ArgumentNullException("Socket", "Reader object is empty");
            }

            _curType = ReceivedValueType.SetReaderParams;
            //发送数据
            try
            {
                var command = Commands.SetBuzzerCommand((byte)deviceNo, buzzerCtrl);
                SentCommand(command);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred when setting the buzzer", ex);
            }
        }
        
        public void SetRelays(int deviceNo, byte relayOnOff)
        {
            if (CSocket == null || !CSocket.Connected)
            {
                throw new ArgumentNullException("Socket", "Reader object is empty");
            }

            _curType = ReceivedValueType.SetReaderParams;
            //发送数据
            try
            {
                var command = Commands.SetRelaysCommand((byte)deviceNo, relayOnOff);
                SentCommand(command);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred when setting relay", ex);
            }
        }

        public void QuickWriteTag(int deviceNo, byte[] data)
        {
            if (CSocket == null || !CSocket.Connected)
            {
                throw new ArgumentNullException("Socket", "Reader object is empty");
            }

            _curType = ReceivedValueType.QuickWriteTag;
            //发送数据
            try
            {
                var command = Commands.QuickWriteTagCommand((byte)deviceNo, data);
                SentCommand(command);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred when the fast write tag", ex);
            }
        }

        public void ReadTag(int deviceNo, byte area, byte address, byte length)
        {
            if (CSocket == null || !CSocket.Connected)
            {
                throw new ArgumentNullException("Socket", "Reader object is empty");
            }

            _curType = ReceivedValueType.ReadTag;
            //发送数据
            try
            {
                var command = Commands.ReadEpcTag((byte)deviceNo, area, address, length);
                SentCommand(command);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred when reading tags", ex);
            }
        }

        public void WriteTagSingleWord(int device, byte area, byte address, byte[] data)
        {
            if (CSocket == null || !CSocket.Connected)
            {
                throw new ArgumentNullException("Socket", "Reader object is empty");
            }

            _curType = ReceivedValueType.WriteTag;
            //发送数据
            try
            {
                var command = Commands.WriteTagSingleWord((byte)device, area, address, 0x01, data[0], data[1]);
                SentCommand(command);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred while writing tag", ex);
            }
        }

        public void WriteTagMultiWords(int device, byte area, byte address, byte length, byte[] data)
        {
            if (CSocket == null || !CSocket.Connected)
            {
                throw new ArgumentNullException("Socket", "Reader object is empty");
            }

            if (data.Length < length * 2)
            {
                throw new Exception("An exception occurred while writing tags");
            }

            _curType = ReceivedValueType.WriteTag;
            //发送数据
            try
            {
                var command = Commands.WriteTagMultiWord((byte)device, area, address, length, data);
                SentCommand(command);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred while writing tags", ex);
            }
        }

        public void InitilizeTag(int deviceNo)
        {
            if (CSocket == null || !CSocket.Connected)
            {
                throw new ArgumentNullException("Socket", "Reader object is empty");
            }

            _curType = ReceivedValueType.InitilizeTag;
            //发送数据
            try
            {
                var command = Commands.InitilizeTagCommand((byte)deviceNo);
                SentCommand(command);
            }
            catch (Exception ex)
            {
                throw new Exception("Initialized when an exception occurs", ex);
            }
        }

        public void LockTag(int deviceNo, byte lockType, byte[] password)
        {
            if (CSocket == null || !CSocket.Connected)
            {
                throw new ArgumentNullException("Socket", "Reader object is empty");
            }

            _curType = ReceivedValueType.LockTag;
            //发送数据
            try
            {
                var command = Commands.LockTagCommand((byte)deviceNo, lockType, password);
                SentCommand(command);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred while locking tag", ex);
            }
        }

        public void UnlockTag(int deviceNo, byte unlockType, byte[] password)
        {
            if (CSocket == null || !CSocket.Connected)
            {
                throw new ArgumentNullException("Socket", "Reader object is empty");
            }

            _curType = ReceivedValueType.UnlockTag;
            //发送数据
            try
            {
                var command = Commands.UnlockTagCommand((byte)deviceNo, unlockType, password);
                SentCommand(command);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred while unlocking tag", ex);
            }
        }

        public void KillTag(int deviceNo, byte[] password)
        {
            if (CSocket == null || !CSocket.Connected)
            {
                throw new ArgumentNullException("Socket", "Reader object is empty");
            }

            _curType = ReceivedValueType.KillTag;
            //发送数据
            try
            {
                var command = Commands.KillTagCommand((byte)deviceNo, password);
                SentCommand(command);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred while destruction of the tag", ex);
            }
        }

        public void SetSingleParams(ReceivedValueType type, int deviceNo, byte msb, byte lsb, byte data)
        {
            if (CSocket == null || !CSocket.Connected)
            {
                throw new ArgumentNullException("Socket", "Reader object is empty");
            }

            _curType = type;
            //发送数据
            try
            {
                var command = Commands.SetSingleReaderParamCommand((byte)deviceNo, msb, lsb, data);
                SentCommand(command);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred when setting the reader", ex);
            }
        }

        public void SetMultiParams(ReceivedValueType type, int deviceNo, byte msb, byte lsb, byte[] data)
        {
            if (CSocket == null || !CSocket.Connected)
            {
                throw new ArgumentNullException("Socket", "Reader object is empty");
            }

            _curType = ReceivedValueType.SetReaderParams;
            //发送数据
            try
            {
                var command = Commands.SetMultiReaderParamsCommand((byte)deviceNo, msb, lsb, data);
                SentCommand(command);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred when setting the reader", ex);
            }
        }

        public void ResetReader(int deviceNo) 
        {
            if (CSocket == null || !CSocket.Connected)
            {
                throw new ArgumentNullException("Socket", "Reader object is empty");
            }

            _curType = ReceivedValueType.Reset;
            //发送数据
            try
            {
                var command = Commands.ResetReaderCommand((byte)deviceNo);
                SentCommand(command);
            }
            catch (Exception ex)
            {
                throw new Exception("Reset exception occurs when the reader", ex);
            }
        }

        public void RegisterOperateResultDataReceivedEvent(GetOperateResultHandler handlerGetOperatreResult)
        {
            _getOperateResultHandler = handlerGetOperatreResult;
            _isReceiving = true;
            Task.Factory.StartNew(ReceivedDataProcessThread);
        }

        public void ClearDataReceivedEvent()
        {
            _getOperateResultHandler = null;
            _isReceiving = false;//遇到Receive方法阻塞时无效
        }



        //数据接收与处理线程
        private void ReceivedDataProcessThread()
        {
            var buffer = new List<byte>(4096);   //默认分配1页内存，并始终限制不允许超过 
            var message = new byte[1024];

            //发送数据
            try
            {
                //接收数据
                CSocket.ReceiveTimeout = 0;
                do
                {
                    var receivedCount = CSocket.Receive(message);
                    buffer.AddRange(message.Take(receivedCount));

                    //结果处理
                    switch (_curType)
                    {
                        case ReceivedValueType.GetFirmwareVersion:
                            var version = DataProcessUtils.ProcessFirmwareVersion(buffer);
                            _getOperateResultHandler?.Invoke(ReceivedValueType.GetFirmwareVersion, version);
                            break;
                        case ReceivedValueType.IdentifySingleTag:
                            DataProcessUtils.ProcessSingleIndentifyDataReceived(buffer, _getOperateResultHandler);
                            break;
                        case ReceivedValueType.IdentifyMultieTag:
                            DataProcessUtils.ProcessMultiIndentifyDataReceived(buffer, _getOperateResultHandler);
                            break;
                        case ReceivedValueType.GetCommunicationInterfaceType:
                            var ciType = DataProcessUtils.ProcessCommunicationInterfaceType(buffer);
                            _getOperateResultHandler?.Invoke(ReceivedValueType.GetCommunicationInterfaceType, ciType);
                            break;
                        case ReceivedValueType.GetBaudRate:
                            var baudrate = DataProcessUtils.ProcessBaudRate(buffer);
                            _getOperateResultHandler?.Invoke(ReceivedValueType.GetBaudRate, baudrate);
                            break;
                        case ReceivedValueType.GetWorkMode:
                            var workMode = DataProcessUtils.ProcessWorkMode(buffer);
                            _getOperateResultHandler?.Invoke(ReceivedValueType.GetWorkMode, workMode);
                            break;
                        case ReceivedValueType.GetReadTagTimeInterval:
                            var rtInterval = DataProcessUtils.ProcessReadTagTimeInterval(buffer);
                            _getOperateResultHandler?.Invoke(ReceivedValueType.GetReadTagTimeInterval, rtInterval);
                            break;
                        case ReceivedValueType.GetAdjacentDiscriminantTime:
                            var time = DataProcessUtils.ProcessAdjacentDiscriminantTime(buffer);
                            _getOperateResultHandler?.Invoke(ReceivedValueType.GetAdjacentDiscriminantTime, time);
                            break;
                        case ReceivedValueType.GetAdjacentDiscriminant:
                            var adInterval = DataProcessUtils.ProcessAdjacentDiscriminant(buffer);
                            _getOperateResultHandler?.Invoke(ReceivedValueType.GetAdjacentDiscriminant, adInterval);
                            break;
                        case ReceivedValueType.GetTriggerSwitch:
                            var ts = DataProcessUtils.ProcessTriggerSwitch(buffer);
                            _getOperateResultHandler?.Invoke(ReceivedValueType.GetTriggerSwitch, ts);
                            break;
                        case ReceivedValueType.GetTriggerDelay:
                            var td = DataProcessUtils.ProcessTriggerDelay(buffer);
                            _getOperateResultHandler?.Invoke(ReceivedValueType.GetTriggerDelay, td);
                            break;
                        case ReceivedValueType.GetDeviceNumber:
                            var num = DataProcessUtils.ProcessDeviceNumber(buffer);
                            _getOperateResultHandler?.Invoke(ReceivedValueType.GetDeviceNumber, num);
                            break;
                        case ReceivedValueType.GetTransmitPower:
                            var power = DataProcessUtils.ProcessTransmitPower(buffer);
                            _getOperateResultHandler?.Invoke(ReceivedValueType.GetTransmitPower, power);
                            break;
                        case ReceivedValueType.GetAntenna:
                            var ant = DataProcessUtils.ProcessAntenna(buffer);
                            _getOperateResultHandler?.Invoke(ReceivedValueType.GetAntenna, ant);
                            break;
                        case ReceivedValueType.GetReadTagType:
                            var rtType = DataProcessUtils.ProcessReadTagType(buffer);
                            _getOperateResultHandler?.Invoke(ReceivedValueType.GetReadTagType, rtType);
                            break;
                        case ReceivedValueType.GetWiegandParam:
                            DataProcessUtils.ProcessWiegandParam(buffer, _getOperateResultHandler);
                            break;
                        case ReceivedValueType.GetHopping:
                            var hopping = DataProcessUtils.ProcessHopping(buffer);
                            _getOperateResultHandler?.Invoke(ReceivedValueType.GetHopping, hopping);
                            break;
                        case ReceivedValueType.GetBands:
                            var bands = DataProcessUtils.ProcessBands(buffer);
                            _getOperateResultHandler?.Invoke(ReceivedValueType.GetBands, bands);
                            break;
                        case ReceivedValueType.QuickWriteTag:
                            var qwt = DataProcessUtils.ProcessQuickWriteTag(buffer);
                            _getOperateResultHandler?.Invoke(ReceivedValueType.QuickWriteTag, qwt);
                            break;
                        case ReceivedValueType.ReadTag:
                            var tag = DataProcessUtils.ProcessReadTag(buffer);
                            _getOperateResultHandler?.Invoke(ReceivedValueType.ReadTag, tag);
                            break;
                        case ReceivedValueType.WriteTag:
                            var wt = DataProcessUtils.ProcessWriteTag(buffer);
                            _getOperateResultHandler?.Invoke(ReceivedValueType.WriteTag, wt);
                            break;
                        case ReceivedValueType.InitilizeTag:
                            var it = DataProcessUtils.ProcessInitilizeTag(buffer);
                            _getOperateResultHandler?.Invoke(ReceivedValueType.InitilizeTag, it);
                            break;
                        case ReceivedValueType.LockTag:
                            var lt = DataProcessUtils.ProcessLockTag(buffer);
                            _getOperateResultHandler?.Invoke(ReceivedValueType.LockTag, lt);
                            break;
                        case ReceivedValueType.UnlockTag:
                            var ult = DataProcessUtils.ProcessUnlockTag(buffer);
                            _getOperateResultHandler?.Invoke(ReceivedValueType.UnlockTag, ult);
                            break;
                        case ReceivedValueType.KillTag:
                            var kt = DataProcessUtils.ProcessKillTag(buffer);
                            _getOperateResultHandler?.Invoke(ReceivedValueType.KillTag, kt);
                            break;
                        case ReceivedValueType.Reset:
                            var reset = DataProcessUtils.ProcessResetReader(buffer);
                            _getOperateResultHandler?.Invoke(ReceivedValueType.Reset, reset);
                            break;
                        default://ReceivedValueType.SetReaderParams
                            var result = DataProcessUtils.ProcessSetParams(buffer);
                            _getOperateResultHandler?.Invoke(ReceivedValueType.SetReaderParams, result);
                            break;
                    }
                    buffer.Clear();
                }
                while (_isReceiving);
            }
            catch (Exception ex)
            {
                Console.WriteLine("处理数据时发生异常，" + ex.Message);
            }
        }

        private void SentCommand(byte[] data)
        {
            if (data == null || CSocket == null) return;
            try
            {
                CSocket.Send(data);

                //if (CommandListener != null)
                //{
                //    CommandListener(data.ToArray());
                //}
            }
            catch (Exception ex)
            {
                throw new Exception("串口写入指定长度字节数组时发生异常", ex);
            }
        }

        /*
        /// <summary>
        /// 设置设备的参数
        /// </summary>
        /// <param name="device"></param>
        /// <param name="mode">0--UDP Client; 1--TCP Client; 2--UDP Server; 3--TCP Server</param>
        /// <param name="baudrate">波特率</param>
        public static void SetDeviceWorkMode(Device device, int mode = 3, int baudrate = -1)
        {
            if(device?.OriginalBytes == null)
                return;

            var recByte = device.OriginalBytes;
            //初始化
            var cmd = new byte[40];
            //Mac地址（6：[0]--[5]）
            for (var i = 0; i < 6; i++)
            {
                cmd[i] = recByte[i];
            }
            //密码（6：[6]--[11]）(110415万能密码)
            cmd[6] = 0x31;
            cmd[7] = 0x31;
            cmd[8] = 0x30;
            cmd[9] = 0x34;
            cmd[10] = 0x31;
            cmd[11] = 0x35;
            //连接目标Ip（4：[12]--[15]）
            cmd[12] = recByte[7];
            cmd[13] = recByte[8];
            cmd[14] = recByte[9];
            cmd[15] = recByte[10];
            //连接目标端口（2：[16]--[17]）
            cmd[16] = recByte[11];
            cmd[17] = recByte[12];
            //模块Ip（4：[18]--[21]）
            cmd[21] = recByte[16];
            cmd[20] = recByte[15];
            cmd[19] = recByte[14];
            cmd[18] = recByte[13];

            //模块端口（2：[22]--[23]）(eg:20108==8C 4E)
            cmd[22] = recByte[17];
            cmd[23] = recByte[18];

            //网关（4：[24]--[27]）
            cmd[24] = recByte[19];
            cmd[25] = recByte[20];
            cmd[26] = recByte[21];
            cmd[27] = recByte[22];

            //工作模式（1：[28]）
            cmd[28] = (byte)mode;

            //串口端波特率（3：[29]--[31]）(eg:115200==00 C2 01)
            if (baudrate > 0)
            {
                var br = TextEncoder.IntToBytes(baudrate);
                cmd[29] = br[0];
                cmd[30] = br[1];
                cmd[31] = br[2];
            }
            else
            {
                cmd[29] = recByte[24];
                cmd[30] = recByte[25];
                cmd[31] = recByte[26];
            }

            //停止位（1：[32]）
            cmd[32] = recByte[27];

            //独立ID（3：[33]--[35]）
            cmd[33] = 0x00;
            cmd[34] = 0x00;
            cmd[35] = 0x00;

            //子网掩码（4：[36]--[39]）
            cmd[36] = recByte[31];
            cmd[37] = recByte[32];
            cmd[38] = recByte[33];
            cmd[39] = recByte[34];

            //发送命令
            try
            {
                var port = int.Parse("1500");
                var udpSend = new UdpClient();

                //得到客户机IP  
                var ipep = new IPEndPoint(IPAddress.Parse("255.255.255.255"), port);

                udpSend.Send(cmd, cmd.Length, ipep);
            }
            catch (Exception ex)
            {
                throw new Exception("设置工作模式异常", ex);
            }
        }
        */

        /// <summary>
        /// 设置设备的参数
        /// </summary>
        /// <param name="device"></param>
        /// <param name="baudrate">波特率，默认值-1为不设置该参数</param>
        /// <param name="spBits">串口参数位，默认值-1为不设置该参数</param>
        /// <param name="mode">0--UDP Client; 1--TCP Client; 2--UDP Server; 3--TCP Server，默认值-1为不设置该参数</param>
        /// <param name="ip">模块IP地址，默认值NULL为不设置该参数</param>
        /// <param name="port">模块端口，默认值-1为不设置该参数</param>
        /// <param name="subNetMask">子网掩码，默认值NULL为不设置该参数</param>
        /// <param name="targetIp">连接目标IP地址，默认值NULL为不设置该参数</param>
        /// <param name="targetPort">连接目标端口，默认值-1为不设置该参数</param>
        /// <param name="gateway">网关，默认值NULL为不设置该参数</param>
        public static void SetDeviceParams(Device device, int baudrate = -1, int spBits = -1, int mode = -1,
            string ip = null, int port = -1, string subNetMask = null, string targetIp = null, int targetPort = -1,
            string gateway = null)
        {
            if (device?.OriginalBytes == null)
                return;

            try
            {
                var recByte = device.OriginalBytes;
                //初始化
                var cmd = new byte[40];
                //Mac地址（6：[0]--[5]）
                for (var i = 0; i < 6; i++)
                {
                    cmd[i] = recByte[i];
                }
                //密码（6：[6]--[11]）(110415万能密码)
                cmd[6] = 0x31;
                cmd[7] = 0x31;
                cmd[8] = 0x30;
                cmd[9] = 0x34;
                cmd[10] = 0x31;
                cmd[11] = 0x35;
                //连接目标Ip（4：[12]--[15]）
                if (targetIp != null && mode == 1)
                {
                    var tIp = targetIp.Split('.');
                    for (var i = 0; i < tIp.Length; i++)
                    {
                        var num = TextEncoder.IntToBytes(Convert.ToInt32(tIp[i]));
                        cmd[15 - i] = num[0];
                    }
                }
                else
                {
                    cmd[12] = recByte[7];
                    cmd[13] = recByte[8];
                    cmd[14] = recByte[9];
                    cmd[15] = recByte[10];
                }
                //连接目标端口（2：[16]--[17]）
                if (targetPort > 0 && mode == 1)
                {
                    var dPort = TextEncoder.IntToBytes(targetPort);
                    cmd[16] = dPort[0];
                    cmd[17] = dPort[1];
                }
                else
                {
                    cmd[16] = recByte[11];
                    cmd[17] = recByte[12];
                }
                //模块Ip（4：[18]--[21]）
                if (ip != null)
                {
                    var tIp = ip.Split('.');
                    for (var i = 0; i < tIp.Length; i++)
                    {
                        var num = TextEncoder.IntToBytes(Convert.ToInt32(tIp[i]));
                        cmd[21 - i] = num[0];
                    }
                }
                else
                {
                    cmd[21] = recByte[16];
                    cmd[20] = recByte[15];
                    cmd[19] = recByte[14];
                    cmd[18] = recByte[13];
                }

                //模块端口（2：[22]--[23]）(eg:20108==8C 4E)
                if (port > 0)
                {
                    var dPort = TextEncoder.IntToBytes(port);
                    cmd[22] = dPort[0];
                    cmd[23] = dPort[1];
                }
                else
                {
                    cmd[22] = recByte[17];
                    cmd[23] = recByte[18];
                }

                //网关（4：[24]--[27]）
                if (gateway != null && mode == 1)
                {
                    var gate = gateway.Split('.');
                    for (var i = 0; i < gate.Length; i++)
                    {
                        var num = TextEncoder.IntToBytes(Convert.ToInt32(gate[i]));
                        cmd[27 - i] = num[0];
                    }
                }
                else
                {
                    cmd[24] = recByte[19];
                    cmd[25] = recByte[20];
                    cmd[26] = recByte[21];
                    cmd[27] = recByte[22];
                }

                //工作模式（1：[28]）
                cmd[28] = (byte)mode;

                //串口端波特率（3：[29]--[31]）(eg:115200==00 C2 01)
                if (baudrate > 0)
                {
                    var br = TextEncoder.IntToBytes(baudrate);
                    cmd[29] = br[0];
                    cmd[30] = br[1];
                    cmd[31] = br[2];
                }
                else
                {
                    cmd[29] = recByte[24];
                    cmd[30] = recByte[25];
                    cmd[31] = recByte[26];
                }

                //停止位（1：[32]）
                if (spBits >= 0)
                {
                    cmd[32] = (byte)spBits;
                }
                else
                {
                    cmd[32] = recByte[27];
                }

                //独立ID（3：[33]--[35]）
                cmd[33] = 0x00;
                cmd[34] = 0x00;
                cmd[35] = 0x00;

                //子网掩码（4：[36]--[39]）
                if (subNetMask != null)
                {
                    var snm = subNetMask.Split('.');
                    for (var i = 0; i < snm.Length; i++)
                    {
                        var num = TextEncoder.IntToBytes(Convert.ToInt32(snm[i]));
                        cmd[39 - i] = num[0];
                    }
                }
                else
                {
                    cmd[36] = recByte[31];
                    cmd[37] = recByte[32];
                    cmd[38] = recByte[33];
                    cmd[39] = recByte[34];
                }

                //发送命令
                var udpSend = new UdpClient();

                //得到客户机IP  
                var ipep = new IPEndPoint(IPAddress.Parse("255.255.255.255"), int.Parse("1500"));

                udpSend.Send(cmd, cmd.Length, ipep);
            }
            catch (Exception ex)
            {
                throw new Exception("设置工作模式异常", ex);
            }
        }

    }
}
