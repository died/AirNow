using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;


namespace AirNow
{
    /// <summary>
    /// Interaction logic for PopUp.xaml
    /// </summary>
    public partial class PopUp : UserControl
    {
        public static readonly DependencyProperty Pm25Property = DependencyProperty.Register("Pm25", typeof(string), typeof(PopUp));
        public static readonly DependencyProperty AqiProperty = DependencyProperty.Register("Aqi", typeof(string), typeof(PopUp));
        public PopUp()
        {
            InitializeComponent();
            DataContext = this;
        }

        public string Pm25
        {
            get => (string)GetValue(Pm25Property);
            set
            {
                SetValue(Pm25Property, value);
                int.TryParse(value, out var pm25Value);
                if (pm25Value >= 64) Img.Source = new BitmapImage(new Uri("pack://application:,,,/factory_red.ico", UriKind.RelativeOrAbsolute));
                else if (pm25Value >= 50) Img.Source = new BitmapImage(new Uri("pack://application:,,,/factory_orange.ico", UriKind.RelativeOrAbsolute));
                else if (pm25Value >= 35) Img.Source = new BitmapImage(new Uri("pack://application:,,,/factory_yellow.ico", UriKind.RelativeOrAbsolute));
                else if (pm25Value >= 20) Img.Source = new BitmapImage(new Uri("pack://application:,,,/factory_green.ico", UriKind.RelativeOrAbsolute));
                else Img.Source = new BitmapImage(new Uri("pack://application:,,,/factory_greenyellow.ico", UriKind.RelativeOrAbsolute));
            }
            //set => SetValue(Pm25Property, value);
        }

        public string Aqi
        {
            get => (string)GetValue(AqiProperty);
            set => SetValue(AqiProperty, value);
        }

    }
}
