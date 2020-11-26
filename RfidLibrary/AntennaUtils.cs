using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RfidLibrary
{
    public class AntennaUtils
    {
        /// <summary>
        /// 根据整数型天线号获取1-4号可用天线
        /// </summary>
        /// <param name="ant">可用天线号</param>
        /// <returns>返回长度为4布尔型数组，代表1-4号天线是否可用</returns>
        public static bool[] GetBoolsAntenna(int ant)
        {
            var ants = new bool[4];
            var bina = TextEncoder.ByteToString((byte)ant);
            if (bina.Length < 4) return null;
            var newBina = bina.Reverse().ToArray();
            for (var i = 0; i < 4; i++)
            {
                if (newBina[i] != '1') continue;
                ants[i] = true;
            }
            return ants;
        }

        /// <summary>
        /// 根据1-4号天线可用的布尔型数组获取Byte类型值
        /// </summary>
        /// <param name="ants">1-4号天线可用的布尔型数组</param>
        /// <returns>返回可用天线Byte值</returns>
        public static byte GetByteAntenna(bool[] ants)
        {
            var sb = new StringBuilder();
            foreach (var t in ants)
            {
                sb.Append(t ? '1' : '0');
            }
            var array = sb.ToString().ToCharArray();
            Array.Reverse(array);
            var b = Convert.ToUInt16(new string(array), 2);
            return (byte)b;
        }
    }
}
