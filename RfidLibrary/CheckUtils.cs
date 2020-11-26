using System;

namespace RfidLibrary
{
    public sealed class CheckUtils
    {
        public static byte SumCheck(byte[] data, int offset, int length)
        {
            try
            {
                var sum = 0;
                for (var i = offset; i < length + offset; i++)
                {
                    sum += data[i];
                }
                sum = ~sum + 1;
                if (sum > 255)
                    throw new ArgumentOutOfRangeException("Beyond the scope of Byte");//超出了Byte的范围
                return (byte)sum;
            }
            catch (Exception ex)
            {
                throw new Exception("Checksum calculation anomaly", ex);//检验和计算异常
            }
        }
    }
}
