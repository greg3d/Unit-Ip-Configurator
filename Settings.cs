using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net;
using System.Windows;
using System.Windows.Threading;

namespace ADC_IP_Configurator
{

    public class ADC
    {
        public string Name { get; private set; }
        public string Version { get; private set; }
        public IPAddress Ip { get; private set; }
        public int Port { get; private set; }
        public int UID { get; private set; }
        public byte[] MAC { get; private set; } = new byte[6];

        public bool NotSameSubnet { get; private set; }

        private IPAddress mask = IPAddress.Parse("255.255.255.0");
        public ADC(byte[] data)
        {
            UID = data[2] + data[3] * 256;

            Buffer.BlockCopy(data, 4, MAC, 0, 6);
            byte[] adr = new byte[4];
            Buffer.BlockCopy(data, 10, adr, 0, 4);
            Ip = new IPAddress(adr);
            Port = data[14] + data[15] * 256;
            byte[] ver = new byte[16];
            byte[] nam = new byte[15];
            Buffer.BlockCopy(data, 16, ver, 0, 16);
            Buffer.BlockCopy(data, 32, nam, 0, 15);

            Version = System.Text.Encoding.Default.GetString(ver);
            Name = System.Text.Encoding.Default.GetString(nam);
        }

        public void CompareSubnets(IPAddress cmpadr, IPAddress cmpmask)
        {
            uint address1 = BitConverter.ToUInt32(Ip.GetAddressBytes(), 0);
            uint mask1 = BitConverter.ToUInt32(mask.GetAddressBytes(), 0);

            uint address2 = BitConverter.ToUInt32(cmpadr.GetAddressBytes(), 0);
            uint mask2 = BitConverter.ToUInt32(cmpmask.GetAddressBytes(), 0);

            if ((address1 & mask1) == (address2 & mask2))
            {
                NotSameSubnet = false;
            }
            else
            {
                NotSameSubnet = true;
            }
        }

        public string[] ToTable
        {
            get
            {
                string mac = BitConverter.ToString(MAC);
                string warning = string.Empty;
                if (NotSameSubnet)
                {
                    warning = "(!)";
                }

                string[] returnString = { mac, Ip.ToString(), Name, Version, warning };
                return returnString;
            }
        }


        public override string ToString()
        {
            string mac = BitConverter.ToString(MAC);

            string warning = "";
            if (NotSameSubnet)
            {
                warning = "(!)";
            }

            string returnString = string.Empty;
            if (this.Name != string.Empty)
                returnString = String.Format("{1} {2} {3} {4} {5}", UID, mac, Name, Version, Ip.ToString(), warning);
            return returnString;
        }
    }

    class MagicAttribute : Attribute { }
    class NoMagicAttribute : Attribute { }

    [Magic]
    public abstract class BaseVM : INotifyPropertyChanged
    {
        protected virtual void RaisePropertyChanged(string propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName)); // некоторые из нас здесь используют Dispatcher, для безопасного взаимодействия с UI thread
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }

    [Magic]
    class Settings : BaseVM
    {
        public float Set1 { get; set; } // Set1
        public int PortDest { get; set; }
        public int PortMy { get; set; }
        public string NewAddr { get; set; } = "0.0.0.0";
        public Visibility SubnetWarning { get; set; } = Visibility.Hidden;
        public bool ChangeIPEnable { get; set; } = false;
        public bool DetectEnable { get; set; } = true;

        private ADC _cur;
        public ADC CurrentADC
        {
            get
            {
                return _cur;
            }
            set
            {
                if (value != _cur)
                {
                    _cur = value;
                    if (value != null)
                    {
                        NewAddr = value.Ip.ToString();
                        SubnetWarning = value.NotSameSubnet ? Visibility.Visible : Visibility.Hidden;
                        ChangeIPEnable = !value.NotSameSubnet;
                    }
                    else
                    {
                        NewAddr = "0.0.0.0";
                        SubnetWarning = Visibility.Hidden;
                        ChangeIPEnable = false;
                    }

                }

                RaisePropertyChanged("CurrentADC");
                //RaisePropertyChanged("NewAddr");
            }
        }

        //public List<ADC> ADCList { get; set; } = new List<ADC>();




        private ObservableCollection<ADC> adclist;
        public ObservableCollection<ADC> ADCList
        {
            get { return adclist; }
            set
            {
                if (value != adclist)
                    adclist = value;
                RaisePropertyChanged("ADCList");
            }
        }



        private static Settings instance;
        private Settings()
        {
            // defaults
            ADCList = new ObservableCollection<ADC>();
            PortMy = 12345;
            PortDest = 10000;
        }
        public static Settings getInstance()
        {
            if (instance == null)
            {
                instance = new Settings();
            }
            return instance;
        }

        public void ClearList()
        {
            ADCList.Clear();
        }

        public void AddNewADC(ADC adc)
        {
            Dispatcher.CurrentDispatcher.Invoke(() =>
            {
                ADCList.Add(adc);
            });

            RaisePropertyChanged("ADCList");
        }

    }
}
