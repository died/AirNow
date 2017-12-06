using System;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Newtonsoft.Json;

namespace AirNow
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public SerialPort MySerialPort = new SerialPort();
        public string Pm25Value;
        public string AqiValue;
        public MainWindow()
        {
            InitializeComponent();
            BtnRefresh.Content = Char.ConvertFromUtf32(81);
            
            GetAqi();
            SetTimer();
            GetComPorts();
        }

        /// <summary>
        /// Get Com Port List
        /// </summary>
        public void GetComPorts()
        {
            var list = SerialPort.GetPortNames();
            if (list.Any())
            {
                PortSelect.ItemsSource = list;

                if (list.Length == 1)
                {
                    PortSelect.SelectedIndex = 0;
                }
            }
        }

        private void SetTimer()
        {
            DispatcherTimer timer = new DispatcherTimer {Interval = TimeSpan.FromMinutes(59)};
            timer.Tick += GetAqiTimer;
            timer.Start();
        }

        public void GetAqiTimer(object sender, EventArgs e)
        {
            GetAqi();
        }

        public void GetAqi()
        {
            var url = "https://api.waqi.info/feed/here/?token=886256f608f0f43cc8c0056eed865a23732760ec";
            var result = GetHtmlUtf8(url);
            Console.WriteLine(result);
            dynamic json = JsonConvert.DeserializeObject(result);
            if (json.status != null && json.status == "ok")
            {
                if (json.data != null && json.data.aqi!=null)
                {
                    Aqi.Content = json.data.aqi;
                    SetAqi(json.data.aqi.ToString());
                    if (json.data.iaqi == null) return;
                    var ia = json.data.iaqi;
                    AqiCo.Content = ia.co.v ?? "";
                    Humidity.Content = ia.h.v ?? "";
                    AqiNo2.Content = ia.no2.v ?? "";
                    AqiO3.Content = ia.o3.v ?? "";
                    Pressure.Content = ia.p.v ?? "";
                    AqiPm10.Content = ia.pm10.v ?? "";
                    AqiPm25.Content = ia.pm25.v ?? "";
                    AqiSo2.Content = ia.so2.v ?? "";
                    Temperature.Content = ia.t.v ?? "";
                    //loc
                    if (json.data.city != null && json.data.time != null)
                    {
                        var loc = json.data.city.name;
                        var updateTime = json.data.time.s;
                        if (!string.IsNullOrEmpty(loc.ToString()) && !string.IsNullOrEmpty(updateTime.ToString()))
                            InfoLabel.Content = $"AQI City: {loc} Time: {updateTime}";
                    }
                }
            }
            else
            {
                InfoLabel.Content = "Load AQI Info Fail";
            }
        }

        /// <summary>
        /// Open and start read com port
        /// </summary>
        /// <param name="com"></param>
        public void ReadComPort(string com)
        {
            if(MySerialPort.IsOpen) MySerialPort.Close();
            MySerialPort = new SerialPort(com)
            {
                BaudRate = 2400,
                Parity = Parity.None,
                StopBits = StopBits.One,
                DataBits = 8,
                Handshake = Handshake.None,
                RtsEnable = true
            };

            MySerialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);

            try
            {
                MySerialPort.Open();
            }
            catch (UnauthorizedAccessException ex)
            {
                //Access to the port 'COM5' is denied
                Console.WriteLine(ex.Message);
                InfoLabel.Content = $"{ex.Message}";
            }
            catch (Exception ex2)
            {
                Console.WriteLine(ex2.Message);
                InfoLabel.Content = $"{ex2.Message}";
            }
        }

        /// <summary>
        /// Handler for get data from Airnow
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataReceivedHandler(object sender,SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            string indata = sp.ReadLine();
            if (!string.IsNullOrEmpty(indata))
            {
                var ar = indata.Split('\t');
                if (ar.Length == 5)
                {
                    double.TryParse(ar[4], out var hcho);
                    SetPm25(ar[1]);
                    ShowAirNowData(ar[0], ar[1], ar[2], ar[3], $"{hcho * 0.001:0.000}");
                }
            }
            //debug
            //Console.WriteLine(indata);
        }

        private void SetPm25(string text)
        {
            Pm25Value = string.IsNullOrEmpty(text) ? string.Empty : text;
            //Dispatcher.BeginInvoke(new Action(() => { PopUp.Pm25Label.Content = text; }));
            Dispatcher.BeginInvoke(new Action(() => { PopUp.Pm25 = text; }));
            SetToolTipText();
        }

        private void SetAqi(string text)
        {
            AqiValue = string.IsNullOrEmpty(text) ? string.Empty : text;
            Dispatcher.BeginInvoke(new Action(() => { PopUp.Aqi = text; }));
            SetToolTipText();
        }

        //Show airnow data
        private void ShowAirNowData(string pm1,string pm25,string pm10,string co2,string hcho)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                SetPm25(pm25);
                Pm1.Content = pm1;
                Pm25.Content = pm25;
                Pm10.Content = pm10;
                Co2.Content = co2;
                Hcho.Content = hcho;
                if (WindowState == WindowState.Minimized)
                {
                    int.TryParse(pm25, out var pm25Value);
                    if (pm25Value >= 64) Tbi.IconSource = new BitmapImage(new Uri("pack://application:,,,/factory_red.ico", UriKind.RelativeOrAbsolute));
                    else if (pm25Value >= 50) Tbi.IconSource = new BitmapImage(new Uri("pack://application:,,,/factory_orange.ico", UriKind.RelativeOrAbsolute));
                    else if (pm25Value >= 35) Tbi.IconSource = new BitmapImage(new Uri("pack://application:,,,/factory_yellow.ico", UriKind.RelativeOrAbsolute));
                    else if (pm25Value >= 20) Tbi.IconSource = new BitmapImage(new Uri("pack://application:,,,/factory_green.ico", UriKind.RelativeOrAbsolute));
                    else Tbi.IconSource = new BitmapImage(new Uri("pack://application:,,,/factory_greenyellow.ico", UriKind.RelativeOrAbsolute));
                }
            }));
        }

        private void SetToolTipText()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                var text = (string.IsNullOrEmpty(Pm25Value) ? "" :$"PM 2.5: {Pm25Value}")+
                    (string.IsNullOrEmpty(AqiValue) ? "" : $", AQI: {AqiValue}"); 
                Tbi.ToolTipText = text;
            }));
        }

        //stop read when keyin enter or esc
        private void OnKeyDownHandler(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return || e.Key == Key.Escape)
            {
                MySerialPort.Close();
            }
        }

        #region override

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                Hide();
            }

            base.OnStateChanged(e);
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            Tbi.IconSource = null;

            base.OnClosing(e);
        }
        #endregion

        #region webtool
        /// <summary>
        /// Get url's html content
        /// </summary>
        /// <param name="url"></param>
        /// <param name="encode"></param>
        /// <returns></returns>
        public static string GetHtml(string url, Encoding encode)
        {
            string res = null;
            var wRq = (HttpWebRequest)WebRequest.Create(url);
            wRq.Method = "GET";
            wRq.UserAgent = "Mozilla/5.0+(Windows+NT+6.1;+WOW64)+AppleWebKit/536.11+(KHTML,+like+Gecko)+Chrome/20.0.1132.57+Safari/536.11";
            //add for .net 4.5+ ?
            //wRq.ServerCertificateValidationCallback += (sender, cert, chain, error) => cert.GetCertHashString() == "xxxxxxxxxxxxxxxx";
            try
            {
                using (WebResponse wRs = wRq.GetResponse())
                {
                    using (var s = wRs.GetResponseStream())
                    {
                        if (s != null)
                        {
                            using (var sr = new StreamReader(s, encode))
                            {
                                res = sr.ReadToEnd();
                            }
                        }
                    }
                }
                return res;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Get html by UTF8 edcoding
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static string GetHtmlUtf8(string url)
        {
            return GetHtml(url, Encoding.UTF8);
        }
        #endregion

        private void Tbi_OnTrayPopupOpen(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("popup opened.");
        }

        //put select port try to read
        private void PortSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ReadComPort(PortSelect.SelectedValue.ToString());
        }

        private void BtnRefresh_OnClick(object sender, RoutedEventArgs e)
        {
            BtnRefresh.IsEnabled = false;
            GetComPorts();
            BtnRefresh.IsEnabled = true;
        }

        /// <summary>
        /// Bring window back
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Tbi_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            if (!IsVisible)
            {
                Show();
            }

            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
            }

            Activate();
            Topmost = true;  // important
            Topmost = false; // important
            Focus();         // important

            Tbi.IconSource = new BitmapImage(new Uri("pack://application:,,,/factory_icon.ico", UriKind.RelativeOrAbsolute));
        }
    }
}
