using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SearcherIPPort;
using System.Threading;
using System.Net.Sockets;
using P2PDocDistribution;
using System.Net;
using WindowsFormsApplication2;
using System.IO;
using UnZip;

namespace Mainform
{
    public partial class Form1 : Form
    {
        //已扫描端口数目
        double scannedCount = 0;
        //正在运行的线程数目
        int runningThreadCount = 0;
        //最大工作线程数
        static int maxThread = 255;
        //默认IP范围设置
        string host = null;
        int startIP = 1;
        int endIP = 255;
        int port = 12346; 
        string addresIP = "192.168.40.";
        //发送文件默认IP范围设置
        int startIP1 = 1;
        int endIP1 = 255;
        string addresIP1 = "192.168.40.";
        //命令发送
        TcpClient tcpclient;
        //ipList
        List<string> ipList = new List<string>();
        //监听文件分发反馈
        TcpListener t2;
        P2PDoc p2p=new P2PDoc();
        //finishIP
        List<string> finishIP = new List<string>();
        //fileIP
        List<string> fileIP = new List<string>();
        //iplist
        List<string> iplist = new List<string>();
        //isFileFinish
        bool isFileFinish = false;
        string ThisIP;

        public Form1()
        {
            InitializeComponent();
            startListen();
            getSelf();
        }
        private void getSelf()
        {
            IPHostEntry ipe = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipa = null;
            foreach (IPAddress ip in ipe.AddressList)
            {
                iplist.Add(ip.ToString());
            }
            foreach (IPAddress ip in ipe.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                    continue;
                ipa = ip;
                break;
            }
            ThisIP = ipa.ToString();
        }
        //监听结束传输文件反馈信息
        private void startListen()
        {
            t2 = new TcpListener(12347);
            t2.Start();
            Thread th = new Thread(new ThreadStart(listenFinish));
            th.IsBackground = true;
            th.Start();
        }
        private void listenFinish()
        {
            while (true)
            {
                Socket sock2 = t2.AcceptSocket();
                NetworkStream ns2 = new NetworkStream(sock2);

                byte[] recCmdByte = new byte[4096];
                int readNum = ns2.Read(recCmdByte, 0, 4096);
                string temp = Encoding.Default.GetString(recCmdByte, 0, readNum);
                if (temp.StartsWith("finish:"))
                {
                    temp = temp.Replace("finish:", "");
                    string tempIP = temp.Split(';')[0];
                    string sendIP = temp.Split(';')[3];
                    
                    if (p2p.excUser.Find(delegate(string str) { return str.Equals(sendIP); }) != null)
                    p2p.removeList(tempIP,sendIP);
                    else p2p.removeList(tempIP,ThisIP+":12346");
                    UpdateTextBox(txtReceive, temp);
                    if (p2p.userlist.Count==0&&p2p.excUser.Count==0)
                    {
                       // MessageBox.Show(temp.Split(';')[2] + "分发完毕");
                        String name = temp.Split(';')[2];
                        if (temp.Split(';')[2].EndsWith(".zip")==true)
                        {
                            name=name.Remove(name.LastIndexOf("."));
                        }
                        UpdateTextBox(txtReceive, name + "分发完毕");
                        isFileFinish = true;
                    }
                }

            }
        }


        //扫描
        private void button1_Click(object sender, EventArgs e)
        {
            frmSet frm = new frmSet();
            frm.SearcheEvent += new frmSet.SearchDeletegate(set_SearcheEvent);
            frm.ShowDialog();
        }
        void set_SearcheEvent(object sender, SearchEventArgs e)
        {
            startIP = Convert.ToInt32(e.SartIP.Split('.')[3]);
            endIP = Convert.ToInt32(e.EndIP.Split('.')[3]);
            addresIP = e.SartIP.Substring(0, e.SartIP.LastIndexOf('.') + 1);
            port = e.Port;
            label1.Text = string.Format("扫描IP段指定端口 {0}-{1}:{2}",
                addresIP + startIP, endIP, port);
        }
        private void button2_Click(object sender, EventArgs e)
        {
            backgroundWorker1.CancelAsync();
            if (!backgroundWorker1.IsBusy)
            {
                listBox1.Items.Clear();
                scannedCount = 0;
                runningThreadCount = 0;
                backgroundWorker1.RunWorkerAsync();
            }
        }
        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            double total = Convert.ToDouble(endIP - startIP + 1);
            for (int ip = startIP; ip <= endIP; ip++)
            {
                if (backgroundWorker1.CancellationPending)
                {
                    e.Cancel = true;
                    break;
                }
                host = addresIP + ip.ToString();
                if (!iplist.Contains(host))
                {
                    Thread thread = new Thread(() => Scan(host, port));
                    thread.IsBackground = true;
                    thread.Start();
                    runningThreadCount++;
                }
                scannedCount++;
                UpdateLabText(labTip, string.Format("正在扫描第：{0}台，共{1}台，进度：{2}%",
                       scannedCount, total, Convert.ToInt32((scannedCount / total) * 100)));
                backgroundWorker1.ReportProgress(Convert.ToInt32((scannedCount / total) * 100));
                Thread.Sleep(10);
                while (runningThreadCount >= maxThread) ;

            }
        }
        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            labTip.Text = "扫描完成！";
        }
        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
        }
        //扫描
        public void Scan(string m_host, int m_port)
        {
            TcpClient tc = new TcpClient();
            tc.SendTimeout = tc.ReceiveTimeout = 2000;
            try
            {
                IAsyncResult oAsyncResult = tc.BeginConnect(m_host, m_port, null, null);
                oAsyncResult.AsyncWaitHandle.WaitOne(1000, true);

                if (tc.Connected)
                {
                    UpdateListBox(listBox1, m_host + ":" + m_port.ToString());
                    ipList.Add(m_host);
                }
            }
            catch (System.Net.Sockets.SocketException e)
            {
                MessageBox.Show("Port {0} is closed", host.ToString());
                Console.WriteLine(e.Message);
            }
            finally
            {
                tc.Close();
                tc = null;
                runningThreadCount--;
            }
        }
        //扫描动态更新控件
        delegate void SetLabCallback(Label lb, string text);
        public void UpdateLabText(Label lb, string text)
        {
            try
            {
                if (lb.InvokeRequired)
                {
                    SetLabCallback d = new SetLabCallback(UpdateLabText);
                    this.Invoke(d, new object[] { lb, text });
                }
                else
                {
                    lb.Text = text.Trim();
                }
            }
            catch
            {
            }
        }
        delegate void SetTextCallback(TextBox txtBox, string text);
        private void UpdateTextBox(TextBox txtBox, string text)
        {
            if (txtBox.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(UpdateTextBox);
                this.Invoke(d, new object[] { txtBox, text });
            }
            else
            {
                txtBox.AppendText(text + Environment.NewLine);
            }
        }
        delegate void SetListCallback(ListBox lstBox, string text);
        private void UpdateListBox(ListBox lstBox, string text)
        {
            if (lstBox.InvokeRequired)
            {
                SetListCallback d = new SetListCallback(UpdateListBox);
                this.Invoke(d, new object[] { lstBox, text });
            }
            else
            {
                lstBox.Items.Add(text.Trim());
            }
        }

     
        //文件分发
        private void button8_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Multiselect = true;
            openFileDialog1.FileName = Environment.SpecialFolder.MyComputer.ToString();
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                for (int i = 0; i < openFileDialog1.FileNames.Length;i++)
                    listBox4.Items.Add(openFileDialog1.FileNames[i]); 
            }  
        }
        private void button9_Click(object sender, EventArgs e)
        {
            if (listBox2.Items.Count == 0)
            {
                MessageBox.Show("未选择IP");
            }
            else if (listBox2.Items[0].ToString().Split(':')[1] != "12346")
            {
                MessageBox.Show("端口错误");
            }
            else if (listBox4.Items.Count == 0)
            {
                MessageBox.Show("请选择文件");
            }
            else
            {
                txtReceive.AppendText("文件开始传输，请耐心等待" + Environment.NewLine);
                fileIP.Clear();
                for (int i = 0; i < listBox2.Items.Count; i++)
                {
                    fileIP.Add(listBox2.Items[i].ToString());
                }
                Thread th = new Thread(() => send(fileIP));
                th.IsBackground = true;
                th.Start();
            }
        }
        private void send(List<string> user)
        {
            for (int i = 0; i < listBox4.Items.Count; i++)
            {
                isFileFinish = false;
                p2p = new P2PDoc(user);              
                p2p.P2PSendFile(listBox4.Items[i].ToString());
                while (!isFileFinish) ;
            }
        }
        private void button10_Click(object sender, EventArgs e)
        {
            frmSet frm = new frmSet();
            frm.SearcheEvent += new frmSet.SearchDeletegate(set_SearcheEvent1);
            frm.ShowDialog();
        }
        void set_SearcheEvent1(object sender, SearchEventArgs e)
        {
            startIP1 = Convert.ToInt32(e.SartIP.Split('.')[3]);
            endIP1 = Convert.ToInt32(e.EndIP.Split('.')[3]);
            addresIP1 = e.SartIP.Substring(0, e.SartIP.LastIndexOf('.') + 1);
        }
        private void listBox4_DoubleClick(object sender, EventArgs e)
        {
            if (listBox4.SelectedItem != null)
            {
                listBox4.Items.Remove(listBox4.SelectedItem);
            }
        }

        //已选IP
        private void listBox1_DoubleClick(object sender, EventArgs e)
        {
            listBox2.Items.Add(listBox1.SelectedItem.ToString());
        }
        private void listBox2_DoubleClick(object sender, EventArgs e)
        {
            if (listBox2.SelectedItem != null)
            {
                listBox2.Items.Remove(listBox2.SelectedItem);
            }
        }
        private void button11_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < listBox1.Items.Count; i++)
            {
                if (listBox1.Items[i].ToString().Length != 0)
                {
                    listBox2.Items.Add(listBox1.Items[i].ToString());
                }
            }
        }
        private void button12_Click(object sender, EventArgs e)
        {
            listBox2.Items.Clear();
        }
        private void button4_Click(object sender, EventArgs e)
        {
            listBox2.Items.Add(textBox1.Text);
        }

        //命令广播
        private void button6_Click(object sender, EventArgs e)
        {
            int count = listBox2.Items.Count;
            int i = 0;
            if (count == 0)
            {
                MessageBox.Show("当前可连接的客户端数目为0");
                return;
            }
            button6.Enabled = false;
            for (i = 0; i < count; i++)
            {
                string ip = null;
                try
                {
                    string[] strarr = listBox2.Items[i].ToString().Split(':');
                    ip = strarr[0];
                }
                catch
                {
                    MessageBox.Show("得到地址错误!!!!");
                    continue;
                }
                int port = 12346;
                IPAddress ipadd = IPAddress.Parse(ip);
                IPEndPoint IPendp = new IPEndPoint(ipadd, port);
                try
                {
                    tcpclient = new TcpClient(ip, port);
                    tcpclient.SendTimeout = tcpclient.ReceiveTimeout = 3000;

                    if (tcpclient.Connected)
                    {
                        try
                        {
                            if (tcpclient.Connected)
                            {
                                NetworkStream ns = tcpclient.GetStream();
                                if (ns.CanWrite)
                                {
                                    Byte[] sendBytes = Encoding.Default.GetBytes("bcdedit /bootsequence {36ccff7f-5339-11e4-91be-97a799449345} \n shutdown -r -t 0");
                                    ns.Write(sendBytes, 0, sendBytes.Length);
                                }
                                else
                                {
                                    MessageBox.Show("一键关机出现错误");
                                    ns.Close();
                                    continue;
                                }
                                txtReceive.AppendText("客户端" + ip + "重启成功，如果需要对之前的继续操作，请等待一分钟" + "\n");
                            }
                            else
                            {
                                txtReceive.AppendText("客户端" + ip + "由于网络原因重启失败\n");
                                continue;
                            }
                            tcpclient.Close();
                        }
                        catch
                        {
                            txtReceive.AppendText("客户端" + ip + "由于网络原因重启失败\n");
                            continue;
                        }
                    }
                }
                catch { txtReceive.AppendText("客户端" + ip + "由于网络原因重启失败\n"); continue; }
            }
            button6.Enabled = true;
        }
        private void button7_Click(object sender, EventArgs e)
        {
            int count = listBox2.Items.Count;
            int i = 0;
            if (count == 0)
            {
                MessageBox.Show("当前可连接的客户端数目为0");
                return;
            }
            button7.Enabled = false;
            for (i = 0; i < count; i++)
            {
                string ip = null;
                try
                {
                    string[] strarr = listBox2.Items[i].ToString().Split(':');
                    ip = strarr[0];
                }
                catch
                {
                    MessageBox.Show("得到地址错误!!!!");
                    continue;
                }
                int port = 12346;
                IPAddress ipadd = IPAddress.Parse(ip);
                IPEndPoint IPendp = new IPEndPoint(ipadd, port);
                try
                {
                    tcpclient = new TcpClient(ip, port);
                    tcpclient.SendTimeout = tcpclient.ReceiveTimeout = 3000;

                    if (tcpclient.Connected)
                    {
                        try
                        {
                            if (tcpclient.Connected)
                            {
                               // btnConnect.Enabled = true;
                                NetworkStream ns = tcpclient.GetStream();
                                if (ns.CanWrite)
                                {
                                    Byte[] sendBytes = Encoding.Default.GetBytes("shutdown -s -t 0");
                                    ns.Write(sendBytes, 0, sendBytes.Length);
                                }
                                else
                                {
                                    MessageBox.Show("一键关机出现错误");
                                    ns.Close();
                                    continue;
                                }
                                txtReceive.AppendText("客户端" + ip + "关闭成功，请等待一分钟" + "\n");
                            }
                            else
                            {
                                txtReceive.AppendText("客户端" + ip + "由于网络原因关闭失败\n");
                                continue;
                            }
                            tcpclient.Close();
                        }
                        catch
                        {
                            txtReceive.AppendText("客户端" + ip + "由于网络原因关闭失败\n");
                            continue;
                        }
                    }
                }
                catch { txtReceive.AppendText("客户端" + ip + "由于网络原因关闭失败\n"); continue; }
            }
            button7.Enabled = true;
        }
        private void button5_Click(object sender, EventArgs e)
        {
            int count = listBox2.Items.Count;
            int i = 0;
            if (count == 0)
            {
                MessageBox.Show("当前可连接的客户端数目为0");
                return;
            }
            button5.Enabled = false;
            for (i = 0; i < count; i++)
            {
                string ip = null;
                try
                {
                    string[] strarr = listBox2.Items[i].ToString().Split(':');
                    ip = strarr[0];
                }
                catch
                {
                    MessageBox.Show("得到地址错误!!!!");
                    continue;
                }
                int port = 12346;
                IPAddress ipadd = IPAddress.Parse(ip);
                IPEndPoint IPendp = new IPEndPoint(ipadd, port);
                try
                {
                    tcpclient = new TcpClient(ip, port);
                    tcpclient.SendTimeout = tcpclient.ReceiveTimeout = 3000;

                    if (tcpclient.Connected)
                    {
                        try
                        {
                            if (tcpclient.Connected)
                            {
                                NetworkStream ns = tcpclient.GetStream();
                                if (ns.CanWrite)
                                {
                                    Byte[] sendBytes = Encoding.Default.GetBytes(textBox2.Text);
                                    ns.Write(sendBytes, 0, sendBytes.Length);
                                }
                                else
                                {
                                    MessageBox.Show("一键关机出现错误");
                                    ns.Close();
                                    continue;
                                }
                                txtReceive.AppendText("客户端" + ip + "执行命令成功，如果需要对之前的继续操作，请等待一分钟" + "\n");
                            }
                            else
                            {
                                txtReceive.AppendText("客户端" + ip + "由于网络原因执行命令失败\n");
                                continue;
                            }
                            tcpclient.Close();
                        }
                        catch
                        {
                            txtReceive.AppendText("客户端" + ip + "由于网络原因执行命令失败\n");
                            continue;
                        }
                    }
                }
                catch { txtReceive.AppendText("客户端" + ip + "由于网络原因重启失败\n"); continue; }
            }
            button5.Enabled = true;
        }

        //MAC地址
        private void button3_Click(object sender, EventArgs e)
        {
            MacAdress mc;
            mc = new MacAdress(ipList, p2p.userlist, p2p.sendedUser, p2p.excUser);
            mc.ShowDialog();
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem != null)
            {
                listBox2.Items.Add(listBox1.SelectedItem.ToString());
            }
        }


        //文件解压缩
        private void button15_Click(object sender, EventArgs e)
        {
            UpdateTextBox(txtReceive, "解压缩开始，请稍等！");
            for (int i = 0; i < listBox3.Items.Count; i++)
            {
                Zip unzip = new Zip();
                String name = listBox3.Items[i].ToString();
                try
                {
                    if (name.StartsWith("FIL") == true && name.EndsWith("zip") == true)
                    {
                        name = name.Substring(4);
                        String newname = name.Remove(name.LastIndexOf("."));//取文件最后的一个.
                        Thread th = new Thread(() => unzip.unZip(name, newname));
                        th.IsBackground = true;
                        th.Start();
                        Thread listen = new Thread(() => listenzip(name, th, "unzip"));
                        listen.IsBackground = true;
                        listen.Start();
                    }
                    else
                    {
                        MessageBox.Show("选择文件" + name + "不合法，注意目前只支持zip格式的解压缩！！");

                    }
                }
                catch
                {
                    MessageBox.Show("解压缩过程中出现问题请稍后再试！");
                }
            }
        }
        private void button10_Click_1(object sender, EventArgs e)
        {
            string path = "";
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.ShowNewFolderButton = false;
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                path = folderBrowserDialog.SelectedPath;
            }
            listBox3.Items.Add("DIR " + path);  
        }
        private void button14_Click(object sender, EventArgs e)
        {
            listBox3.Items.Clear();
        }
        private void button17_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Multiselect = true;
            openFileDialog1.FileName = Environment.SpecialFolder.MyComputer.ToString();
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                for (int i = 0; i < openFileDialog1.FileNames.Length; i++)
                    listBox3.Items.Add("FIL " + openFileDialog1.FileNames[i]);
            }
        }
        private void button16_Click(object sender, EventArgs e)
        {
            UpdateTextBox(txtReceive, "压缩开始，请稍等！");
            for (int i = 0; i < listBox3.Items.Count; i++)
            {
                try
                {
                    Zip unZip = new Zip();
                    String name = listBox3.Items[i].ToString();
                    if (name.StartsWith("DIR ") == true)
                    {
                        name = name.Substring(4);
                        Thread th = new Thread(() => unZip.ZipFileFromDirectory(name, name + ".zip", 5, 50 * 1024 * 1024));
                        th.IsBackground = true;
                        th.Start();
                        Thread listen = new Thread(() => listenzip(name, th, "zip"));
                        listen.IsBackground = true;
                        listen.Start();
                    }
                    else
                        if (name.StartsWith("FIL") == true)
                        {
                            name = name.Substring(4);
                            Thread th = new Thread(() => unZip.ZipFile(name, name + ".zip", 5, 50 * 1024 * 1024));
                            th.IsBackground = true;
                            th.Start();
                            Thread listen = new Thread(() => listenzip(name, th, "zip"));
                            listen.IsBackground = true;
                            listen.Start();
                        }
                }
                catch 
                {
                    MessageBox.Show("压缩过程中出现问题，请重试！！");
                }


            }
        }
        private void button13_Click(object sender, EventArgs e)
        {
            UpdateTextBox(txtReceive, "发送文件开始.先压缩文件夹，请稍等！");
            Thread th = new Thread(() =>allsendfolder());
            th.IsBackground = true;
            th.Start();
            
        }
        public void allsendfolder()
        { 
            if (listBox2.Items.Count == 0)
                {
                    MessageBox.Show("未选择IP");
                }
                else if (listBox2.Items[0].ToString().Split(':')[1] != "12346")
                {
                    MessageBox.Show("端口错误");
                }
            else
            {
                    for (int i = 0; i < listBox3.Items.Count; i++)
                    {
                        try
                        {
                            Zip unZip = new Zip();
                            String name = listBox3.Items[i].ToString();
                            if (name.StartsWith("DIR ") == true)
                            {
                                name = name.Substring(4);
                                unZip.ZipFileFromDirectory(name, name + ".zip", 5, 50 * 1024 * 1024);    
                            }
                        }
                        catch
                        {
                            MessageBox.Show("压缩过程中出现问题，请重试！！");
                        }
                    } 
                    UpdateTextBox(txtReceive,"文件压缩操作完成,等待进入发送阶段！");
                    fileIP.Clear();
                    for (int j = 0; j < listBox2.Items.Count; j++)
                    {
                        fileIP.Add(listBox2.Items[j].ToString());
                    }
                    Thread th = new Thread(() => sendfolder(fileIP));
                    th.IsBackground = true;
                    th.Start();
                } 
        }
        public void listenzip(String name, Thread thread, String status)
        {
            while (thread.IsAlive == true) Thread.Sleep(100);
            if (status.Equals("zip"))
                UpdateTextBox(txtReceive, name + " 压缩操作完成！");
            else
               UpdateTextBox(txtReceive, name + " 解压缩操作完成！");

        }
        private void sendfolder(List<string> user)
        {
            UpdateTextBox(txtReceive, "进入发送阶段，请稍后！");
            for (int i = 0; i < listBox3.Items.Count; i++)
            {
                UpdateTextBox(txtReceive, "发送文件夹" + listBox3.Items[i].ToString().Substring(4) + ".zip" + "开始");
                isFileFinish = false;
                p2p = new P2PDoc(user);
                String name = listBox3.Items[i].ToString().Substring(4) + ".zip";
                p2p.P2PSendFile(name);
                while (!isFileFinish) ;
            } 
            UpdateTextBox(txtReceive, "进行删除分发产生的客户端临时压缩文件，请稍后1分钟再进行其他操作"); 
            foreach (String hostclient in user)
                {
                    try
                    {
                        int portclient = 12346;
                        String ipclient = hostclient.Split(':')[0];
                        IPAddress ipadd = IPAddress.Parse(ipclient);
                        IPEndPoint IPendp = new IPEndPoint(ipadd, portclient);
                        TcpClient deltcpclient=new TcpClient(ipclient,portclient);
                        deltcpclient.SendTimeout = deltcpclient.ReceiveTimeout = 10000;
                        if (deltcpclient.Connected)
                        {
                                if (deltcpclient.Connected)
                                {
                                    NetworkStream ns = deltcpclient.GetStream();
                                    if (ns.CanWrite)
                                    {
                                        String delcmd=null;
                                        for (int j = 0; j < listBox3.Items.Count; j++)
                                        {
                                            delcmd +="del "+listBox3.Items[j].ToString().Substring(4) + ".zip"+'\n';
                                        }
                                        Byte[] sendBytes = Encoding.Default.GetBytes(delcmd);
                                        ns.Write(sendBytes, 0, sendBytes.Length);
                                    }
                                    else
                                    {
                                        MessageBox.Show("删除客户端压缩文件出现错误");
                                        ns.Close();
                                        continue;
                                    }

                                }
                                else
                                {
                                    MessageBox.Show("删除客户端压缩文件出现错误");
                                    continue;
                                }
                                deltcpclient.Close();
                        } 
                       
                        }
                    catch { MessageBox.Show("删除客户端压缩文件出现错误"); ; continue; }
                }
            System.Threading.Thread.Sleep(5000);
            UpdateTextBox(txtReceive, "删除临时压缩文件完成,文件夹分发操作全部完成！"); 
        }

        private void listBox2_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
