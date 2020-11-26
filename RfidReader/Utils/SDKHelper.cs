using System;
using System.Runtime.InteropServices;

namespace RfidReader.Utils
{
    public class SdkHelper
    {
        [DllImport("EPCSDK.dll")]
        public static extern IntPtr OpenComm(int portNo);

        [DllImport("EPCSDK.dll")]
        public static extern void CloseComm(IntPtr hCom);

        [DllImport("EPCSDK.dll")]
        public static extern bool ReadFirmwareVersion(IntPtr hCom, out int main, out int sub, byte readerAddr);


        [DllImport("EPCSDK.dll")]
        public static extern bool GetReaderParameters(IntPtr hCom, int addr, int paramNum, byte[] parms, byte readerAddr);
        [DllImport("EPCSDK.dll")]
        public static extern bool SetReaderParameters(IntPtr hCom, int addr, int paramNum, byte[] parms, byte readerAddr);

        [DllImport("EPCSDK.dll")]
        public static extern bool StopReading(IntPtr hCom, byte readerAddr);
        [DllImport("EPCSDK.dll")]
        public static extern bool ResumeReading(IntPtr hCom, byte readerAddr);

        [DllImport("EPCSDK.dll")]
        public static extern bool IdentifySingleTag(IntPtr hCom, byte[] tagId, byte[] antennaNo, byte readerAddr);

        [DllImport("EPCSDK.dll")]
        public static extern bool IdentifyUploadedSingleTag(IntPtr hCom, byte[] tagId, byte[] devNos, byte[] antennaNo);

        [DllImport("EPCSDK.dll")]
        public static extern bool IdentifyUploadedMultiTags(IntPtr hCom, out byte tagNum, byte[] tagIds, byte[] devNos, byte[] antennaNos);



        [DllImport("EPCSDK.dll")]
        public static extern bool ReadTag(IntPtr hCom, byte memBank, byte address, byte length, byte[] data, byte readerAddr);

        [DllImport("EPCSDK.dll")]
        public static extern bool WriteTagSingleWord(IntPtr hCom, byte memBank, byte address, byte data1, byte data2, byte readerAddr);

        [DllImport("EPCSDK.dll")]
        public static extern bool FastWriteTagID(IntPtr hCom, int bytesNum, byte[] bytes, byte readerAddr);

        [DllImport("EPCSDK.dll")]
        public static extern bool FastWriteTagID_Lock(IntPtr hCom, int bytesNum, byte[] bytes, byte readerAddr);


        [DllImport("EPCSDK.dll")]
        public static extern bool InitializeTag(IntPtr hCom, byte readerAddr);

        [DllImport("EPCSDK.dll")]
        public static extern bool LockPassWordTag(IntPtr hCom, byte passwd1, byte passwd2, byte passwd3, byte passwd4, byte lockType, byte readerAddr);

        [DllImport("EPCSDK.dll")]
        public static extern bool UnlockPassWordTag(IntPtr hCom, byte passwd1, byte passwd2, byte passwd3, byte passwd4, byte lockType, byte readerAddr);

        [DllImport("EPCSDK.dll")]
        public static extern bool KillTag(IntPtr hCom, byte passwd1, byte passwd2, byte passwd3, byte passwd4, byte readerAddr);
    }
}
