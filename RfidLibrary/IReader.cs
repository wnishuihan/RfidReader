namespace RfidLibrary
{
    public interface IReader
    {
        /// <summary>
        /// 获取当前IReader对象的类型
        /// </summary>
        /// <returns>enum，SerialPort--当前连接为串口，Socket--当前连接为网口</returns>
        ReaderType GetReadType();

        event ReaderCommandListenerHandle CommandListener;

        /// <summary>
        /// 串口是否打开或网络是否连接
        /// </summary>
        /// <returns>true or false</returns>
        bool IsOpenOrConnection();

        /// <summary>
        /// 关闭串口或断开连接
        /// </summary>
        /// <returns>true or false</returns>
        bool DisConnection();

        /// <summary>
        /// 获取硬件版本号
        /// </summary>
        /// <param name="deviceNo">设备号</param>
        void GetReadFirmwareVersion(int deviceNo);

        /// <summary>
        /// 单卡读取
        /// </summary>
        /// <param name="deviceNo">设备号</param>
        void IdentifySingleTag(int deviceNo);

        /// <summary>
        /// 多卡读取
        /// </summary>
        /// <param name="deviceNo">设备号</param>
        void IdentifyMultiTag(int deviceNo);

        /// <summary>
        /// 停止读卡
        /// </summary>
        /// <param name="deviceNo">设备号</param>
        void StopReadingTag(int deviceNo);



        /// <summary>
        /// 获取通信接口类型
        /// </summary>
        /// <param name="deviceNo"></param>
        void GetCommunicationType(int deviceNo);

        /// <summary>
        /// 获取波特率
        /// </summary>
        /// <param name="deviceNo"></param>
        void GetBaudRate(int deviceNo);

        /// <summary>
        /// 获取工作模式
        /// </summary>
        /// <param name="deviceNo"></param>
        void GetWorkMode(int deviceNo);

        /// <summary>
        /// 设置工作模式
        /// </summary>
        /// <param name="deviceNo"></param>
        /// <param name="mode">工作模式（1--主从模式，2--定时模式， 3--触发模式）</param>
        void SetWorkMode(int deviceNo, byte mode);

        /// <summary>
        /// 获取读卡时间间隔
        /// </summary>
        /// <param name="deviceNo"></param>
        void GetReadTagTimeInterval(int deviceNo);

        /// <summary>
        /// 设置读卡时间间隔
        /// </summary>
        /// <param name="deviceNo"></param>
        /// <param name="interval">读卡时间间隔</param>
        void SetReadTagTimeInterval(int deviceNo, byte interval);

        /// <summary>
        /// 获取相邻判别时间
        /// </summary>
        /// <param name="deviceNo"></param>
        void GetAdjacentDiscriminantTime(int deviceNo);

        /// <summary>
        /// 设置相邻判别时间
        /// </summary>
        /// <param name="deviceNo"></param>
        /// <param name="time">相邻判别时间</param>
        void SetAdjacentDiscriminantTime(int deviceNo, byte time);

        /// <summary>
        /// 获取相邻判别
        /// </summary>
        /// <param name="deviceNo"></param>
        void GetAdjacentDiscriminant(int deviceNo);

        /// <summary>
        /// 设置相邻判别
        /// </summary>
        /// <param name="deviceNo"></param>
        /// <param name="adjacentDiscriminant"></param>
        void SetAdjacentDiscriminant(int deviceNo, byte adjacentDiscriminant);

        /// <summary>
        /// 获取触发开关(0--触发关,1--触发开)
        /// </summary>
        /// <param name="deviceNo"></param>
        void GetTriggerSwitch(int deviceNo);

        /// <summary>
        /// 设置触发开关
        /// </summary>
        /// <param name="deviceNo"></param>
        /// <param name="triggerSwitch">触发开关(0--触发关,1--触发开)</param>
        void SetTriggerSwitch(int deviceNo, byte triggerSwitch);

        /// <summary>
        /// 获取延迟时间(0--240)
        /// </summary>
        /// <param name="deviceNo"></param>
        void GetTriggerDelay(int deviceNo);

        /// <summary>
        /// 设置延迟时间
        /// </summary>
        /// <param name="deviceNo"></param>
        /// <param name="delay">延迟时间(0--240)</param>
        void SetTriggerDelay(int deviceNo, byte delay);

        /// <summary>
        /// 获取设备地址
        /// </summary>
        /// <param name="deviceNo"></param>
        void GetDeviceNumber(int deviceNo);

        /// <summary>
        /// 设置设备地址
        /// </summary>
        /// <param name="deviceNo"></param>
        /// <param name="number">设备地址</param>
        void SetDeviceNumber(int deviceNo, byte number);

        /// <summary>
        /// 获取发射功率
        /// </summary>
        /// <param name="deviceNo"></param>
        void GetTransmitPower(int deviceNo);

        /// <summary>
        /// 设置发射功率
        /// </summary>
        /// <param name="deviceNo"></param>
        /// <param name="power">发射功率</param>
        void SetTransmitPower(int deviceNo, byte power);

        /// <summary>
        /// 获取天线号
        /// </summary>
        /// <param name="deviceNo"></param>
        void GetAntenna(int deviceNo);

        /// <summary>
        /// 设置天线号
        /// </summary>
        /// <param name="deviceNo"></param>
        /// <param name="antenna"></param>
        void SetAntenna(int deviceNo, byte antenna);

        /// <summary>
        /// 获取读卡类型（单标签/多标签）
        /// </summary>
        /// <param name="deviceNo"></param>
        void GetReadTagType(int deviceNo);

        /// <summary>
        /// 设置读卡类型
        /// </summary>
        /// <param name="deviceNo">设备号</param>
        /// <param name="type">读卡类型(0：单标签，1：多标签)</param>
        void SetReadTagType(int deviceNo, byte type);

        /// <summary>
        /// 设置通信接口类型
        /// </summary>
        /// <param name="deviceNo">设备号</param>
        /// <param name="type">通信接口类型代码。1-RS485,2-Wiegand, 3-RS232</param>
        void SetCommunicationType(int deviceNo, byte type);

        /// <summary>
        /// 设置波特率
        /// </summary>
        /// <param name="deviceNo">设备号</param>
        /// <param name="baudRate">波特率代码。0x00-9600bps, 0x01-19200bps, 0x02-38400bps, 0x03-57600bps, 0x04-115200bps</param>
        void SetBaudRate(int deviceNo, byte baudRate);

        /// <summary>
        /// 获取韦根参数
        /// </summary>
        /// <param name="deviceNo">设备号</param>
        void GetWiegandParams(int deviceNo);

        /// <summary>
        /// 设置韦根参数
        /// </summary>
        /// <param name="deviceNo">设备号</param>
        /// <param name="type">韦根类型(1：wiegand26,2：wiegand34,3：wiegand32)</param>
        /// <param name="width">脉冲宽度</param>
        /// <param name="period">脉冲周期</param>
        void SetWiegandParams(int deviceNo, byte type, byte width, byte period);

        /// <summary>
        /// 获取跳频设置(0：跳频，1-50：固定频率工作方式 频率值由此数值决定)
        /// </summary>
        /// <param name="deviceNo"></param>
        void GetHopping(int deviceNo);

        /// <summary>
        /// 跳频设置
        /// </summary>
        /// <param name="deviceNo">设备号</param>
        /// <param name="frequency">频率(0：跳频，1-50：固定频率工作方式 频率值由此数值决定)</param>
        void SetHopping(int deviceNo, byte frequency);

        /// <summary>
        /// 获取频点
        /// </summary>
        /// <param name="deviceNo"></param>
        void GetBands(int deviceNo);

        /// <summary>
        /// 设置频点
        /// </summary>
        /// <param name="deviceNo">设备号</param>
        /// <param name="bands">频段点</param>
        void SetBands(int deviceNo, byte[] bands);

        /// <summary>
        /// 蜂鸣器设置
        /// </summary>
        /// <param name="deviceNo">设备号</param>
        /// <param name="buzzerCtrl">蜂鸣器声音(0：关闭；1:连续BB声；>=2: 只响一次BEEP声)</param>
        void SetBuzzer(int deviceNo, byte buzzerCtrl);

        /// <summary>
        /// 设置继电器开关
        /// </summary>
        /// <param name="deviceNo">设备号</param>
        /// <param name="relayOnOff">继电器开关(0:关闭，1:打开)</param>
        void SetRelays(int deviceNo, byte relayOnOff);

        /// <summary>
        /// 快写标签
        /// </summary>
        /// <param name="deviceNo">设备号</param>
        /// <param name="data">数据</param>
        void QuickWriteTag(int deviceNo, byte[] data);

        /// <summary>
        /// 读取标签
        /// </summary>
        /// <param name="deviceNo">设备号</param>
        /// <param name="area">要读取的区号</param>
        /// <param name="address">地址</param>
        /// <param name="length">长度</param>
        void ReadTag(int deviceNo, byte area, byte address, byte length);

        /// <summary>
        /// 标签单字节写入
        /// </summary>
        /// <param name="device">设备号</param>
        /// <param name="area">要写入的区号</param>
        /// <param name="address">地址</param>
        /// <param name="data">要写入的数据</param>
        void WriteTagSingleWord(int device, byte area, byte address, byte[] data);

        /// <summary>
        /// 标签多字节写入
        /// </summary>
        /// <param name="device">设备号</param>
        /// <param name="area">区号</param>
        /// <param name="address">地址</param>
        /// <param name="length">长度</param>
        /// <param name="data">数据</param>
        void WriteTagMultiWords(int device, byte area, byte address, byte length, byte[] data);

        /// <summary>
        /// 初始化标签
        /// </summary>
        /// <param name="deviceNo"></param>
        void InitilizeTag(int deviceNo);

        /// <summary>
        /// 锁定标签
        /// </summary>
        /// <param name="deviceNo">设备号</param>
        /// <param name="lockType">锁定类型(00：USER; 01：TID; 02：EPC; 03：ACCESS; 04：KILL; 05：ALL; 其它值：不锁定)</param>
        /// <param name="password">密码</param>
        void LockTag(int deviceNo, byte lockType, byte[] password);

        /// <summary>
        /// 解锁标签
        /// </summary>
        /// <param name="deviceNo">设备号</param>
        /// <param name="unlockType">解锁类型(00：USER; 01：TID; 02：EPC; 03：ACCESS; 04：KILL; 05：ALL; 其它值：不解锁)</param>
        /// <param name="password">密码</param>
        void UnlockTag(int deviceNo, byte unlockType, byte[] password);

        /// <summary>
        /// 销毁标签
        /// </summary>
        /// <param name="deviceNo">设备号</param>
        /// <param name="password">密码</param>
        void KillTag(int deviceNo, byte[] password);

        /// <summary>
        /// 设置单个参数
        /// </summary>
        /// <param name="type">操作类型</param>
        /// <param name="deviceNo">设备号</param>
        /// <param name="msb">高位地址</param>
        /// <param name="lsb">低位地址</param>
        /// <param name="data">参数</param>
        void SetSingleParams(ReceivedValueType type, int deviceNo, byte msb, byte lsb, byte data);

        /// <summary>
        /// 设置多个参数
        /// </summary>
        /// <param name="type">操作类型</param>
        /// <param name="deviceNo">设备号</param>
        /// <param name="msb">高位地址</param>
        /// <param name="lsb">低位地址</param>
        /// <param name="data">参数</param>
        /// <param>从机回：E4 04 62 （00）usercode（00）Status，（B6）Checksum; Status00： 成功； 其它值：失败</param>
        void SetMultiParams(ReceivedValueType type, int deviceNo, byte msb, byte lsb, byte[] data);

        /// <summary>
        /// 复位读写器
        /// </summary>
        /// <param name="deviceNo"></param>
        void ResetReader(int deviceNo);

        /// <summary>
        /// 注册数据接收事件
        /// </summary>
        /// <param name="handlerGetOperatreResult">数据接收委托</param>
        void RegisterOperateResultDataReceivedEvent(GetOperateResultHandler handlerGetOperatreResult);

        /// <summary>
        /// 清除数据接收事件
        /// </summary>
        void ClearDataReceivedEvent();
    }

    public enum ReaderType
    {
        SerialPort,
        Socket
    }

}
