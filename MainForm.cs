using Emgu.CV;
using Emgu.CV.CvEnum;
using System.IO.Ports;

namespace RemoteControl
{
    public partial class MainForm : Form
    {
        private SerialPortHandler? serialPortHandler;
        private MouseListener? mouseListener;
        private MousePacketBuilder mousePacketBuilder = new();
        private KeyboardListener? keyboardListener;
        private KeyboardPacketBuilder keyboardPacketBuilder = new();
        FullScreenForm? fullScreenForm;

        private VideoCapture _videoCapture;
        private bool _isCapturing = false;
        private System.Windows.Forms.Timer _frameTimer;
        private readonly int _targetFPS = 30; // 目标帧率

        public MainForm()
        {
            InitializeComponent();
            this.Text = "Remote Control";
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
            List<string> devices = new List<string>();
            for (int i = 0; i < 10; i++) // 假设最多有 10 个设备
            {
                try
                {
                    using (var capture = new VideoCapture(i, VideoCapture.API.Any))
                    {
                        if (capture.IsOpened)
                        {
                            devices.Add($"设备 {i}");
                        }
                    }
                }
                catch
                {
                    // 忽略异常
                }
            }
            if (devices.Count > 0)
            {
                // 将设备名称添加到 ComboBox 中
                foreach (var device in devices)
                {
                    comboBox1.Items.Add(device);
                }

                // 默认选择第一个设备
                comboBox1.SelectedIndex = 0;
            }
            else
            {
                MessageBox.Show("未找到摄像头设备！");
            }
        }

        private void VideoNewFrame(object sender, EventArgs eventArgs)
        {
            if (_videoCapture == null || !_videoCapture.IsOpened)
            {
                return;
            }
            using var frame = _videoCapture.QueryFrame();
            if (frame == null)
            {
                return;
            }
            // 将帧转换为 Bitmap
            var bitmap = frame.ToBitmap();
            // 将新帧显示在 PictureBox 中
            NewFrameToPicBox(bitmap);
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
            mouseListener?.Dispose();
            keyboardListener?.Dispose();
            // 关闭视频设备
            _videoCapture?.Stop();
            _videoCapture?.Dispose();
            _frameTimer?.Stop();
            _frameTimer?.Dispose();
            _frameTimer = null;
            _videoCapture = null;
            serialPortHandler?.ClosePort();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private async void CaptureBtnClick(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex == -1)
            {
                MessageBox.Show("请选择一个视频设备！");
                return;
            }

            if (_isCapturing)
            {
                // 如果正在捕获，停止捕获
                _videoCapture?.Stop();
                _videoCapture?.Dispose();
                _frameTimer?.Stop();
                _frameTimer?.Dispose();
                _frameTimer = null;
                _videoCapture = null;
                _isCapturing = false;
                captureBtn.Text = "开始捕获";
            }
            else
            {
                captureBtn.Enabled = false;
                int deviceIndex = comboBox1.SelectedIndex;
                _videoCapture = await Task.Run(() => new VideoCapture(deviceIndex, VideoCapture.API.Any));
                if (!_videoCapture.IsOpened)
                {
                    MessageBox.Show("无法打开视频设备！");
                    return;
                }
                // 设置捕获的分辨率
                _videoCapture.Set(CapProp.FrameWidth, 1920);
                _videoCapture.Set(CapProp.FrameHeight, 1080);

                // 获取实际设置的分辨率
                int width = (int)_videoCapture.Get(CapProp.FrameWidth);
                int height = (int)_videoCapture.Get(CapProp.FrameHeight);
                Console.WriteLine($"设置的分辨率: {width}x{height}");

                _frameTimer = new System.Windows.Forms.Timer
                {
                    Interval = 1000 / _targetFPS
                }; // 设置定时器间隔
                _frameTimer.Tick += VideoNewFrame;
                _frameTimer.Start();
                _isCapturing = true;
                captureBtn.Text = "停止捕获";
                captureBtn.Enabled = true;
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
            // 当 FullScreenForm 关闭时触发
            fullScreenForm?.Dispose();
            fullScreenForm = null;
            keyboardListener!.Stop();
            mouseListener!.Stop();
        }
    }
}
