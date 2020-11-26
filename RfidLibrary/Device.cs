using System;
using System.Linq;
using System.Text;

namespace RfidLibrary
{
    public class Device
    {
        public int Id { get; set; }

        public string IpAddress { get; set; }

        public string TargetIpAddress { get; set; }

        public int Port { get; set; }

        public int TargetPort { get; set; }

        public string Gateway { get; set; }

        public string MacAddress { get; private set; }

        private string _mode;
        public string Mode 
        {
            get { return _mode;}
            set
            {
                _mode = value;
                if (_mode.Contains("UDP Client"))
                {
                    _originalBytes[23] = 0x00;
                }
                else if (_mode.Contains("TCP Client"))
                {
                    _originalBytes[23] = 0x01;
                }
                else if (_mode.Contains("UDP Server"))
                {
                    _originalBytes[23] = 0x02;
                }
                else
                {
                    _originalBytes[23] = 0x03;
                }
            }
        }

        public int ModeIndex { get; private set; }

        public string SubNetMask { get; private set; }

        //串口参数
        public int BuadRate { get; private set; }

        /// <summary>
        /// 数据位：0--5位数据位, 1--6位数据位, 2--7位数据位, 3--8位数据位
        /// </summary>
        public int DataBits { get; private set; }

        /// <summary>
        /// 停止位：0--1位停止位, 1--2位停止位
        /// </summary>
        public int StopBits { get; private set; }

        /// <summary>
        /// 校验位：0--无, 1--ODD奇校验, 2--EVEN偶校验, 3--Mark置一, 4--Clear清零
        /// </summary>
        public int CheckBits { get; private set; }

        private byte[] _originalBytes;
        public byte[] OriginalBytes
        {
            get
            {
                return _originalBytes;
            }
            set
            {
                var builder = new StringBuilder();
                _originalBytes = value;
                //MacAddress
                var str = new StringBuilder().Append($"{_originalBytes[0]:X2} ").Append(" ")
                    .Append($"{_originalBytes[1]:X2} ").Append(" ")
                    .Append($"{_originalBytes[2]:X2} ").Append(" ")
                    .Append($"{_originalBytes[3]:X2} ").Append(" ")
                    .Append($"{_originalBytes[4]:X2} ").Append(" ")
                    .Append($"{_originalBytes[5]:X2} ").ToString();
                MacAddress = str;
                //IpAddress
                var ip = new StringBuilder().Append(Convert.ToString(_originalBytes[16], 10))
                    .Append(".")
                    .Append(Convert.ToString(_originalBytes[15], 10))
                    .Append(".")
                    .Append(Convert.ToString(_originalBytes[14], 10))
                    .Append(".")
                    .Append(Convert.ToString(_originalBytes[13], 10));
                IpAddress = ip.ToString();
                //TargetIpAddres
                var tIp = new StringBuilder().Append(Convert.ToString(_originalBytes[10], 10))
                    .Append(".")
                    .Append(Convert.ToString(_originalBytes[9], 10))
                    .Append(".")
                    .Append(Convert.ToString(_originalBytes[8], 10))
                    .Append(".")
                    .Append(Convert.ToString(_originalBytes[7], 10));
                TargetIpAddress = tIp.ToString();
                //Port
                var port = new[] { _originalBytes[17], _originalBytes[18] };
                Port = BitConverter.ToInt16(port, 0);
                //TargetPort
                var tPort = new[] { _originalBytes[11], _originalBytes[12] };
                TargetPort = BitConverter.ToInt16(tPort, 0);
                //Gateway
                var gateWay = new StringBuilder().Append(Convert.ToString(_originalBytes[22], 10))
                    .Append(".")
                    .Append(Convert.ToString(_originalBytes[21], 10))
                    .Append(".")
                    .Append(Convert.ToString(_originalBytes[20], 10))
                    .Append(".")
                    .Append(Convert.ToString(_originalBytes[19], 10));
                Gateway = gateWay.ToString();
                //Mode
                if (Convert.ToInt32(Convert.ToString(_originalBytes[23], 10)) == 0)
                {
                    _mode = "UDP Client";
                    ModeIndex = 0;
                }
                else if (Convert.ToInt32(Convert.ToString(_originalBytes[23], 10)) == 1)
                {
                    _mode = "TCP Client";
                    ModeIndex = 1;
                }
                else if (Convert.ToInt32(Convert.ToString(_originalBytes[23], 10)) == 2)
                {
                    _mode = "UDP Server";
                    ModeIndex = 2;
                }
                else if (Convert.ToInt32(Convert.ToString(_originalBytes[23], 10)) == 3)
                {
                    _mode = "TCP Server";
                    ModeIndex = 3;
                }

                //Baud Rate
                builder.Clear();
                builder.Append("0x").Append(_originalBytes[26].ToString("X2")).Append(_originalBytes[25].ToString("X2")).Append(_originalBytes[24].ToString("X2"));
                BuadRate = Convert.ToInt32(builder.ToString(), 16);
                
                //Subnet Mask
                var sbm = new StringBuilder().Append(Convert.ToString(_originalBytes[34], 10))
                    .Append(".")
                    .Append(Convert.ToString(_originalBytes[33], 10))
                    .Append(".")
                    .Append(Convert.ToString(_originalBytes[32], 10))
                    .Append(".")
                    .Append(Convert.ToString(_originalBytes[31], 10));
                SubNetMask = sbm.ToString();

                //DataBits、StopBits、CheckBits
                GetSerialPortParams(_originalBytes[27]);
            }
        }

        /*------------------------------------------------
         * -----------------------------------------------
         * -----------------------------------------------
         */

        private void GetSerialPortParams(byte data)
        {
            try
            {
                var p = Convert.ToString(data, 2);
                var temp = p.Reverse().ToArray();
                var binary = new[] {"0", "0", "0", "0", "0", "0", "0", "0"};
                var length = temp.Length;
                if (length > 8)
                    length = 8;
                for (var i = 0; i < length; i++)
                {
                    binary[i] = temp[i].ToString();
                }
                DataBits = Convert.ToInt16(binary[0] + binary[1], 2) ;
                StopBits = Convert.ToInt16(binary[2]);
                CheckBits = Convert.ToInt16(binary[3]) == 0 ? 0 : Convert.ToInt16(binary[4] + binary[5], 2) + 1;
            }
            catch (Exception)
            {
                DataBits = -1;
                StopBits = -1;
                CheckBits = -1;
            }
        }

        /// <summary>
        /// 根据数据位、停止位、校验位获取串口参数位
        /// </summary>
        /// <param name="dataBits">数据位</param>
        /// <param name="stopBits">停止位</param>
        /// <param name="checkBits">校验位</param>
        /// <returns>返回byte型串口参数位</returns>
        public static byte GetByteSerialPortParams(int dataBits, int stopBits, int checkBits)
        {
            try
            {
                var binary = new[] { "0", "0", "0", "0", "0", "0", "0", "0" };
                //数据位
                var ds = Convert.ToString(dataBits, 2);
                if (ds.Length == 1)
                    ds = "0" + ds;
                binary[7] = ds.Substring(0, 1);
                binary[6] = ds.Substring(1, 1);
                //停止位
                binary[5] = stopBits == 0 ? "0" : "1";
                //校验位
                if (checkBits == 0)
                {
                    binary[4] = "0";
                }
                else
                {
                    var cb = Convert.ToString(stopBits, 2);
                    if (cb.Length == 1)
                        cb = "0" + ds;
                    binary[4] = "1";
                    binary[3] = cb.Substring(0, 1);
                    binary[2] = cb.Substring(1, 1);
                }
                var temp = string.Join("", binary);
                return (byte)Convert.ToInt16(temp, 2);
            }
            catch (Exception)
            {
                return 3;//默认值
            }
        }
    }
}
