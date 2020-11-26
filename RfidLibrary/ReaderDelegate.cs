namespace RfidLibrary
{
    /// <summary>
    /// 读卡器发送命令监听事件
    /// </summary>
    /// <param name="command">命令数组</param>
    public delegate void ReaderCommandListenerHandle(byte[] command);

    /// <summary>
    /// 获取操作结果委托
    /// </summary>
    /// <param name="result">操作结果</param>
    public delegate void GetOperateResultHandler(ReceivedValueType resultType, object result);

    /// <summary>
    /// 搜索设备委托
    /// </summary>
    /// <param name="device">在线设备</param>
    public delegate void HandleDevice(Device device);

}
