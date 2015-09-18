using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;

namespace ServerConsole
{
    class Program
    {
        public static int Port { get; set; }
        public static string ServerIP;
        public static string ThisIP { get; set; }
        private static TcpClient tc;
        private static NetworkStream ns;
        private static bool iswork = false;
        private static TcpListener uc;
        private static NetworkStream ns2;
        private static bool iswork2 = true;
        private static TcpListener uc2;
        private static int blkSize =64*1024;//65536;
        private static List<string> selfIP = new List<string>();

        static void Main(string[] args)
        {
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
            ServerIP = ipa.ToString();
            Console.WriteLine("客户端启动");
            StartListen();
            while (true) { }
        }
        private static void StartListen()
        {
            iswork = true;
            Thread th = new Thread(new ThreadStart(listen));
            th.IsBackground = true;
            th.Start();
        }

        private static void listen()
        {
            uc = new TcpListener(12346);
            uc.Start();
            while (iswork)
            {
                //TcpClient client = uc.AcceptTcpClient();
                uc.BeginAcceptSocket(clientConnect, uc);//异步接受客户端的连接请求  clientConnect为连接的回调函数
                iswork = false;
                while (iswork == false) { }
            }
        }
        private static void clientConnect(IAsyncResult ar)
        {
            Socket socket = ((TcpListener)ar.AsyncState).EndAcceptSocket(ar);//获取服务端socket

            NetworkStream ns = new NetworkStream(socket);
            string recCmd;//接收到的命令
            byte[] recCmdByte = new byte[blkSize];
            int readNum = ns.Read(recCmdByte, 0, blkSize);
            recCmd = Encoding.Default.GetString(recCmdByte, 0, readNum);
            Console.WriteLine(recCmd);
            if (recCmd.StartsWith("sendFile:"))//发送文件
            {
                Console.Write("进入发送文件");
                IPEndPoint clientipe = (IPEndPoint)socket.RemoteEndPoint;
                String recIP = clientipe.Address.ToString();
                recCmd = recCmd.Substring(9);
                string[] temp = recCmd.Split(';');//接收的命令包含IP:Port和文件路径两段
                string ip = temp[0].Split(':')[0];
                string port = temp[0].Split(':')[1];
                string filePath = temp[1];

                //......发送文件
                string sendCmd = "rec:" + filePath;
                byte[] sendCmdByte = Encoding.Default.GetBytes(sendCmd);
                TcpClient sendTc = new TcpClient();
                sendTc.Connect(ip, int.Parse(port));
                NetworkStream sendNs = sendTc.GetStream();
                sendNs.Write(sendCmdByte, 0, sendCmdByte.Length);//发生命令，让客户端接收文件
                sendNs.Read(new byte[blkSize], 0, blkSize);//接收响应ok

                FileStream fs = new FileStream(filePath, FileMode.Open);
                byte[] readBlk = new byte[blkSize];
                while (true)
                {
                    int readSize = fs.Read(readBlk, 0, blkSize);
                    if (readSize == 0) break;
                    sendNs.Write(readBlk, 0, readSize);
                }
                fs.Close();
                sendNs.Close();
                Console.WriteLine(ThisIP + ":" + filePath + " 发送完成,接收IP： " + ip);

            }
            iswork = true;
        }
    }
}
