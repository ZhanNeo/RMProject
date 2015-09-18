using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace MACAddressLog
{
    public class MacAddressLog
    {
        String locationMap;
        public MacAddressLog():this("map.dat")//默认读取“map.dat”文件获取位置分布
        {
        }
        public MacAddressLog(String filePath)
        {
            FileStream fs = new FileStream(filePath, FileMode.Open);
            StreamReader sr = new StreamReader(fs);
            locationMap = sr.ReadToEnd();
            sr.Close();
            fs.Close();
        }
        public int getRowNum(String MACAddress)
        {
            int ind=locationMap.IndexOf(MACAddress);
            if (ind == -1)
            {
                Console.WriteLine("该mac地址不存在");
                return -1;
            }
            else return Int32.Parse(locationMap.Substring(ind - 4, 2));
        }
        public int getColNum(String MACAddress)
        {
            int ind = locationMap.IndexOf(MACAddress);
            if (ind == -1)
            {
                Console.WriteLine("该mac地址不存在");
                return -1;
            }
            else return Int32.Parse(locationMap.Substring(ind - 2, 1));
        }

    }
}
