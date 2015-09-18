using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Diagnostics;
using UnZip;

namespace ClientConsole
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
        private static int blkSize = 1024 * 64;//65536;

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
            //Thread th2 = new Thread(new ThreadStart(listen2));
            //th2.IsBackground = true;
            //th2.Start();
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
        public static void clientConnect(IAsyncResult ar)
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
                String recIP = clientipe.Address.ToString();//获取服务端IP
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
            else if (recCmd.StartsWith("rec:"))//接收文件
            {
                Console.Write("进入接收文件");
                IPEndPoint clientipe = (IPEndPoint)socket.RemoteEndPoint;
                String recIP = clientipe.Address.ToString();
                recCmd = recCmd.Substring(4);
                FileStream fs = new FileStream(recCmd, FileMode.Create);
                byte[] sendCmdByte = Encoding.Default.GetBytes("ok");//回应ok

                ns.Write(sendCmdByte, 0, sendCmdByte.Length);

                byte[] recBlock = new byte[blkSize];
                while (true)
                {
                    int readSize = ns.Read(recBlock, 0, blkSize);
                    if (readSize == 0) break;
                    fs.Write(recBlock, 0, readSize);
                }
                fs.Close();

                Console.WriteLine(ThisIP + ":" + recCmd + " 接收完成,来自IP: " + recIP);
                Console.WriteLine("服务器主机IP:" + ServerIP);
                if (recCmd.EndsWith(".zip") == true)
                {
                    Zip unzip = new Zip();
                    String name;
                    name = "" + recCmd;
                    Console.WriteLine(name + "解压缩操作开始！");
                    String newname = name.Remove(name.LastIndexOf("."));//取文件最后的一个.
                 /*   
                  * Thread th = new Thread(() => unzip.unZip(name, newname));
                    th.IsBackground = true;
                    th.Start();
                    Thread listen = new Thread(() => listenzip(name, th, "unzip"));
                    listen.IsBackground = true;
                    listen.Start();
                  */
                    try
                    {
                        unzip.unZip(name, newname);
                        Console.WriteLine(name + "解压缩操作完成！");
                    }
                    catch 
                    {
                        Console.WriteLine(name + "解压缩操作失败，请检查相关设置！");
                    }
                }
                TcpClient tc = new TcpClient(ServerIP, 12347);//向主服务端发生完成消息
                NetworkStream tns = tc.GetStream();
                byte[] finMesByte = Encoding.Default.GetBytes("finish:" + ThisIP + ":" + Port + ";" + "RecFileComplete" + ";" + recCmd + ";" + recIP + ":" + Port);
                tns.Write(finMesByte, 0, finMesByte.Length);
                tns.Flush();
                tns.Close();
               
            }
            else
            {
                if (recCmd.StartsWith("\0") || recCmd.Equals(""))
                {
                    Console.WriteLine("已经被连接");
                    IPEndPoint clientipe = (IPEndPoint)socket.RemoteEndPoint;
                    ServerIP = clientipe.Address.ToString();
                    iswork = true;
                    Console.Write("命令出错");
                    return;
                }
                byte[] cmdTemp = Encoding.Default.GetBytes(recCmd);
                string cmd = Encoding.Default.GetString(cmdTemp).Trim('\0');
                Console.Write("命令执行" + cmd);
                Process p = new Process();
                p.StartInfo.FileName = "cmd.exe";
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.CreateNoWindow = true;
                p.Start();
                p.StandardInput.WriteLine(cmd);
                p.StandardInput.WriteLine("exit");
                p.WaitForExit();
                p.Close();
            }
            iswork = true;
        }
       /* public  static void listenzip(String name, Thread thread, String status)
        {
            while (thread.IsAlive == true) Thread.Sleep(100);
            if (status.Equals("unzip"))
                Console.WriteLine(name + "解压缩操作完成！");

        }*/


    } 

}
