using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace SearcherIPPort
{
    public partial class frmSet : Form
    {
        public delegate void SearchDeletegate(object sender,SearchEventArgs e);
        public event SearchDeletegate SearcheEvent;
        public frmSet()
        {
            InitializeComponent();
        }

        private void Form2_Load(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            
            //验证IP
            string startIP=txtStartIP.Text.Trim();
            string endIP=txtEndIP.Text.Trim();

            if (string.IsNullOrEmpty(startIP) || string.IsNullOrEmpty(endIP))
            {
                MessageBox.Show("IP地址不能为空！");
                return;
            }

            if (!IPCheck(startIP))
            {
                MessageBox.Show("开始地址错误！");
                txtStartIP.Focus();
                return;
            }

            if (!IPCheck(endIP))
            {
                MessageBox.Show("结束地址错误");
                txtEndIP.Focus();
                return;
            }

            if (!startIP.Substring(0, startIP.LastIndexOf('.') + 1).Equals(
                endIP.Substring(0, endIP.LastIndexOf('.') + 1)))
            {
                MessageBox.Show("IP地址前3位必须相同！");
                return;
            }

                        
            //验证端口
            int port=80;

            if (!string.IsNullOrEmpty(txtPort.Text.Trim()))
            {
                port = Convert.ToInt32(txtPort.Text.Trim());
            }
            else
            {
                MessageBox.Show("端口错误！");
                txtPort.Focus();
                return;
            }

            SendValue(startIP, endIP, port);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        /// <summary>
        /// 正规则试验IP地址
        /// </summary>
        /// <param name="IP"></param>
        /// <returns></returns>
        public bool IPCheck(string IP)
        {
            string num = "(25[0-5]|2[0-4]\\d|[0-1]\\d{2}|[1-9]?\\d)";

            return Regex.IsMatch(IP, ("^" + num + "\\." + num + "\\." + num + "\\." + num + "$"));
        }


        /// <summary>
        /// 事件传值，将值传回
        /// </summary>
        /// <param name="StartIP"></param>
        /// <param name="EndIP"></param>
        /// <param name="Port"></param>
        private void SendValue(string StartIP, string EndIP, int Port)
        {
            if (SearcheEvent != null)
            {
                SearcheEvent(this,new SearchEventArgs(StartIP,EndIP,Port));
            }
        }

        private void txtPort_KeyPress(object sender, KeyPressEventArgs e)
        {
             e.Handled = !(Char.IsNumber(e.KeyChar) || e.KeyChar == (char)8 || e.KeyChar == '.');  
             if (!e.Handled) (sender as TextBox).Tag = (sender as TextBox).Text;//记录最后一次正确输入

        }

        private void label6_Click(object sender, EventArgs e)
        {

        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            this.txtPort.Text = "12345";
        }

        private void txtPort_TextChanged(object sender, EventArgs e)
        {

        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            this.txtPort.Text = "12346";
        }
    }
}
