using System.IO.Ports;
using AForge.Video;
using AForge.Video.DirectShow;

namespace RemoteControl
{
    public partial class Form1 : Form
    {
        private VideoCaptureDevice? videoSource;
        private SerialPortHandler? serialPortHandler;
        private MouseListener? mouseTracker;
        private readonly KeyboardListener keyboardListener;
        private DateTime lastUpdate = DateTime.MinValue;
        private byte key = 0x00;
        FullScreenForm? fullScreenForm;
        public Form1()
        {
            InitializeComponent();
            this.Text = "USB摄像头捕获";
            // 初始化 ComboBox 并列举所有视频设备
            InitializeVideoDevices();
            InitializeSerialPorts();
            keyboardListener = new(OnKeyPress);
            keyboardListener.Start();
        }

        private void OnMouseMove(int x, int y, int deltaX, int deltaY)
        {
            if (mousePosLabel.InvokeRequired)
            {
                mousePosLabel.Invoke(() => mousePosLabel.Text = $"X = {x} Y = {y}\nΔX = {deltaX} ΔY = {deltaY}");
            }
            serialPortHandler?.SendCommand(BuildMousePackat(deltaX, deltaY));
        }

        private void OnKeyPress(Keys keys)
        {
            kbLabel.Text = keys.ToString();
        }

        private void InitializeSerialPorts()
        {
            // 获取所有可用的串口设备
            var portNames = SerialPort.GetPortNames();

            // 将串口名称添加到 portDeviceBox 中
            foreach (var portName in portNames)
            {
                portDeviceBox.Items.Add(portName);
            }

            // 默认选择第一个串口
            if (portDeviceBox.Items.Count > 0)
            {
                portDeviceBox.SelectedIndex = 0;
            }
        }

        private MouseListener InitMouseListener(SerialPortHandler h)
        {
            // L ↓513 ↑514
            // R ↓516 ↑517
            // M ↓519 ↑520
            return new MouseListener(OnMouseMove,
                i =>
                {
                    byte b;
                    if (i >= 0)
                    {
                        b = (byte)(i & 0xFF);
                    }
                    else
                    {
                        b = (byte)(i & 0xFF | 0x80);
                    }
                    h.SendCommand(BuildMouseWheelPackat(b));
                    Console.WriteLine($"wheel: {BitConverter.ToString([b])}");
                },
                i =>
                {
                    key = (byte)((1 << (i - 513) / 3) | key);
                    h.SendCommand(BuildMousePackat(0, 0));
                    Console.WriteLine($"btn down: {i} key status: {BitConverter.ToString([key])}");
                },
                i =>
                {
                    key = (byte)(~(1 << (i - 514) / 3) & key);
                    h.SendCommand(BuildMousePackat(0, 0));
                    Console.WriteLine($"btn up: {i} key status: {BitConverter.ToString([key])}");
                });
        }

        private byte[] BuildCommandPacket(byte address, byte command, byte[] data)
        {

            // 后续数据长度
            byte length = (byte)data.Length;

            // 计算累加和
            byte sum = (byte)(0x57 + 0xAB + address + command + length);
            foreach (byte b in data)
            {
                sum += b;
            }

            // 构建完整的数据包
            byte[] packet = new byte[6 + data.Length];
            packet[0] = 0x57;
            packet[1] = 0xAB;
            packet[2] = address;
            packet[3] = command;
            packet[4] = length;
            Array.Copy(data, 0, packet, 5, data.Length);
            packet[^1] = sum;

            Console.WriteLine($"发送包 {BitConverter.ToString(packet)}");
            return packet;
        }

        private void InitializeVideoDevices()
        {
            // 获取所有可用的视频设备
            FilterInfoCollection videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            if (videoDevices.Count > 0)
            {
                // 将设备名称添加到 ComboBox 中
                foreach (FilterInfo device in videoDevices)
                {
                    comboBox1.Items.Add(device.Name);
                }

                // 默认选择第一个设备
                comboBox1.SelectedIndex = 0;
            }
            else
            {
                MessageBox.Show("未找到摄像头设备！");
            }
        }


        //private void Video_NewFrame(object sender, NewFrameEventArgs eventArgs)
        //{
        //    lock (lockObject)
        //    {
        //        // 限制刷新频率 (例如每100ms更新一次)
        //        if (DateTime.Now.Subtract(lastUpdate).TotalMilliseconds < 100)
        //        {
        //            return;
        //        }
        //        lastUpdate = DateTime.Now;

        //        // 释放旧图像资源
        //        if (pictureBox1.Image != null)
        //        {
        //            pictureBox1.Image.Dispose();
        //        }

        //        // 克隆新帧并显示在 PictureBox
        //        pictureBox1.Image = (Bitmap)eventArgs.Frame.Clone();
        //    }
        //}

        private void VideoNewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            // 限制刷新频率
            if (DateTime.Now.Subtract(lastUpdate).TotalMilliseconds < 25)
            {
                return;
            }
            lastUpdate = DateTime.Now;

            Bitmap newFrame;

            try
            {
                // 克隆新帧
                newFrame = (Bitmap)eventArgs.Frame.Clone();
                NewFrameToPicBox(newFrame);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing new frame: {ex.Message}");
            }
        }

        private void NewFrameToPicBox(Bitmap bitmap)
        {
            if (fullScreenForm != null)
            {
                fullScreenForm.UpdateFrame(bitmap);
            }
            else
            {
                // 确保在主线程更新 PictureBox
                if (miniPicBox.InvokeRequired)
                {
                    miniPicBox.Invoke(() =>
                    {
                        // 释放旧图像资源
                        miniPicBox.Image?.Dispose();

                        // 更新 PictureBox 的图像
                        miniPicBox.Image = bitmap;
                    });
                }
                else
                {
                    // 释放旧图像资源
                    miniPicBox.Image?.Dispose();

                    // 更新 PictureBox 的图像
                    miniPicBox.Image = bitmap;
                }
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            if (videoSource != null && videoSource.IsRunning)
            {
                videoSource.SignalToStop();
                videoSource = null;
            }
            mouseTracker?.Dispose();
            keyboardListener.Dispose();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void CaptureBtnClick(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex == -1)
            {
                MessageBox.Show("请选择一个视频设备！");
                return;
            }

            if (videoSource != null)
            {
                if (videoSource.IsRunning)
                {
                    // 如果摄像头正在运行，停止捕获
                    videoSource.SignalToStop();
                    videoSource.WaitForStop();
                    captureBtn.Text = "开始捕获";
                }
                else
                {
                    // 如果摄像头没有运行，开始捕获
                    videoSource.Start();
                    captureBtn.Text = "停止捕获";
                }
            }
            else
            {
                // 获取所有可用的视频设备
                FilterInfoCollection videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

                if (videoDevices.Count > 0)
                {
                    // 根据 ComboBox 中选中的设备创建 VideoCaptureDevice
                    videoSource = new VideoCaptureDevice(videoDevices[comboBox1.SelectedIndex].MonikerString);
                    videoSource.NewFrame += new NewFrameEventHandler(VideoNewFrame);

                    // 开始捕获
                    videoSource.Start();
                    captureBtn.Text = "停止捕获";
                }
                else
                {
                    MessageBox.Show("未找到摄像头设备！");
                }
            }
        }

        private void ComBtnClick(object sender, EventArgs e)
        {
            if (portDeviceBox.SelectedIndex < 0)
            {
                MessageBox.Show("未选择串口设备！");
                return;
            }
            portDeviceBox.Enabled = false;
            if (serialPortHandler == null || !serialPortHandler.IsOpen())
            {
                var portName = SerialPort.GetPortNames()[portDeviceBox.SelectedIndex];
                serialPortHandler = new SerialPortHandler(portName, 9600, OnCh9329Response);
                if (!serialPortHandler.IsOpen())
                {
                    serialPortHandler.OpenPort();
                }
                serialPortLabel.Text = $"串口已打开 {portName}";
            }
            serialPortHandler.SendCommand(BuildCommandPacket(0x00, 0x01, []));
            mouseTracker = InitMouseListener(serialPortHandler);
            mouseTracker.StartTracking();
        }

        void OnCh9329Response(object? sender, Ch9329ResponseEventArgs e)
        {
            if (!e.IsValid)
            {
                Console.WriteLine("收到无效包！");
                return;
            }
            Console.WriteLine($"收到有效包：地址 {e.Address}, 命令 {e.Command}, 数据 {BitConverter.ToString(e.Data)}");

        }

        private byte[] BuildMousePackat(int deltaX, int deltaY)
        {
            byte[] data =
            [
                // 第一个字节固定为 0x01
                0x01,
                // 第二个字节：鼠标按键值
                key, // 仅取最低 3 位，表示中键、右键、左键
                // 第三个字节：X 方向移动距离
                (byte)(deltaX >= 0 ? deltaX & 0x7F : (0x80 | (deltaX & 0x7F))),
                // 第四个字节：Y 方向移动距离
                (byte)(deltaY >= 0 ? deltaY & 0x7F : (0x80 | (deltaY & 0x7F))),
                // 第五个字节：滚轮滚动值，默认为 0（无滚动）
                0x00, // 可根据需求添加滚动逻辑
            ];

            // 构建最终命令包
            return BuildCommandPacket(0x00, 0x05, data);
        }
        private byte[] BuildMouseWheelPackat(byte w)
        {
            byte[] data =
            [
                // 第一个字节固定为 0x01
                0x01,
                // 第二个字节：鼠标按键值
                0x00, // 仅取最低 3 位，表示中键、右键、左键
                // 第三个字节：X 方向移动距离
                0x00,
                // 第四个字节：Y 方向移动距离
                0x00,
                // 第五个字节：滚轮滚动值，默认为 0（无滚动）
                w, // 可根据需求添加滚动逻辑
            ];

            // 构建最终命令包
            return BuildCommandPacket(0x00, 0x05, data);
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            fullScreenForm = new FullScreenForm();
            fullScreenForm.FormClosed += FullScreenForm_FormClosed;
            // 显示全屏表单
            fullScreenForm.Show();
        }
        private void FullScreenForm_FormClosed(object? sender, FormClosedEventArgs e)
        {
            // 当 FullScreenForm 关闭时触发
            fullScreenForm?.Dispose();
            fullScreenForm = null;
        }
    }
}
