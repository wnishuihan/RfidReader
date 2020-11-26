using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Reflection;

namespace RfidLibrary
{
    public class ProxySerialPort : IReader
    {
        /// <summary>
        /// 串口命令监听事件
        /// </summary>
        public event ReaderCommandListenerHandle CommandListener;

        public SerialPort CSerialPort { get; private set; }

        private readonly List<byte> _buffer;

        private GetOperateResultHandler _getOperateResultHandler;

        private ReceivedValueType _curType;//当前操作

        /// <summary>
        /// 构建
        /// </summary>
        /// <param name="portName"></param>
        /// <param name="baudRate"></param>
        public ProxySerialPort(string portName, int baudRate = 9600)
        {
            if (string.IsNullOrEmpty(portName))
            {
                throw new ArgumentNullException("portName", "Serial port name can not be empty");//串口名称不能为空
            }

            try
            {
                CSerialPort = new SerialPort(portName, baudRate);
                //CSerialPort = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One);
                _buffer = new List<byte>(4096); //默认分配1页内存，并始终限制不允许超过 
            }
            catch (Exception ex)
            {
                throw new Exception("获取串口代理发生异常", ex);
            }
        }

        // <summary>
        /// 获取系统当前可用串口列表
        /// <returns>可用串口名称数组</returns>
        /// <exception ref="System.Exception">获取系统可用串口名称数组异常</exception>
        public static string[] GetAvailableSerialPort()
        {
            try
            {
                var ports = SerialPort.GetPortNames();
                Array.Sort(ports);
                return ports;
            }
            catch (Exception ex)
            {
                throw new Exception("Exception occurs when the available serial acquisition system", ex);//获取系统可用串口时发生异常
            }
        }

        public ReaderType GetReadType()
        {
            return ReaderType.SerialPort;
        }

        //发送命令
        private void SentCommand(byte[] data, int offset = 0, int count = 0)
        {
            if (data == null || CSerialPort == null)
                throw new ArgumentNullException("Command or serial port object is null");//命令或串口对象为空
            try
            {
                var isPortOpen = CSerialPort.IsOpen;

                if (isPortOpen == false)
                {
                    CSerialPort.Open();
                }

                CSerialPort.Write(data, offset, count);

                CommandListener?.Invoke(data.Skip(offset).Take(count).ToArray());

                //if (isPortOpen == false)
                //{
                //    CSerialPort.Close();
                //}
            }
            catch (Exception ex)
            {
                throw new Exception("Serial port write exception occurs when the specified length byte array", ex);//串口写入指定长度字节数组时发生异常
            }
        }

        public bool IsOpenOrConnection()
        {
            return CSerialPort.IsOpen;
        }

        // 打开串口
        public void OpenSerialPort()
        {
            try
            {
                if (!CSerialPort.IsOpen)
                {
                    CSerialPort.Open();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Exception occurs while opening {CSerialPort.PortName}", ex); //打开{0}时发生异常
            }
        }

        //关闭串口
        public bool DisConnection()
        {
            var result = false;
            try
            {
                if (CSerialPort.IsOpen)
                {
                    CSerialPort.DiscardInBuffer();
                    CSerialPort.DiscardOutBuffer();
                    ClearDataReveived(CSerialPort);
                    CSerialPort.Close();
                    result = true;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"An exception occurred when closing {CSerialPort.PortName}", ex);//关闭{CSerialPort.PortName}时发生异常
            }
            finally
            {
                CSerialPort = null;
            }
            return result;
        }

        public void GetReadFirmwareVersion(int deviceNo)
        {
            try
            {
                if (!CSerialPort.IsOpen)
                {
                    CSerialPort.Open();
                }
                _curType = ReceivedValueType.GetFirmwareVersion;
                var cmd = Commands.GetFirmwareVersionCommand((byte) deviceNo);
                SentCommand(cmd, 0, cmd.Length);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred while retrieving firmware version", ex);//获取固件版本信息时发生异常
            }
        }

        public void IdentifySingleTag(int deviceNo)
        {
            try
            {
                if (!CSerialPort.IsOpen)
                {
                    CSerialPort.Open();
                }

                _curType = ReceivedValueType.IdentifySingleTag;
                var devNo = (byte) deviceNo; //设备号
                var cmd = Commands.GetIdentifySingleTagCommand(devNo);
                cmd[4] = CheckUtils.SumCheck(cmd, 0, 4); //和校验
                SentCommand(cmd, 0, cmd.Length);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred while reading a single card", ex);//单卡读取时发生异常
            }
        }

        public void IdentifyMultiTag(int deviceNo)
        {
            try
            {
                if (!CSerialPort.IsOpen)
                {
                    CSerialPort.Open();
                }

                _curType = ReceivedValueType.IdentifyMultieTag;
                var cmd = Commands.GetIdentifyMultiTagCommand((byte) deviceNo);
                cmd[4] = CheckUtils.SumCheck(cmd, 0, 4); //和校验
                SentCommand(cmd, 0, cmd.Length);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred while reading multi-tag", ex);//多卡读取时发生异常
            }
        }

        public void StopReadingTag(int deviceNo)
        {
            try
            {
                _curType = ReceivedValueType.StopIdentify;
                var cmd = Commands.GetStopReadingCommand((byte) deviceNo);
                SentCommand(cmd, 0, cmd.Length);

                CSerialPort.DiscardInBuffer();
                CSerialPort.DiscardOutBuffer();
            }
            catch (Exception ex)
            {
                throw new Exception("Stop abnormal reader", ex);//停止读卡异常
            }
        }

        public void GetCommunicationType(int deviceNo)
        {
            try
            {
                if (!CSerialPort.IsOpen)
                {
                    CSerialPort.Open();
                }

                _curType = ReceivedValueType.GetCommunicationInterfaceType;
                var devNo = (byte) deviceNo; //设备号
                var cmd = Commands.GetCommunicationTypeCommand(devNo);
                SentCommand(cmd, 0, cmd.Length);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred while getting the type of communication interface", ex);//获取通信接口类型时发生异常
            }
        }

        public void GetBaudRate(int deviceNo)
        {
            try
            {
                if (!CSerialPort.IsOpen)
                {
                    CSerialPort.Open();
                }

                _curType = ReceivedValueType.GetBaudRate;
                var devNo = (byte) deviceNo; //设备号
                var cmd = Commands.GetBaudRateCommand(devNo);
                SentCommand(cmd, 0, cmd.Length);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred while getting the baud rate", ex);//获取波特率时发生异常
            }
        }

        public void GetWorkMode(int deviceNo)
        {
            try
            {
                if (!CSerialPort.IsOpen)
                {
                    CSerialPort.Open();
                }

                _curType = ReceivedValueType.GetWorkMode;
                var devNo = (byte) deviceNo; //设备号
                var cmd = Commands.GetWorkModeTypeCommand(devNo);
                SentCommand(cmd, 0, cmd.Length);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred while getting work mode", ex);//获取工作模式时发生异常
            }
        }

        public void SetWorkMode(int deviceNo, byte mode)
        {
            try
            {
                if (!CSerialPort.IsOpen)
                {
                    CSerialPort.Open();
                }

                _curType = ReceivedValueType.SetWorkMode;
                var devNo = (byte) deviceNo; //设备号
                var cmd = Commands.SetWorkModeCommand(devNo, mode);
                SentCommand(cmd, 0, cmd.Length);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred when setting the operating mode", ex);//设置工作模式时发生异常
            }
        }

        public void GetReadTagTimeInterval(int deviceNo)
        {
            try
            {
                if (!CSerialPort.IsOpen)
                {
                    CSerialPort.Open();
                }

                _curType = ReceivedValueType.GetReadTagTimeInterval;
                var devNo = (byte) deviceNo; //设备号
                var cmd = Commands.GetReadTagTimeIntervalCommand(devNo);
                SentCommand(cmd, 0, cmd.Length);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred when obtaining the reader time interval", ex);//获取读卡时间间隔时发生异常
            }
        }

        public void SetReadTagTimeInterval(int deviceNo, byte interval)
        {
            try
            {
                if (!CSerialPort.IsOpen)
                {
                    CSerialPort.Open();
                }

                _curType = ReceivedValueType.SetTimingInterval;
                var devNo = (byte) deviceNo; //设备号
                var cmd = Commands.SetReadTagTimeIntervalCommand(devNo, interval);
                SentCommand(cmd, 0, cmd.Length);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred while setting the timing interval", ex);//设置定时间隔时发生异常
            }
        }

        public void GetAdjacentDiscriminantTime(int deviceNo)
        {
            try
            {
                if (!CSerialPort.IsOpen)
                {
                    CSerialPort.Open();
                }

                _curType = ReceivedValueType.GetAdjacentDiscriminantTime;
                var devNo = (byte) deviceNo; //设备号
                var cmd = Commands.GetAdjacentDiscriminantTimeCommand(devNo);
                SentCommand(cmd, 0, cmd.Length);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred while obtaining the adjacent discriminating time", ex);//获取相邻判别时间时发生异常
            }
        }

        public void SetAdjacentDiscriminantTime(int deviceNo, byte time)
        {
            try
            {
                if (!CSerialPort.IsOpen)
                {
                    CSerialPort.Open();
                }

                _curType = ReceivedValueType.SetAdjacentDiscriminantTime;
                var devNo = (byte) deviceNo; //设备号
                var cmd = Commands.SetAdjacentDiscriminantTimeCommand(devNo, time);
                SentCommand(cmd, 0, cmd.Length);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred while setting adjacent to determine the duration of", ex);//设置相邻判别持续时间时发生异常
            }
        }

        public void GetAdjacentDiscriminant(int deviceNo)
        {
            try
            {
                if (!CSerialPort.IsOpen)
                {
                    CSerialPort.Open();
                }

                _curType = ReceivedValueType.GetAdjacentDiscriminant;
                var devNo = (byte) deviceNo; //设备号
                var cmd = Commands.GetAdjacentDiscriminantCommand(devNo);
                SentCommand(cmd, 0, cmd.Length);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred while getting neighboring discrimination", ex);//获取相邻判别时发生异常
            }
        }

        public void SetAdjacentDiscriminant(int deviceNo, byte adjacentDiscriminant)
        {
            try
            {
                if (!CSerialPort.IsOpen)
                {
                    CSerialPort.Open();
                }

                _curType = ReceivedValueType.SetAdjacentDiscriminant;
                var devNo = (byte) deviceNo; //设备号
                var cmd = Commands.SetAdjacentDiscriminantCommand(devNo, adjacentDiscriminant);
                SentCommand(cmd, 0, cmd.Length);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred while setting adjacent sentence", ex); //设置相邻判时发生异常
            }
        }

        public void GetTriggerSwitch(int deviceNo)
        {
            try
            {
                if (!CSerialPort.IsOpen)
                {
                    CSerialPort.Open();
                }

                _curType = ReceivedValueType.GetTriggerSwitch;
                var devNo = (byte) deviceNo; //设备号
                var cmd = Commands.GetTriggerSwitchCommand(devNo);
                SentCommand(cmd, 0, cmd.Length);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred while getting the trigger switch", ex);//获取触发开关时发生异常
            }
        }

        public void SetTriggerSwitch(int deviceNo, byte triggerSwitch)
        {
            try
            {
                if (!CSerialPort.IsOpen)
                {
                    CSerialPort.Open();
                }

                _curType = ReceivedValueType.SetTriggerSwitch;
                var devNo = (byte) deviceNo; //设备号
                var cmd = Commands.SetTriggerSwitchCommand(devNo, triggerSwitch);
                SentCommand(cmd, 0, cmd.Length);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred while setting the trigger switch", ex);//设置触发开关时发生异常
            }
        }

        public void GetTriggerDelay(int deviceNo)
        {
            try
            {
                if (!CSerialPort.IsOpen)
                {
                    CSerialPort.Open();
                }

                _curType = ReceivedValueType.GetTriggerDelay;
                var devNo = (byte) deviceNo; //设备号
                var cmd = Commands.GetTriggerDelayCommand(devNo);
                SentCommand(cmd, 0, cmd.Length);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred while obtaining the delay time", ex);//获取延迟时间时发生异常
            }
        }

        public void SetTriggerDelay(int deviceNo, byte delay)
        {
            try
            {
                if (!CSerialPort.IsOpen)
                {
                    CSerialPort.Open();
                }

                _curType = ReceivedValueType.GetTriggerDelay;
                var devNo = (byte) deviceNo; //设备号
                var cmd = Commands.SetTriggerDelayCommand(devNo, delay);
                SentCommand(cmd, 0, cmd.Length);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred while setting the delay time", ex);//设置延迟时间时发生异常
            }
        }

        public void GetDeviceNumber(int deviceNo)
        {
            try
            {
                if (!CSerialPort.IsOpen)
                {
                    CSerialPort.Open();
                }

                _curType = ReceivedValueType.GetDeviceNumber;
                var devNo = (byte) deviceNo; //设备号
                var cmd = Commands.GetDeviceNumberCommand(devNo);
                SentCommand(cmd, 0, cmd.Length);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred while getting the device address", ex);//获取设备地址时发生异常
            }
        }

        public void SetDeviceNumber(int deviceNo, byte number)
        {
            try
            {
                if (!CSerialPort.IsOpen)
                {
                    CSerialPort.Open();
                }

                _curType = ReceivedValueType.SetDeviceNumber;
                var devNo = (byte) deviceNo; //设备号
                var cmd = Commands.SetDeviceNumberCommand(devNo, number);
                SentCommand(cmd, 0, cmd.Length);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred when setting the device address", ex);//设置设备地址时发生异常
            }
        }

        public void GetTransmitPower(int deviceNo)
        {
            try
            {
                if (!CSerialPort.IsOpen)
                {
                    CSerialPort.Open();
                }

                _curType = ReceivedValueType.GetTransmitPower;
                var devNo = (byte) deviceNo; //设备号
                var cmd = Commands.GetTransmitPowerCommand(devNo);
                SentCommand(cmd, 0, cmd.Length);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred while obtaining transmission power", ex);//获取发射功率时发生异常
            }
        }

        public void SetTransmitPower(int deviceNo, byte power)
        {
            try
            {
                if (!CSerialPort.IsOpen)
                {
                    CSerialPort.Open();
                }

                _curType = ReceivedValueType.SetTransmitPower;
                var devNo = (byte) deviceNo; //设备号
                var cmd = Commands.SetTransmitPowerCommand(devNo, power);
                SentCommand(cmd, 0, cmd.Length);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred while setting the transmission power", ex);//设置发射功率时发生异常
            }
        }

        public void GetAntenna(int deviceNo)
        {
            try
            {
                if (!CSerialPort.IsOpen)
                {
                    CSerialPort.Open();
                }

                _curType = ReceivedValueType.GetAntenna;
                var devNo = (byte) deviceNo; //设备号
                var cmd = Commands.GetAntennaCommand(devNo);
                SentCommand(cmd, 0, cmd.Length);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred while obtaining antenna Information", ex);//获取天线信息时发生异常
            }
        }

        public void SetAntenna(int deviceNo, byte antenna)
        {
            try
            {
                if (!CSerialPort.IsOpen)
                {
                    CSerialPort.Open();
                }

                _curType = ReceivedValueType.SetAntenna;
                var devNo = (byte) deviceNo; //设备号
                var cmd = Commands.SetAntennaCommand(devNo, antenna);
                SentCommand(cmd, 0, cmd.Length);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred when setting up the antenna", ex);//设置天线时发生异常
            }
        }

        public void GetReadTagType(int deviceNo)
        {
            try
            {
                if (!CSerialPort.IsOpen)
                {
                    CSerialPort.Open();
                }

                _curType = ReceivedValueType.GetReadTagType;
                var devNo = (byte) deviceNo; //设备号
                var cmd = Commands.GetReadTagTypeCommand(devNo);
                SentCommand(cmd, 0, cmd.Length);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred while obtaining the card reader mode", ex);//获取读卡方式时发生异常
            }
        }

        public void SetReadTagType(int deviceNo, byte type)
        {
            try
            {
                if (!CSerialPort.IsOpen)
                {
                    CSerialPort.Open();
                }

                _curType = ReceivedValueType.SetReadTagType;
                var devNo = (byte) deviceNo; //设备号
                var cmd = Commands.SetReadTagTypeCommand(devNo, type);
                SentCommand(cmd, 0, cmd.Length);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred when setting up the reader mode", ex);//设置读卡方式时发生异常
            }
        }

        public void SetCommunicationType(int deviceNo, byte type)
        {
            try
            {
                if (!CSerialPort.IsOpen)
                {
                    CSerialPort.Open();
                }

                _curType = ReceivedValueType.SetCommunicationInterfaceType;
                var devNo = (byte) deviceNo; //设备号
                var cmd = Commands.SetCommunicationInterfaceCommand(devNo, type);
                SentCommand(cmd, 0, cmd.Length);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred when setting the type of communication interface mode", ex);//设置通信接口类型方式时发生异常
            }
        }

        public void SetBaudRate(int deviceNo, byte baudRate)
        {
            try
            {
                if (!CSerialPort.IsOpen)
                {
                    CSerialPort.Open();
                }

                _curType = ReceivedValueType.SetBaudRate;
                var devNo = (byte) deviceNo; //设备号
                var cmd = Commands.SetBaudRateCommand(devNo, baudRate);
                SentCommand(cmd, 0, cmd.Length);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred when setting the baud rate", ex);//设置波特率时发生异常
            }
        }

        public void GetWiegandParams(int deviceNo)
        {
            try
            {
                if (!CSerialPort.IsOpen)
                {
                    CSerialPort.Open();
                }

                _curType = ReceivedValueType.GetWiegandParam;
                var devNo = (byte) deviceNo; //设备号
                var cmd = Commands.GetWiegandParamsCommand(devNo);
                SentCommand(cmd, 0, cmd.Length);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred while getting Wiegand parameters", ex);//获取韦根参数时发生异常
            }
        }

        public void SetWiegandParams(int deviceNo, byte type, byte width, byte period)
        {
            try
            {
                if (!CSerialPort.IsOpen)
                {
                    CSerialPort.Open();
                }

                _curType = ReceivedValueType.SetWiegand;
                var devNo = (byte) deviceNo; //设备号
                var cmd = Commands.SetWiegandParamsCommand(devNo, new[] {type, width, period});
                SentCommand(cmd, 0, cmd.Length);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred when setting parameters Wiegand", ex);//设置韦根参数时发生异常
            }
        }

        public void GetHopping(int deviceNo)
        {
            try
            {
                if (!CSerialPort.IsOpen)
                {
                    CSerialPort.Open();
                }

                _curType = ReceivedValueType.GetHopping;
                var devNo = (byte) deviceNo; //设备号
                var cmd = Commands.GetHoppingCommand(devNo);
                SentCommand(cmd, 0, cmd.Length);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred while getting hopping parameters", ex);//获取跳频参数时发生异常
            }
        }

        public void SetHopping(int deviceNo, byte frequency)
        {
            try
            {
                if (!CSerialPort.IsOpen)
                {
                    CSerialPort.Open();
                }

                _curType = ReceivedValueType.SetHopping;
                var devNo = (byte) deviceNo; //设备号
                var cmd = Commands.SetHoppingCommand(devNo, frequency);
                SentCommand(cmd, 0, cmd.Length);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred while setting parameters of frequency hopping", ex);//设置跳频参数时发生异常
            }
        }

        public void GetBands(int deviceNo)
        {
            try
            {
                if (!CSerialPort.IsOpen)
                {
                    CSerialPort.Open();
                }

                _curType = ReceivedValueType.GetBands;
                var devNo = (byte) deviceNo; //设备号
                var cmd = Commands.GetBandsCommand(devNo);
                SentCommand(cmd, 0, cmd.Length);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred while obtaining frequency", ex);//获取频点时发生异常
            }
        }

        public void SetBands(int deviceNo, byte[] bands)
        {
            try
            {
                if (!CSerialPort.IsOpen)
                {
                    CSerialPort.Open();
                }

                _curType = ReceivedValueType.SetBands;
                var devNo = (byte) deviceNo; //设备号
                var cmd = Commands.SetBandsCommand(devNo, bands);
                SentCommand(cmd, 0, cmd.Length);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred while setting frequency", ex);//设置频点时发生异常
            }
        }

        public void SetBuzzer(int deviceNo, byte buzzerCtrl)
        {
            try
            {
                if (!CSerialPort.IsOpen)
                {
                    CSerialPort.Open();
                }

                _curType = ReceivedValueType.SetBuzzer;
                var devNo = (byte) deviceNo; //设备号
                var cmd = Commands.SetBuzzerCommand(devNo, buzzerCtrl);
                SentCommand(cmd, 0, cmd.Length);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred when setting the buzzer", ex);//设置蜂鸣器时发生异常
            }
        }
        
        public void SetRelays(int deviceNo, byte relayOnOff)
        {
            try
            {
                if (!CSerialPort.IsOpen)
                {
                    CSerialPort.Open();
                }

                _curType = ReceivedValueType.SetRelays;
                var devNo = (byte) deviceNo; //设备号
                var cmd = Commands.SetRelaysCommand(devNo, relayOnOff);
                SentCommand(cmd, 0, cmd.Length);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred when setting relay", ex);//设置继电器时发生异常
            }
        }

        public void QuickWriteTag(int deviceNo, byte[] data)
        {
            try
            {
                if (!CSerialPort.IsOpen)
                {
                    CSerialPort.Open();
                }

                _curType = ReceivedValueType.QuickWriteTag;
                var devNo = (byte) deviceNo; //设备号
                var cmd = Commands.QuickWriteTagCommand(devNo, data);
                SentCommand(cmd, 0, cmd.Length);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred when the fast write tag", ex);//快写标签时发生异常
            }
        }

        public void ReadTag(int deviceNo, byte area, byte address, byte length)
        {
            try
            {
                if (!CSerialPort.IsOpen)
                {
                    CSerialPort.Open();
                }

                _curType = ReceivedValueType.ReadTag;
                var devNo = (byte) deviceNo; //设备号
                var cmd = Commands.ReadEpcTag(devNo, area, address, length);
                SentCommand(cmd, 0, cmd.Length);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred when reading tags", ex);//读标签时发生异常
            }
        }

        public void WriteTagSingleWord(int device, byte area, byte address, byte[] data)
        {
            try
            {
                if (!CSerialPort.IsOpen)
                {
                    CSerialPort.Open();
                }

                _curType = ReceivedValueType.WriteTag;
                var devNo = (byte) device; //设备号
                var cmd = Commands.WriteTagSingleWord(devNo, area, address, 0x01, data[0], data[1]);
                SentCommand(cmd, 0, cmd.Length);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred while writing tag", ex);//写标签时发生异常
            }
        }

        public void WriteTagMultiWords(int device, byte area, byte address, byte length, byte[] data)
        {
            try
            {
                if (!CSerialPort.IsOpen)
                {
                    CSerialPort.Open();
                }

                if (data.Length < length*2)
                {
                    throw new Exception("An exception occurred while writing tags");//写标签时发生异常
                }
                _curType = ReceivedValueType.WriteTag;
                var devNo = (byte)device; //设备号
                var cmd = Commands.WriteTagMultiWord(devNo, area, address, length, data);
                SentCommand(cmd, 0, cmd.Length);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred while writing tags", ex);//写标签时发生异常
            }
        }

        public void InitilizeTag(int deviceNo)
        {
            try
            {
                if (!CSerialPort.IsOpen)
                {
                    CSerialPort.Open();
                }

                _curType= ReceivedValueType.InitilizeTag;
                var devNo = (byte) deviceNo; //设备号
                var cmd = Commands.InitilizeTagCommand(devNo);
                SentCommand(cmd, 0, cmd.Length);
            }
            catch (Exception ex)
            {
                throw new Exception("Initialized when an exception occurs", ex);//初始化时发生异常
            }
        }

        public void LockTag(int deviceNo, byte lockType, byte[] password)
        {
            try
            {
                if (!CSerialPort.IsOpen)
                {
                    CSerialPort.Open();
                }

                _curType= ReceivedValueType.LockTag;
                var devNo = (byte) deviceNo; //设备号
                var cmd = Commands.LockTagCommand(devNo, lockType, password);
                SentCommand(cmd, 0, cmd.Length);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred while locking tag", ex);//锁定标签时发生异常
            }
        }

        public void UnlockTag(int deviceNo, byte unlockType, byte[] password)
        {
            try
            {
                if (!CSerialPort.IsOpen)
                {
                    CSerialPort.Open();
                }

                _curType= ReceivedValueType.UnlockTag;
                var devNo = (byte) deviceNo; //设备号
                var cmd = Commands.UnlockTagCommand(devNo, unlockType, password);
                SentCommand(cmd, 0, cmd.Length);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred while unlocking tag", ex);//解锁标签时发生异常
            }
        }

        public void KillTag(int deviceNo, byte[] password)
        {
            try
            {
                if (!CSerialPort.IsOpen)
                {
                    CSerialPort.Open();
                }

                _curType = ReceivedValueType.KillTag;
                var devNo = (byte) deviceNo; //设备号
                var cmd = Commands.KillTagCommand(devNo, password);
                SentCommand(cmd, 0, cmd.Length);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred while destruction of the tag", ex);//销毁标签时发生异常
            }
        }

        public void SetSingleParams(ReceivedValueType type, int deviceNo, byte msb, byte lsb, byte data)
        {
            try
            {
                if (!CSerialPort.IsOpen)
                {
                    CSerialPort.Open();
                }

                _curType = type;
                var devNo = (byte)deviceNo; //设备号
                var cmd = Commands.SetSingleReaderParamCommand(devNo, msb, lsb, data);
                SentCommand(cmd, 0, cmd.Length);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred when setting the reader", ex);//设置读卡器时发生异常
            }
        }

        public void SetMultiParams(ReceivedValueType type, int deviceNo, byte msb, byte lsb, byte[] data)
        {
            try
            {
                if (!CSerialPort.IsOpen)
                {
                    CSerialPort.Open();
                }

                _curType = type;
                var devNo = (byte) deviceNo; //设备号
                var cmd = Commands.SetMultiReaderParamsCommand(devNo, msb, lsb, data);
                SentCommand(cmd, 0, cmd.Length);
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred when setting the reader", ex);//设置读卡器时发生异常
            }
        }

        public void ResetReader(int deviceNo)
        {
            try
            {
                if (!CSerialPort.IsOpen)
                {
                    CSerialPort.Open();
                }

                _curType = ReceivedValueType.Reset;
                var devNo = (byte) deviceNo; //设备号
                var cmd = Commands.ResetReaderCommand(devNo);
                SentCommand(cmd, 0, cmd.Length);
            }
            catch (Exception ex)
            {
                throw new Exception("Reset exception occurs when the reader", ex);//复位读写器时发生异常
            }
        }

        //注册操作结果接收事件
        public void RegisterOperateResultDataReceivedEvent(GetOperateResultHandler handlerGetOperatreResult)
        {
            _getOperateResultHandler = handlerGetOperatreResult;
            CSerialPort.DataReceived += CSerialPort_OperateResultDataReceied;
        }

        //清除串口所有事件
        public void ClearDataReceivedEvent()
        {
            ClearDataReveived(CSerialPort);
            _getOperateResultHandler = null;
        }

        private void CSerialPort_OperateResultDataReceied(object sender, SerialDataReceivedEventArgs e)
        {
            var n = CSerialPort.BytesToRead;
            var buf = new byte[n];
            CSerialPort.Read(buf, 0, n);

            
            _buffer.AddRange(buf);
            switch (_curType)
            {
                case ReceivedValueType.QuickWriteTag:
                    var quickWrite = ProcessQuickWriteTag(_buffer);//快写标签结果处理
                    _getOperateResultHandler?.Invoke(ReceivedValueType.QuickWriteTag, quickWrite);
                    break;
                case ReceivedValueType.InitilizeTag:
                    var init = ProcessInitilizeTag(_buffer);//初始化结果
                    _getOperateResultHandler?.Invoke(ReceivedValueType.InitilizeTag, init);
                    break;
                case ReceivedValueType.LockTag:
                    //锁定标签结果处理
                    var lockTag = ProcessLockTag(_buffer);
                    _getOperateResultHandler?.Invoke(ReceivedValueType.LockTag, lockTag);
                    break;
                case ReceivedValueType.WriteTag:
                    var writeTag = ProcessWriteTag(_buffer);
                    _getOperateResultHandler?.Invoke(ReceivedValueType.WriteTag, writeTag);
                    break;
                case ReceivedValueType.UnlockTag:
                    var unlock = ProcessUnlockTag(_buffer);//解锁标签结果处理
                    _getOperateResultHandler?.Invoke(ReceivedValueType.UnlockTag, unlock);
                    break;
                case ReceivedValueType.KillTag:
                    var kill = ProcessKillTag(_buffer);//销毁标签结果处理
                    _getOperateResultHandler?.Invoke(ReceivedValueType.KillTag, kill);
                    break;
                case ReceivedValueType.Reset:
                    var reset = ProcessResetReader(_buffer);//复位读写器结果处理
                    _getOperateResultHandler?.Invoke(ReceivedValueType.Reset, reset);
                    break;
                case ReceivedValueType.SetBuzzer:
                    var buzzer = ProcessSetBuzzer(_buffer);//控制蜂鸣器结果处理
                    _getOperateResultHandler?.Invoke(ReceivedValueType.SetBuzzer, buzzer);
                    break;
                case ReceivedValueType.SetRelays:
                    var relay = ProcessSetRelays(_buffer);//控制继电器结果处理
                    _getOperateResultHandler?.Invoke(ReceivedValueType.Reset, relay);
                    break;
                case ReceivedValueType.GetFirmwareVersion:
                    var version = ProcessFirmwareVersion(_buffer);//版本号查找
                    _getOperateResultHandler?.Invoke(ReceivedValueType.GetFirmwareVersion, version);
                    break;
                case ReceivedValueType.GetCommunicationInterfaceType:
                    var commType = ProcessCommunicationInterfaceType(_buffer);//通信接口类型
                    _getOperateResultHandler?.Invoke(ReceivedValueType.GetCommunicationInterfaceType, commType);
                    break;
                case ReceivedValueType.GetBaudRate:
                    var baudRate = ProcessBaudRate(_buffer);//波特率
                    _getOperateResultHandler?.Invoke(ReceivedValueType.GetBaudRate, baudRate);
                    break;
                case ReceivedValueType.SetBaudRate:
                    var result = ProcessSetBaudrate(_buffer);
                    _getOperateResultHandler?.Invoke(ReceivedValueType.SetBaudRate, result);
                    break;
                case ReceivedValueType.GetWorkMode:
                    var workMode = ProcessWorkMode(_buffer);//工作模式
                    _getOperateResultHandler?.Invoke(ReceivedValueType.GetWorkMode, workMode);
                    break;
                case ReceivedValueType.GetReadTagTimeInterval:
                    var interval = ProcessReadTagTimeInterval(_buffer);//读卡时间间隔
                    _getOperateResultHandler?.Invoke(ReceivedValueType.GetReadTagTimeInterval, interval);
                    break;
                case ReceivedValueType.GetAdjacentDiscriminantTime:
                    var adt = ProcessAdjacentDiscriminantTime(_buffer);//相邻判别时间
                    _getOperateResultHandler?.Invoke(ReceivedValueType.GetAdjacentDiscriminantTime, adt);
                    break;
                case ReceivedValueType.GetAdjacentDiscriminant:
                    var ad = ProcessAdjacentDiscriminant(_buffer);
                    _getOperateResultHandler?.Invoke(ReceivedValueType.GetAdjacentDiscriminant, ad);
                    break;
                case ReceivedValueType.GetDeviceNumber:
                    var address = ProcessDeviceNumber(_buffer);//设备地址
                    _getOperateResultHandler?.Invoke(ReceivedValueType.GetDeviceNumber, address);
                    break;
                case ReceivedValueType.GetTransmitPower:
                    var power = ProcessTransmitPower(_buffer);//发射功率
                    _getOperateResultHandler?.Invoke(ReceivedValueType.GetTransmitPower, power);
                    break;
                case ReceivedValueType.GetReadTagType:
                    var rType = ProcessReadTagType(_buffer);//读卡类型
                    _getOperateResultHandler?.Invoke(ReceivedValueType.GetReadTagType, rType);
                    break;
                case ReceivedValueType.GetTriggerSwitch:
                    var tSwitch = ProcessTriggerSwitch(_buffer);//触发开关
                    _getOperateResultHandler?.Invoke(ReceivedValueType.GetTriggerSwitch, tSwitch);
                    break;
                case ReceivedValueType.GetTriggerDelay:
                    var delay = ProcessTriggerDelay(_buffer);//延迟时间
                    _getOperateResultHandler?.Invoke(ReceivedValueType.GetTriggerDelay, delay);
                    break;
                case ReceivedValueType.GetWiegandParam:
                    ProcessWiegandParam(_buffer);//韦根参数（类型、宽度、周期）
                    break;
                case ReceivedValueType.GetWiegandProtocol:
                    var wiegandType = ProcessWiegandProtocol(_buffer);//韦根类型
                    _getOperateResultHandler?.Invoke(ReceivedValueType.GetWiegandProtocol, wiegandType);
                    break;
                case ReceivedValueType.GetWiegandWidth:
                    var wiegandWidth = ProcessWiegandWidth(_buffer);//韦根宽度
                    _getOperateResultHandler?.Invoke(ReceivedValueType.GetWiegandWidth, wiegandWidth);
                    break;
                case ReceivedValueType.GetWiegandPeriod:
                    var wiegandPeriod = ProcessWiegandPeriod(_buffer);//韦根周期
                    _getOperateResultHandler?.Invoke(ReceivedValueType.GetTriggerDelay, wiegandPeriod);
                    break;
                case ReceivedValueType.GetHopping:
                    var hopping = ProcessHopping(_buffer);//跳频
                    _getOperateResultHandler?.Invoke(ReceivedValueType.GetHopping, hopping);
                    break;
                case ReceivedValueType.GetBands:
                    var bands = ProcessBands(_buffer);//频段
                    _getOperateResultHandler?.Invoke(ReceivedValueType.GetBands, bands);
                    break;
                case ReceivedValueType.GetAntenna:
                    var antenna = ProcessAntenna(_buffer);//天线
                    _getOperateResultHandler?.Invoke(ReceivedValueType.GetAntenna, antenna);
                    break;
                case ReceivedValueType.ReadTag:
                    var readTag = ProcessReadTag(_buffer);//读卡
                    _getOperateResultHandler?.Invoke(ReceivedValueType.ReadTag, readTag);
                    break;
                case ReceivedValueType.IdentifySingleTag:
                    ProcessSingleIndentifyDataReceived(_buffer);
                    break;
                case ReceivedValueType.IdentifyMultieTag:
                    ProcessMultiIndentifyDataReceived(_buffer);
                    break;
                case ReceivedValueType.StopIdentify:
                    _getOperateResultHandler?.Invoke(ReceivedValueType.StopIdentify, true);
                    break;
                default:
                    var msg = ProcessSetParams(_buffer);//设置参数返回的结果
                    _getOperateResultHandler?.Invoke(ReceivedValueType.SetReaderParams, msg);
                    break;
            }
            
        }

        private void ClearDataReveived(SerialPort serialPort)
        {
            var field = typeof (SerialPort).GetField("DataReceived",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static);
            if (field != null)
            {
                var fieldValue = field.GetValue(serialPort);
                var value = fieldValue as Delegate;
                if (value != null)
                {
                    var objectDelegate = value;
                    foreach (var handler in objectDelegate.GetInvocationList())
                    {
                        typeof (SerialPort).GetEvent("DataReceived").RemoveEventHandler(serialPort, handler);
                    }
                }
            }
        }

        /*
         * +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
         * 数据处理方法
         * +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
         */
        private void ProcessMultiIndentifyDataReceived(List<byte> buffer)
        {
            //完整性判断  
            while (buffer.Count >= 17)
            {
                var data = buffer.Take(17).ToArray();
                var checkSum = CheckUtils.SumCheck(data.Take(15).ToArray(), 0, 15);
                if (data[0] == 0x00 && data[16] == 0xFF && data[15] == checkSum)
                {
                    try
                    {
                        var devNo = data[1].ToString();
                        var antNo = data[14].ToString();
                        var epcData = TextEncoder.ByteArrayToHexString(data.Skip(2).Take(12).ToArray());

                        _getOperateResultHandler?.Invoke(ReceivedValueType.IdentifyTag, new[] { epcData, antNo, devNo });
                        buffer.RemoveRange(0, 17);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("EPC:{0}", ex.Message);
                    }
                    _buffer.Clear();
                }
                else
                {
                    buffer.RemoveAt(0);
                }
            }
        }

        private void ProcessSingleIndentifyDataReceived(List<byte> buffer)
        {
            //完整性判断  
            while (buffer.Count >= 18)
            {
                var data = buffer.Take(18).ToArray();
                var checkSum = CheckUtils.SumCheck(data.Take(17).ToArray(), 0, 17);
                if (data[0] == 0xD0 && data[1] == 0x10 && data[2] == 0x82 && data[17] == checkSum)
                {
                    try
                    {
                        var devNo = data[3].ToString();
                        var antNo = data[4].ToString();
                        var epcData = TextEncoder.ByteArrayToHexString(data.Skip(5).Take(12).ToArray());

                        _getOperateResultHandler?.Invoke(ReceivedValueType.IdentifyTag, new[] { epcData, antNo, devNo });
                        buffer.RemoveRange(0, 17);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("EPC:{0}", ex.Message);
                    }
                    _buffer.Clear();
                }
                else
                {
                    buffer.RemoveAt(0);
                }
            }
        }

        //硬件版本号处理
        private string ProcessFirmwareVersion(List<byte> buffer)
        {
            if (buffer == null) return null;
            //完整性判断
            while (buffer.Count >= 7)
            {
                var data = buffer.Take(7).ToArray();
                if (data[0] == 0xD0 && data[2] == 0x6A)
                {
                    _buffer.Clear();
                    var checkSum = CheckUtils.SumCheck(data.Take(6).ToArray(), 0, 6);
                    if (data[6] != checkSum) return null;
                    var version = $"{data[4]}.{data[5]}";
                    return version;
                }
                buffer.RemoveAt(0);
            }
            return null;
        }

        //通信接口类型处理
        private int? ProcessCommunicationInterfaceType(List<byte> buffer)
        {
            if (CSerialPort == null)
            {
                return null;
            }

            //完整性判断  
            while (buffer.Count >= 8)
            {
                var data = buffer.Take(8).ToArray();
                if (data[0] == 0xD0 && data[2] == 0x61 && data[5] == 0x72)
                {
                    _buffer.Clear();
                    var checkSum = CheckUtils.SumCheck(data.Take(7).ToArray(), 0, 7); //和校验
                    if (data[7] != checkSum) continue;
                    var type = (int) data[6];
                    return type;
                }
                buffer.RemoveAt(0);
            }
            return null;
        }

        private int? ProcessBaudRate(List<byte> buffer)
        {
            if (CSerialPort == null)
            {
                return null;
            }

            //完整性判断  
            while (buffer.Count >= 8)
            {
                var data = buffer.Take(8).ToArray();
                if (data[0] == 0xD0 && data[2] == 0x61 && data[5] == 0x85)
                {
                    _buffer.Clear();
                    var checkSum = CheckUtils.SumCheck(data.Take(7).ToArray(), 0, 7); //和校验
                    if (data[7] != checkSum) continue;
                    switch (data[6])
                    {
                        case 0x00:
                            return 9600;
                        case 0x01:
                            return 19200;
                        case 0x02:
                            return 38400;
                        case 0x03:
                            return 57600;
                        case 0x04:
                            return 115200;
                        default:
                            return null;
                    }
                }
                buffer.RemoveAt(0);
            }
            return null;
        }

        //工作模式处理
        private int? ProcessWorkMode(List<byte> buffer)
        {
            if (CSerialPort == null)
            {
                return null;
            }

            //完整性判断  
            while (buffer.Count >= 8)
            {
                var data = buffer.Take(8).ToArray();
                if (data[0] == 0xD0 && data[2] == 0x61 && data[5] == 0x70)
                {
                    _buffer.Clear();
                    var checkSum = CheckUtils.SumCheck(data.Take(7).ToArray(), 0, 7); //和校验
                    if (data[7] != checkSum) continue;
                    var type = (int) data[6];
                    return type;
                }
                buffer.RemoveAt(0);
            }
            return null;
        }

        //读卡时间间隔处理
        private int? ProcessReadTagTimeInterval(List<byte> buffer)
        {
            if (CSerialPort == null)
            {
                return null;
            }

            //完整性判断  
            while (buffer.Count >= 8)
            {
                var data = buffer.Take(8).ToArray();
                if (data[0] == 0xD0 && data[2] == 0x61 && data[5] == 0x71)
                {
                    _buffer.Clear();
                    var checkSum = CheckUtils.SumCheck(data.Take(7).ToArray(), 0, 7); //和校验
                    if (data[7] != checkSum) continue;
                    var type = (int) data[6];
                    return type;
                }
                buffer.RemoveAt(0);
            }
            return null;
        }

        //相邻判别时间处理
        private int? ProcessAdjacentDiscriminantTime(List<byte> buffer)
        {
            if (CSerialPort == null)
            {
                return null;
            }

            //完整性判断  
            while (buffer.Count >= 8)
            {
                var data = buffer.Take(8).ToArray();
                if (data[0] == 0xD0 && data[2] == 0x61 && data[5] == 0x7A)
                {
                    _buffer.Clear();
                    var checkSum = CheckUtils.SumCheck(data.Take(7).ToArray(), 0, 7); //和校验
                    if (data[7] != checkSum) continue;
                    var type = (int) data[6];
                    return type;
                }
                buffer.RemoveAt(0);
            }
            return null;
        }

        //相邻判别处理
        private bool ProcessAdjacentDiscriminant(List<byte> buffer)
        {
            if (CSerialPort == null)
            {
                return false;
            }

            //完整性判断  
            while (buffer.Count >= 8)
            {
                var data = buffer.Take(8).ToArray();
                if (data[0] == 0xD0 && data[2] == 0x61 && data[5] == 0x7B)
                {
                    _buffer.Clear();
                    var checkSum = CheckUtils.SumCheck(data.Take(7).ToArray(), 0, 7); //和校验
                    if (data[7] != checkSum) continue;
                    var type = (data[6] == 1); //1——启动，2——不启动
                    return type;
                }
                buffer.RemoveAt(0);
            }
            return false;
        }

        //设备地址处理
        private int? ProcessDeviceNumber(List<byte> buffer)
        {
            if (CSerialPort == null)
            {
                return null;
            }

            //完整性判断  
            while (buffer.Count >= 8)
            {
                var data = buffer.Take(8).ToArray();
                if (data[0] == 0xD0 && data[2] == 0x61 && data[5] == 0x64)
                {
                    _buffer.Clear();
                    var checkSum = CheckUtils.SumCheck(data.Take(7).ToArray(), 0, 7); //和校验
                    if (data[7] != checkSum) continue;
                    var type = (int) data[6];
                    return type;
                }
                buffer.RemoveAt(0);
            }
            return null;
        }

        //发射功率数据处理
        private int? ProcessTransmitPower(List<byte> buffer)
        {
            if (CSerialPort == null)
            {
                return null;
            }

            //完整性判断  
            while (buffer.Count >= 8)
            {
                var data = buffer.Take(8).ToArray();
                if (data[0] == 0xD0 && data[2] == 0x61 && data[5] == 0x65)
                {
                    _buffer.Clear();
                    var checkSum = CheckUtils.SumCheck(data.Take(7).ToArray(), 0, 7); //和校验
                    if (data[7] != checkSum) continue;
                    var type = (int) data[6];
                    return type;
                }
                buffer.RemoveAt(0);
            }
            return null;
        }

        private int? ProcessAntenna(List<byte> buffer)
        {
            if (CSerialPort == null)
            {
                return null;
            }

            //完整性判断  
            while (buffer.Count >= 8)
            {
                var data = buffer.Take(8).ToArray();
                if (data[0] == 0xD0 && data[2] == 0x61 && data[5] == 0x8A)
                {
                    _buffer.Clear();
                    var checkSum = CheckUtils.SumCheck(data.Take(7).ToArray(), 0, 7); //和校验
                    if (data[7] != checkSum) continue;
                    var type = (int) data[6];
                    return type;
                }
                buffer.RemoveAt(0);
            }
            return null;
        }

        //读卡类型处理
        private int? ProcessReadTagType(List<byte> buffer)
        {
            if (CSerialPort == null)
            {
                return null;
            }

            //完整性判断  
            while (buffer.Count >= 8)
            {
                var data = buffer.Take(8).ToArray();
                if (data[0] == 0xD0 && data[2] == 0x61 && data[5] == 0x87)
                {
                    _buffer.Clear();
                    var checkSum = CheckUtils.SumCheck(data.Take(7).ToArray(), 0, 7); //和校验
                    if (data[7] != checkSum) continue;
                    var type = (int) data[6]; //0：EPC 单标签识别;1：EPC 多标签识别; 2：18000_6B 单标签识别; 3：18000_6B 多标签识别

                    return type;
                }
                buffer.RemoveAt(0);
            }
            return null;
        }

        //触发模式开关
        private int? ProcessTriggerSwitch(List<byte> buffer)
        {
            if (CSerialPort == null)
            {
                return null;
            }

            //完整性判断  
            while (buffer.Count >= 8)
            {
                var data = buffer.Take(8).ToArray();
                if (data[0] == 0xD0 && data[2] == 0x61 && data[5] == 0x80)
                {
                    _buffer.Clear();
                    var checkSum = CheckUtils.SumCheck(data.Take(7).ToArray(), 0, 7); //和校验
                    if (data[7] != checkSum) continue;
                    var type = (int) data[6]; //0：不触发;1：触发

                    return type;
                }
                buffer.RemoveAt(0);
            }
            return null;
        }

        //延迟时间
        private int? ProcessTriggerDelay(List<byte> buffer)
        {
            if (CSerialPort == null)
            {
                return null;
            }

            //完整性判断  
            while (buffer.Count >= 8)
            {
                var data = buffer.Take(8).ToArray();
                if (data[0] == 0xD0 && data[2] == 0x61 && data[5] == 0x84)
                {
                    _buffer.Clear();
                    var checkSum = CheckUtils.SumCheck(data.Take(7).ToArray(), 0, 7); //和校验
                    if (data[7] != checkSum) continue;
                    var type = (int) data[6]; //0-240

                    return type;
                }
                buffer.RemoveAt(0);
            }
            return null;
        }

        //韦根参数处理
        private void ProcessWiegandParam(List<byte> buffer)
        {
            if (CSerialPort == null)
            {
                return;
            }

            //完整性判断  
            while (buffer.Count >= 11)
            {
                var data = buffer.Take(11).ToArray();
                if (data[0] == 0xD0 && data[2] == 0x63 && data[6] == 0x73)
                {
                    _buffer.Clear();
                    var checkSum = CheckUtils.SumCheck(data.Take(10).ToArray(), 0, 10); //和校验
                    if (data[10] != checkSum) continue;
                    var type = (int)data[7]; //1：wiegand26,2：wiegand34,3：wiegand32
                    _getOperateResultHandler?.Invoke(ReceivedValueType.GetWiegandProtocol, type);
                    var width = (int) data[8];
                    _getOperateResultHandler?.Invoke(ReceivedValueType.GetWiegandWidth, width);
                    var period = (int)data[9];
                    _getOperateResultHandler?.Invoke(ReceivedValueType.GetWiegandPeriod, period);
                    return;
                }
                buffer.RemoveAt(0);
            }
        }

        //韦根协议
        private int? ProcessWiegandProtocol(List<byte> buffer)
        {
            if (CSerialPort == null)
            {
                return null;
            }

            //完整性判断  
            while (buffer.Count >= 8)
            {
                var data = buffer.Take(8).ToArray();
                if (data[0] == 0xD0 && data[2] == 0x61 && data[5] == 0x73)
                {
                    _buffer.Clear();
                    var checkSum = CheckUtils.SumCheck(data.Take(7).ToArray(), 0, 7); //和校验
                    if (data[7] != checkSum) continue;
                    var type = (int) data[6]; //1：wiegand26,2：wiegand34,3：wiegand32

                    return type;
                }
                buffer.RemoveAt(0);
            }
            return null;
        }

        //韦根宽度
        private int? ProcessWiegandWidth(List<byte> buffer)
        {
            if (CSerialPort == null)
            {
                return null;
            }

            //完整性判断  
            while (buffer.Count >= 8)
            {
                var data = buffer.Take(8).ToArray();
                if (data[0] == 0xD0 && data[2] == 0x61 && data[5] == 0x74)
                {
                    _buffer.Clear();
                    var checkSum = CheckUtils.SumCheck(data.Take(7).ToArray(), 0, 7); //和校验
                    if (data[7] != checkSum) continue;
                    var type = (int) data[6]; //1-255

                    return type;
                }
                buffer.RemoveAt(0);
            }
            return null;
        }

        //韦根周期
        private int? ProcessWiegandPeriod(List<byte> buffer)
        {
            if (CSerialPort == null)
            {
                return null;
            }

            //完整性判断  
            while (buffer.Count >= 8)
            {
                var data = buffer.Take(8).ToArray();
                if (data[0] == 0xD0 && data[2] == 0x61 && data[5] == 0x75)
                {
                    _buffer.Clear();
                    var checkSum = CheckUtils.SumCheck(data.Take(7).ToArray(), 0, 7); //和校验
                    if (data[7] != checkSum) continue;
                    var type = (int) data[6]; //1-255

                    return type;
                }
                buffer.RemoveAt(0);
            }
            return null;
        }

        //跳频
        private int? ProcessHopping(List<byte> buffer)
        {
            if (CSerialPort == null)
            {
                return null;
            }

            //完整性判断  
            while (buffer.Count >= 8)
            {
                var data = buffer.Take(8).ToArray();
                if (data[0] == 0xD0 && data[2] == 0x61 && data[5] == 0x90)
                {
                    _buffer.Clear();
                    var checkSum = CheckUtils.SumCheck(data.Take(7).ToArray(), 0, 7); //和校验
                    if (data[7] != checkSum) continue;
                    var type = (int) data[6]; //0-50

                    return type;
                }
                buffer.RemoveAt(0);
            }
            return null;
        }

        //
        private bool[] ProcessBands(List<byte> buffer)
        {
            if (CSerialPort == null)
            {
                return null;
            }

            //完整性判断  
            while (buffer.Count >= 15)
            {
                var data = buffer.Take(15).ToArray();
                if (data[0] == 0xD0 && data[2] == 0x63 && data[6] == 0x92)
                {
                    _buffer.Clear();

                    var checkSum = CheckUtils.SumCheck(data.Take(15).ToArray(), 0, 14); //和校验
                    if (data[14] != checkSum) continue;
                    var bands = new bool[56];
                    var band = data.Skip(7).Take(7).ToArray();
                    for (var i = 0; i < band.Length; i ++)
                    {
                        var bina = TextEncoder.ByteToString(band[i]);
                        if (bina.Length < 8) continue;
                        var newBina = bina.Reverse().ToArray();
                        for (var j = 0; j < 8; j++)
                        {
                            if (newBina[j] != '1') continue;
                            bands[i*8 + j] = true;
                        }
                    }
                    return bands;
                }
                buffer.RemoveAt(0);
            }
            return null;
        }

        //设置波特率
        private bool ProcessSetBaudrate(List<byte> buffer)
        {
            if (CSerialPort == null)
            {
                return false;
            }

            //完整性判断  
            while (buffer.Count >= 6)
            {
                var data = buffer.Take(6).ToArray();
                if (data[0] == 0xD4 && data[1] == 0x04 && data[2] == 0xA9)
                {
                    _buffer.Clear();

                    var checkSum = CheckUtils.SumCheck(data.Take(6).ToArray(), 0, 5); //和校验
                    if (data[5] != checkSum) continue;
                    return (data[4] == 0x00);
                }
                buffer.RemoveAt(0);
            }
            return false;
        }

        //设置读卡器参数处理
        private bool ProcessSetParams(List<byte> buffer)
        {
            if (CSerialPort == null)
            {
                return false;
            }

            //完整性判断  
            while (buffer.Count >= 6)
            {
                var data = buffer.Take(6).ToArray();
                if (data[0] == 0xD4 && data[1] == 0x04 && (data[2] == 0x60 || data[2] == 0x62))
                {
                    _buffer.Clear();

                    var checkSum = CheckUtils.SumCheck(data.Take(6).ToArray(), 0, 5); //和校验
                    if (data[5] != checkSum) continue;
                    return (data[4] == 0x00);
                }
                buffer.RemoveAt(0);
            }
            return false;
        }

        //快写标签结果处理
        private bool ProcessQuickWriteTag(List<byte> buffer)
        {
            if (CSerialPort == null)
            {
                return false;
            }

            //完整性判断  
            while (buffer.Count >= 6)
            {
                var data = buffer.Take(6).ToArray();
                if (data[0] == 0xD0 && data[1] == 0x04 && data[2] == 0x9C)
                {
                    _buffer.Clear();

                    var checkSum = CheckUtils.SumCheck(data.Take(6).ToArray(), 0, 5); //和校验
                    if (data[5] != checkSum) continue;
                    return (data[4] == 0x00);
                }
                buffer.RemoveAt(0);
            }
            return false;
        }

        //写标签
        private bool ProcessWriteTag(List<byte> buffer)
        {
            if (CSerialPort == null)
            {
                return false;
            }

            //完整性判断  
            while (buffer.Count >= 6)
            {
                var data = buffer.Take(6).ToArray();
                if (data[0] == 0xD0 && data[1] == 0x04 && data[2] == 0x81)
                {
                    _buffer.Clear();

                    var checkSum = CheckUtils.SumCheck(data.Take(6).ToArray(), 0, 5); //和校验
                    if (data[5] != checkSum) continue;
                    return (data[4] == 0x00);
                }
                buffer.RemoveAt(0);
            }
            return false;
        }

        //读标签
        private byte[] ProcessReadTag(List<byte> buffer)
        {
            if (CSerialPort == null)
            {
                return null;
            }

            while (buffer.Count >= 10)
            {
                if (buffer[0] == 0xD0 && buffer[2] == 0x80)
                {
                    var len = buffer[1];
                    if (buffer.Count >= len + 2)
                    {
                        var data = buffer.Take(len + 2).ToArray();
                        var checkSum = CheckUtils.SumCheck(data.Take(data.Length).ToArray(), 0, data.Length - 1); //和校验
                        if (data[data.Length - 1] == checkSum)
                        {
                            _buffer.Clear();
                            //数据长度
                            var length = data[6] * 2;
                            return data.Skip(7).Take(length).ToArray();
                        }
                    }
                    else
                    {
                        break;
                    }
                }
                buffer.RemoveAt(0);
            }
            return null;
        }

        //初始化EPC结果处理
        private bool ProcessInitilizeTag(List<byte> buffer)
        {
            if (CSerialPort == null)
            {
                return false;
            }

            //完整性判断  
            while (buffer.Count >= 6)
            {
                var data = buffer.Take(6).ToArray();
                if (data[0] == 0xD4 && data[1] == 0x04 && data[2] == 0x99)
                {
                    _buffer.Clear();

                    var checkSum = CheckUtils.SumCheck(data.Take(6).ToArray(), 0, 5); //和校验
                    if (data[5] != checkSum) continue;
                    return (data[4] == 0x00);
                }
                buffer.RemoveAt(0);
            }
            return false;
        }

        //锁定标签结果处理
        private bool ProcessLockTag(List<byte> buffer)
        {
            if (CSerialPort == null)
            {
                return false;
            }

            //完整性判断  
            while (buffer.Count >= 6)
            {
                var data = buffer.Take(6).ToArray();
                if (data[0] == 0xD4 && data[1] == 0x04 && data[2] == 0xA5)
                {
                    _buffer.Clear();

                    var checkSum = CheckUtils.SumCheck(data.Take(6).ToArray(), 0, 5); //和校验
                    if (data[5] != checkSum) continue;
                    return (data[4] == 0x00);
                }
                buffer.RemoveAt(0);
            }
            return false;
        }

        //解锁标签结果处理
        private bool ProcessUnlockTag(List<byte> buffer)
        {
            if (CSerialPort == null)
            {
                return false;
            }

            //完整性判断  
            while (buffer.Count >= 6)
            {
                var data = buffer.Take(6).ToArray();
                if (data[0] == 0xD4 && data[1] == 0x04 && data[2] == 0xA6)
                {
                    _buffer.Clear();

                    var checkSum = CheckUtils.SumCheck(data.Take(6).ToArray(), 0, 5); //和校验
                    if (data[5] != checkSum) continue;
                    return (data[4] == 0x00);
                }
                buffer.RemoveAt(0);
            }
            return false;
        }

        //销毁标签结果处理
        private bool ProcessKillTag(List<byte> buffer)
        {
            if (CSerialPort == null)
            {
                return false;
            }

            //完整性判断  
            while (buffer.Count >= 6)
            {
                var data = buffer.Take(6).ToArray();
                if (data[0] == 0xD4 && data[1] == 0x04 && data[2] == 0x86)
                {
                    _buffer.Clear();

                    var checkSum = CheckUtils.SumCheck(data.Take(6).ToArray(), 0, 5); //和校验
                    if (data[5] != checkSum) continue;
                    return (data[4] == 0x00);
                }
                buffer.RemoveAt(0);
            }
            return false;
        }

        //复位读写器结果处理
        private bool ProcessResetReader(List<byte> buffer)
        {
            if (CSerialPort == null)
            {
                return false;
            }

            //完整性判断  
            while (buffer.Count >= 6)
            {
                var data = buffer.Take(6).ToArray();
                if (data[0] == 0xD4 && data[1] == 0x04 && data[2] == 0x65)
                {
                    _buffer.Clear();

                    var checkSum = CheckUtils.SumCheck(data.Take(6).ToArray(), 0, 5); //和校验
                    if (data[5] != checkSum) continue;
                    return (data[4] == 0x00);
                }
                buffer.RemoveAt(0);
            }
            return false;
        }

        //设置蜂鸣器结果处理
        private bool ProcessSetBuzzer(List<byte> buffer)
        {
            if (CSerialPort == null)
            {
                return false;
            }

            //完整性判断  
            while (buffer.Count >= 6)
            {
                var data = buffer.Take(6).ToArray();
                if (data[0] == 0xD4 && data[1] == 0x04 && data[2] == 0xB0)
                {
                    _buffer.Clear();

                    var checkSum = CheckUtils.SumCheck(data.Take(6).ToArray(), 0, 5); //和校验
                    if (data[5] != checkSum) continue;
                    return (data[4] == 0x00);
                }
                buffer.RemoveAt(0);
            }
            return false;
        }

        //设置继电器结果处理
        private bool ProcessSetRelays(List<byte> buffer)
        {
            if (CSerialPort == null)
            {
                return false;
            }

            //完整性判断  
            while (buffer.Count >= 6)
            {
                var data = buffer.Take(6).ToArray();
                if (data[0] == 0xD4 && data[1] == 0x04 && data[2] == 0xB1)
                {
                    _buffer.Clear();

                    var checkSum = CheckUtils.SumCheck(data.Take(6).ToArray(), 0, 5); //和校验
                    if (data[5] != checkSum) continue;
                    return (data[4] == 0x00);
                }
                buffer.RemoveAt(0);
            }
            return false;
        }
    }

    public enum ReceivedValueType
    {
        IdentifyTag,//标签识别
        IdentifySingleTag,//单标签识别
        IdentifyMultieTag,//多标签识别
        StopIdentify,//停止识别标签
        GetFirmwareVersion, //版本号
        GetCommunicationInterfaceType, //通信接口类型
        GetBaudRate, //波特率
        GetWorkMode, //工作模式
        GetReadTagTimeInterval, //读卡时间间隔
        GetAdjacentDiscriminant, //相邻判别
        GetAdjacentDiscriminantTime, //相邻判别时间
        GetDeviceNumber, //设备号
        GetTransmitPower, //发射功率
        GetAntenna, //天线
        GetReadTagType, //读卡方式
        GetTriggerSwitch, //触发开关
        GetTriggerDelay, //延迟时间
        GetWiegandParam,//获取韦根参数
        GetWiegandProtocol, //韦根协议
        GetWiegandWidth, //韦根宽度
        GetWiegandPeriod, //韦根周期
        GetHopping, //跳频
        GetBands,  //频点
        ReadTag,//读取标签
        WriteTag,//写标签

        InitilizeTag, //初始化
        QuickWriteTag, //快写标签
        LockTag, //锁定标签
        UnlockTag, //解锁标签
        KillTag, //销毁标签
        Reset, //复位
        SetCommunicationInterfaceType, //设置通信接口
        SetBaudRate, //波特率
        SetWiegand, //韦根参数
        SetWorkMode, //工作模式
        SetAdjacentDiscriminant, //相邻判别
        SetAdjacentDiscriminantTime, //相邻判别时间
        SetTimingInterval, //定时间隔
        SetTriggerSwitch, //触发开关
        SetTriggerDelay, //延迟关闭时间
        SetDeviceNumber, //设备号
        SetTransmitPower, //传输功率
        SetAntenna, //设置天线
        SetReadTagType, //读卡方式
        SetHopping, //跳频设置
        SetBands, //频点设置
        SetBuzzer, //蜂鸣器
        SetRelays, //继电器
        SetReaderParams//设置读写器
    }
}