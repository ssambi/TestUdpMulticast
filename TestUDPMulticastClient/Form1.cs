using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;
using TestUDPMulticastServer;

namespace TestUDPMulticastClient
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.RealTime;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Text += " " + GetLocalIPv4(NetworkInterfaceType.Ethernet);

            Task.Factory.StartNew(() =>
            {
                UdpClient client = new UdpClient();

                IPEndPoint localEp = new IPEndPoint(IPAddress.Any, Constants.MulticastPort);

                client.Client.Bind(localEp);

                IPAddress multicastaddress = IPAddress.Parse(Constants.MulticastAddress);
                client.JoinMulticastGroup(multicastaddress);

                Stopwatch sw = Stopwatch.StartNew();

                int receivedCount = 0;
                List<long> elapsedMicrosecs = new List<long>();

                int lastIntData = -1;
                int outOfOrderCount = 0;

                while (receivedCount < Constants.RepeatCount)
                {
                    Byte[] data = client.Receive(ref localEp);

                    string strData = Encoding.Unicode.GetString(data);
                    int intData = int.Parse(strData);
                    if (lastIntData != (intData - 1))
                    {
                        outOfOrderCount++;
                    }
                    lastIntData = intData;

                    if (receivedCount == 0)
                        sw.Restart();
                    
                    elapsedMicrosecs.Add(sw.ElapsedMicroSeconds());

                    receivedCount++;

                    
                    //Debug.WriteLine(strData);
                    //Debug.WriteLine(strData);
                    //Debug.WriteLine(sw.ElapsedMicroSeconds());
                }

                long[] timesBetween = elapsedMicrosecs.Select((el, idx) =>
                {
                    if (idx == 0)
                        return Constants.IntervalMsec * 1000;
                    return el - elapsedMicrosecs[idx - 1];
                }).ToArray();

                int intervalMicroSec = Constants.IntervalMsec * 1000;
                string msg = string.Format(@"Avg:{0}
StdDev:{1}
Min:{2}
Max:{3}
%({5}-{6}):{4}
OutOfOrder:{7}",
timesBetween.Average(),
timesBetween.StdDev(),
timesBetween.Min(),
timesBetween.Max(),
timesBetween.Count(tb => tb < (intervalMicroSec+100) && tb > (intervalMicroSec-100)) / (double)Constants.RepeatCount,
intervalMicroSec-100,
intervalMicroSec+100,
outOfOrderCount);
                textBox1.Invoke(new Action( () => textBox1.Text = msg ));

                ClientData clientData = new ClientData()
                {
                    IpAddress = GetLocalIPv4(NetworkInterfaceType.Ethernet),
                    Avg = timesBetween.Average(),
                    StdDev = timesBetween.StdDev(),
                    Min = timesBetween.Min(),
                    Max = timesBetween.Max(),
                    PercWithinDelta = timesBetween.Count(tb => tb < (intervalMicroSec + 100) && tb > (intervalMicroSec - 100)) / (double)Constants.RepeatCount,
                    OutOfOrder = outOfOrderCount
                };

                WebClient wc = new WebClient();

                XmlSerializer serializer = new XmlSerializer(typeof(ClientData));
                using (StreamWriter streamWriter = new StreamWriter(wc.OpenWrite(Constants.ClientDataUrl)))
                {
                    serializer.Serialize(streamWriter, clientData);
                }                    
                
            });
        }

        internal static string GetLocalIPv4(NetworkInterfaceType _type)
        {
            string output = "";
            foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (item.NetworkInterfaceType == _type && item.OperationalStatus == OperationalStatus.Up)
                {
                    IPInterfaceProperties adapterProperties = item.GetIPProperties();

                    if (adapterProperties.GatewayAddresses.FirstOrDefault() != null)
                    {
                        foreach (UnicastIPAddressInformation ip in adapterProperties.UnicastAddresses)
                        {
                            if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            {
                                output = ip.Address.ToString();
                            }
                        }
                    }
                }
            }

            return output;
        }
    }
}
