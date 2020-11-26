using DevExpress.Mvvm;
using System;
using RfidLibrary;
using System.Windows.Threading;
using System.Threading;
using System.Windows;
using RfidReader.Model;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;
using System.Net.Sockets;
using DevExpress.Xpf.Bars;

namespace RfidReader.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private IReader _reader;
        private int _no;//读卡编号
        //private int _count;//读卡次数
        private DispatcherTimer _timer;
        private bool _isOpen;
        //private bool _isIpPortSelect;

        private bool _isWorkModeParamsGet;
        private bool _isReaderParamsGet;

        private IMessageBoxService MessageBoxService => GetService<IMessageBoxService>();

        public ISplashScreenService SplashScreenService => GetService<ISplashScreenService>();

        public MainViewModel()
        {
            Initilized();
            Dispatcher = Dispatcher.CurrentDispatcher;
            _tagRecordsName = GetPropertyName(() => new MainViewModel().TagRecords);

            Messenger.Default.Register<Message>(this, messag =>
            {
                ChangeUiLanguage();
            });
        }

        private Dispatcher Dispatcher { get; }

        private void Initilized()
        {
            IsRefreshEnabled = true;
            GetAvailableSerialPort();
            if (AvailableSerialPorts.Length > 0)
            {
                SelectedSerialPortIndex = 0;
                IsOpenSerialPortEnabled = true;
            }

            IsReaderAddressEnabled = true;
            IsBaudrateEnabled = true;

            IsSerialPortEnabled = true;
            IsNetworkEnabled = true;
            IsIpAddressEnabled = true;
            IsPortEnabled = true;
            IsSearchButtonEnabled = true;
            IsConnectionButtonEnabled = true;
            IsDisConnectionButtonEnabled = false;
            IsSingleTagIdentify = true;
            AutoSave = false;
            Chinese = true;

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
            _timer.Tick += timer_Tick;
            _tagRecords = new ObservableCollection<TagRecord>();
            BaudRateList = new[] { "9600bps", "19200bps", "38400bps", "57600bps", "115200bps", "2400bps" };
            BaudRate = 0;

            ReaderAddressInts = new int[255];
            DeviceNumberCollection = new GalleryCollection<int>();
            for (var i = 0; i < 255; i++)
            {
                ReaderAddressInts[i] = i;
                DeviceNumberCollection.Add(i);
            }
            ReaderAddress = 0;

            RwTagArea = 0;//保留区
            RwTagAddressRange = new[] { 0, 1, 2, 3 };
            RwTagAddress = -1;
            RwTagLength = -1;

            CommunicationInterfaceType = -1;
            TransmitPower = "130";

            Buzzer = -1;
            Relays = -1;

            SearchDevices = new ObservableCollection<Device>();
            InitSetParams();
        }

        public int PageViewSelectedIndex
        {
            get { return GetProperty(() => PageViewSelectedIndex); }
            set { SetProperty(() => PageViewSelectedIndex, value); }
        }

        public void PageViewSelectedItemChanged()
        {
            if (_reader == null || !_reader.IsOpenOrConnection())
            {
                return;
            }

            switch (PageViewSelectedIndex)
            {
                case 2:
                    if (!_isWorkModeParamsGet)
                    {
                        Task.Factory.StartNew(GetWorkModePageParams);
                        _isWorkModeParamsGet = true;
                    }
                    break;
                case 3:
                    if (!_isReaderParamsGet)
                    {
                        Task.Factory.StartNew(GetReaderParameterPageParams);
                        _isReaderParamsGet = true;
                    }
                    break;
            }
        }

        /*
         * +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
         * 连接方式
         * +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
         */
        #region 
        //串口连接是否可用
        public bool IsSerialPortEnabled
        {
            get { return GetProperty(() => IsSerialPortEnabled); }
            set { SetProperty(() => IsSerialPortEnabled, value); }
        }

        //网口连接是否可用
        public bool IsNetworkEnabled
        {
            get { return GetProperty(() => IsNetworkEnabled); }
            set { SetProperty(() => IsNetworkEnabled, value); }
        }

        //刷新按钮是否可用
        public bool IsRefreshEnabled
        {
            get { return GetProperty(() => IsRefreshEnabled); }
            set { SetProperty(() => IsRefreshEnabled, value); }
        }

        //打开串口按钮是否可用
        public bool IsOpenSerialPortEnabled
        {
            get { return GetProperty(() => IsOpenSerialPortEnabled); }
            set { SetProperty(() => IsOpenSerialPortEnabled, value); }
        }

        //关闭串口按钮是否可用
        public bool IsClosedSerialPortEnabled
        {
            get { return GetProperty(() => IsClosedSerialPortEnabled); }
            set { SetProperty(() => IsClosedSerialPortEnabled, value); }
        }

        //设备号是否可用
        public bool IsReaderAddressEnabled
        {
            get { return GetProperty(() => IsReaderAddressEnabled); }
            set { SetProperty(() => IsReaderAddressEnabled, value); }
        }

        //波特率是否可用
        public bool IsBaudrateEnabled
        {
            get { return GetProperty(() => IsBaudrateEnabled); }
            set { SetProperty(() => IsBaudrateEnabled, value); }
        }

        //IP地址是否可用
        public bool IsIpAddressEnabled
        {
            get { return GetProperty(() => IsIpAddressEnabled); }
            set { SetProperty(() => IsIpAddressEnabled, value); }
        }

        //端口是否可用
        public bool IsPortEnabled
        {
            get { return GetProperty(() => IsPortEnabled); }
            set { SetProperty(() => IsPortEnabled, value); }
        }

        //搜索按钮是否可用
        public bool IsSearchButtonEnabled
        {
            get { return GetProperty(() => IsSearchButtonEnabled); }
            set { SetProperty(() => IsSearchButtonEnabled, value); }
        }

        //连接按钮是否可用
        public bool IsConnectionButtonEnabled
        {
            get { return GetProperty(() => IsSearchButtonEnabled); }
            set { SetProperty(() => IsConnectionButtonEnabled, value); }
        }

        //断开按钮是否可用
        public bool IsDisConnectionButtonEnabled
        {
            get { return GetProperty(() => IsDisConnectionButtonEnabled); }
            set { SetProperty(() => IsDisConnectionButtonEnabled, value); }
        }

        //当前可用串口
        public string[] AvailableSerialPorts
        {
            get { return GetProperty(() => AvailableSerialPorts); }
            set { SetProperty(() => AvailableSerialPorts, value); }
        }

        //当前串口
        public string SerialPortName
        {
            get { return GetProperty(() => SerialPortName); }
            set { SetProperty(() => SerialPortName, value); }
        }

        //当前串口索引
        public int SelectedSerialPortIndex
        {
            get { return GetProperty(() => SelectedSerialPortIndex); }
            set { SetProperty(() => SelectedSerialPortIndex, value); }
        }

        //读头地址
        public int ReaderAddress
        {
            get { return GetProperty(() => ReaderAddress); }
            set { SetProperty(() => ReaderAddress, value); }
        }

        public int[] ReaderAddressInts
        {
            get { return GetProperty(() => ReaderAddressInts); }
            set { SetProperty(() => ReaderAddressInts, value); }
        }

        //波特率
        public int BaudRate
        {
            get { return GetProperty(() => BaudRate); }
            set { SetProperty(() => BaudRate, value); }
        }

        //IP地址
        public string IpAddress
        {
            get { return GetProperty(() => IpAddress); }
            set { SetProperty(() => IpAddress, value); }
        }

        //端口
        public int? Port
        {
            get { return GetProperty(() => Port); }
            set { SetProperty(() => Port, value); }
        }

        public string[] BaudRateList
        {
            get { return GetProperty(() => BaudRateList); }
            set { SetProperty(() => BaudRateList, value); }
        }

        public bool Chinese
        {
            get { return GetProperty(() => Chinese); }
            set { SetProperty(() => Chinese, value); }
        }

        public bool English
        {
            get { return GetProperty(() => English); }
            set { SetProperty(() => English, value); }
        }

        /// <summary>
        /// 获取系统当前可用串口列表
        /// </summary>
        /// <returns>可用串口名称数组</returns>
        /// <exception ref="System.Exception">获取系统可用串口名称数组异常</exception>
        public void GetAvailableSerialPort()
        {
            try
            {
                var ports = System.IO.Ports.SerialPort.GetPortNames();
                Array.Sort(ports);
                AvailableSerialPorts = ports;
            }
            catch (Exception ex)
            {
                throw new Exception(GetString("StrQuerySerialPortException", "Exception occurs when the available serial acquisition system"), ex);
            }
        }

        private int _connectCount;
        private void timer_Tick(object sender, EventArgs e)
        {
            _connectCount++;
            if (_isOpen)
            {
                _timer.Stop();
                if (_reader.IsOpenOrConnection())
                {
                    var str1 = GetString("StrOpen", "Open ");
                    var str2 = GetString("StrSuccess", " success");
                    var str3 = GetString("StrQueryParams", "Querying the reader parameters...");
                    StatusTipInfo = $"{str1}{SerialPortName}{str2}，{str3}";

                    CloseLoading();

                    IsRefreshEnabled = false;
                    IsOpenSerialPortEnabled = false;
                    IsClosedSerialPortEnabled = true;
                    IsNetworkEnabled = false;
                    OpenOrConnectionOkDoThis();
                }
                else
                {
                    var str1 = GetString("StrOpen", "Open ");
                    var str2 = GetString("StrFail", " fail");
                    StatusTipInfo = $"{str1}{SerialPortName}{str2}";
                    CloseLoading();
                }
            }
            else
            {
                if (_connectCount <= 8) return;
                _timer.Stop();
                var str1 = GetString("StrOpen", "Open ");
                var str2 = GetString("StrFail", " fail");
                StatusTipInfo = $"{str1}{SerialPortName}{str2}";
                CloseLoading();

                if (_reader == null || !_reader.IsOpenOrConnection()) return;

                _reader.DisConnection();
                _reader = null;
            }
        }

        /// <summary>
        /// 打开串口
        /// </summary>
        public void OpenSerialPort()
        {
            try
            {
                //ShowLoading("请稍后...", "正在打开串口...");
                ShowLoading(null, null);
                _reader = null;
                var baudRate = Convert.ToInt32(BaudRateList[BaudRate].Replace("bps", ""));
                _reader = new ProxySerialPort(SerialPortName, baudRate);
                ((ProxySerialPort)_reader).OpenSerialPort();

                _reader.ClearDataReceivedEvent();
                _reader.RegisterOperateResultDataReceivedEvent(ReceivedOperateResponse); //注册监听
                StopReadTag();
                IsLiveDevicePageContentEnabled = false;//在线设备不可用
                Thread.Sleep(400);
                GetVersionInfo();
                _timer.Start();
            }
            catch (Exception ex)
            {
                CloseLoading();
                StatusTipInfo = $"{ex.Message}, {ex.InnerException?.Message ?? string.Empty}";
            }
        }

        /// <summary>
        /// 通过网络连接
        /// </summary>
        public void Connection()
        {
            if (Port == null)
            {
                StatusTipInfo = GetString("StrPortCanNotEmpty", "Ports can not be empty");
                return;
            }
            ShowLoading(null, null);
            StatusTipInfo = GetString("StrConnectingTarget", "The connection target apparatus ...");
            try
            {
                _reader = new ProxySocket(IpAddress, (int)Port, ConnectionCallback);
            }
            catch (Exception ex)
            {
                StatusTipInfo = ex.Message;
            }
        }

        private void ConnectionCallback(IAsyncResult ar)
        {
            try
            {
                var s = (Socket)ar.AsyncState;

                if (_reader != null && _reader.IsOpenOrConnection())
                {
                    IsReaderAddressEnabled = false;
                    IsBaudrateEnabled = false;
                    IsSerialPortEnabled = false;
                    IsIpAddressEnabled = false;
                    IsPortEnabled = false;
                    IsSearchButtonEnabled = false;
                    IsConnectionButtonEnabled = false;
                    IsDisConnectionButtonEnabled = true;

                    StatusTipInfo = GetString("StrConnectionOkAndGetVersion", "The connection is successful, the hardware version Getting information ...");//"连接成功，正在获取硬件版本信息...";
                    _reader.RegisterOperateResultDataReceivedEvent(ReceivedOperateResponse);
                    _reader.StopReadingTag(ReaderAddress);
                    Thread.Sleep(100);
                    _reader.GetReadFirmwareVersion(ReaderAddress);

                    OpenOrConnectionOkDoThis();
                    StatusTipInfo = GetString("StrConnectionSuccess", "Connection succeeded"); //"连接成功";
                }
                else
                {
                    StatusTipInfo = GetString("StrConnectionFail", "Connection failed");//"连接失败";

                    IsReaderAddressEnabled = true;
                    IsSerialPortEnabled = true;
                    IsIpAddressEnabled = true;
                    IsPortEnabled = true;
                    IsSearchButtonEnabled = true;
                    IsConnectionButtonEnabled = true;
                    IsDisConnectionButtonEnabled = false;
                }

                s.EndConnect(ar);
            }
            catch (SocketException se)
            {
                StatusTipInfo = se.Message;
                ClosedOrDisconnectionOkDoThis();
            }
            catch (Exception ex)
            {
                if (_reader == null || !_reader.IsOpenOrConnection())
                {

                }
                StatusTipInfo = ex.Message;
                ClosedOrDisconnectionOkDoThis();
            }
            CloseLoading();
        }

        /// <summary>
        /// 关闭串口
        /// </summary>
        public void ClosedSerialPort()
        {
            try
            {
                _reader.ResetReader(ReaderAddress);
                _reader.DisConnection();
                _reader = null;
                var str1 = GetString("StrClose", "Close ");
                var str2 = GetString("StrSuccess", " success");
                StatusTipInfo = $"{str1}{SerialPortName}{str2}";
                ClosedOrDisconnectionOkDoThis();
            }
            catch (Exception ex)
            {
                StatusTipInfo = ex.Message;
            }
        }

        /// <summary>
        /// 断开网络连接
        /// </summary>
        public void DisConnection()
        {
            CloseLoading();
            if (_reader != null)
            {
                _reader.ResetReader(ReaderAddress);
                _reader.DisConnection();
                StatusTipInfo = GetString("StrDisConnection", "Disconnect");//"断开连接";
            }

            ClosedOrDisconnectionOkDoThis();
        }

        //刷新串口
        public void RefreshSerialPort()
        {
            GetAvailableSerialPort();
            if (AvailableSerialPorts.Length <= 0) return;
            IsOpenSerialPortEnabled = true;
            //RaisePropertyChanged(()=>IsOpenSerialPortEnabled);
        }

        //搜索设备
        public void SearchDevice()
        {
            try
            {
                SearchDevices.Clear();
                IsSetDeviceParamsEnabled = false;
                StatusTipInfo = "";
                _deviceCount = 0;
                InitSetParams();
                ProxySocket.SearchingDevices(SearchDevicesCallback);
            }
            catch (Exception ex)
            {
                StatusTipInfo = ex.Message;
            }
        }

        private int _deviceCount;
        private void SearchDevicesCallback(Device device)
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(delegate
            {
                try
                {
                    //var baudrate = Convert.ToInt32(BaudRateList[BaudRate].Replace("bps", ""));
                    //if (device.BuadRate != baudrate)
                    //{
                    //    ProxySocket.SetDeviceWorkMode(device, 3, baudrate);//如果不是TCP客户端模式，设置为TCP客户端模式
                    //}
                    _deviceCount++;
                    device.Id = _deviceCount;
                    SearchDevices.Add(device);
                    //IpCollection.Add(device.IpAddress);
                    //PortCollection.Add(device.Port);
                    StatusTipInfo = $"{GetString("StrSearchTo", "Search to devices")} {device.IpAddress}:{device.Port}";

                    //if (_isIpPortSelect) return;
                    //IpAddress = device.IpAddress;
                    //Port = device.Port;
                    //_isIpPortSelect = true;
                }
                catch (Exception ex)
                {
                    StatusTipInfo = ex.Message;
                }
            }));
        }

        //转到搜索页
        public void GotoSearchPage()
        {
            PageViewSelectedIndex = 4;
            IsLiveDevicePageContentEnabled = true;
        }

        /// <summary>
        /// 获取版本信息
        /// </summary>
        private void GetVersionInfo()
        {
            try
            {
                _reader.GetReadFirmwareVersion(ReaderAddress);
            }
            catch (Exception ex)
            {
                StatusTipInfo = ex.Message;
            }
        }

        /// <summary>
        /// 停止读卡
        /// </summary>
        public void StopReadingTag()
        {
            _reader.StopReadingTag(ReaderAddress);
        }


        #endregion

        /*
         * +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
         * 标签识别
         * +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
         */
        #region
        //单卡读取Checked
        public bool IsSingleTagIdentify
        {
            get { return GetProperty(() => IsSingleTagIdentify); }
            set { SetProperty(() => IsSingleTagIdentify, value); }
        }

        //多卡读取Checked
        public bool IsMultiTagIdentify
        {
            get { return GetProperty(() => IsMultiTagIdentify); }
            set { SetProperty(() => IsMultiTagIdentify, value); }
        }

        //读卡按钮Enabled
        public bool IsReadTagButtonEnabled
        {
            get { return GetProperty(() => IsReadTagButtonEnabled); }
            set { SetProperty(() => IsReadTagButtonEnabled, value); }
        }

        //暂停读卡按钮Enabled
        public bool IsStopReadTagButtonEnabled
        {
            get { return GetProperty(() => IsStopReadTagButtonEnabled); }
            set { SetProperty(() => IsStopReadTagButtonEnabled, value); }
        }

        //自动保存
        public bool AutoSave
        {
            get { return GetProperty(() => AutoSave); }
            set { SetProperty(() => AutoSave, value, RaiseAutoSaveChecked); }
        }

        //读卡内容
        private string _tagRecordsName;
        private ObservableCollection<TagRecord> _tagRecords;
        public ObservableCollection<TagRecord> TagRecords
        {
            get { return _tagRecords; }
            set { SetProperty(ref _tagRecords, value, _tagRecordsName); }
        }

        /// <summary>
        /// 标签读取
        /// </summary>
        public void IdentifyTag()
        {
            try
            {
                Console.WriteLine("Identify tag event was raised!");
                if (IsSingleTagIdentify)
                {
                    _reader.IdentifySingleTag(ReaderAddress);
                    StatusTipInfo = GetString("StrSingleReading", "Singer tag reader");
                }
                else
                {
                    //_count = 0;
                    StatusTipInfo = GetString("StrBeginReader", "Beginning readers");
                    IsStopReadTagButtonEnabled = true;
                    IsReadTagButtonEnabled = false;

                    _reader.IdentifyMultiTag(ReaderAddress);
                }
            }
            catch (Exception ex)
            {
                var info = GetString("StrReaderAbnormal", "Reader abnormal");
                StatusTipInfo = $"{info}，{ex.Message}";
            }
        }

        /// <summary>
        /// 暂停读卡
        /// </summary>
        public void StopReadTag()
        {
            try
            {
                _reader.StopReadingTag(ReaderAddress);
                StatusTipInfo = GetString("StrStopSuccess", "Stop reading the tag success");

                IsStopReadTagButtonEnabled = false;
                IsReadTagButtonEnabled = true;
            }
            catch (Exception ex)
            {
                var info = GetString("StrStopFail", "Stop reading labels fail");
                StatusTipInfo = $"{info}！{ex.Message}";
            }
        }

        /// <summary>
        /// 清除读卡信息
        /// </summary>
        public void ClearTagRecords()
        {
            _tagRecords.Clear();
            //_count = 0;
            _no = 0;
            StatusTipInfo = GetString("StrClearTagRecords", "Clear reader records"); //"清除读卡记录";
        }

        /// <summary>
        /// 保存读卡内容
        /// </summary>
        private void SaveTagContentToLocal()
        {
            if (!AutoSave) return;

            var filePath = AppDomain.CurrentDomain.BaseDirectory + "BCBTag.txt";

            try
            {
                if (_tagRecords.Count < 1)
                {
                    return;
                }

                var tag = new string[_tagRecords.Count];
                for (var i = 0; i < _tagRecords.Count; i++)
                {
                    tag[i] = _tagRecords[i].EpcContent;
                }
                File.WriteAllLines(filePath, tag);
                //using (var fs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
                //{
                //    var sw = new StreamWriter(fs);
                //    foreach (var t in _tagRecords)
                //    {
                //        sw.WriteLine(t.EpcContent);
                //    }
                //    sw.Flush();
                //    sw.Close();
                //    sw.Dispose(); //释放资源
                //}
            }
            catch (IOException ex)
            {
                //MessageBox.Show(ex.Message, GetString("msgError", "Error"), MessageBoxButtons.OK);
                StatusTipInfo = ex.Message;
            }
        }

        private void RaiseAutoSaveChecked()
        {
            SaveTagContentToLocal();//切换自动保存时保存一份
        }

        #endregion

        /*
         * +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
         * 标签操作
         * +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
         */
        #region
        /*
         * Control Status
         */

        public bool IsQuickWriteEnabled
        {
            get { return GetProperty(() => IsQuickWriteEnabled); }
            set { SetProperty(() => IsQuickWriteEnabled, value); }
        }

        public bool IsRwTagEnabled
        {
            get { return GetProperty(() => IsRwTagEnabled); }
            set { SetProperty(() => IsRwTagEnabled, value); }
        }

        public bool IsWriteTagButtonEnabled
        {
            get { return GetProperty(() => IsWriteTagButtonEnabled); }
            set { SetProperty(() => IsWriteTagButtonEnabled, value); }
        }

        public bool IsReadTagEnabled
        {
            get { return GetProperty(() => IsReadTagEnabled); }
            set { SetProperty(() => IsReadTagEnabled, value); }
        }

        public bool IsOtherOperateEnabled
        {
            get { return GetProperty(() => IsOtherOperateEnabled); }
            set { SetProperty(() => IsOtherOperateEnabled, value); }
        }

        /*
         * Propertys
         */
        //快写标签内容
        public string QuickWriteTagContent
        {
            get { return GetProperty(() => QuickWriteTagContent); }
            set { SetProperty(() => QuickWriteTagContent, value); }
        }

        //读写区号
        public int RwTagArea
        {
            get { return GetProperty(() => RwTagArea); }
            set { SetProperty(() => RwTagArea, value, RaiseReadTagAreaChanged); }
        }

        //读写地址
        public int RwTagAddress
        {
            get { return GetProperty(() => RwTagAddress); }
            set { SetProperty(() => RwTagAddress, value, RaiseAddressChanged); }
        }

        //读写地址范围
        public int[] RwTagAddressRange
        {
            set { SetProperty(() => RwTagAddressRange, value); }
            get { return GetProperty(() => RwTagAddressRange); }
        }

        //读写长度
        public int RwTagLength
        {
            get { return GetProperty(() => RwTagLength); }
            set { SetProperty(() => RwTagLength, value); }
        }

        //读写长度范围
        public int[] RwTagLengthRange
        {
            get { return GetProperty(() => RwTagLengthRange); }
            set { SetProperty(() => RwTagLengthRange, value); }
        }

        //读写标签内容
        public string RwTagContent
        {
            get { return GetProperty(() => RwTagContent); }
            set { SetProperty(() => RwTagContent, value); }
        }

        //锁卡权限
        public int LockTagPermission
        {
            get { return GetProperty(() => LockTagPermission); }
            set { SetProperty(() => LockTagPermission, value); }
        }

        //访问密码
        public string AccessPassword
        {
            get { return GetProperty(() => AccessPassword); }
            set { SetProperty(() => AccessPassword, value); }
        }

        //销毁密码
        public string KillPassword
        {
            get { return GetProperty(() => KillPassword); }
            set { SetProperty(() => KillPassword, value); }
        }

        /*
         * Command
         */

        //快写标签
        public void QuickWriteTag()
        {
            try
            {
                var r = Regex.IsMatch(QuickWriteTagContent, "^[0-9A-Fa-f\\s]+$");
                if (string.IsNullOrEmpty(QuickWriteTagContent))
                {
                    ShowMessage(GetString("QuickWriteTip", "Data must be 4,8,12 or 16 hexadecimal characters(0~9, A~F)"));
                    return;
                }
                var data = TextEncoder.HexStringToByteArray(QuickWriteTagContent);
                if (r && data.Length % 2 == 0)
                {
                    _reader.QuickWriteTag(ReaderAddress, data);
                }
                else
                {
                    StatusTipInfo = GetString("QuickWriteTip", "Data must be 4,8,12 or 16 hexadecimal characters(0~9, A~F)");
                }

            }
            catch (Exception ex)
            {
                StatusTipInfo = ex.Message;
            }
        }

        //读取标签
        public void ReadTag()
        {
            try
            {
                if (!CheckRwItem()) return;

                _reader.ReadTag(ReaderAddress, (byte)RwTagArea, (byte)RwTagAddress, (byte)RwTagLength);
            }
            catch (Exception ex)
            {
                StatusTipInfo = ex.Message;
            }
        }

        //写入标签
        public void WriteTag()
        {
            try
            {
                if (!CheckRwItem()) return;

                var r = Regex.IsMatch(RwTagContent, "^[0-9A-Fa-f\\s]+$");
                if (string.IsNullOrEmpty(RwTagContent))
                {
                    StatusTipInfo = GetString("WriteTagTip2", "Data must be hexadecimal characters(0~9, A~F)");
                    return;
                }

                var data = TextEncoder.HexStringToByteArray(RwTagContent);
                if (r && data.Length >= RwTagLength * 2)
                {
                    _reader.WriteTagMultiWords(ReaderAddress,
                        (byte)RwTagArea, (byte)RwTagAddress, (byte)RwTagLength, data);
                }
                else
                {
                    StatusTipInfo = GetString("WriteTagTip3", "Data length is not enough");
                }
            }
            catch (Exception ex)
            {
                StatusTipInfo = ex.Message;
            }
        }

        //初始化标签
        public void InitilizeTag()
        {
            try
            {
                _reader.InitilizeTag(ReaderAddress);
            }
            catch (Exception ex)
            {
                StatusTipInfo = ex.Message;
            }
        }

        //锁定标签
        public void LockTag()
        {
            try
            {
                if (string.IsNullOrEmpty(AccessPassword))
                {
                    ShowMessage(GetString("StrPwdIsNotNull", "Password can not be blank"));
                    return;
                }
                var pwd = TextEncoder.HexStringToByteArray(AccessPassword);
                if (pwd.Length != 4)
                {
                    ShowMessage(GetString("StrFormatFail", "Password malformed"));
                    return;
                }
                _reader.LockTag(ReaderAddress, (byte)LockTagPermission, pwd);
            }
            catch (Exception ex)
            {
                StatusTipInfo = ex.Message;
            }
        }

        //解锁标签
        public void UnLockTag()
        {
            try
            {
                if (string.IsNullOrEmpty(AccessPassword))
                {
                    ShowMessage(GetString("StrPwdIsNotNull", "Password can not be blank"));
                    return;
                }
                var pwd = TextEncoder.HexStringToByteArray(AccessPassword);
                if (pwd.Length != 4)
                {
                    ShowMessage(GetString("StrFormatFail", "Password malformed"));
                    return;
                }
                _reader.UnlockTag(ReaderAddress, (byte)LockTagPermission, pwd);
            }
            catch (Exception ex)
            {
                StatusTipInfo = ex.Message;
            }
        }

        //销毁标签
        public void KillTag()
        {
            try
            {
                if (string.IsNullOrEmpty(KillPassword))
                {
                    ShowMessage(GetString("StrPwdIsNotNull", "Password can not be blank"));
                    return;
                }
                var pwd = TextEncoder.HexStringToByteArray(KillPassword);
                if (pwd.Length != 4)
                {
                    ShowMessage(GetString("StrFormatFail", "Password malformed"));
                    return;
                }
                _reader.KillTag(ReaderAddress, pwd);
            }
            catch (Exception ex)
            {
                StatusTipInfo = ex.Message;
            }
        }

        private void RaiseReadTagAreaChanged()
        {
            switch (RwTagArea)
            {
                case 0://保留区
                    RwTagAddressRange = new[] { 0, 1, 2, 3 };
                    break;
                case 1://EPC区
                    RwTagAddressRange = new[] { 2, 3, 4, 5, 6, 7 };
                    break;
                case 2:
                    RwTagAddressRange = new[] { 0, 1, 2, 3, 4, 5 };
                    break;
                case 3:
                    var addressRange = new List<int>();
                    for (var i = 0; i < 32; i++)
                    {
                        addressRange.Add(i);
                    }
                    RwTagAddressRange = null;
                    RwTagAddressRange = addressRange.ToArray();
                    break;
            }
            if (!RwTagAddressRange.Any(r => r == RwTagAddress))
                RwTagAddress = -1;
            RwTagLengthRange = null;
            RaisePropertyChanged(() => RwTagAddress);

            RaiseAddressChanged();
        }

        private void RaiseAddressChanged()
        {
            var lenRange = new List<int>();
            switch (RwTagArea)
            {
                case 0:
                    for (var i = 1; i <= 4; i++)
                    {
                        if (RwTagAddress + i > 4) continue;
                        lenRange.Add(i);
                    }
                    break;
                case 1:
                    for (var i = 1; i <= 8; i++)
                    {
                        if (RwTagAddress + i > 8) continue;
                        lenRange.Add(i);
                    }
                    break;
                case 2:
                    for (var i = 1; i <= 6; i++)
                    {
                        if (RwTagAddress + i > 6) continue;
                        lenRange.Add(i);
                    }
                    break;
                case 3:
                    for (var i = 1; i <= 32; i++)
                    {
                        if (RwTagAddress + i > 32) continue;
                        lenRange.Add(i);
                    }
                    break;
            }
            RwTagLengthRange = lenRange.ToArray();
            if (!RwTagLengthRange.Any(r => r == RwTagLength))
            {
                RwTagLength = -1;
            }
            RaisePropertyChanged(() => RwTagLength);
        }

        //
        private bool CheckRwItem()
        {
            if (RwTagArea < 0)
            {
                StatusTipInfo = GetString("StrSelectArea", "Please select the area");//"请选择区号";
                return false;
            }
            if (RwTagAddress < 0)
            {
                StatusTipInfo = GetString("StrSelectAddress", "Please select the address");//"请选择地址";
                return false;
            }
            if (RwTagLength < 0)
            {
                StatusTipInfo = GetString("StrSelectLength", "Please select the length");//"请选择长度";
                return false;
            }
            return true;
        }

        #endregion

        /*
         * +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
         * 工作模式
         * +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
         */
        #region

        public bool IsWorkModePageDefaultParamButtonEnabled
        {
            get { return GetProperty(() => IsWorkModePageDefaultParamButtonEnabled); }
            set { SetProperty(() => IsWorkModePageDefaultParamButtonEnabled, value); }
        }

        public bool IsWorkModePageGetParamButtonEnabled
        {
            get { return GetProperty(() => IsWorkModePageGetParamButtonEnabled); }
            set { SetProperty(() => IsWorkModePageGetParamButtonEnabled, value); }
        }

        public bool IsWorkModePageSetParamButtonEnabled
        {
            get { return GetProperty(() => IsWorkModePageSetParamButtonEnabled); }
            set { SetProperty(() => IsWorkModePageSetParamButtonEnabled, value); }
        }

        public bool IsRs232ParamsEnabled
        {
            get { return GetProperty(() => IsRs232ParamsEnabled); }
            set { SetProperty(() => IsRs232ParamsEnabled, value); }
        }

        public bool IsRs485ParamsEnabled
        {
            get { return GetProperty(() => IsRs485ParamsEnabled); }
            set { SetProperty(() => IsRs485ParamsEnabled, value); }
        }

        //public bool IsAdjacentDiscriminantEnabled
        //{
        //    get { return GetProperty(() => IsAdjacentDiscriminantEnabled); }
        //    set { SetProperty(() => IsAdjacentDiscriminantEnabled, value); }
        //}

        public bool IsTimingParamEnabled
        {
            get { return GetProperty(() => IsTimingParamEnabled); }
            set { SetProperty(() => IsTimingParamEnabled, value); }
        }

        public bool IsTriggerModeEnabled
        {
            get { return GetProperty(() => IsTriggerModeEnabled); }
            set { SetProperty(() => IsTriggerModeEnabled, value); }
        }

        //主从模式
        public bool MasterSlaveMode
        {
            get { return GetProperty(() => MasterSlaveMode); }
            set { SetProperty(() => MasterSlaveMode, value); }
        }

        //定时模式
        public bool TimingMode
        {
            get { return GetProperty(() => TimingMode); }
            set { SetProperty(() => TimingMode, value, () => IsTimingParamEnabled = value); }
        }

        //触发模式
        public bool TriggerMode
        {
            get { return GetProperty(() => TriggerMode); }
            set { SetProperty(() => TriggerMode, value, () => IsTriggerModeEnabled = value); }
        }

        public bool IsWiegandParamsEnabled
        {
            get { return GetProperty(() => IsWiegandParamsEnabled); }
            set { SetProperty(() => IsWiegandParamsEnabled, value); }
        }

        public int CommunicationInterfaceType
        {
            get { return GetProperty(() => CommunicationInterfaceType); }
            set { SetProperty(() => CommunicationInterfaceType, value, RaiseCommunicationInterfaceTypeChanged); }
        }

        public int Rs232BaudRate
        {
            get { return GetProperty(() => Rs232BaudRate); }
            set { SetProperty(() => Rs232BaudRate, value); }
        }

        public int Rs485BaudRate
        {
            get { return GetProperty(() => Rs485BaudRate); }
            set { SetProperty(() => Rs485BaudRate, value); }
        }

        //相邻判别
        public bool AdjacentDiscriminant
        {
            get { return GetProperty(() => AdjacentDiscriminant); }
            set { SetProperty(() => AdjacentDiscriminant, value, RaiseAdjacentDiscriminantChanged); }
        }

        private void RaiseAdjacentDiscriminantChanged()
        {
            if (!AdjacentDiscriminant)
                AdjacentDiscriminantTime = 0;
        }

        //相邻判别持续时间
        public int AdjacentDiscriminantTime
        {
            get { return GetProperty(() => AdjacentDiscriminantTime); }
            set { SetProperty(() => AdjacentDiscriminantTime, value); }
        }

        //定时间隔？读卡时间间隔
        public int ReadTagTimeInterval
        {
            get { return GetProperty(() => ReadTagTimeInterval); }
            set { SetProperty(() => ReadTagTimeInterval, value); }
        }

        //设备号
        public int DeviceNumber
        {
            get { return GetProperty(() => DeviceNumber); }
            set { SetProperty(() => DeviceNumber, value); }
        }

        public ObservableCollection<int> DeviceNumberCollection
        {
            get { return GetProperty(() => DeviceNumberCollection); }
            set { SetProperty(() => DeviceNumberCollection, value); }
        }

        //触发开关
        public int TriggerSwitch
        {
            get { return GetProperty(() => TriggerSwitch); }
            set { SetProperty(() => TriggerSwitch, value); }
        }

        //延迟时间
        public int TriggerDelay
        {
            get { return GetProperty(() => TriggerDelay); }
            set { SetProperty(() => TriggerDelay, value); }
        }

        //韦根协议
        public int WiegandProtocol
        {
            get { return GetProperty(() => WiegandProtocol); }
            set { SetProperty(() => WiegandProtocol, value); }
        }

        //韦根宽度
        public int WiegandWidth
        {
            get { return GetProperty(() => WiegandWidth); }
            set { SetProperty(() => WiegandWidth, value); }
        }

        //韦根周期
        public int WiegandPeriod
        {
            get { return GetProperty(() => WiegandPeriod); }
            set { SetProperty(() => WiegandPeriod, value); }
        }

        private void RaiseCommunicationInterfaceTypeChanged()
        {
            IsRs232ParamsEnabled = false;
            IsRs485ParamsEnabled = false;
            IsWiegandParamsEnabled = false;
            switch (CommunicationInterfaceType)
            {
                case 0:
                    IsRs485ParamsEnabled = true;
                    break;
                case 1:
                    IsWiegandParamsEnabled = true;
                    break;
                case 2:
                    IsRs232ParamsEnabled = true;
                    break;
            }
        }

        /*
         * Command
         */

        //默认参数
        public void GetWorkModePageDefaultParameters()
        {
            TimingMode = true;
            AdjacentDiscriminant = false;
            AdjacentDiscriminantTime = 0;
            ReadTagTimeInterval = 10;
            CommunicationInterfaceType = 2;
            Rs232BaudRate = 0;//9600bps
            Rs485BaudRate = -1;

            DeviceNumber = 0;
            TriggerSwitch = -1;
            TriggerDelay = 0;

            WiegandProtocol = -1;
            WiegandPeriod = 0;
            WiegandWidth = 0;

            StatusTipInfo = GetString("StrGetWorkModeDefaultParams", "Get mode default parameters"); //"获取工作模式默认参数";
        }

        //工作模式页参数查询
        public void QueryWorkModePageParameters()
        {
            Task.Factory.StartNew(GetWorkModePageParams);
        }

        //工作模式页参数设置
        public void SetWorkModePageParameters()
        {
            Task.Factory.StartNew(SetWorkModePageParams);
        }

        //获取工作模式页参数
        private void GetWorkModePageParams()
        {
            _reader.GetWorkMode(ReaderAddress);
            Thread.Sleep(400);
            _reader.GetAdjacentDiscriminant(ReaderAddress);
            Thread.Sleep(400);
            _reader.GetAdjacentDiscriminantTime(ReaderAddress);
            Thread.Sleep(400);
            _reader.GetCommunicationType(ReaderAddress);
            Thread.Sleep(400);
            _reader.GetDeviceNumber(ReaderAddress);

            Thread.Sleep(400);
            if (TimingMode)
                _reader.GetReadTagTimeInterval(ReaderAddress);

            Thread.Sleep(400);
            if (TriggerMode)
            {
                _reader.GetTriggerSwitch(ReaderAddress);
                Thread.Sleep(400);
                _reader.GetTriggerDelay(ReaderAddress);
            }

            Thread.Sleep(400);
            if (CommunicationInterfaceType == 1)
            {
                _reader.GetWiegandParams(ReaderAddress);
            }
            else
            {
                _reader.GetBaudRate(ReaderAddress);
            }
        }

        private void SetWorkModePageParams()
        {
            //设置相邻判别
            var ad = AdjacentDiscriminant ? 1 : 2;
            _reader.SetAdjacentDiscriminant(ReaderAddress, (byte)ad);
            if (AdjacentDiscriminantTime >= 0)
            {
                Thread.Sleep(100);
                _reader.SetAdjacentDiscriminantTime(ReaderAddress, (byte)AdjacentDiscriminantTime);
            }
            else
            {
                StatusTipInfo = GetString("StrUnlawfulAdjacentDiscrTime", "Unlawful discrimination adjacent duration"); //"非法的相邻判别持续时间";
            }

            //设置模式、间隔时间、通信接口类型
            byte workMode;
            if (MasterSlaveMode)
                workMode = 1;
            else if (TimingMode)
                workMode = 2;
            else
                workMode = 3;
            var timeInterval = (byte)ReadTagTimeInterval;
            var commType = (byte)(CommunicationInterfaceType + 1);
            _reader.SetMultiParams(ReceivedValueType.SetReaderParams, ReaderAddress, 0x00, 0x70, new[] { workMode, timeInterval, commType });

            //设置波特率
            Thread.Sleep(100);
            if (CommunicationInterfaceType == 0)
            {
                _reader.SetBaudRate(ReaderAddress, GetByteBaudRate(Rs485BaudRate));
            }
            else if (CommunicationInterfaceType == 2)
            {
                _reader.SetBaudRate(ReaderAddress, GetByteBaudRate(Rs232BaudRate));
            }

            //韦根协议设置
            if (CommunicationInterfaceType == 1)
            {
                var wiegProtocol = (byte)(WiegandProtocol + 1);
                Thread.Sleep(100);
                _reader.SetMultiParams(ReceivedValueType.SetReaderParams, ReaderAddress, 0x00, 0x73, new[] { wiegProtocol, (byte)WiegandWidth, (byte)WiegandPeriod });
            }

            //触发模式参数
            if (TriggerMode)
            {
                Thread.Sleep(100);
                _reader.SetTriggerSwitch(ReaderAddress, (byte)TriggerSwitch);
                if (TriggerSwitch == 1)
                {
                    Thread.Sleep(100);
                    _reader.SetTriggerDelay(ReaderAddress, (byte)TriggerDelay);
                }
            }

            //设备地址
            Thread.Sleep(100);
            _reader.SetDeviceNumber(ReaderAddress, (byte)DeviceNumber);
        }

        #endregion

        /*
         * +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
         * 读写器参数
         * +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
         */
        #region
        //发射功率
        public string TransmitPower
        {
            get { return GetProperty(() => TransmitPower); }
            set { SetProperty(() => TransmitPower, value); }
        }

        //单卡读取
        public bool SingleTag
        {
            get { return GetProperty(() => SingleTag); }
            set { SetProperty(() => SingleTag, value); }
        }

        //多卡读取
        public bool MultiTag
        {
            get { return GetProperty(() => MultiTag); }
            set { SetProperty(() => MultiTag, value); }
        }

        public bool Antenna1Checked
        {
            get { return GetProperty(() => Antenna1Checked); }
            set { SetProperty(() => Antenna1Checked, value); }
        }

        public bool Antenna2Checked
        {
            get { return GetProperty(() => Antenna2Checked); }
            set { SetProperty(() => Antenna2Checked, value); }
        }

        public bool Antenna3Checked
        {
            get { return GetProperty(() => Antenna3Checked); }
            set { SetProperty(() => Antenna3Checked, value); }
        }

        public bool Antenna4Checked
        {
            get { return GetProperty(() => Antenna4Checked); }
            set { SetProperty(() => Antenna4Checked, value); }
        }

        //跳频
        public bool HoppingChecked
        {
            get { return GetProperty(() => HoppingChecked); }
            set { SetProperty(() => HoppingChecked, value, () => IsBandsEnabled = value); }
        }

        //定频
        public bool FixedFrequencyChecked
        {
            get { return GetProperty(() => FixedFrequencyChecked); }
            set { SetProperty(() => FixedFrequencyChecked, value, () => IsFixedFrequencyEnabled = value); }
        }

        //频率
        public int Frequency
        {
            get { return GetProperty(() => Frequency); }
            set { SetProperty(() => Frequency, value); }
        }

        public bool IsFixedFrequencyEnabled
        {
            get { return GetProperty(() => IsFixedFrequencyEnabled); }
            set { SetProperty(() => IsFixedFrequencyEnabled, value); }
        }

        public bool IsBandsEnabled
        {
            get { return GetProperty(() => IsBandsEnabled); }
            set { SetProperty(() => IsBandsEnabled, value); }
        }

        public bool IsReaderParamPageDefaultButtonEnabled
        {
            get { return GetProperty(() => IsReaderParamPageDefaultButtonEnabled); }
            set { SetProperty(() => IsReaderParamPageDefaultButtonEnabled, value); }
        }

        public bool IsReaderParamPageQueryParamsButtonEnabled
        {
            get { return GetProperty(() => IsReaderParamPageQueryParamsButtonEnabled); }
            set { SetProperty(() => IsReaderParamPageQueryParamsButtonEnabled, value); }
        }

        public bool IsReaderParamPageSetParamsButtonEnabled
        {
            get { return GetProperty(() => IsReaderParamPageSetParamsButtonEnabled); }
            set { SetProperty(() => IsReaderParamPageSetParamsButtonEnabled, value); }
        }

        public bool IsSetupButtonEnable
        {
            get { return GetProperty(() => IsSetupButtonEnable); }
            set { SetProperty(() => IsSetupButtonEnable, value); }
        }

        public bool IsResetButtonEnable
        {
            get { return GetProperty(() => IsResetButtonEnable); }
            set { SetProperty(() => IsResetButtonEnable, value); }
        }

        //声音
        public int Buzzer
        {
            get { return GetProperty(() => Buzzer); }
            set { SetProperty(() => Buzzer, value); }
        }

        //继电器
        public int Relays
        {
            get { return GetProperty(() => Relays); }
            set { SetProperty(() => Relays, value); }
        }

        #region 50个频点

        public bool Band1
        {
            get { return GetProperty(() => Band1); }
            set { SetProperty(() => Band1, value); }
        }
        
        public bool Band2
        {
            get { return GetProperty(() => Band2); }
            set { SetProperty(() => Band2, value); }
        }
        
        public bool Band3
        {
            get { return GetProperty(() => Band3); }
            set { SetProperty(() => Band3, value); }
        }
        
        public bool Band4
        {
            get { return GetProperty(() => Band4); }
            set { SetProperty(() => Band4, value); }
        }
        
        public bool Band5
        {
            get { return GetProperty(() => Band5); }
            set { SetProperty(() => Band5, value); }
        }
        
        public bool Band6
        {
            get { return GetProperty(() => Band6); }
            set { SetProperty(() => Band6, value); }
        }
        
        public bool Band7
        {
            get { return GetProperty(() => Band7); }
            set { SetProperty(() => Band7, value); }
        }
        
        public bool Band8
        {
            get { return GetProperty(() => Band8); }
            set { SetProperty(() => Band8, value); }
        }
        
        public bool Band9
        {
            get { return GetProperty(() => Band9); }
            set { SetProperty(() => Band9, value); }
        }
        
        public bool Band10
        {
            get { return GetProperty(() => Band10); }
            set { SetProperty(() => Band10, value); }
        }
        
        public bool Band11
        {
            get { return GetProperty(() => Band11); }
            set { SetProperty(() => Band11, value); }
        }
        
        public bool Band12
        {
            get { return GetProperty(() => Band12); }
            set { SetProperty(() => Band12, value); }
        }
        
        public bool Band13
        {
            get { return GetProperty(() => Band13); }
            set { SetProperty(() => Band13, value); }
        }
        
        public bool Band14
        {
            get { return GetProperty(() => Band14); }
            set { SetProperty(() => Band14, value); }
        }
        
        public bool Band15
        {
            get { return GetProperty(() => Band15); }
            set { SetProperty(() => Band15, value); }
        }
        
        public bool Band16
        {
            get { return GetProperty(() => Band16); }
            set { SetProperty(() => Band16, value); }
        }
        
        public bool Band17
        {
            get { return GetProperty(() => Band17); }
            set { SetProperty(() => Band17, value); }
        }
        
        public bool Band18
        {
            get { return GetProperty(() => Band18); }
            set { SetProperty(() => Band18, value); }
        }
        
        public bool Band19
        {
            get { return GetProperty(() => Band19); }
            set { SetProperty(() => Band19, value); }
        }
        
        public bool Band20
        {
            get { return GetProperty(() => Band20); }
            set { SetProperty(() => Band20, value); }
        }
        
        public bool Band21
        {
            get { return GetProperty(() => Band21); }
            set { SetProperty(() => Band21, value); }
        }
        
        public bool Band22
        {
            get { return GetProperty(() => Band22); }
            set { SetProperty(() => Band22, value); }
        }
        
        public bool Band23
        {
            get { return GetProperty(() => Band23); }
            set { SetProperty(() => Band23, value); }
        }
        
        public bool Band24
        {
            get { return GetProperty(() => Band24); }
            set { SetProperty(() => Band24, value); }
        }
        
        public bool Band25
        {
            get { return GetProperty(() => Band25); }
            set { SetProperty(() => Band25, value); }
        }
        
        public bool Band26
        {
            get { return GetProperty(() => Band26); }
            set { SetProperty(() => Band26, value); }
        }
        
        public bool Band27
        {
            get { return GetProperty(() => Band27); }
            set { SetProperty(() => Band27, value); }
        }
        
        public bool Band28
        {
            get { return GetProperty(() => Band28); }
            set { SetProperty(() => Band28, value); }
        }
        
        public bool Band29
        {
            get { return GetProperty(() => Band29); }
            set { SetProperty(() => Band29, value); }
        }
        
        public bool Band30
        {
            get { return GetProperty(() => Band30); }
            set { SetProperty(() => Band30, value); }
        }
        
        public bool Band31
        {
            get { return GetProperty(() => Band31); }
            set { SetProperty(() => Band31, value); }
        }
        
        public bool Band32
        {
            get { return GetProperty(() => Band32); }
            set { SetProperty(() => Band32, value); }
        }
        
        public bool Band33
        {
            get { return GetProperty(() => Band33); }
            set { SetProperty(() => Band33, value); }
        }
        
        public bool Band34
        {
            get { return GetProperty(() => Band34); }
            set { SetProperty(() => Band34, value); }
        }
        
        public bool Band35
        {
            get { return GetProperty(() => Band35); }
            set { SetProperty(() => Band35, value); }
        }
        
        public bool Band36
        {
            get { return GetProperty(() => Band36); }
            set { SetProperty(() => Band36, value); }
        }
        
        public bool Band37
        {
            get { return GetProperty(() => Band37); }
            set { SetProperty(() => Band37, value); }
        }
        
        public bool Band38
        {
            get { return GetProperty(() => Band38); }
            set { SetProperty(() => Band38, value); }
        }
        
        public bool Band39
        {
            get { return GetProperty(() => Band39); }
            set { SetProperty(() => Band39, value); }
        }
        
        public bool Band40
        {
            get { return GetProperty(() => Band40); }
            set { SetProperty(() => Band40, value); }
        }
        
        public bool Band41
        {
            get { return GetProperty(() => Band41); }
            set { SetProperty(() => Band41, value); }
        }
        
        public bool Band42
        {
            get { return GetProperty(() => Band42); }
            set { SetProperty(() => Band42, value); }
        }
        
        public bool Band43
        {
            get { return GetProperty(() => Band43); }
            set { SetProperty(() => Band43, value); }
        }
        
        public bool Band44
        {
            get { return GetProperty(() => Band44); }
            set { SetProperty(() => Band44, value); }
        }
        
        public bool Band45
        {
            get { return GetProperty(() => Band45); }
            set { SetProperty(() => Band45, value); }
        }
        
        public bool Band46
        {
            get { return GetProperty(() => Band46); }
            set { SetProperty(() => Band46, value); }
        }
        
        public bool Band47
        {
            get { return GetProperty(() => Band47); }
            set { SetProperty(() => Band47, value); }
        }
        
        public bool Band48
        {
            get { return GetProperty(() => Band48); }
            set { SetProperty(() => Band48, value); }
        }
        
        public bool Band49
        {
            get { return GetProperty(() => Band49); }
            set { SetProperty(() => Band49, value); }
        }
        
        public bool Band50
        {
            get { return GetProperty(() => Band50); }
            set { SetProperty(() => Band50, value); }
        }

        #endregion

        /*
         * Command
         */

        public void GetDefaultReaderParamsPageParams()
        {
            TransmitPower = "130";
            Antenna1Checked = true;
            Antenna2Checked = false;
            Antenna3Checked = false;
            Antenna4Checked = false;
            SingleTag = true;
            MultiTag = false;
            HoppingChecked = true;
            FixedFrequencyChecked = false;
            IsFixedFrequencyEnabled = false;
            Band1 = true;
            Band11 = true;
            Band21 = true;
            Band31 = true;
            Band41 = true;
            Band50 = true;

            StatusTipInfo = GetString("StrGetReaderDefaultParams", "Get the reader default parameters"); //"获取读写器默认参数";
        }

        public void QueryReaderParamPageParams()
        {
            Task.Factory.StartNew(GetReaderParameterPageParams);
        }

        public void SetReaderParamPageParams()
        {
            Task.Factory.StartNew(SetReaderParams);
        }

        public void SetBuzzerAndRelays()
        {
            if (Buzzer >= 0)
                _reader.SetBuzzer(ReaderAddress, (byte)Buzzer);
            if (Relays < 0) return;
            Thread.Sleep(100);
            _reader.SetRelays(ReaderAddress, (byte)Relays);
        }

        public void Reset()
        {
            _reader.ResetReader(ReaderAddress);
        }

        //获取读写器参数页的参数
        private void GetReaderParameterPageParams()
        {
            _reader.GetTransmitPower(ReaderAddress);
            Thread.Sleep(400);
            _reader.GetAntenna(ReaderAddress);
            Thread.Sleep(400);
            _reader.GetReadTagType(ReaderAddress);
            Thread.Sleep(400);
            _reader.GetHopping(ReaderAddress);
            Thread.Sleep(400);
            _reader.GetBands(ReaderAddress);
        }

        private void SetReaderParams()
        {
            //发射功率
            Thread.Sleep(100);
            _reader.SetTransmitPower(ReaderAddress, Convert.ToByte(TransmitPower));

            //设置天线
            Thread.Sleep(100);
            _reader.SetAntenna(ReaderAddress, AntennaUtils.GetByteAntenna(new[] { Antenna1Checked, Antenna2Checked, Antenna3Checked, Antenna4Checked }));

            //设置读卡方式
            Thread.Sleep(100);
            var rType = MultiTag ? 1 : 0;
            _reader.SetReadTagType(ReaderAddress, (byte)rType);

            //设置频率
            Thread.Sleep(100);
            var frequency = HoppingChecked ? 0 : 1;
            if (FixedFrequencyChecked)
                frequency = Frequency + 1;
            _reader.SetHopping(ReaderAddress, (byte)frequency);

            //设置频段
            if (!HoppingChecked) return;
            Thread.Sleep(100);
            _reader.SetBands(ReaderAddress, GetBands());
        }

        #endregion

        /*
         * +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
         * 在线设备
         * +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
         */
        #region

        public bool IsLiveDevicePageContentEnabled
        {
            get { return GetProperty(() => IsLiveDevicePageContentEnabled); }
            set { SetProperty(() => IsLiveDevicePageContentEnabled, value); }
        }

        public bool IsTargetParamsEnabled
        {
            get { return GetProperty(() => IsTargetParamsEnabled); }
            set { SetProperty(() => IsTargetParamsEnabled, value); }
        }

        public bool IsSetDeviceParamsEnabled
        {
            get { return GetProperty(() => IsSetDeviceParamsEnabled); }
            set { SetProperty(() => IsSetDeviceParamsEnabled, value); }
        }

        public ObservableCollection<Device> SearchDevices
        {
            get { return GetProperty(() => SearchDevices); }
            set { SetProperty(() => SearchDevices, value); }
        }

        public object SelectDevice
        {
            get { return GetProperty(() => SelectDevice); }
            set { SetProperty(() => SelectDevice, value); }
        }

        public int SpBaudrate
        {
            get { return GetProperty(() => SpBaudrate); }
            set { SetProperty(() => SpBaudrate, value); }
        }

        public int DataBits
        {
            get { return GetProperty(() => DataBits); }
            set { SetProperty(() => DataBits, value); }
        }

        public int StopBits
        {
            get { return GetProperty(() => StopBits); }
            set { SetProperty(() => StopBits, value); }
        }

        public int CheckBits
        {
            get { return GetProperty(() => CheckBits); }
            set { SetProperty(() => CheckBits, value); }
        }

        public int DeviceMode
        {
            get { return GetProperty(() => DeviceMode); }
            set { SetProperty(() => DeviceMode, value, RaiseDeviceModeChanged); }
        }

        public string SetIp
        {
            get { return GetProperty(() => SetIp); }
            set { SetProperty(() => SetIp, value); }
        }

        public int? SetPort
        {
            get { return GetProperty(() => SetPort); }
            set { SetProperty(() => SetPort, value); }
        }

        public string SetSubNetMask
        {
            get { return GetProperty(() => SetSubNetMask); }
            set { SetProperty(() => SetSubNetMask, value); }
        }

        public string SetTargetIp
        {
            get { return GetProperty(() => SetTargetIp); }
            set { SetProperty(() => SetTargetIp, value); }
        }

        public int? SetTargetPort
        {
            get { return GetProperty(() => SetTargetPort); }
            set { SetProperty(() => SetTargetPort, value); }
        }

        public string SetGateway
        {
            get { return GetProperty(() => SetGateway); }
            set { SetProperty(() => SetGateway, value); }
        }

        public void DeviceRowSelected()
        {
            var device = SelectDevice as Device;
            if (device == null)
            {
                StatusTipInfo = GetString("StrSelectedDeviceNull", "The selected equipment is null");
                return;
            }

            SpBaudrate = GetIndexBaudRate(device.BuadRate.ToString());
            DataBits = device.DataBits;
            StopBits = device.StopBits;
            CheckBits = device.CheckBits;

            DeviceMode = device.ModeIndex;
            SetIp = device.IpAddress;
            SetPort = device.Port;
            SetSubNetMask = device.SubNetMask;

            SetGateway = device.Gateway;
            SetTargetIp = device.TargetIpAddress;
            SetTargetPort = device.TargetPort;

            //连接处
            IpAddress = device.IpAddress;
            Port = device.Port;

            if (!IsSetDeviceParamsEnabled)
                IsSetDeviceParamsEnabled = true;
        }

        public void GetSearchDeviceDefaultParams()
        {
            SpBaudrate = 0;
            DataBits = 3;
            StopBits = 0;
            CheckBits = 0;

            SetIp = "192.168.1.200";
            SetPort = 4196;
            DeviceMode = 3;
            SetSubNetMask = "255.255.255.0";

            SetTargetIp = "192.168.1.3";
            SetTargetPort = 4196;
            SetGateway = "192.168.1.3";
        }

        public void SetDeviceParams()
        {
            var device = SelectDevice as Device;
            var baudrate = Convert.ToInt32(BaudRateList[SpBaudrate].Replace("bps", ""));
            var spBits = Device.GetByteSerialPortParams(DataBits, StopBits, CheckBits);
            try
            {
                if (SetPort == null || SetTargetPort == null)
                {
                    StatusTipInfo = GetString("StrPortCanNotEmpty", "Ports can not be empty");
                    return;
                }
                ProxySocket.SetDeviceParams(device, baudrate, spBits, DeviceMode, SetIp, (int)SetPort, SetSubNetMask, SetTargetIp, (int)SetTargetPort, SetGateway);
                StatusTipInfo = GetString("StrSetDevParamsSuccess", "Module parameter setting success");
            }
            catch (Exception ex)
            {
                StatusTipInfo = ex.Message;
            }
        }

        private void InitSetParams()
        {
            SpBaudrate = -1;
            DataBits = -1;
            StopBits = -1;
            CheckBits = -1;

            SetIp = null;
            SetPort = null;
            DeviceMode = -1;
            SetSubNetMask = null;

            SetTargetIp = null;
            SetTargetPort = null;
            SetGateway = null;
        }

        private void RaiseDeviceModeChanged()
        {
            IsTargetParamsEnabled = DeviceMode == 1;
        }

        #endregion

        /*
         * ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
         * 杂项
         * ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
         */
        #region
        //打开串口成功需要处理的事情
        private void OpenOrConnectionOkDoThis()
        {
            IsReaderAddressEnabled = false;//设备号不可修改
            IsBaudrateEnabled = false;//波特率

            IsReadTagButtonEnabled = true;//读卡按钮

            IsRwTagEnabled = true;
            IsOtherOperateEnabled = true;
            IsQuickWriteEnabled = true;

            IsWorkModePageDefaultParamButtonEnabled = true;
            IsWorkModePageGetParamButtonEnabled = true;
            IsWorkModePageSetParamButtonEnabled = true;

            IsReaderParamPageDefaultButtonEnabled = true;
            IsReaderParamPageQueryParamsButtonEnabled = true;
            IsReaderParamPageSetParamsButtonEnabled = true;
            IsSetupButtonEnable = true;
            IsResetButtonEnable = false;//复位

            //IsLiveDevicePageContentEnabled = false;

            StatusTipInfo = GetString("StrQueryParamOver", "End the reader parameter query!");
        }

        //关闭串口成功需要处理的事情
        private void ClosedOrDisconnectionOkDoThis()
        {
            IsOpenSerialPortEnabled = true;//打卡串口按钮可见
            IsClosedSerialPortEnabled = false;//关闭串口按钮
            IsReadTagButtonEnabled = false;//读卡按钮
            IsStopReadTagButtonEnabled = false;//停止读卡按钮

            IsReaderAddressEnabled = true;//设备号可用
            IsBaudrateEnabled = true;//波特率
            VersionInfo = string.Empty;

            IsNetworkEnabled = true;//网口连接可用
            IsSerialPortEnabled = true;//串口连接
            IsIpAddressEnabled = true;//IP地址
            IsPortEnabled = true;//端口
            IsSearchButtonEnabled = true;//搜索按钮
            IsConnectionButtonEnabled = true;//连接按钮
            IsDisConnectionButtonEnabled = false;//断开连接按钮

            IsQuickWriteEnabled = false;//快写标签
            IsReadTagEnabled = false;//读标签
            IsRwTagEnabled = false;//读/写标签
            IsOtherOperateEnabled = false;//标签其他操作

            IsWorkModePageDefaultParamButtonEnabled = false;//工作模式页默认按钮
            IsWorkModePageGetParamButtonEnabled = false;//工作模式页获取参数按钮
            IsWorkModePageSetParamButtonEnabled = false;//工作模式页设置参按钮

            IsReaderParamPageDefaultButtonEnabled = false;//读写器参数页默认按钮
            IsReaderParamPageQueryParamsButtonEnabled = false;//读写器参数页获取参数按钮
            IsReaderParamPageSetParamsButtonEnabled = false;//读写器参数页设置参数按钮
            IsSetupButtonEnable = false;//读写器参数页设置按钮
            IsResetButtonEnable = false;//读写器参数页重置按钮
        }

        //是否显示Loading
        public bool IsShowLoading
        {
            get { return GetProperty(() => IsShowLoading); }
            set { SetProperty(() => IsShowLoading, value); }
        }

        //Loading主提示内容
        public string LoadingMainInfo
        {
            get { return GetProperty(() => LoadingMainInfo); }
            set { SetProperty(() => LoadingMainInfo, value); }
        }

        //Loading次提示内容
        public string LoadingSubInfo
        {
            get { return GetProperty(() => LoadingSubInfo); }
            set { SetProperty(() => LoadingSubInfo, value); }
        }

        //接收设置读写器返回的数据
        private void ReceivedOperateResponse(ReceivedValueType resultType, object result)
        {
            //var msg = string.Empty;
            switch (resultType)
            {
                case ReceivedValueType.QuickWriteTag:
                    StatusTipInfo = (bool)result ? GetString("StrFastWriteSuccess", "Fast Write tag success") : GetString("StrFastWriteFail", "Fast Write tag fail");//"快写标签成功" : "快写标签失败";
                    break;
                case ReceivedValueType.InitilizeTag:
                    StatusTipInfo = (bool)result ? GetString("StrInitializeComplete", "Initialization is complete") : GetString("StrFailedInitialize", "Failed to initialize");//"初始化完成" : "初始化失败";
                    break;
                case ReceivedValueType.KillTag:
                    StatusTipInfo = (bool)result ? GetString("StrKillSuccess", "Kill tag success") : GetString("StrKillFail", "Failed to kill tag");//"销毁标签成功" : "销毁标签失败";
                    break;
                case ReceivedValueType.LockTag:
                    StatusTipInfo = (bool)result ? GetString("StrLockSuccess", "Lock tag success") : GetString("StrFailedLock", "Failed to lock tag");//"锁定标签成功" : "标签锁定失败";
                    break;
                case ReceivedValueType.SetBaudRate:
                    var msg = (bool)result;
                    StatusTipInfo = msg ? GetString("StrBaudrareSetSuccess", "Baud rate setting success") : GetString("StrBaudrareSetFail", "Failure to set the baud rate");//"波特率设置失败" : "波特率设置成功";
                    return;
                case ReceivedValueType.SetReaderParams:
                    StatusTipInfo = GetString("StrReaderParamsSetSuccess", "The reader parameter setting success");//"读写器参数设置成功";
                    return;
                case ReceivedValueType.UnlockTag:
                    StatusTipInfo = (bool)result ? GetString("StrUnlockSuccess", "Unlock success") : GetString("StrFailedUnlock", "Failed to unlock"); //"解锁成功" : "解锁失败";
                    break;
                case ReceivedValueType.WriteTag:
                    StatusTipInfo = (bool)result ? GetString("StrWriteTagSuccess", "Write tag success") : GetString("StrFailedWriteTag", "Write tag failed"); //"写卡成功" : "写卡失败";
                    return;
                case ReceivedValueType.Reset:
                    StatusTipInfo = (bool)result ? GetString("StrResetSuccess", "Reset success") : GetString("StrFailedReset", "Reset failed"); //"复位成功" : "复位失败";
                    break;
                case ReceivedValueType.ReadTag:
                    if (result == null)
                    {
                        StatusTipInfo = GetString("StrFailedReadTag", "Failure to read the tag"); //"读取标签失败";
                    }
                    else
                    {
                        var tag = result as byte[];
                        if (tag != null)
                        {
                            RwTagContent = TextEncoder.ByteArrayToHexString(tag);
                            RaisePropertyChanged(() => RwTagContent);
                            StatusTipInfo = GetString("StrReadTagSuccess", "Read the tag success"); //"读取标签成功";
                        }
                    }
                    break;
                case ReceivedValueType.GetFirmwareVersion:
                    if (result == null)
                    {
                        StatusTipInfo = GetString("StrFailedGetVersion", "Get firmware version fails");//"获取硬件版本失败";
                        _isOpen = false;
                    }
                    else
                    {
                        _isOpen = true;
                        VersionInfo = $"{GetString("Version", "Version")}：{result}";
                    }
                    break;
                case ReceivedValueType.GetCommunicationInterfaceType:
                    if (result == null)
                    {
                        StatusTipInfo = GetString("StrFailedGetCommunicationInterface", "Get communication interface failure"); //"获取通讯接口失败";
                    }
                    else
                    {
                        CommunicationInterfaceType = (int)result - 1;
                        RaisePropertyChanged(() => CommunicationInterfaceType);
                        StatusTipInfo = GetString("StrGetCommunicationInterfaceSuccess", "Being successful communication interface"); //"获取通讯接口成功";
                    }
                    break;
                case ReceivedValueType.GetBaudRate:
                    if (result == null)
                    {
                        StatusTipInfo = GetString("StrGetBaudrareFail", "Failure to obtain the baud rate"); //"获取波特率失败";
                    }
                    else
                    {

                        if (CommunicationInterfaceType == 0)
                        {
                            switch ((int)result)
                            {
                                case 9600:
                                    Rs485BaudRate = 0;
                                    break;
                                case 19200:
                                    Rs485BaudRate = 1;
                                    break;
                                case 38400:
                                    Rs485BaudRate = 2;
                                    break;
                                case 57600:
                                    Rs485BaudRate = 3;
                                    break;
                                case 115200:
                                    Rs485BaudRate = 4;
                                    break;
                                default:
                                    Rs485BaudRate = -1;
                                    break;
                            }
                        }
                        if (CommunicationInterfaceType == 2)
                        {
                            switch ((int)result)
                            {
                                case 9600:
                                    Rs232BaudRate = 0;
                                    break;
                                case 19200:
                                    Rs232BaudRate = 1;
                                    break;
                                case 38400:
                                    Rs232BaudRate = 2;
                                    break;
                                case 115200:
                                    Rs232BaudRate = 3;
                                    break;
                                default:
                                    Rs232BaudRate = -1;
                                    break;
                            }
                        }
                        StatusTipInfo = GetString("StrGetBaudrareSuccess", "Get baud rate success"); //"获取波特率成功";
                    }
                    break;
                case ReceivedValueType.GetWorkMode:
                    if (result == null)
                    {
                        StatusTipInfo = GetString("StrFailedGetWorkMode", "Operating mode of acquisition failure"); //"工作模式获取失败";
                    }
                    else
                    {
                        var workMode = (int)result;
                        switch (workMode)
                        {
                            case 1:
                                MasterSlaveMode = true;
                                break;
                            case 2:
                                TimingMode = true;
                                break;
                            case 3:
                                TriggerMode = true;
                                break;
                        }
                        StatusTipInfo = GetString("StrGetWorkModeSuccess", "Being successful operating mode"); //"工作模式获取成功";
                    }
                    break;
                case ReceivedValueType.GetAdjacentDiscriminant:
                    if (result == null)
                    {
                        StatusTipInfo = GetString("StrFailedGetAd", "Being adjacent to determine failure"); //"获取相邻判别失败";
                    }
                    else
                    {
                        AdjacentDiscriminant = (bool)result;
                        StatusTipInfo = GetString("StrGetAdSuccess", "Being adjacent to determine success"); //"获取相邻判别成功";
                    }
                    break;
                case ReceivedValueType.GetAdjacentDiscriminantTime:
                    if (result == null)
                    {
                        StatusTipInfo = GetString("StrFailedGetAdTime", "Being adjacent to determine the interval failed"); //"获取相邻判别间隔时间失败";
                    }
                    else
                    {
                        AdjacentDiscriminantTime = (int)result;
                        StatusTipInfo = GetString("StrGetAdTimeSuccess", "Being adjacent to determine the success of the interval"); //"获取相邻判别间隔时间成功";
                    }
                    break;
                case ReceivedValueType.GetDeviceNumber:
                    if (result == null)
                    {
                        StatusTipInfo = GetString("StrFailedGetDeviceNumber", "Get device number failed"); //"获取设备号失败";
                    }
                    else
                    {
                        DeviceNumber = (int)result;
                        StatusTipInfo = GetString("StrGetDeviceNumberSuccess", "Being successful device number"); //"获取设备号成功";
                    }
                    break;
                case ReceivedValueType.GetReadTagTimeInterval:
                    if (result == null)
                    {
                        StatusTipInfo = GetString("StrFailedGetTimingInterval", "Failure to obtain timing interval"); //"获取定时间隔失败";
                    }
                    else
                    {
                        ReadTagTimeInterval = (int)result;
                        StatusTipInfo = GetString("StrGetTimingIntervalSuccess", "Being successful at timed intervals"); //"获取定时间隔成功";
                    }
                    break;
                case ReceivedValueType.GetReadTagType:
                    if (result == null)
                    {
                        StatusTipInfo = GetString("StrFailedGetReadTagType", "Get Reader mode failure"); //"获取读卡方式失败";
                    }
                    else
                    {
                        var rtType = (int)result;
                        SingleTag = rtType == 0;
                        MultiTag = rtType == 1;
                        StatusTipInfo = GetString("StrGetReadTagTypeSuccess", "Being successful reader mode"); //"获取读卡方式成功";
                    }
                    break;
                case ReceivedValueType.GetTransmitPower:
                    if (result == null)
                    {
                        StatusTipInfo = GetString("StrFailedGetPower", "Get power failure"); //"获取功率失败";
                    }
                    else
                    {
                        TransmitPower = result.ToString();
                        StatusTipInfo = GetString("StrGetPowerSuccess", "Get power success"); //"获取功率成功";
                    }
                    break;
                case ReceivedValueType.GetAntenna:
                    if (result == null)
                    {
                        StatusTipInfo = GetString("StrFailedGetAntenna", "Get the antenna failure"); //"获取天线失败";
                    }
                    else
                    {
                        var ants = AntennaUtils.GetBoolsAntenna((int)result);
                        if (ants != null && ants.Length == 4)
                        {
                            Antenna1Checked = ants[0];
                            Antenna2Checked = ants[1];
                            Antenna3Checked = ants[2];
                            Antenna4Checked = ants[3];
                        }
                        StatusTipInfo = GetString("StrGetAntennaSuccess", "Get Antenna success"); //"获取天线成功";
                    }
                    break;
                case ReceivedValueType.GetHopping:
                    if (result == null)
                    {
                        StatusTipInfo = GetString("StrFailedGetHopping", "Get hopping fail"); //"获取跳频失败";
                    }
                    else
                    {
                        var hopping = (int)result;
                        if (hopping > 0)
                        {
                            FixedFrequencyChecked = true;
                            Frequency = hopping - 1;
                        }
                        else
                        {
                            HoppingChecked = true;
                        }
                        StatusTipInfo = GetString("StrGetHoppingSuccess", "Get hopping success"); //"获取跳频成功";
                    }
                    break;
                case ReceivedValueType.GetBands:
                    if (result == null)
                    {
                        StatusTipInfo = GetString("StrFailedGetFrequency", "Get frequent point of failure"); //"获取频点失败";
                    }
                    else
                    {
                        var bands = (bool[])result;
                        InitBands(bands);
                        StatusTipInfo = GetString("StrGetFrequencySuccess", "Being successful frequent point"); //"获取频点成功";
                    }
                    break;
                case ReceivedValueType.GetTriggerSwitch:
                    if (result == null)
                    {
                        StatusTipInfo = GetString("StrFailedGetTriggerSwitch", "Get trigger switch failed state"); //"获取触发开关状态失败";
                    }
                    else
                    {
                        TriggerSwitch = (int)result;
                        StatusTipInfo = GetString("StrGetTriggerSwitchSuccess", "Get trigger switch state success"); //"获取触发开关状态成功";
                    }
                    break;
                case ReceivedValueType.GetTriggerDelay:
                    if (result == null)
                    {
                        StatusTipInfo = GetString("StrFailedGetTriggerDelay", "Get off delay parameters fail"); //"获取延时关闭参数失败";
                    }
                    else
                    {
                        TriggerDelay = (int)result;
                        StatusTipInfo = GetString("StrGetTriggerDelaySuccess", "Get off delay parameters success"); //"获取延时关闭参数成功";
                    }
                    break;
                case ReceivedValueType.GetWiegandPeriod:
                    if (result == null)
                    {
                        StatusTipInfo = GetString("StrFailedGetWiegandPeriod", "Get Wiegand period fails"); //"获取韦根周期失败";
                    }
                    else
                    {
                        WiegandPeriod = (int)result;
                        StatusTipInfo = GetString("StrGetWiegandPeriodSuccess", "Get Wiegand period success"); //"获取韦根周期成功";
                    }
                    break;
                case ReceivedValueType.GetWiegandWidth:
                    if (result == null)
                    {
                        StatusTipInfo = GetString("StrFailedGetWiegandWidth", "Get Wiegand width fail"); //"获取韦根宽度失败";
                    }
                    else
                    {
                        WiegandWidth = (int)result;
                        StatusTipInfo = GetString("StrGetFWiegandWidthSuccess", "Get Wiegand width of success"); //"获取韦根宽度成功";
                    }
                    break;
                case ReceivedValueType.GetWiegandProtocol:
                    if (result == null)
                    {
                        StatusTipInfo = GetString("StrFailedGetWiegandProtocol", "Get Wiegand protocol failed"); //"获取韦根协议失败";
                    }
                    else
                    {
                        WiegandProtocol = (int)result - 1;
                        StatusTipInfo = GetString("StrGetWiegandProtocolSuccess", "Get Wiegand protocol success"); //"获取韦根协议成功";
                    }
                    break;
                case ReceivedValueType.IdentifyTag:
                    if (result == null)
                    {
                        StatusTipInfo = GetString("StrReadTagException", "Read tag exception"); //"读卡异常";
                    }
                    else
                    {
                        ReceivedIdentifyTagResponse(result);
                    }
                    break;
                default:
                    if (result == null)
                    {
                        StatusTipInfo = GetString("StrFailedOperate", "Operation failed");//"操作失败";
                    }
                    else
                    {
                        StatusTipInfo = (bool)result ? GetString("StrOperateSuccess", "Successful operation") : GetString("StrFailedOperate", "Operation failed");//"操作成功" : "操作失败";
                    }
                    break;
            }
            //ShowMessage(msg);
        }

        //接收读卡结果
        private void ReceivedIdentifyTagResponse(object obj)
        {
            try
            {
                var data = (string[])obj;
                if (data == null || data.Length != 3 || string.IsNullOrEmpty(data[0]))
                {
                    return;
                }
                var tag = new TagRecord()
                {
                    Id = _no + 1,
                    Count = 1,
                    AntennaNo = data[1],
                    DeviceNo = data[2],
                    EpcContent = data[0]
                };


                Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(delegate
                {
                    var exist = false;
                    foreach (var record in _tagRecords)
                    {
                        if (record.EpcContent.Equals(tag.EpcContent))
                        {
                            exist = true;
                            record.Count++;
                            //_count++;
                            if (record.AntennaNo.Contains(tag.AntennaNo)) break;
                            record.AntennaNo = record.AntennaNo + "," + tag.AntennaNo;
                            RaisePropertyChanged(() => TagRecords);
                        }
                    }

                    if (exist) return;
                    _tagRecords.Add(tag);
                    SaveTagContentToLocal();//有新的卡都进来时存储
                    RaisePropertyChanged(() => TagRecords);
                    _no++;
                    //_count++;
                }));
            }
            catch (Exception ex)
            {
                StatusTipInfo = $"{GetString("StrReadTagException", "Read tag exception")}，{ex.Message}";
            }
        }

        private void InitBands(bool[] bands)
        {
            if (bands == null || bands.Length < 50) return;
            Band1 = bands[0];
            Band2 = bands[1];
            Band3 = bands[2];
            Band4 = bands[3];
            Band5 = bands[4];
            Band6 = bands[5];
            Band7 = bands[6];
            Band8 = bands[7];
            Band9 = bands[8];
            Band10 = bands[9];

            Band11 = bands[10];
            Band12 = bands[11];
            Band13 = bands[12];
            Band14 = bands[13];
            Band15 = bands[14];
            Band16 = bands[15];
            Band17 = bands[16];
            Band18 = bands[17];
            Band19 = bands[18];
            Band20 = bands[19];

            Band21 = bands[20];
            Band22 = bands[21];
            Band23 = bands[22];
            Band24 = bands[23];
            Band25 = bands[24];
            Band26 = bands[25];
            Band27 = bands[26];
            Band28 = bands[27];
            Band29 = bands[28];
            Band30 = bands[29];

            Band31 = bands[30];
            Band32 = bands[31];
            Band33 = bands[32];
            Band34 = bands[33];
            Band35 = bands[34];
            Band36 = bands[35];
            Band37 = bands[36];
            Band38 = bands[37];
            Band39 = bands[38];
            Band40 = bands[39];

            Band41 = bands[40];
            Band42 = bands[41];
            Band43 = bands[42];
            Band44 = bands[43];
            Band45 = bands[44];
            Band46 = bands[45];
            Band47 = bands[46];
            Band48 = bands[47];
            Band49 = bands[48];
            Band50 = bands[49];
        }

        private byte[] GetBands()
        {
            var bands = new bool[56];
            bands[0] = Band1;
            bands[1] = Band2;
            bands[2] = Band3;
            bands[3] = Band4;
            bands[4] = Band5;
            bands[5] = Band6;
            bands[6] = Band7;
            bands[7] = Band8;
            bands[8] = Band9;
            bands[9] = Band10;

            bands[10] = Band11;
            bands[11] = Band12;
            bands[12] = Band13;
            bands[13] = Band14;
            bands[14] = Band15;
            bands[15] = Band16;
            bands[16] = Band17;
            bands[17] = Band18;
            bands[18] = Band19;
            bands[19] = Band20;

            bands[20] = Band21;
            bands[21] = Band22;
            bands[22] = Band23;
            bands[23] = Band24;
            bands[24] = Band25;
            bands[25] = Band26;
            bands[26] = Band27;
            bands[27] = Band28;
            bands[28] = Band29;
            bands[29] = Band30;

            bands[30] = Band31;
            bands[31] = Band32;
            bands[32] = Band33;
            bands[33] = Band34;
            bands[34] = Band35;
            bands[35] = Band36;
            bands[36] = Band37;
            bands[37] = Band38;
            bands[38] = Band39;
            bands[39] = Band40;

            bands[40] = Band41;
            bands[41] = Band42;
            bands[42] = Band43;
            bands[43] = Band44;
            bands[44] = Band45;
            bands[45] = Band46;
            bands[46] = Band47;
            bands[47] = Band48;
            bands[48] = Band49;
            bands[49] = Band50;

            bands[50] = false;
            bands[51] = false;
            bands[52] = false;
            bands[53] = false;
            bands[54] = false;
            bands[55] = false;

            //
            //var sb = new StringBuilder();
            //var bands = new byte[7];
            //var k = 0;
            //for (var i = 0; i < _bands.Length; i++)
            //{
            //    k++;
            //    sb.Append(_bands[i] ? '1' : '0');

            //    if (k != 8) continue;
            //    var array = sb.ToString().ToCharArray();
            //    Array.Reverse(array);
            //    var b = Convert.ToUInt16(new string(array), 2);
            //    bands[(i + 1) / 8 - 1] = (byte)b;
            //    k = 0;
            //    sb.Clear();
            //}
            return BandUtils.BoolstoBytesBands(bands);
        }

        private void ShowMessage(string msg)
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(delegate
            {
                MessageBoxService.Show(msg);
            }));
        }

        private void ShowLoading(string mainInfo, string subInfo)
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(delegate
            {
                if (!string.IsNullOrEmpty(mainInfo))
                {
                    LoadingMainInfo = mainInfo;
                    RaisePropertyChanged(() => LoadingMainInfo);
                }
                if (!string.IsNullOrEmpty(subInfo))
                {
                    LoadingSubInfo = subInfo;
                    RaisePropertyChanged(() => LoadingSubInfo);
                }
                IsShowLoading = true;
            }));
        }

        private void CloseLoading()
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(delegate
            {
                IsShowLoading = false;
                LoadingMainInfo = "Loading";
                LoadingSubInfo = "Please waiting...";
            }));
        }

        //取得国际化字符串
        private string GetString(string name, string defString)
        {
            var s = Application.Current.FindResource(name);
            return s?.ToString() ?? defString;
        }

        //根据选择的波特率返回字节型波特率代码
        private byte GetByteBaudRate(int baudRate)
        {
            switch (baudRate)
            {
                case 1:
                    return 0x01;//19200bps
                case 2:
                    return 0x02;//38400bps
                case 3:
                    if (baudRate == Rs232BaudRate)
                        return 0x04;//115200bps
                    return 0x03;//57600bps
                case 4:
                    return 0x04;//115200bps
                default:
                    return 0x00;//9600bps
            }
        }

        //根据字符串类型的波特率获取控件index
        private int GetIndexBaudRate(string baudRate)
        {
            if (baudRate.StartsWith("9600"))
            {
                return 0;
            }
            if (baudRate.StartsWith("19200"))
            {
                return 1;
            }
            if (baudRate.StartsWith("38400"))
            {
                return 2;
            }
            if (baudRate.StartsWith("57600"))
            {
                return 3;
            }
            if (baudRate.StartsWith("115200"))
            {
                return 4;
            }
            if (baudRate.StartsWith("2400"))
            {
                return 5;
            }
            return -1;
        }

        public string StatusTipInfo
        {
            get { return GetProperty(() => StatusTipInfo); }
            set { SetProperty(() => StatusTipInfo, value); }
        }

        public string VersionInfo
        {
            get { return GetProperty(() => VersionInfo); }
            set { SetProperty(() => VersionInfo, value); }
        }

        private void ChangeUiLanguage()
        {
            if (VersionInfo == null) return;
            var versionStr = GetString("Version", "Version");
            if (VersionInfo.Contains("Version"))
            {
                VersionInfo = VersionInfo.Replace("Version", versionStr);
            }
            else if (VersionInfo.Contains("版本"))
            {
                VersionInfo = VersionInfo.Replace("版本", versionStr);
            }
            StatusTipInfo = GetString("SwitchLanguage", "");
        }

        #endregion

    }
}