using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Net;
using System.Threading;

namespace ClientP2PHandle
{
    public class ClientP2P
    {
        public static int Port { get; set; }
        public static string ServerIP;
        public static string ThisIP { get; set; }
        private static TcpClient tc;
        private static UdpClient ut;
        private static NetworkStream ns;
        private static bool iswork = false;
        private static TcpListener uc;
        private static int blkSize = 65536;
        private static string text;

        public ClientP2P()
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

            StartListen();
        }
        private static void StartListen()
        {
            iswork = true;
            Thread th = new Thread(new ThreadStart(listen));
            th.IsBackground = true;
            th.Start();
        }
        public static byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }
        private static void listen()
        {
            uc = new TcpListener(12346);
            uc.Start();
            while(iswork)
            {
                uc.BeginAcceptSocket(clientConnect, uc);//异步接受客户端的连接请求  clientConnect为连接的回调函数
                iswork = false;
                while (iswork == false){ }
            }
        }
        private static void clientConnect(IAsyncResult ar)
        {
            Socket socket = ((TcpListener)ar.AsyncState).EndAcceptSocket(ar);
            ns = new NetworkStream(socket);
            
            byte[] recCmd = new byte[1024];
            ns.Read(recCmd, 0, 1024);
            text = System.Text.Encoding.Default.GetString(recCmd);
            if (text.StartsWith("sendFile:"))
            {
                IPEndPoint clientipe = (IPEndPoint)socket.RemoteEndPoint;
                String recIP = clientipe.Address.ToString();
                text = text.Substring(9);
                string[] temp = text.Split(';');
                string ip = temp[0].Split(':')[0];
                string port = temp[0].Split(':')[1];
                string filePath = temp[1];
                //发送文件
                string cmd = "rec:" + filePath;
                byte[] data1 = System.Text.Encoding.Default.GetBytes(cmd);
                TcpClient tc2 = new TcpClient();
                tc2.Connect(ip, int.Parse(port));
                NetworkStream ns2 = tc2.GetStream();
                ns2.Write(data1, 0, data1.Length);
                ns2.Read(new byte[1024], 0, 1024);
                FileStream fs = new FileStream(filePath.Replace("\0", ""), FileMode.Open);
                byte[] readBlk = new byte[blkSize];
                while (true)
                {
                    int readSize = fs.Read(readBlk, 0, blkSize);
                    if (readSize == 0) break;
                    ns2.Write(readBlk, 0, readSize);
                }
                fs.Close();
                ns2.Close();
               
                //回馈信息
                tc = new TcpClient(recIP, 12347);
                NetworkStream ns1 = tc.GetStream();
                StreamWriter sw = new StreamWriter(ns1);
                sw.WriteLine("finish:" + ThisIP + ":" + Port);
                sw.Flush();
                sw.Close();
            }
            else if (text.StartsWith("rec:"))
            {
                //接收文件
                IPEndPoint clientipe = (IPEndPoint)socket.RemoteEndPoint;
                String recIP = clientipe.Address.ToString();
                text = text.Substring(4);
                text = text.Replace("\0", "");
                FileStream fs = new FileStream(text, FileMode.Create);
                ns.Write(GetBytes("ok"), 0, GetBytes("ok").Length);
                byte[] recBlock = new byte[blkSize];
                while (true)
                {
                    int readSize = ns.Read(recBlock, 0, blkSize);
                    if (readSize == 0) break;
                    fs.Write(recBlock, 0, readSize);
                }
                fs.Close();

                //回馈信息
                tc = new TcpClient(ServerIP, 12347);
                NetworkStream ns1 = tc.GetStream();
                StreamWriter sw = new StreamWriter(ns1);
                sw.WriteLine("finish:" + ThisIP + ":" + Port);
                sw.Flush();
                sw.Close();
            }
            else
            {
                IPEndPoint clientipe = (IPEndPoint)socket.RemoteEndPoint;
                ServerIP = clientipe.Address.ToString();
            }
            iswork = true;
        }
    }
}
