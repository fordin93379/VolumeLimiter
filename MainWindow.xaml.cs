using System;
using System.Threading;
using System.Windows;
using NAudio.CoreAudioApi;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;


namespace VolumeLimiter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private NotifyIcon notifyIcon;
        private float volumeLevel;
        public MainWindow()
        {
            
            InitializeComponent();
            
            // creating tray icon
            notifyIcon = new NotifyIcon();
            notifyIcon.Icon = new Icon(System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/VolumeLimiter;component/icon.ico")).Stream);
            notifyIcon.Visible = true; 
            notifyIcon.MouseClick += NotifyIcon_MouseClick;
            
            
            // Removing from taskbar and folding window
            Hide();
            ShowInTaskbar = false;
            Topmost = true; // making always top window
            
            // removing frames
            Window window = System.Windows.Application.Current.MainWindow;
            Deactivated += MainWindow_Deactivated;
            window.AllowsTransparency = true;
            window.WindowStyle = WindowStyle.None; 

            // setting pos in left down corner
            window.Left = SystemParameters.PrimaryScreenWidth - window.Width;
            window.Top = SystemParameters.PrimaryScreenHeight - window.Height - (SystemParameters.PrimaryScreenHeight - SystemParameters.WorkArea.Height);

            //Я знаю, что это страшный костыль, но у меня нет других идей, как сжать это в один файл)
            if (!File.Exists("/VolumeLimiter.dll.config"))
                using (FileStream file = new FileStream("/VolumeLimiter.dll.config", FileMode.OpenOrCreate))
                {
                    byte[] buffer = Encoding.Default.GetBytes("<configuration>\n    <appSettings>\n        <add key=\"VolumeLimit\" value=\"50\" />\n    </appSettings>\n</configuration>");
                    file.Write(buffer, 0, buffer.Length);
                }
            

            //loading volume limit, and if it exists set slider pos
            string? parameterValue = ConfigurationManager.AppSettings["VolumeLimit"];
            if(parameterValue is string)
            {
                Slider.Value = double.Parse(parameterValue) * 10d; 
            }
            
            //starting thread with volume level checker
            new Thread(() =>
            {
                while (true)
                {
                    if(GetMasterVolume() > volumeLevel)
                    {
                        SetMasterVolume(volumeLevel);
                    }
                    Thread.Sleep(2);
                }
            }).Start();
        }
        
        
        //Click handler, on click hides or shows window
        private void NotifyIcon_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (Visibility.Hidden == Visibility)
                {
                    Show();
                    Activate();
                }
                else
                    Hide();
            }
            
        }
        
        //if user press on other window hiding our window
        private void MainWindow_Deactivated(object sender, EventArgs e)
        {
            Hide();
        }
        
        //updating slider position
        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            volumeLevel = (float)Slider.Value * 0.1f;
            
        }
        
        //Getting volume level form api
        public float GetMasterVolume()
        {
            // Create an instance of the MMDeviceEnumerator
            MMDeviceEnumerator enumerator = new MMDeviceEnumerator();

            // Get the default audio endpoint device (which represents the speakers)
            MMDevice speakers = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

            // Get the audio endpoint volume control
            AudioEndpointVolume volumeControl = speakers.AudioEndpointVolume;

            // Get the current volume level (0.0 to 1.0)
            return volumeControl.MasterVolumeLevelScalar;
            
        }
        
        //Setting system volume
        public void SetMasterVolume(float volume)
        {
            // Create an instance of the MMDeviceEnumerator
            MMDeviceEnumerator enumerator = new MMDeviceEnumerator();

            // Get the default audio endpoint device (which represents the speakers)
            MMDevice speakers = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

            // Get the audio endpoint volume control
            AudioEndpointVolume volumeControl = speakers.AudioEndpointVolume;

            // Set the volume level (0.0 to 1.0)
            volumeControl.MasterVolumeLevelScalar = volume; 
        }
        //On slider release, saving state
        private void slider_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            config.AppSettings.Settings["VolumeLimit"].Value = volumeLevel.ToString();
            config.Save();
            ConfigurationManager.RefreshSection("appSettings");
        }

        
    }
}