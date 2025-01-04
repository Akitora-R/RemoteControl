using System.IO.Ports;
using AForge.Video;
using AForge.Video.DirectShow;

namespace RemoteControl
{
    public partial class MainForm : Form
    {
        private VideoCaptureDevice? videoSource;
        private SerialPortHandler? serialPortHandler;
        private MouseListener? mouseListener;
        private MousePacketBuilder mousePacketBuilder = new();
        private KeyboardListener? keyboardListener;
        private KeyboardPacketBuilder keyboardPacketBuilder = new();
        FullScreenForm? fullScreenForm;
        private DateTime lastUpdate = DateTime.MinValue;

        public MainForm()
        {
            InitializeComponent();
            this.Text = "USB摄像头捕获";
            // 初始化 ComboBox 并列举所有视频设备
            InitializeVideoDevices();
            InitializeSerialPorts();
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

        private KeyboardListener InitKbListener(SerialPortHandler h)
        {
            return new((key, pressed) =>
            {
                if (key == Keys.Escape)
                {
                    return;
                }
                if (pressed)
                {
                    h.SendCommand(keyboardPacketBuilder.OnKeyPress(key));
                }
                else
                {
                    h.SendCommand(keyboardPacketBuilder.OnKeyUp(key));
                }
            });
        }

        private MouseListener InitMouseListener(SerialPortHandler h)
        {
            // L ↓513 ↑514
            // R ↓516 ↑517
            // M ↓519 ↑520
            return new MouseListener(
                (int x, int y, int deltaX, int deltaY) =>
            {
                var p = mousePacketBuilder.BuildMousePackat(deltaX, deltaY);
                h.SendCommand(p);
            },
            i =>
            {
                var p = mousePacketBuilder.BuildMouseWheelPackat(i);
                h.SendCommand(p);
                Console.WriteLine($"wheel: {BitConverter.ToString(p)}");
            },
            i =>
            {
                //key = (byte)((1 << (i - 513) / 3) | key);
                //h.SendCommand(BuildMousePackat(0, 0));
                //Console.WriteLine($"btn down: {i} key status: {BitConverter.ToString([key])}");
                h.SendCommand(mousePacketBuilder.OnKeyPress(i));
            },
            i =>
            {
                //key = (byte)(~(1 << (i - 514) / 3) & key);
                //h.SendCommand(BuildMousePackat(0, 0));
                //Console.WriteLine($"btn up: {i} key status: {BitConverter.ToString([key])}");
                h.SendCommand(mousePacketBuilder.OnKeyUp(i));
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

            //Console.WriteLine($"发送包 {BitConverter.ToString(packet)}");
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
            mouseListener?.Dispose();
            keyboardListener?.Dispose();
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
            comBtn.Enabled = false;
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
            mouseListener = InitMouseListener(serialPortHandler);
            keyboardListener = InitKbListener(serialPortHandler);
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

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            if (serialPortHandler == null || !serialPortHandler.IsOpen())
            {
                return;
            }
            keyboardListener!.Start();
            mouseListener!.Start();
            fullScreenForm = new FullScreenForm();
            fullScreenForm.FormClosed += FullScreenForm_FormClosed;
            // 显示全屏表单
            fullScreenForm.Show();
        }
        private void FullScreenForm_FormClosed(object? sender, FormClosedEventArgs e)
        {
            serialPortHandler?.SendCommand(keyboardPacketBuilder.Reset());
            // 当 FullScreenForm 关闭时触发
            fullScreenForm?.Dispose();
            fullScreenForm = null;
            keyboardListener!.Stop();
            mouseListener!.Stop();
        }
    }
}
