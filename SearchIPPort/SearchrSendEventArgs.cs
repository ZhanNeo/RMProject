using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SearcherIPPort
{
    public class SearchrSendEventArgs:EventArgs
    {
        private string ip;
        public string IP
        {
            get { return ip; }
            set { ip = value; }
        }

        private int port;
        public int Port
        {
            get { return port; }
            set { port = value; }
        }
        public SearchrSendEventArgs(string ip, int port)
        {
            this.ip = ip;
            this.port = port;
        }  
    }
}
