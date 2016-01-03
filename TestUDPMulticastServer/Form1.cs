using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace TestUDPMulticastServer
{
    public partial class Form1 : Form
    {
        public List<ClientData> clientDataList = new List<ClientData>();

        public Form1()
        {
            InitializeComponent();

            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.RealTime;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            Task t = Task.Factory.StartNew(new Action(Start));
            t.ContinueWith((task) => button1.Invoke(new Action(() => button1.Enabled = true)));
        }

        private void Start()
        {
            UdpClient udpclient = new UdpClient();

            IPAddress multicastaddress = IPAddress.Parse(Constants.MulticastAddress);
            udpclient.JoinMulticastGroup(multicastaddress);
            IPEndPoint remoteep = new IPEndPoint(multicastaddress, Constants.MulticastPort);

            Byte[] buffer = new byte[16];
            Array.Clear(buffer, 0, buffer.Length);

            Stopwatch sw = Stopwatch.StartNew();

            for (int i = 0; i < Constants.RepeatCount; i++)
            {
                while (sw.ElapsedMicroSeconds() < (i * Constants.IntervalMsec * 1000))
                {
                    ;
                }
                string s = i.ToString();
                Encoding.Unicode.GetBytes(s, 0, s.Length, buffer, 0);
                udpclient.Send(buffer, buffer.Length, remoteep);
            }

            //Debug.WriteLine(sw.ElapsedMicroSeconds());
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            HttpListener httpListener = new HttpListener();
            httpListener.Prefixes.Add(Constants.ClientDataUrl);


            Task.Factory.StartNew(() =>
            {
                httpListener.Start();
                while (httpListener.IsListening)
                {
                    ThreadPool.QueueUserWorkItem((c) =>
                    {
                        var ctx = c as HttpListenerContext;

                        string body;
                        ClientData clientData;
                        using (StreamReader sr = new StreamReader(ctx.Request.InputStream))
                        {
                            //body = sr.ReadToEnd();
                            XmlSerializer serializer = new XmlSerializer(typeof(ClientData));
                            clientData = (ClientData)serializer.Deserialize(sr);
                        }

                        if (clientDataList.Count == 0)
                        {
                            this.Invoke(new Action(() => 
                            {
                                timer1.Enabled = true;
                                timer1.Start();
                            }));
                            
                        }
                        clientDataList.Add(clientData);

                        try
                        {
                            byte[] buf = Encoding.UTF8.GetBytes("OK");
                            ctx.Response.ContentLength64 = buf.Length;
                            ctx.Response.OutputStream.Write(buf, 0, buf.Length);
                        }
                        catch { } // suppress any exceptions
                        finally
                        {
                            // always close the stream
                            ctx.Response.OutputStream.Close();
                        }
                    }, httpListener.GetContext());
                }
            });
        }

        private void AllDataTimer_Tick(object sender, EventArgs e)
        {
            timer1.Stop();

            StringBuilder sb = new StringBuilder();

            
            sb.AppendFormat("RepeatCount: ").AppendLine(Constants.RepeatCount.ToString());
            sb.Append("IntervalMsec: ").AppendLine(Constants.IntervalMsec.ToString());
            sb.AppendLine("---------------------------------------------------");
            sb.AppendLine();

            foreach (var cd in clientDataList)
            {
                sb.Append("IP:").AppendLine(cd.IpAddress);
                sb.Append("Avg:").AppendLine(cd.Avg.ToString());
                sb.Append("StdDev:").AppendLine(cd.StdDev.ToString());
                sb.Append("Min:").AppendLine(cd.Min.ToString());
                sb.Append("Max:").AppendLine(cd.Max.ToString());
                sb.Append("Perc:").AppendLine(cd.PercWithinDelta.ToString());
                sb.Append("OutOfOrder:").AppendLine(cd.OutOfOrder.ToString());
                sb.AppendLine();
            }

            sb.AppendLine();
            sb.AppendLine("---------------------------------------------------");
            sb.AppendLine();

            sb.Append("Total avg: ").AppendLine(clientDataList.Average(cd => cd.Avg).ToString());
            sb.Append("Total min: ").AppendLine(clientDataList.Min(cd => cd.Min).ToString());
            sb.Append("Total max: ").AppendLine(clientDataList.Max(cd => cd.Max).ToString());
            sb.Append("Total perc: ").AppendLine(clientDataList.Average(cd => cd.PercWithinDelta).ToString());
            sb.Append("Total out of order: ").AppendLine(clientDataList.Sum(cd => cd.OutOfOrder).ToString());

            textBox1.Text = sb.ToString();
        }
    }

    
}
