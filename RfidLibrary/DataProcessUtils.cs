using System;
using System.Collections.Generic;
using System.Linq;

namespace RfidLibrary
{
    public class DataProcessUtils
    {
        //硬件版本号处理
        public static string ProcessFirmwareVersion(List<byte> buffer)
        {
            if (buffer == null) return null;
            //完整性判断
            while (buffer.Count >= 7)
            {
                var data = buffer.Take(7).ToArray();
                if (data[0] == 0xD0 && data[2] == 0x6A)
                {
                    var checkSum = CheckUtils.SumCheck(data.Take(6).ToArray(), 0, 6);
                    if (data[6] != checkSum) return null;
                    var version = $"{data[4]}.{data[5]}";
                    return version;
                }
                buffer.RemoveAt(0);
            }
            return null;
        }

        //设置读卡器参数处理
        public static bool ProcessSetParams(List<byte> buffer)
        {
            if (buffer == null)
            {
                return false;
            }

            //完整性判断  
            while (buffer.Count >= 6)
            {
                var data = buffer.Take(6).ToArray();
                if (data[0] == 0xD4 && data[1] == 0x04 && (data[2] == 0x60 || data[2] == 0x62))
                {
                    var checkSum = CheckUtils.SumCheck(data.Take(6).ToArray(), 0, 5); //和校验
                    if (data[5] != checkSum) continue;
                    return (data[4] == 0x00);
                }
                buffer.RemoveAt(0);
            }
            return false;
        }

        //多卡读取结果处理
        public static void ProcessMultiIndentifyDataReceived(List<byte> buffer, GetOperateResultHandler handler)
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

                        handler?.Invoke(ReceivedValueType.IdentifyTag, new[] { epcData, antNo, devNo });
                        buffer.RemoveRange(0, 17);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("EPC:{0}", ex.Message);
                    }
                }
                else
                {
                    buffer.RemoveAt(0);
                }
            }
        }

        //单卡读取结果处理
        public static void ProcessSingleIndentifyDataReceived(List<byte> buffer, GetOperateResultHandler handler)
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

                        handler?.Invoke(ReceivedValueType.IdentifyTag, new[] { epcData, antNo, devNo });
                        buffer.RemoveRange(0, 17);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("EPC:{0}", ex.Message);
                    }
                }
                else
                {
                    buffer.RemoveAt(0);
                }
            }
        }

        //通信接口类型处理
        public static int? ProcessCommunicationInterfaceType(List<byte> buffer)
        {
            //完整性判断  
            while (buffer.Count >= 8)
            {
                var data = buffer.Take(8).ToArray();
                if (data[0] == 0xD0 && data[2] == 0x61 && data[5] == 0x72)
                {
                    var checkSum = CheckUtils.SumCheck(data.Take(7).ToArray(), 0, 7); //和校验
                    if (data[7] != checkSum) continue;
                    var type = (int)data[6];
                    return type;
                }
                buffer.RemoveAt(0);
            }
            return null;
        }

        //获取波特率结果处理
        public static int? ProcessBaudRate(List<byte> buffer)
        {
            //完整性判断  
            while (buffer.Count >= 8)
            {
                var data = buffer.Take(8).ToArray();
                if (data[0] == 0xD0 && data[2] == 0x61 && data[5] == 0x85)
                {
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
        public static int? ProcessWorkMode(List<byte> buffer)
        {
            //完整性判断  
            while (buffer.Count >= 8)
            {
                var data = buffer.Take(8).ToArray();
                if (data[0] == 0xD0 && data[2] == 0x61 && data[5] == 0x70)
                {
                    var checkSum = CheckUtils.SumCheck(data.Take(7).ToArray(), 0, 7); //和校验
                    if (data[7] != checkSum) continue;
                    var type = (int)data[6];
                    return type;
                }
                buffer.RemoveAt(0);
            }
            return null;
        }

        //读卡时间间隔处理
        public static int? ProcessReadTagTimeInterval(List<byte> buffer)
        {
            //完整性判断  
            while (buffer.Count >= 8)
            {
                var data = buffer.Take(8).ToArray();
                if (data[0] == 0xD0 && data[2] == 0x61 && data[5] == 0x71)
                {
                    var checkSum = CheckUtils.SumCheck(data.Take(7).ToArray(), 0, 7); //和校验
                    if (data[7] != checkSum) continue;
                    var type = (int)data[6];
                    return type;
                }
                buffer.RemoveAt(0);
            }
            return null;
        }

        //相邻判别时间处理
        public static int? ProcessAdjacentDiscriminantTime(List<byte> buffer)
        {
            //完整性判断  
            while (buffer.Count >= 8)
            {
                var data = buffer.Take(8).ToArray();
                if (data[0] == 0xD0 && data[2] == 0x61 && data[5] == 0x7A)
                {
                    var checkSum = CheckUtils.SumCheck(data.Take(7).ToArray(), 0, 7); //和校验
                    if (data[7] != checkSum) continue;
                    var type = (int)data[6];
                    return type;
                }
                buffer.RemoveAt(0);
            }
            return null;
        }

        //相邻判别处理
        public static bool ProcessAdjacentDiscriminant(List<byte> buffer)
        {
            //完整性判断  
            while (buffer.Count >= 8)
            {
                var data = buffer.Take(8).ToArray();
                if (data[0] == 0xD0 && data[2] == 0x61 && data[5] == 0x7B)
                {
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
        public static int? ProcessDeviceNumber(List<byte> buffer)
        {
            //完整性判断  
            while (buffer.Count >= 8)
            {
                var data = buffer.Take(8).ToArray();
                if (data[0] == 0xD0 && data[2] == 0x61 && data[5] == 0x64)
                {
                    var checkSum = CheckUtils.SumCheck(data.Take(7).ToArray(), 0, 7); //和校验
                    if (data[7] != checkSum) continue;
                    var type = (int)data[6];
                    return type;
                }
                buffer.RemoveAt(0);
            }
            return null;
        }

        //发射功率数据处理
        public static int? ProcessTransmitPower(List<byte> buffer)
        {
            //完整性判断  
            while (buffer.Count >= 8)
            {
                var data = buffer.Take(8).ToArray();
                if (data[0] == 0xD0 && data[2] == 0x61 && data[5] == 0x65)
                {
                    var checkSum = CheckUtils.SumCheck(data.Take(7).ToArray(), 0, 7); //和校验
                    if (data[7] != checkSum) continue;
                    var type = (int)data[6];
                    return type;
                }
                buffer.RemoveAt(0);
            }
            return null;
        }

        //获取天线结果处理
        public static int? ProcessAntenna(List<byte> buffer)
        {
            //完整性判断  
            while (buffer.Count >= 8)
            {
                var data = buffer.Take(8).ToArray();
                if (data[0] == 0xD0 && data[2] == 0x61 && data[5] == 0x8A)
                {
                    var checkSum = CheckUtils.SumCheck(data.Take(7).ToArray(), 0, 7); //和校验
                    if (data[7] != checkSum) continue;
                    var type = (int)data[6];
                    return type;
                }
                buffer.RemoveAt(0);
            }
            return null;
        }

        //读卡类型处理
        public static int? ProcessReadTagType(List<byte> buffer)
        {
            //完整性判断  
            while (buffer.Count >= 8)
            {
                var data = buffer.Take(8).ToArray();
                if (data[0] == 0xD0 && data[2] == 0x61 && data[5] == 0x87)
                {
                    var checkSum = CheckUtils.SumCheck(data.Take(7).ToArray(), 0, 7); //和校验
                    if (data[7] != checkSum) continue;
                    var type = (int)data[6]; //0：EPC 单标签识别;1：EPC 多标签识别; 2：18000_6B 单标签识别; 3：18000_6B 多标签识别

                    return type;
                }
                buffer.RemoveAt(0);
            }
            return null;
        }

        //触发模式开关
        public static int? ProcessTriggerSwitch(List<byte> buffer)
        {
            //完整性判断  
            while (buffer.Count >= 8)
            {
                var data = buffer.Take(8).ToArray();
                if (data[0] == 0xD0 && data[2] == 0x61 && data[5] == 0x80)
                {
                    var checkSum = CheckUtils.SumCheck(data.Take(7).ToArray(), 0, 7); //和校验
                    if (data[7] != checkSum) continue;
                    var type = (int)data[6]; //0：不触发;1：触发

                    return type;
                }
                buffer.RemoveAt(0);
            }
            return null;
        }

        //延迟时间
        public static int? ProcessTriggerDelay(List<byte> buffer)
        {
            //完整性判断  
            while (buffer.Count >= 8)
            {
                var data = buffer.Take(8).ToArray();
                if (data[0] == 0xD0 && data[2] == 0x61 && data[5] == 0x84)
                {
                    var checkSum = CheckUtils.SumCheck(data.Take(7).ToArray(), 0, 7); //和校验
                    if (data[7] != checkSum) continue;
                    var type = (int)data[6]; //0-240

                    return type;
                }
                buffer.RemoveAt(0);
            }
            return null;
        }

        //韦根参数处理
        public static void ProcessWiegandParam(List<byte> buffer, GetOperateResultHandler handler)
        {
            //完整性判断  
            while (buffer.Count >= 11)
            {
                var data = buffer.Take(11).ToArray();
                if (data[0] == 0xD0 && data[2] == 0x63 && data[6] == 0x73)
                {
                    var checkSum = CheckUtils.SumCheck(data.Take(10).ToArray(), 0, 10); //和校验
                    if (data[10] != checkSum) continue;
                    var type = (int)data[7]; //1：wiegand26,2：wiegand34,3：wiegand32
                    handler?.Invoke(ReceivedValueType.GetWiegandProtocol, type);
                    var width = (int)data[8];
                    handler?.Invoke(ReceivedValueType.GetWiegandWidth, width);
                    var period = (int)data[9];
                    handler?.Invoke(ReceivedValueType.GetWiegandPeriod, period);
                    return;
                }
                buffer.RemoveAt(0);
            }
        }

        //韦根协议
        public static int? ProcessWiegandProtocol(List<byte> buffer)
        {
            //完整性判断  
            while (buffer.Count >= 8)
            {
                var data = buffer.Take(8).ToArray();
                if (data[0] == 0xD0 && data[2] == 0x61 && data[5] == 0x73)
                {
                    var checkSum = CheckUtils.SumCheck(data.Take(7).ToArray(), 0, 7); //和校验
                    if (data[7] != checkSum) continue;
                    var type = (int)data[6]; //1：wiegand26,2：wiegand34,3：wiegand32

                    return type;
                }
                buffer.RemoveAt(0);
            }
            return null;
        }

        //韦根宽度
        public static int? ProcessWiegandWidth(List<byte> buffer)
        {
            //完整性判断  
            while (buffer.Count >= 8)
            {
                var data = buffer.Take(8).ToArray();
                if (data[0] == 0xD0 && data[2] == 0x61 && data[5] == 0x74)
                {
                    var checkSum = CheckUtils.SumCheck(data.Take(7).ToArray(), 0, 7); //和校验
                    if (data[7] != checkSum) continue;
                    var type = (int)data[6]; //1-255

                    return type;
                }
                buffer.RemoveAt(0);
            }
            return null;
        }

        //韦根周期
        public static int? ProcessWiegandPeriod(List<byte> buffer)
        {
            //完整性判断  
            while (buffer.Count >= 8)
            {
                var data = buffer.Take(8).ToArray();
                if (data[0] == 0xD0 && data[2] == 0x61 && data[5] == 0x75)
                {
                    var checkSum = CheckUtils.SumCheck(data.Take(7).ToArray(), 0, 7); //和校验
                    if (data[7] != checkSum) continue;
                    var type = (int)data[6]; //1-255

                    return type;
                }
                buffer.RemoveAt(0);
            }
            return null;
        }

        //跳频
        public static int? ProcessHopping(List<byte> buffer)
        {
            //完整性判断  
            while (buffer.Count >= 8)
            {
                var data = buffer.Take(8).ToArray();
                if (data[0] == 0xD0 && data[2] == 0x61 && data[5] == 0x90)
                {
                    var checkSum = CheckUtils.SumCheck(data.Take(7).ToArray(), 0, 7); //和校验
                    if (data[7] != checkSum) continue;
                    var type = (int)data[6]; //0-50

                    return type;
                }
                buffer.RemoveAt(0);
            }
            return null;
        }

        //获取频点结果处理
        public static bool[] ProcessBands(List<byte> buffer)
        {
            //完整性判断  
            while (buffer.Count >= 15)
            {
                var data = buffer.Take(15).ToArray();
                if (data[0] == 0xD0 && data[2] == 0x63 && data[6] == 0x92)
                {
                    var checkSum = CheckUtils.SumCheck(data.Take(15).ToArray(), 0, 14); //和校验
                    if (data[14] != checkSum) continue;
                    var bands = new bool[56];
                    var band = data.Skip(7).Take(7).ToArray();
                    for (var i = 0; i < band.Length; i++)
                    {
                        var bina = TextEncoder.ByteToString(band[i]);
                        if (bina.Length < 8) continue;
                        var newBina = bina.Reverse().ToArray();
                        for (var j = 0; j < 8; j++)
                        {
                            if (newBina[j] != '1') continue;
                            bands[i * 8 + j] = true;
                        }
                    }
                    return bands;
                }
                buffer.RemoveAt(0);
            }
            return null;
        }

        //设置波特率
        public static bool ProcessSetBaudrate(List<byte> buffer)
        {
            //完整性判断  
            while (buffer.Count >= 6)
            {
                var data = buffer.Take(6).ToArray();
                if (data[0] == 0xD4 && data[1] == 0x04 && data[2] == 0xA9)
                {
                    var checkSum = CheckUtils.SumCheck(data.Take(6).ToArray(), 0, 5); //和校验
                    if (data[5] != checkSum) continue;
                    return (data[4] == 0x00);
                }
                buffer.RemoveAt(0);
            }
            return false;
        }

        //快写标签结果处理
        public static bool ProcessQuickWriteTag(List<byte> buffer)
        {
            //完整性判断  
            while (buffer.Count >= 6)
            {
                var data = buffer.Take(6).ToArray();
                if (data[0] == 0xD0 && data[1] == 0x04 && data[2] == 0x9C)
                {
                    var checkSum = CheckUtils.SumCheck(data.Take(6).ToArray(), 0, 5); //和校验
                    if (data[5] != checkSum) continue;
                    return (data[4] == 0x00);
                }
                buffer.RemoveAt(0);
            }
            return false;
        }

        //写标签
        public static bool ProcessWriteTag(List<byte> buffer)
        {
            //完整性判断  
            while (buffer.Count >= 6)
            {
                var data = buffer.Take(6).ToArray();
                if (data[0] == 0xD0 && data[1] == 0x04 && data[2] == 0x81)
                {
                    var checkSum = CheckUtils.SumCheck(data.Take(6).ToArray(), 0, 5); //和校验
                    if (data[5] != checkSum) continue;
                    return (data[4] == 0x00);
                }
                buffer.RemoveAt(0);
            }
            return false;
        }

        //读标签
        public static byte[] ProcessReadTag(List<byte> buffer)
        {
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
        public static bool ProcessInitilizeTag(List<byte> buffer)
        {
            //完整性判断  
            while (buffer.Count >= 6)
            {
                var data = buffer.Take(6).ToArray();
                if (data[0] == 0xD4 && data[1] == 0x04 && data[2] == 0x99)
                {
                    var checkSum = CheckUtils.SumCheck(data.Take(6).ToArray(), 0, 5); //和校验
                    if (data[5] != checkSum) continue;
                    return (data[4] == 0x00);
                }
                buffer.RemoveAt(0);
            }
            return false;
        }

        //锁定标签结果处理
        public static bool ProcessLockTag(List<byte> buffer)
        {
            //完整性判断  
            while (buffer.Count >= 6)
            {
                var data = buffer.Take(6).ToArray();
                if (data[0] == 0xD4 && data[1] == 0x04 && data[2] == 0xA5)
                {
                    var checkSum = CheckUtils.SumCheck(data.Take(6).ToArray(), 0, 5); //和校验
                    if (data[5] != checkSum) continue;
                    return (data[4] == 0x00);
                }
                buffer.RemoveAt(0);
            }
            return false;
        }

        //解锁标签结果处理
        public static bool ProcessUnlockTag(List<byte> buffer)
        {
            //完整性判断  
            while (buffer.Count >= 6)
            {
                var data = buffer.Take(6).ToArray();
                if (data[0] == 0xD4 && data[1] == 0x04 && data[2] == 0xA6)
                {
                    var checkSum = CheckUtils.SumCheck(data.Take(6).ToArray(), 0, 5); //和校验
                    if (data[5] != checkSum) continue;
                    return (data[4] == 0x00);
                }
                buffer.RemoveAt(0);
            }
            return false;
        }

        //销毁标签结果处理
        public static bool ProcessKillTag(List<byte> buffer)
        {
            //完整性判断  
            while (buffer.Count >= 6)
            {
                var data = buffer.Take(6).ToArray();
                if (data[0] == 0xD4 && data[1] == 0x04 && data[2] == 0x86)
                {
                    var checkSum = CheckUtils.SumCheck(data.Take(6).ToArray(), 0, 5); //和校验
                    if (data[5] != checkSum) continue;
                    return (data[4] == 0x00);
                }
                buffer.RemoveAt(0);
            }
            return false;
        }

        //复位读写器结果处理
        public static bool ProcessResetReader(List<byte> buffer)
        {
            //完整性判断  
            while (buffer.Count >= 6)
            {
                var data = buffer.Take(6).ToArray();
                if (data[0] == 0xD4 && data[1] == 0x04 && data[2] == 0x65)
                {
                    var checkSum = CheckUtils.SumCheck(data.Take(6).ToArray(), 0, 5); //和校验
                    if (data[5] != checkSum) continue;
                    return (data[4] == 0x00);
                }
                buffer.RemoveAt(0);
            }
            return false;
        }

        //设置蜂鸣器结果处理
        public static bool ProcessSetBuzzer(List<byte> buffer)
        {
            //完整性判断  
            while (buffer.Count >= 6)
            {
                var data = buffer.Take(6).ToArray();
                if (data[0] == 0xD4 && data[1] == 0x04 && data[2] == 0xB0)
                {
                    var checkSum = CheckUtils.SumCheck(data.Take(6).ToArray(), 0, 5); //和校验
                    if (data[5] != checkSum) continue;
                    return (data[4] == 0x00);
                }
                buffer.RemoveAt(0);
            }
            return false;
        }

        //设置继电器结果处理
        public static bool ProcessSetRelays(List<byte> buffer)
        {
            //完整性判断  
            while (buffer.Count >= 6)
            {
                var data = buffer.Take(6).ToArray();
                if (data[0] == 0xD4 && data[1] == 0x04 && data[2] == 0xB1)
                {
                    var checkSum = CheckUtils.SumCheck(data.Take(6).ToArray(), 0, 5); //和校验
                    if (data[5] != checkSum) continue;
                    return (data[4] == 0x00);
                }
                buffer.RemoveAt(0);
            }
            return false;
        }


    }
}
