using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net.NetworkInformation;

namespace changeMACAddress
{
    class Program
    {
        static void Main(string[] args)
        {
            FileStream fs = new FileStream("map.dat", FileMode.Open);
            StreamReader sr = new StreamReader(fs);
            String locationMap = sr.ReadToEnd();
            String input = Console.ReadLine();
            locationMap.Replace(locationMap.Substring(locationMap.IndexOf(input) + 4, 12), GetMacAddress());
            sr.Close();
            fs.Close();
            fs = new FileStream("map.dat", FileMode.Create, FileAccess.Write);
            StreamWriter sw = new StreamWriter(fs);
            sw.Write(locationMap);
            sw.Close();
            fs.Close();
        }
        public static String GetMacAddress()
        {
            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
            return interfaces[0].GetPhysicalAddress().ToString();
        }
    }
}
