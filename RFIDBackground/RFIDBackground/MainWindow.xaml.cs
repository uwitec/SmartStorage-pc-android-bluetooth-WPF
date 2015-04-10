using MahApps.Metro;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RFIDBackground
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private String readInfo = "";
        private Boolean closeFlag = true;
        private Boolean serialFlag = true;
        private Storyboard closeAnimate;
        private Theme currentTheme;
        private Accent currentAccent;
        private NotifyIcon notifyIcon = null;
        private SerialPort serialPort;
        private Thread RcvThread;
        public MainWindow()
        {
            InitializeComponent();
            InitialTray();
            closeAnimate = (Storyboard)this.Resources["CloseStoryBoard"];
            List<String> ComList;
            if (GetComList(out ComList) > 0)
            {
                foreach (String com in ComList)
                {
                    SerialComboBox.Items.Add(com);
                }
                SerialComboBox.SelectedIndex = 0;
            }
            BaudComboBox.Items.Add("4800");
            BaudComboBox.Items.Add("9600");
            BaudComboBox.Items.Add("19200");
            BaudComboBox.Items.Add("38400");
            BaudComboBox.Items.Add("57600");
            BaudComboBox.Items.Add("115200");
            BaudComboBox.SelectedIndex = 1;
            serialPort = new SerialPort();
        }

        private void InitialTray()
        {
            notifyIcon = new NotifyIcon();
            notifyIcon.BalloonTipText = "RFID智能仓储";
            notifyIcon.Text = "RFID智能仓储";
            notifyIcon.Icon = Properties.Resources.RFID;
            notifyIcon.Visible = true;
            notifyIcon.ShowBalloonTip(500);
            notifyIcon.MouseClick += new System.Windows.Forms.MouseEventHandler(notifyIcon_MouseClick);
            System.Windows.Forms.MenuItem exit = new System.Windows.Forms.MenuItem("退出");
            exit.Click += new EventHandler(exit_Click);
            System.Windows.Forms.MenuItem[] childen = new System.Windows.Forms.MenuItem[] { exit };
            notifyIcon.ContextMenu = new System.Windows.Forms.ContextMenu(childen);
            this.StateChanged += new EventHandler(SysTray_StateChanged);
        }

        private void SysTray_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized)
            {
                this.Visibility = Visibility.Hidden;
                this.WindowState = WindowState.Normal;
            }
        }

        private void exit_Click(object sender, EventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        private void notifyIcon_MouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                if (this.Visibility == Visibility.Visible)
                {
                    this.Visibility = Visibility.Hidden;
                }
                else
                {
                    this.WindowState = WindowState.Normal;
                    this.Visibility = Visibility.Visible;
                    this.Activate();
                }
            }
        }

        private void SetButton_Click(object sender, RoutedEventArgs e)
        {
            var flyout = this.Flyouts.Items[0] as Flyout;
            if (flyout == null)
            {
                return;
            }
            if (serialFlag)
            {
                if (!flyout.IsOpen)
                {
                    List<String> ComList;
                    SerialComboBox.Items.Clear();
                    if (GetComList(out ComList) > 0)
                    {
                        foreach (String com in ComList)
                        {
                            SerialComboBox.Items.Add(com);
                        }
                        SerialComboBox.SelectedIndex = 0;
                    }
                }
                flyout.IsOpen = !flyout.IsOpen;
            }
            else
            {
                StatusTextBlock.Text = "现在不能操作！";
            }
        }

        public Int32 GetComList(out List<String> ComList)
        {
            Microsoft.Win32.RegistryKey keyCom = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("Hardware\\DeviceMap\\SerialComm");
            if (keyCom != null)
            {
                string[] sSubKeys = keyCom.GetValueNames();
                ComList = new List<string>();
                foreach (string sName in sSubKeys)
                {
                    ComList.Add((string)keyCom.GetValue(sName));
                }
                return ComList.Count;
            }
            else
            {
                ComList = null;
                return 0;
            }
        }

        public void serialOpen()
        {
            currentAccent = ThemeManager.DefaultAccents.First(x => x.Name == "Orange");
            ThemeManager.ChangeTheme(System.Windows.Application.Current, currentAccent, currentTheme);
            StatusRing.IsActive = true;
            serialFlag = false;
            StatusTextBlock.Text = "打开成功";
            SwitchButton.Count = "关闭串口";
        }

        public void serialClose()
        {
            currentAccent = ThemeManager.DefaultAccents.First(x => x.Name == "Blue");
            ThemeManager.ChangeTheme(System.Windows.Application.Current, currentAccent, currentTheme);
            StatusRing.IsActive = false;
            serialFlag = true;
            StatusTextBlock.Text = "关闭成功";
            SwitchButton.Count = "打开串口";
        }

        private void SwitchButton_Click(object sender, RoutedEventArgs e)
        {
            if (serialFlag)
            {
                if (OpenSerialPort())
                {
                    RcvThread = new Thread(ReceiveThread);
                    RcvThread.IsBackground = true;
                    RcvThread.Start((object)serialPort);
                    serialOpen();
                }
                else
                {
                    StatusTextBlock.Text = "打开失败";
                }
            }
            else
            {
                if (CloseSerialPort())
                {
                    serialClose();
                    RcvThread.Abort();
                    RcvThread.Join();
                }
                else
                {
                    StatusTextBlock.Text = "关闭失败";
                }
            }
        }

        private void ReceiveThread(object arg)
        {
            SerialPort serialPort = (SerialPort)arg;
            List<Byte> RcvFrame = new List<Byte>();
            List<Byte> WrtFrame = new List<Byte>();
            Byte Data;
            Boolean IsStart = false;
            try
            {
                while (true)
                {
                    Data = (Byte)serialPort.ReadByte();
                    switch (Data)
                    {
                        case 0xFF:
                            if (IsStart)
                            {
                                RcvFrame.Clear();
                            }
                            else
                            {
                                IsStart = true;
                            }
                            break;
                        case 0xFE:
                            if (IsStart)
                            {
                                IsStart = false;
                            }
                            break;
                        default:
                            if (IsStart)
                            {
                                RcvFrame.Add(Data);
                            }
                            break;
                    }

                    if (!IsStart && RcvFrame.Count > 0)
                    {
                        Byte[] readBuffer = RcvFrame.ToArray();
                        String UID = Encoding.UTF8.GetString(readBuffer);
                        RcvFrame.Clear();
                        Byte[] temp;
                        Byte[] GoodsNumberByte;
                        Byte[] GoodsNameByte;
                        Byte[] GoodsCountByte;
                        Byte[] GoodsDescriptionByte;
                        Byte GoodsNumberByteLength, GoodsNameByteLength, GoodsCountByteLength, GoodsDescriptionByteLength;
                        String GoodsNumber, GoodsName, GoodsCount, GoodsDescription;

                        if (!App.StorageDB.DetailedRegisterTable.Rows.Contains(UID))
                        {
                            App.StorageDB.GetTable("DetailedRegisterTable");
                            GoodsNumber = "无此商品信息";
                            GoodsName = "无此商品信息";
                            GoodsCount = "无此商品信息";
                            GoodsDescription = "无此商品信息";
                        }
                        else
                        {
                            App.StorageDB.GetTable("DetailedRegisterTable");
                            GoodsNumber = ((DataRow)(App.StorageDB.DetailedRegisterTable.Rows.Find(UID)))[1].ToString();
                            GoodsName = ((DataRow)(App.StorageDB.DetailedRegisterTable.Rows.Find(UID)))[2].ToString();
                            GoodsCount = ((DataRow)(App.StorageDB.DetailedRegisterTable.Rows.Find(UID)))[3].ToString();
                            GoodsDescription = ((DataRow)(App.StorageDB.DetailedRegisterTable.Rows.Find(UID)))[4].ToString();
                            
                        }
                        GoodsNumberByte = Encoding.GetEncoding("GBK").GetBytes(GoodsNumber);
                        GoodsNameByte = Encoding.GetEncoding("GBK").GetBytes(GoodsName);
                        GoodsCountByte = Encoding.GetEncoding("GBK").GetBytes(GoodsCount);
                        GoodsDescriptionByte = Encoding.GetEncoding("GBK").GetBytes(GoodsDescription);
                        //System.Windows.Forms.MessageBox.Show(GoodsNumberByte.Length.ToString());
                        GoodsNumberByteLength = (Byte)GoodsNumberByte.Length;
                        //System.Windows.Forms.MessageBox.Show(GoodsNameByte.Length.ToString());
                        GoodsNameByteLength = (Byte)GoodsNameByte.Length;
                        //System.Windows.Forms.MessageBox.Show(GoodsCountByte.Length.ToString());
                        GoodsCountByteLength = (Byte)GoodsCountByte.Length;
                        //System.Windows.Forms.MessageBox.Show(GoodsDescriptionByte.Length.ToString());
                        GoodsDescriptionByteLength = (Byte)GoodsDescriptionByte.Length;
                        temp = Encoding.GetEncoding("GBK").GetBytes(GoodsNumber + GoodsName + GoodsCount + GoodsDescription);
                        WrtFrame.Add(0xFF);
                        WrtFrame.Add(GoodsNumberByteLength);
                        WrtFrame.Add(GoodsNameByteLength);
                        WrtFrame.Add(GoodsCountByteLength);
                        WrtFrame.Add(GoodsDescriptionByteLength);
                        for (int i = 0; i < temp.Length; i++)
                        {
                            WrtFrame.Add(temp[i]);
                        }
                        WrtFrame.Add(0xFE);
                        Byte[] writeBuffer = WrtFrame.ToArray();
                        int length = writeBuffer.Length;
                        serialPort.Write(writeBuffer, 0, length);
                        WrtFrame.Clear();
                    }
                }
            }
            catch
            {
                
            }
        }

        public Boolean OpenSerialPort()
        {
            try
            {
                serialPort.PortName = SerialComboBox.SelectedItem.ToString();
                serialPort.BaudRate = Int32.Parse(BaudComboBox.SelectedItem.ToString());
                serialPort.DataBits = 8;
                serialPort.StopBits = StopBits.One;
                serialPort.Parity = Parity.None;
                //serialport.RtsEnable = true;
                //serialport.DtrEnable = true;
                //serialport.Handshake = Handshake.RequestToSend;
                //serialport.ReadTimeout = 500;
                //serialport.WriteTimeout = 500;
                serialPort.Open();
                if (serialPort.IsOpen)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        public Boolean CloseSerialPort()
        {
            try
            {
                serialPort.Close();
                if (!serialPort.IsOpen)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        private void MetroMainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (closeFlag)
            {
                closeAnimate.Completed += (a, b) =>
                {
                    closeFlag = false;
                    this.Close();
                };
                closeAnimate.Begin();
                MainControl.Content = null;
                notifyIcon.Visible = false;
                e.Cancel = true;
            }
            else
            {
                e.Cancel = false;
            }
        }
    }
}
