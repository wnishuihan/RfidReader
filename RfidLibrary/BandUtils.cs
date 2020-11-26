using System;
using System.Text;

namespace RfidLibrary
{
    public class BandUtils
    {
        /// <summary>
        /// 将布尔型的频点转换成byte型
        /// </summary>
        /// <param name="bands">频点数组</param>
        /// <returns>byte型频点数组</returns>
        public static byte[] BoolstoBytesBands(bool[] bands)
        {
            var sb = new StringBuilder();
            var bs = new byte[7];
            var k = 0;
            for (var i = 0; i < bands.Length; i++)
            {
                k++;
                sb.Append(bands[i] ? '1' : '0');

                if (k != 8) continue;
                var array = sb.ToString().ToCharArray();
                Array.Reverse(array);
                var b = Convert.ToUInt16(new string(array), 2);
                bs[(i + 1) / 8 - 1] = (byte)b;
                k = 0;
                sb.Clear();
            }
            return bs;
        }
    }
}
