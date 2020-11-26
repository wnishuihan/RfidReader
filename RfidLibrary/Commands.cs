namespace RfidLibrary
{
    internal class Commands
    {
        //获取硬件版本号
        public static byte[] GetFirmwareVersionCommand(byte devNo)
        {
            byte[] cmd = { 0xB0, 0x03, 0x6A, devNo, 0x00 };
            var len = cmd.Length;
            cmd[len - 1] = CheckUtils.SumCheck(cmd, 0, len - 1);
            return cmd;
        }

        //停止读卡
        public static byte[] GetStopReadingCommand(byte devNo)
        {
            byte[] cmd = { 0xB0, 0x03, 0xA8, devNo, 0x00 };
            var len = cmd.Length;
            cmd[len - 1] = CheckUtils.SumCheck(cmd, 0, len - 1);
            return cmd;
        }

        //单卡识别
        public static byte[] GetIdentifySingleTagCommand(byte devNo)
        {
            byte[] cmd = { 0xB0, 0x03, 0x82, devNo, 0x00 };
            var len = cmd.Length;
            cmd[len - 1] = CheckUtils.SumCheck(cmd, 0, len - 1);
            return cmd;
        }

        //多卡识别
        public static byte[] GetIdentifyMultiTagCommand(byte devNo)
        {
            //byte[] cmd = { 0xA0, 0x02, 0x65, 0x00 };
            byte[] cmd = { 0xB0, 0x03, 0x65, devNo, 0x00 };
            var len = cmd.Length;
            cmd[len - 1] = CheckUtils.SumCheck(cmd, 0, len - 1);
            return cmd;
        }

        //通信接口类型
        public static byte[] GetCommunicationTypeCommand(byte devNo)
        {
            byte[] cmd = { 0xB0, 0x05, 0x61, devNo, 0x00, 0x72, 0x00 };
            var len = cmd.Length;
            cmd[len - 1] = CheckUtils.SumCheck(cmd, 0, len - 1);
            return cmd;
        }

        //波特率
        public static byte[] GetBaudRateCommand(byte devNo)
        {
            byte[] cmd = { 0xB0, 0x05, 0x61, devNo, 0x00, 0x85, 0x00 };
            var len = cmd.Length;
            cmd[len - 1] = CheckUtils.SumCheck(cmd, 0, len - 1);
            return cmd;
        }

        //工作模式
        public static byte[] GetWorkModeTypeCommand(byte devNo)
        {
            byte[] cmd = { 0xB0, 0x05, 0x61, devNo, 0x00, 0x70, 0x00 };
            var len = cmd.Length;
            cmd[len - 1] = CheckUtils.SumCheck(cmd, 0, len - 1);
            return cmd;
        }

        //读卡时间间隔
        public static byte[] GetReadTagTimeIntervalCommand(byte devNo)
        {
            byte[] cmd = { 0xB0, 0x05, 0x61, devNo, 0x00, 0x71, 0x00 };
            var len = cmd.Length;
            cmd[len - 1] = CheckUtils.SumCheck(cmd, 0, len - 1);
            return cmd;
        }

        //相邻判别时间
        public static byte[] GetAdjacentDiscriminantTimeCommand(byte devNo)
        {
            byte[] cmd = { 0xB0, 0x05, 0x61, devNo, 0x00, 0x7A, 0x00 };
            var len = cmd.Length;
            cmd[len - 1] = CheckUtils.SumCheck(cmd, 0, len - 1);
            return cmd;
        }

        //相邻判别
        public static byte[] GetAdjacentDiscriminantCommand(byte devNo)
        {
            byte[] cmd = { 0xB0, 0x05, 0x61, devNo, 0x00, 0x7B, 0x00 };
            var len = cmd.Length;
            cmd[len - 1] = CheckUtils.SumCheck(cmd, 0, len - 1);
            return cmd;
        }

        //触发开关
        public static byte[] GetTriggerSwitchCommand(byte devNo)
        {
            return QuerySingleReaderParamCommand(devNo, 0x00, 0x80);
        }

        //延迟关闭时间
        public static byte[] GetTriggerDelayCommand(byte devNo)
        {
            return QuerySingleReaderParamCommand(devNo, 0x00, 0x84);
        }

        //用户标识码（设备号）
        public static byte[] GetDeviceNumberCommand(byte devNo)
        {
            byte[] cmd = { 0xB0, 0x05, 0x61, devNo, 0x00, 0x64, 0x00 };
            var len = cmd.Length;
            cmd[len - 1] = CheckUtils.SumCheck(cmd, 0, len - 1);
            return cmd;
        }

        //发射功率
        public static byte[] GetTransmitPowerCommand(byte devNo)
        {
            byte[] cmd = { 0xB0, 0x05, 0x61, devNo, 0x00, 0x65, 0x00 };
            var len = cmd.Length;
            cmd[len - 1] = CheckUtils.SumCheck(cmd, 0, len - 1);
            return cmd;
        }

        //天线号(0：不选任何天线工作, 1：天线1工作, 2：天线2工作, 4：天线3工作, 8：天线4工作, 15：全部天线都工作)
        public static byte[] GetAntennaCommand(byte devNo)
        {
            return QuerySingleReaderParamCommand(devNo, 0x00, 0x8A);
        }

        //单标签和多标签
        public static byte[] GetReadTagTypeCommand(byte devNo)
        {
            byte[] cmd = { 0xB0, 0x05, 0x61, devNo, 0x00, 0x87, 0x00 };
            var len = cmd.Length;
            cmd[len - 1] = CheckUtils.SumCheck(cmd, 0, len - 1);
            return cmd;
        }

        //韦根参数
        public static byte[] GetWiegandParamsCommand(byte devNo)
        {
            return QueryMultiReaderParamsCommand(devNo, 0x03, 0x00, 0x73);
        }

        //获取跳频命令
        public static byte[] GetHoppingCommand(byte devNo)
        {
            return QuerySingleReaderParamCommand(devNo, 0x00, 0x90);
        }

        //获取频段设置
        public static byte[] GetBandsCommand(byte devNo)
        {
            return QueryMultiReaderParamsCommand(devNo, 0x07, 0x00, 0x92);
        }

        ////获取蜂鸣器命令
        //public static byte[] GetBuzzerCommand(byte devNo)
        //{
        //    return QuerySingleReaderParam(devNo, 0x00, 0xB0);
        //}

        /// <summary>
        /// 查询单个参数命令
        /// </summary>
        /// <param name="devNo">设备号</param>
        /// <param name="msb">高位地址</param>
        /// <param name="lsb">低位地址</param>
        /// <returns></returns>
        public static byte[] QuerySingleReaderParamCommand(byte devNo, byte msb, byte lsb)
        {
            byte[] cmd = { 0xB0, 0x05, 0x61, devNo, msb, lsb, 0x00 };
            var len = cmd.Length;
            cmd[len - 1] = CheckUtils.SumCheck(cmd, 0, len - 1);
            return cmd;
        }

        /// <summary>
        /// 查询多个参数命令
        /// </summary>
        /// <param name="devNo">设备号/地址</param>
        /// <param name="length">查询参数的个数</param>
        /// <param name="msb">高位地址</param>
        /// <param name="lsb">低位地址</param>
        /// <returns></returns>
        public static byte[] QueryMultiReaderParamsCommand(byte devNo, byte length, byte msb, byte lsb)
        {
            byte[] cmd = {0xB0, 0x06, 0x63, devNo, length, msb, lsb, 0x00 };
            var len = cmd.Length;
            cmd[len - 1] = CheckUtils.SumCheck(cmd, 0, len - 1);//和校验
            return cmd;
        }

        /// <summary>
        /// 设置单个参数命令
        /// </summary>
        /// <param name="devNo">设备号</param>
        /// <param name="msb">高位地址</param>
        /// <param name="lsb">低位地址</param>
        /// <param name="data">数据</param>
        /// <returns>从机回：（E4 04 60）头，（00）usercode（00）Status，（B8）Checksum;Status 00：成功； 其它值：失败</returns>
        public static byte[] SetSingleReaderParamCommand(byte devNo, byte msb, byte lsb, byte data)
        {
            byte[] cmd = { 0xB0, 0x06, 0x60, devNo, msb, lsb, data, 0x00 };
            var len = cmd.Length;
            cmd[len - 1] = CheckUtils.SumCheck(cmd, 0, len - 1);//和校验
            return cmd;
        }

        /// <summary>
        /// 设置多个参数的命令
        /// </summary>
        /// <param name="devNo">设备号</param>
        /// <param name="msb">高位地址</param>
        /// <param name="lsb">低位地址</param>
        /// <param name="data">参数</param>
        /// <returns>从机回：E4 04 62 （00）usercode（00）Status，（B6）Checksum; Status00： 成功； 其它值：失败</returns>
        public static byte[] SetMultiReaderParamsCommand(byte devNo, byte msb, byte lsb, byte[] data)
        {
            var cmd = new byte[8 + data.Length];
            cmd[0] = 0xB0;
            cmd[1] = (byte)(6 + data.Length);
            cmd[2] = 0x62;
            cmd[3] = devNo;
            cmd[4] = (byte)data.Length;
            cmd[5] = msb;
            cmd[6] = lsb;
            for (var i = 0; i < data.Length; i++)
            {
                cmd[i + 7] = data[i];
            }
            var len = cmd.Length;
            cmd[len - 1] = CheckUtils.SumCheck(cmd, 0, len - 1);//和校验
            return cmd;
        }

        public static byte[] SetCommunicationInterfaceCommand(byte devNo, byte type)
        {
            byte[] cmd = { 0xB0, 0x06, 0x60, devNo, 0x00, 0x72, type, 0x00 };
            var len = cmd.Length;
            cmd[len - 1] = CheckUtils.SumCheck(cmd, 0, len - 1);//和校验
            return cmd;
        }

        //设置波特率
        public static byte[] SetBaudRateCommand(byte devNo, byte baudRate)
        {
            byte[] cmd = { 0xB0, 0x04, 0xA9, devNo, baudRate, 0x00 };
            var len = cmd.Length;
            cmd[len - 1] = CheckUtils.SumCheck(cmd, 0, len - 1);//和校验
            return cmd;
        }

        //设置工作模式
        public static byte[] SetWorkModeCommand(byte devNo, byte workmode)
        {
            byte[] cmd = { 0xB0, 0x06, 0x60, devNo, 0x00, 0x70, workmode, 0x00 };
            var len = cmd.Length;
            cmd[len - 1] = CheckUtils.SumCheck(cmd, 0, len - 1);//和校验
            return cmd;
        }

        //设置读卡间隔
        public static byte[] SetReadTagTimeIntervalCommand(byte devNo, byte interval)
        {
            byte[] cmd = { 0xB0, 0x06, 0x60, devNo, 0x00, 0x71, interval, 0x00 };
            var len = cmd.Length;
            cmd[len - 1] = CheckUtils.SumCheck(cmd, 0, len - 1);//和校验
            return cmd;
        }

        //设置相邻判别
        public static byte[] SetAdjacentDiscriminantCommand(byte devNo, byte adjaceent)
        {
            byte[] cmd = { 0xB0, 0x06, 0x60, devNo, 0x00, 0x7B, adjaceent, 0x00 };
            var len = cmd.Length;
            cmd[len - 1] = CheckUtils.SumCheck(cmd, 0, len - 1);//和校验
            return cmd;
        }

        //设置相邻判别时间
        public static byte[] SetAdjacentDiscriminantTimeCommand(byte devNo, byte adjaceentTime)
        {
            byte[] cmd = { 0xB0, 0x06, 0x60, devNo, 0x00, 0x7A, adjaceentTime, 0x00 };
            var len = cmd.Length;
            cmd[len - 1] = CheckUtils.SumCheck(cmd, 0, len - 1);//和校验
            return cmd;
        }

        //设置触发开关
        public static byte[] SetTriggerSwitchCommand(byte devNo, byte triggerSwitch)
        {
            return SetSingleReaderParamCommand(devNo, 0x00, 0x80, triggerSwitch);
        }

        //设置延迟关闭时间
        public static byte[] SetTriggerDelayCommand(byte devNo, byte delay)
        {
            return SetSingleReaderParamCommand(devNo, 0x00, 0x84, delay);
        }

        //设置用户标识（设备号）
        public static byte[] SetDeviceNumberCommand(byte devNo, byte number)
        {
            byte[] cmd = { 0xB0, 0x06, 0x60, devNo, 0x00, 0x64, number, 0x00 };
            var len = cmd.Length;
            cmd[len - 1] = CheckUtils.SumCheck(cmd, 0, len - 1);//和校验
            return cmd;
        }

        //设置传输功率
        public static byte[] SetTransmitPowerCommand(byte devNo, byte power)
        {
            byte[] cmd = { 0xB0, 0x06, 0x60, devNo, 0x00, 0x65, power, 0x00 };
            var len = cmd.Length;
            cmd[len - 1] = CheckUtils.SumCheck(cmd, 0, len - 1);//和校验
            return cmd;
        }

        //设置天线号(0：不选任何天线工作, 1：天线1工作, 2：天线2工作, 4：天线3工作, 8：天线4工作, 15：全部天线都工作)
        public static byte[] SetAntennaCommand(byte devNo, byte antenna)
        {
            byte[] cmd = { 0xB0, 0x06, 0x60, devNo, 0x00, 0x8A, antenna, 0x00 };
            var len = cmd.Length;
            cmd[len - 1] = CheckUtils.SumCheck(cmd, 0, len - 1);//和校验
            return cmd;
        }

        //设置读卡类型
        public static byte[] SetReadTagTypeCommand(byte devNo, byte type)
        {
            byte[] cmd = { 0xB0, 0x06, 0x60, devNo, 0x00, 0x87, type, 0x00 };
            var len = cmd.Length;
            cmd[len - 1] = CheckUtils.SumCheck(cmd, 0, len - 1);//和校验
            return cmd;
        }

        //韦根参数设置
        public static byte[] SetWiegandParamsCommand(byte devNo, byte[] data)
        {
            return SetMultiReaderParamsCommand(devNo, 0x00, 0x73, data);
        }

        //跳频设置命令
        public static byte[] SetHoppingCommand(byte devNo, byte frequency)
        {
            return SetSingleReaderParamCommand(devNo, 0x00, 0x90, frequency);
        }

        //频段设置命令
        public static byte[] SetBandsCommand(byte devNo, byte[] bands)
        {
            return SetMultiReaderParamsCommand(devNo, 0x00, 0x92, bands);
        }

        //蜂鸣器设置命令
        public static byte[] SetBuzzerCommand(byte devNo, byte buzzerCtrl)
        {
            byte[] cmd = { 0xB0, 0x04, 0xB0, devNo, buzzerCtrl, 0x00 };
            var len = cmd.Length;
            cmd[len - 1] = CheckUtils.SumCheck(cmd, 0, len - 1);//和校验
            return cmd;
        }

        //继电器设置命令
        public static byte[] SetRelaysCommand(byte devNo, byte status)
        {
            byte[] cmd = { 0xB0, 0x04, 0xB1, devNo, status, 0x00 };
            var len = cmd.Length;
            cmd[len - 1] = CheckUtils.SumCheck(cmd, 0, len - 1);//和校验
            return cmd;
        }

        //快写标签
        public static byte[] QuickWriteTagCommand(byte devNo, byte[] data)
        {
            var wordLength = data.Length/2; //1word = 2byte
            var cmd = new byte[6 + data.Length];
            cmd[0] = 0xB0;
            cmd[1] = (byte)(4 + 2 * wordLength);
            cmd[2] = 0x9C;
            cmd[3] = devNo;
            cmd[4] = (byte)(data.Length/2);
            for (var i = 0; i < data.Length; i++)
            {
                cmd[i + 5] = data[i];
            }
            var len = cmd.Length;
            cmd[len - 1] = CheckUtils.SumCheck(cmd, 0, len - 1);//和校验
            return cmd;
        }

        /// <summary>
        /// 读取标签
        /// </summary>
        /// <param name="devNo">设备号</param>
        /// <param name="memBank">区域</param>
        /// <param name="address">地址</param>
        /// <param name="length">长度</param>
        /// <returns>读取失败从机回： E4 04 80 00 05 93，读取成功从机回： E0 08 80 01 02 01 12 34 4E（E0 读取成功数据帧头,08 数据长度,80 标签读取命令,usercode 设备号,01 Membank类型,02 地址, 01 读取长度,12 34 所读取的数据,4E Checksum）</returns>
        public static byte[] ReadEpcTag(byte devNo, byte memBank, byte address, byte length)
        {
            byte[] cmd = { 0xB0, 0x06, 0x80, devNo, memBank, address, length, 0x00 };
            var len = cmd.Length;
            cmd[len - 1] = CheckUtils.SumCheck(cmd, 0, len - 1);//和校验
            return cmd;
        }

        /// <summary>
        /// 写入标签
        /// </summary>
        /// <param name="devNo">设备号</param>
        /// <param name="memBank">区域</param>
        /// <param name="address">地址</param>
        /// <param name="length">长度</param>
        /// <param name="data1">数据1</param>
        /// <param name="data2">数据2</param>
        /// <returns>写入失败回：（ E0 04 81 00 05 96）,写入成功回：（ E0 04 81 00 00 9B）</returns>
        public static byte[] WriteTagSingleWord(byte devNo, byte memBank, byte address, byte length, byte data1, byte data2)
        {
            byte[] cmd = { 0xB0, 0x09, 0x81, devNo, 0x00, memBank, address, length, data1, data2, 0x00 };
            var len = cmd.Length;
            cmd[len - 1] = CheckUtils.SumCheck(cmd, 0, len - 1);//和校验
            return cmd;
        }

        public static byte[] WriteTagMultiWord(byte devNo, byte memBank, byte address, byte length, byte[] data)
        {
            var cmd = new byte[9 + length * 2];
            cmd[0] = 0xB0;
            cmd[1] = (byte)(7 + length * 2);
            cmd[2] = 0x81;
            cmd[3] = devNo;
            cmd[4] = 0x01;//多个字节写入
            cmd[5] = memBank;
            cmd[6] = address;
            cmd[7] = length;
            for (var i = 0; i < length*2; i ++)
            {
                cmd[8 + i] = data[i];
            }
            var len = cmd.Length;
            cmd[len - 1] = CheckUtils.SumCheck(cmd, 0, len - 1);//和校验
            return cmd;
        }

        //初始化EPC标签(从机回：E4	04 99 usercode Status Checksum; Status=00：写入成功；Status=其它值：写入失败；)
        public static byte[] InitilizeTagCommand(byte devNo)
        {
            byte[] cmd = { 0xB0, 0x03, 0x99, devNo, 0x00 };
            var len = cmd.Length;
            cmd[len - 1] = CheckUtils.SumCheck(cmd, 0, len - 1);
            return cmd;
        }

        //锁定标签(从机回： E4	04 A5 （00）usercode	（00）Status	（73）Checksum; Status = 00：写入成功；Status =  其它值：写入失败；)
        public static byte[] LockTagCommand(byte devNo, byte lockType, byte[] password)
        {
            var cmd = new byte[6 + password.Length];
            cmd[0] = 0xB0;
            cmd[1] = 0x08;
            cmd[2] = 0xA5;
            cmd[3] = devNo;
            for (var i = 0; i < password.Length; i++)
            {
                cmd[i + 4] = password[i];
            }
            var len = cmd.Length;
            cmd[len - 2] = lockType;
            cmd[len - 1] = CheckUtils.SumCheck(cmd, 0, len - 1);//和校验
            return cmd;
        }

        //解锁标签(从机回： E4	04 A6 (00)usercode (00)Status (72)Checksum;Status = 00：写入成功；Status =  其它值：写入失败)
        public static byte[] UnlockTagCommand(byte devNo, byte unlockType, byte[] password)
        {
            var cmd = new byte[6 + password.Length];
            cmd[0] = 0xB0;
            cmd[1] = 0x08;
            cmd[2] = 0xA6;
            cmd[3] = devNo;
            for (var i = 0; i < password.Length; i++)
            {
                cmd[i + 4] = password[i];
            }
            var len = cmd.Length;
            cmd[len - 2] = unlockType;
            cmd[len - 1] = CheckUtils.SumCheck(cmd, 0, len - 1);//和校验
            return cmd;
        }

        //销毁标签(从机回：E4 04 86 usercode Status Checksum;Status = 00：写入成功,Status =  其它值：写入失败)
        public static byte[] KillTagCommand(byte devNo, byte[] password)
        {
            var cmd = new byte[10];
            cmd[0] = 0xB0;
            cmd[1] = 0x08;
            cmd[2] = 0x86;
            cmd[3] = devNo;
            cmd[4] = 0x00;
            for (var i = 0; i < password.Length; i++)
            {
                cmd[i + 5] = password[i];
            }
            var len = cmd.Length;
            cmd[len - 1] = CheckUtils.SumCheck(cmd, 0, len - 1);//和校验
            return cmd;
        }

        //复位读写器命令(从机回：E4	04 65 usercode Status Checksum; Status=00  成功, Status=其它值：失败)
        public static byte[] ResetReaderCommand(byte devNo)
        {
            byte[] cmd = { 0xB0, 0x03, 0x65, devNo, 0x00 };
            var len = cmd.Length;
            cmd[len - 1] = CheckUtils.SumCheck(cmd, 0, len - 1);//和校验
            return cmd;
        }

    }
}
