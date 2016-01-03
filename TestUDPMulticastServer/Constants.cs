using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestUDPMulticastServer
{
   public static class Constants
    {
        public const int RepeatCount = 1000;
        public const int IntervalMsec = 3;
        public const string MulticastAddress = "239.0.0.222";
        public const int MulticastPort = 2222;
        public const string ClientDataUrl = "http://localhost:2223/";
    }
}
