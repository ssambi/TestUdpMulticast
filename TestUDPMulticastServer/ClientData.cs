using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestUDPMulticastServer
{
    public class ClientData
    {
        public string IpAddress { get; set; }
        public double Avg { get; set; }
        public double StdDev { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }
        public double PercWithinDelta { get; set; }
        public int OutOfOrder { get; set; }
    }
}
