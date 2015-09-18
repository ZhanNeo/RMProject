using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using MACAddressLog;

namespace WindowsFormsApplication2
{
    public partial class MacAdress : Form
    {
       private int cols_num=8;//实验室电脑列数
       private int rows_num = 13;//实验室电脑行数
       private int w = 66, h = 26, xg = 7, yg = 4, xf = 30, yf = 12;
       private List<string> ipList = new List<string>();
       private List<string> macList = new List<string>();

       private List<string> userlistIP;   //无资源:红色
       private List<string> sendedUserIP; //有资源:蓝色
       private List<string> excUserIP;    //正在发送的:黄色

       [DllImport("Iphlpapi.dll")]
       static extern int SendARP(Int32 DestIP, Int32 SrcIP, ref Int64 MacAddr, ref Int32 PhyAddrLen);
       [DllImport("Ws2_32.dll")]
       static extern Int32 inet_addr(string ipaddr);

       public MacAdress()
       {
           InitializeComponent();
       }
       public MacAdress(List<string> il, List<string> ul, List<string> sl, List<string> el)
        {
            InitializeComponent();
            userlistIP = ul;
            sendedUserIP = sl;
            excUserIP = el;

            for (int i = 0; i < il.Count; i++)
            {
                macList.Add(GetMacAddress(il[i]).Replace("-",""));
                ipList.Add(il[i]);
            }
        }
        public static string GetMacAddress(string RemoteIP)
        {
            StringBuilder macAddress = new StringBuilder();
            try
            {
                Int32 remote = inet_addr(RemoteIP);
                Int64 macInfo = new Int64();
                Int32 length = 6;
                SendARP(remote, 0, ref macInfo, ref length);
                string temp = Convert.ToString(macInfo, 16).PadLeft(12, '0').ToUpper();
                int x = 12;
                for (int i = 0; i < 6; i++)
                {
                    if (i == 5)
                    {
                        macAddress.Append(temp.Substring(x - 2, 2));
                    }
                    else
                    {
                        macAddress.Append(temp.Substring(x - 2, 2) + "-");
                    }
                    x -= 2;
                }
                return macAddress.ToString();
            }
            catch
            {
                return macAddress.ToString();
            }
        }

        private void drawEmptyMap()
        {
            
            Graphics g = CreateGraphics();
            SolidBrush sbw = new SolidBrush(Color.White);
            g.FillRectangle(sbw, xf + 80 * 3 + xg, yf + yg, w * 2 + xg * 2, h);
            for (int i = 0; i < rows_num; i++)//绘制格子
                for (int j = 0; j < cols_num; j++)
                {
                    if (((i == 3 && j == 6) || (i == 8 && j == 6) || (i == 12 && j > 1))) { }
                    else
                    {
                        g.FillRectangle(sbw, xf + 80 * j + xg, yf + 34 * (i + 1) + yg, w, h);
                    }
                }
        }
        private void drawGrid(int row,int col,string IP)
        {
            Graphics g = CreateGraphics();
            Font drawFont = new Font("Arial", 13, FontStyle.Bold);
            SolidBrush greenBrush = new SolidBrush(Color.Green);
            SolidBrush redBrush = new SolidBrush(Color.Red);
            SolidBrush blackBrush = new SolidBrush(Color.Black);
            SolidBrush yellowBrush = new SolidBrush(Color.Yellow);
            SolidBrush blueBrush = new SolidBrush(Color.Blue);
            if (row == -1)
            {
                g.FillRectangle(greenBrush, xf + 80 * 3 + xg, yf + yg, w * 2 + xg * 2, h);
                if (userlistIP.Find(delegate(string str) { return str.Equals("192.168." + IP + ":12346"); }) != null)
                    g.FillRectangle(redBrush, xf + 80 * 3 + xg, yf + yg, w * 2 + xg * 2, h);
                if (sendedUserIP.Find(delegate(string str) { return str.Equals("192.168." + IP + ":12346"); }) != null)
                    g.FillRectangle(blueBrush, xf + 80 * 3 + xg, yf + yg, w * 2 + xg * 2, h);
                if (excUserIP.Find(delegate(string str) { return str.Equals("192.168." + IP + ":12346"); }) != null)
                    g.FillRectangle(yellowBrush, xf + 80 * 3 + xg, yf + yg, w * 2 + xg * 2, h);
                g.DrawString(IP, drawFont, blackBrush, 326, 19);
            }
            else
            {
                g.FillRectangle(greenBrush, xf + 80 * col + xg, yf + 34 * (row + 1) + yg, w, h);
                if (userlistIP.Find(delegate(string str) { return str.Equals("192.168." + IP + ":12346"); }) != null)
                    g.FillRectangle(redBrush, xf + 80 * col + xg, yf + 34 * (row + 1) + yg, w, h);
                if (sendedUserIP.Find(delegate(string str) { return str.Equals("192.168." + IP + ":12346"); }) != null)
                    g.FillRectangle(blueBrush, xf + 80 * col + xg, yf + 34 * (row + 1) + yg, w, h);
                if (excUserIP.Find(delegate(string str) { return str.Equals("192.168." + IP + ":12346"); }) != null)
                    g.FillRectangle(yellowBrush, xf + 80 * col + xg, yf + 34 * (row + 1) + yg, w, h);
                g.DrawString(IP, drawFont, blackBrush, xf + 80 * col + xg + 5, yf + 34 * (row + 1) + yg + 3);
            }
        }

        private void MacAdress_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            drawEmptyMap();
            MacAddressLog ma = new MacAddressLog();
            for(int i=0;i<macList.Count;i++)
            {
                int row = ma.getRowNum(macList[i]) - 1;
                int col = ma.getColNum(macList[i]) - 1;
                drawGrid(row, col, ipList[i].Substring(8));
            }
        }
     
    }
}
