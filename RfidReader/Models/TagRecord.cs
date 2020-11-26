using DevExpress.Mvvm;

namespace RfidReader.Model
{
    public class TagRecord : BindableBase
    {
        //编号
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        //设备号
        public string DeviceNo
        {
            get { return GetProperty(()=>DeviceNo); }
            set { SetProperty(() => DeviceNo, value); }
        }

        //天线号
        public string AntennaNo 
        {
            get { return GetProperty(()=>AntennaNo); }
            set { SetProperty(() => AntennaNo, value); }
        }

        //EPC区内容
        public string EpcContent
        {
            get { return GetProperty(() => EpcContent); }
            set { SetProperty(() => EpcContent, value); }
        }

        //读取次数
        public int Count
        {
            get { return GetProperty(() => Count); }
            set { SetProperty(() => Count, value); }
        }
    }
}
