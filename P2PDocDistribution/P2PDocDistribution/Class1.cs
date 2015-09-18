using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.IO;

namespace P2PDocDistribution
{
    public class P2PDoc
    {
        public List<string> userlist = new List<string>();   //无资源
        public List<string> sendedUser = new List<string>(); //有资源
        public List<string> excUser = new List<string>();    //正在发送的
        public static int Port;
        public static string ThisIP;
        static int startIP = 1;
        static int endIP = 255;
        static double scannedCount = 0;
        static int runningThreadCount = 0;
        static int maxThread = 100;

        public bool isFinish()
        {
            if (userlist.Count == 0 && excUser.Count == 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }




        public P2PDoc()
        {

        }

        public P2PDoc(List<string> user)
        {
            userlist.Clear();
            sendedUser.Clear();
            excUser.Clear();
            //服务器本身信息
            IPHostEntry ipe = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipa = null;
            foreach (IPAddress ip in ipe.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                    continue;
                ipa = ip;
                break;
            }
            ThisIP = ipa.ToString();
            Port = 12346;
            string hostUser = ipa.ToString() + ":" + "12346";
            sendedUser.Add(hostUser);
            
            //获取所有机器信息
           // Work(ipHead, start, end);
            
            userlist.AddRange(user);
        }

        //文件自动分发
        public void P2PSendFile(string filename)
        {
            while (userlist.Count != 0)
            {
                string sendTemp = sendedUser[0];
                string IP = sendTemp.Split(':')[0];
                string port = sendTemp.Split(':')[1];
                string temp = userlist[0];
                excUser.Add(temp);
                userlist.Remove(temp);
                excUser.Add(sendTemp);
                sendedUser.Remove(sendTemp);

                Thread th1 = new Thread(() => clientConnect(IP, int.Parse(port), temp,filename));
                th1.Start();

                while (sendedUser.Count == 0) ;

            }
        }
        private static void clientConnect(string IP, int port, string sendIP,string filename)
        {

            string filePath = filename;
            byte[] b = System.Text.Encoding.Default.GetBytes("sendFile:" + sendIP + ";" + filePath);
            TcpClient tc2 = new TcpClient(IP, port);
            NetworkStream ns2 = tc2.GetStream();
            ns2.Write(b, 0, b.Length);
            ns2.Close();
        }
        
     
        public void removeList(string ip1,string ip2)
        {
            excUser.Remove(ip1);
            excUser.Remove(ip2);
            sendedUser.Add(ip1);
            sendedUser.Add(ip2);
        }
    
    }
}
