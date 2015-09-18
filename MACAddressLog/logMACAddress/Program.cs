using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net.NetworkInformation;

namespace logMACAddress
{
    class Program
    {
        static void Main(string[] args)
        {
            FileStream fs = new FileStream("map.dat", FileMode.Append,FileAccess.Write);
            StreamWriter sw = new StreamWriter(fs);
            String input = Console.ReadLine();
            sw.WriteLine(input + ":" + GetMacAddress());
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
