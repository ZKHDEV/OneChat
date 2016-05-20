using Newtonsoft.Json;
using OneChat.Model;
using OneChat.Service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// “空白页”项模板在 http://go.microsoft.com/fwlink/?LinkId=234238 上有介绍

namespace OneChat
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class ChatPage : Page
    {
        public ChatPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Enabled;   //允许缓存页面
            MessageList = new ObservableCollection<MessageInfo>();
            ClientList = new ObservableCollection<OnlineUserInfo>();

            DisconnectionButton.IsEnabled = false;
            SendBtn.IsEnabled = false;
            MessageTextBox.IsEnabled = false;
            ImgBtn.IsEnabled = false;
        }

        StreamSocket clientSocket;   //客户端Socket
        private ObservableCollection<OnlineUserInfo> ClientList;   //在线用户列表
        private ObservableCollection<MessageInfo> MessageList;   //消息列表

        #region 连接服务器
        /// <summary>
        /// 连接服务器方法
        /// </summary>
        /// <param name="remoteIP">服务器IP</param>
        /// <param name="remotePort">服务器端口</param>
        private async void Connection(string remoteIP, string remotePort)
        {
            if (clientSocket != null)
            {
                clientSocket.Dispose();
                clientSocket = null;
            }

            clientSocket = new StreamSocket();

            try
            {
                ConnectionProgressRing.IsActive = true;
                ConnectionButton.IsEnabled = false;

                await clientSocket.ConnectAsync(new HostName(remoteIP), remotePort);   //尝试连接服务器

                DBManager.NotifyMsg(MessageList, UserNameTextBox.Text.Trim());   //输出聊天记录
                ConnectionProgressRing.IsActive = false;
                DisconnectionButton.IsEnabled = true;
                SendBtn.IsEnabled = true;
                MessageTextBox.IsEnabled = true;
                ImgBtn.IsEnabled = true;
            }
            catch   //连接失败处理
            {
                ConnectionProgressRing.IsActive = false;
                ConnectionButton.IsEnabled = true;
                ShowMessage("连接失败！");
                if (clientSocket != null)
                {
                    clientSocket.Dispose();
                    clientSocket = null;
                }
                return;
            }

            Received();   //开始异步接收信息
            //向服务器发送更新信息
            MessageInfo message = new MessageInfo { MsgType = MessageType.Update, Name = UserNameTextBox.Text.Trim() };
            Task task = SendData(message);   //异步发送信息并且不等待
        }
        #endregion

        private void ConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            //判断输入格式
            if (Regex.IsMatch(RemoteIPTextBox.Text.Trim(), "\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}")
               && Regex.IsMatch(RemotePortTextBox.Text.Trim(), "\\d{1,5}")
               && Int32.Parse(RemotePortTextBox.Text.Trim()) < 65536)
            {
                if (UserNameTextBox.Text == "")
                {
                    ShowMessage("用户名不能为空！");
                    return;
                }
                Connection(RemoteIPTextBox.Text.Trim(), RemotePortTextBox.Text.Trim());   //尝试监听指定地址
            }
            else
            {
                ShowMessage("IP地址或端口格式错误！");
            }
        }

        private void SendBtn_Click(object sender, RoutedEventArgs e)
        {
            if (OnlineUserListBox.SelectedItem == null)
            {
                ShowMessage("请选择发送目标用户！");
                return;
            }
            if (MessageTextBox.Text.Trim() == "")
            {
                ShowMessage("请输入发送内容！");
                return;
            }
            //获取Image的自定义信息，该信息为图片的字节数组
            byte[] imagebuffer = SendImage.Tag as byte[];
            MessageInfo message = new MessageInfo
            {
                MsgType = MessageType.TextMessage,
                Message = MessageTextBox.Text.Trim(),
                Bmp = imagebuffer,
                Name = UserNameTextBox.Text.Trim(),
                Target = (OnlineUserListBox.SelectedItem as OnlineUserInfo).Name,   //选择的目标用户
                Time = DateTime.Now
            };
            Task task = SendData(message);   //发送信息
            MessageList.Add(message);   //添加信息到信息列表
            MessageTextBox.Text = "";
        }

        #region 发送信息
        /// <summary>
        /// 发送信息方法
        /// </summary>
        /// <param name="data">要发送的对象</param>
        private async Task SendData(MessageInfo data)
        {
            string jsonmessage = JsonConvert.SerializeObject(data);   //将数据序列化为Json数据格式
            byte[] buffer = Encoding.UTF8.GetBytes(jsonmessage);   //将Json数据转换为二进制数组

            using (DataWriter writer = new DataWriter(clientSocket.OutputStream))   //创建目标Socket输出流编写器
            {
                uint length = (uint)buffer.Length;
                writer.WriteUInt32(length);   //写入数据包大小
                writer.WriteBytes(buffer);   //写入数据包
                try
                {
                    await writer.StoreAsync();   //提交数据
                    writer.DetachStream();   //分离与编写器关联的流
                }
                catch (Exception ex)
                {
                    ShowMessage(ex.Message);
                }
            }
        }
        #endregion

        #region 接收信息
        private async void Received()
        {
            DataReader reader = new DataReader(clientSocket.InputStream);   //创建Socket输入流读取器
            try   //判断连接是否断开
            {
                while (true)
                {
                    //从输入流加载一个uint类型数据
                    await reader.LoadAsync(sizeof(uint));
                    uint length = reader.ReadUInt32();   //读取数据，该数据表示数据包长度
                    await reader.LoadAsync(length);   //加载数据包
                    byte[] buffer = new byte[length];   //用于存放接收的数据包
                    reader.ReadBytes(buffer);   //加载数据包
                    string jsonmessage = Encoding.UTF8.GetString(buffer);   //将二进制数组转换为字符串
                    MessageInfo message = new MessageInfo();   //用于存放解析后的具体数据类
                    message = JsonConvert.DeserializeObject<MessageInfo>(jsonmessage);   //将Json数据反序列化为MessageInfo类

                    //通知主线程更新UI元素
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        //判断信息类型
                        if (message.MsgType == MessageType.TextMessage)
                        {
                            MessageList.Add(message);
                        }
                        else if (message.MsgType == MessageType.Update)
                        {
                            ClientList.Clear();
                            //更新用户列表
                            message.OnlineUser.ForEach(m => ClientList.Add(new OnlineUserInfo { Name = m }));
                        }
                    });
                }
            }
            catch
            {
                ShowMessage("与服务器连接断开！");
                Disconnection();
            }
        }
        #endregion

        private async void AddImgBtn_Click(object sender, RoutedEventArgs e)   //添加图片
        {
            FileOpenPicker filePicker = new FileOpenPicker();   //创建文件打开对话框
            filePicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;   //指定对话框默认目录
            //指定筛选文件类型
            filePicker.FileTypeFilter.Add(".jpg");
            filePicker.FileTypeFilter.Add(".jpeg");
            filePicker.FileTypeFilter.Add(".png");
            filePicker.FileTypeFilter.Add(".bmp");
            StorageFile file = await filePicker.PickSingleFileAsync();   //获取选中的文件
            if (file != null)
            {
                using (IRandomAccessStream imgStream = await file.OpenAsync(FileAccessMode.Read))   //创建选中文件的随机访问流
                {
                    BitmapImage bmp = new BitmapImage();

                    bmp.SetSource(imgStream);   //通过访问流设置位图源
                    bmp.DecodePixelWidth = 200;   //设置位图解码的宽度
                    SendImage.Source = bmp;

                    using (DataReader reader = new DataReader(imgStream.GetInputStreamAt(0UL)))   //创建访问流的读取器
                    {
                        //将访问流转换为字节数组并存放在Image控件的自定义信息中
                        uint length = (uint)imgStream.Size;
                        await reader.LoadAsync(length);
                        byte[] buffer = new byte[length];
                        reader.ReadBytes(buffer);
                        SendImage.Tag = buffer;
                    }
                }
            }
        }

        private async void ShowMessage(string txt)   //消息提示
        {
            //通知主线程更新UI元素
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                MessageDialog message = new MessageDialog(txt, "提示");
                await message.ShowAsync();
            });
        }

        private async void DisconnectionButton_Click(object sender, RoutedEventArgs e)
        {
            //通知服务器此客户端将断开连接
            MessageInfo message = new MessageInfo { Name = UserNameTextBox.Text.Trim(), MsgType = MessageType.Disconnection };
            await SendData(message);   //异步发送信息并等待发送完成
            Disconnection();
        }

        private async void Disconnection()   //断开连接处理
        {
            if (clientSocket != null)
            {
                clientSocket.Dispose();
                clientSocket = null;
            }

            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                //释放资源并清空用户和消息列表

                ClientList.Clear();
                MessageList.Clear();

                DisconnectionButton.IsEnabled = false;
                ConnectionButton.IsEnabled = true;
                ImgBtn.IsEnabled = false;
                SendBtn.IsEnabled = false;
                MessageTextBox.IsEnabled = false;
                MessageTextBox.Text = "";
            });
        }

        private void DelImgBtn_Click(object sender, RoutedEventArgs e)
        {
            SendImage.Tag = null;
            SendImage.Source = null;
        }

        private void OnlineButton_Click(object sender, RoutedEventArgs e)
        {
            ContentSplitView.IsPaneOpen = !ContentSplitView.IsPaneOpen;
        }
    }
}
