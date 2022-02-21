using System;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;

namespace ADC_IP_Configurator
{
    public class RealData
    {
        public float[] X;
        public float[] Y;
        public int Count = 0;

        public RealData(int n)
        {
            X = new float[n];
            Y = new float[n];
        }
    }

    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        //private Canvas2DD gdi;
        private Settings settings;

        public MainWindow()
        {
            InitializeComponent();

            settings = Settings.getInstance();
            DataContext = settings;

        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            settings.ClearList();
            var processRead = true;
            settings.DetectEnable = false;

            IPEndPoint ip = null;

            var networks = NetworkInterface.GetAllNetworkInterfaces();

            foreach (var iface in networks)
            {

                if (iface.OperationalStatus == OperationalStatus.Up)
                {

                    //Trace.Write(String.Format("{0} ", iface.Name));

                    foreach (var adres in iface.GetIPProperties().UnicastAddresses)
                    {
                        if (!IPAddress.IsLoopback(adres.Address) && adres.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            settings.DetectEnable = false;
                            var startPoint = new IPEndPoint(adres.Address, settings.PortMy);
                            var client = new UdpClient(startPoint);

                            var task1 = Task.Run(() =>
                            {
                                try
                                {
                                    long pcount = 0;
                                    while (processRead)
                                    {
                                        int psize;
                                        var result = client.BeginReceive(null, null);
                                        result.AsyncWaitHandle.WaitOne(500);
                                        if (result.IsCompleted)
                                        {
                                            byte[] data = client.EndReceive(result, ref ip); //принимаем посылку
                                            psize = data.Length;
                                            pcount++;
                                            //Trace.WriteLine("№" + pcount.ToString() + " ip: " + ip.Address + ": " + ip.Port + " - Размер полученной кучи: " + psize);

                                            if (psize > 40)
                                            {

                                                var cur = new ADC(data);
                                                cur.CompareSubnets(adres.Address, adres.IPv4Mask);

                                                App.Current.Dispatcher.Invoke(() =>
                                                {
                                                    settings.AddNewADC(cur);
                                                });


                                            }

                                        }
                                        else
                                        {
                                            processRead = false;
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Trace.WriteLine(ex.Message);
                                }

                            });

                            var task2 = Task.Run(() =>
                            {

                                var endpoint = new IPEndPoint(IPAddress.Broadcast, settings.PortDest);
                                byte[] senddata = new byte[16];
                                senddata[0] = 0x01;
                                client.Send(senddata, 16, endpoint);
                                task1.Wait();
                                client.Close();
                                settings.DetectEnable = true;

                            });
                            //Trace.Write(String.Format("/ {0} {1} {2} / ", adres.Address, adres.Address, adres.IPv4Mask));
                        }
                    }

                    //Trace.WriteLine("");
                }
            }
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            byte[] data = new byte[16];
            data[0] = 0x02;
            data[1] = 0x00;
            data[2] = BitConverter.GetBytes(settings.CurrentADC.UID)[0];
            data[3] = BitConverter.GetBytes(settings.CurrentADC.UID)[1];
            Buffer.BlockCopy(settings.CurrentADC.MAC, 0, data, 4, 6);

            var adr = IPAddress.Parse(settings.NewAddr).GetAddressBytes();

            Buffer.BlockCopy(adr, 0, data, 10, 4);
            data[14] = BitConverter.GetBytes(settings.CurrentADC.Port)[0];
            data[15] = BitConverter.GetBytes(settings.CurrentADC.Port)[1];

            var client = new UdpClient();
            var endpoint = new IPEndPoint(settings.CurrentADC.Ip, settings.CurrentADC.Port);
            //Trace.WriteLine(BitConverter.ToString(data));

            client.Send(data, 16, endpoint);

            client.Close();

            MessageBox.Show("IP Change Done!", "Done");


        }

        private void ADClistBox_Selected(object sender, RoutedEventArgs e)
        {
            settings.NewAddr = settings.CurrentADC.Ip.ToString();
        }

    }
}
