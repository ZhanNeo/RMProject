using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SearcherIPPort
{
    public class SearchEventArgs : EventArgs
    {
        private string startIP;
        private string endIP;       
        private int port;

        public SearchEventArgs(string startIP, string endIP, int port)
        {
            this.startIP = startIP;
            this.endIP = endIP;
            this.port = port;
        }

        public string SartIP
        {
            get { return startIP; }
            set { startIP = value; }
        }

        public string EndIP
        {
            get { return endIP; }
            set { endIP = value; }
        }

        public int Port
        {
            get { return port; }
            set { port = value; }
        }

       
    }
}
