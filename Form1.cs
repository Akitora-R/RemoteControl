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
            this.Text = "USB����ͷ����";
            // ��ʼ�� ComboBox ���о�������Ƶ�豸
            InitializeVideoDevices();
            InitializeSerialPorts();
            keyboardListener = new(OnKeyPress);
            keyboardListener.Start();
        }

        private void OnMouseMove(int x, int y, int deltaX, int deltaY)
        {
            if (mousePosLabel.InvokeRequired)
            {
                mousePosLabel.Invoke(() => mousePosLabel.Text = $"X = {x} Y = {y}\n��X = {deltaX} ��Y = {deltaY}");
            }
            serialPortHandler?.SendCommand(BuildMousePackat(deltaX, deltaY));
        }

        private void OnKeyPress(Keys keys)
        {
            kbLabel.Text = keys.ToString();
        }

        private void InitializeSerialPorts()
        {
            // ��ȡ���п��õĴ����豸
            var portNames = SerialPort.GetPortNames();

            // ������������ӵ� portDeviceBox ��
            foreach (var portName in portNames)
            {
                portDeviceBox.Items.Add(portName);
            }

            // Ĭ��ѡ���һ������
            if (portDeviceBox.Items.Count > 0)
            {
                portDeviceBox.SelectedIndex = 0;
            }
        }

        private MouseListener InitMouseListener(SerialPortHandler h)
        {
            // L ��513 ��514
            // R ��516 ��517
            // M ��519 ��520
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

            // �������ݳ���
            byte length = (byte)data.Length;

            // �����ۼӺ�
            byte sum = (byte)(0x57 + 0xAB + address + command + length);
            foreach (byte b in data)
            {
                sum += b;
            }

            // �������������ݰ�
            byte[] packet = new byte[6 + data.Length];
            packet[0] = 0x57;
            packet[1] = 0xAB;
            packet[2] = address;
            packet[3] = command;
            packet[4] = length;
            Array.Copy(data, 0, packet, 5, data.Length);
            packet[^1] = sum;

            Console.WriteLine($"���Ͱ� {BitConverter.ToString(packet)}");
            return packet;
        }

        private void InitializeVideoDevices()
        {
            // ��ȡ���п��õ���Ƶ�豸
            FilterInfoCollection videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            if (videoDevices.Count > 0)
            {
                // ���豸������ӵ� ComboBox ��
                foreach (FilterInfo device in videoDevices)
                {
                    comboBox1.Items.Add(device.Name);
                }

                // Ĭ��ѡ���һ���豸
                comboBox1.SelectedIndex = 0;
            }
            else
            {
                MessageBox.Show("δ�ҵ�����ͷ�豸��");
            }
        }


        //private void Video_NewFrame(object sender, NewFrameEventArgs eventArgs)
        //{
        //    lock (lockObject)
        //    {
        //        // ����ˢ��Ƶ�� (����ÿ100ms����һ��)
        //        if (DateTime.Now.Subtract(lastUpdate).TotalMilliseconds < 100)
        //        {
        //            return;
        //        }
        //        lastUpdate = DateTime.Now;

        //        // �ͷž�ͼ����Դ
        //        if (pictureBox1.Image != null)
        //        {
        //            pictureBox1.Image.Dispose();
        //        }

        //        // ��¡��֡����ʾ�� PictureBox
        //        pictureBox1.Image = (Bitmap)eventArgs.Frame.Clone();
        //    }
        //}

        private void VideoNewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            // ����ˢ��Ƶ��
            if (DateTime.Now.Subtract(lastUpdate).TotalMilliseconds < 25)
            {
                return;
            }
            lastUpdate = DateTime.Now;

            Bitmap newFrame;

            try
            {
                // ��¡��֡
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
                // ȷ�������̸߳��� PictureBox
                if (miniPicBox.InvokeRequired)
                {
                    miniPicBox.Invoke(() =>
                    {
                        // �ͷž�ͼ����Դ
                        miniPicBox.Image?.Dispose();

                        // ���� PictureBox ��ͼ��
                        miniPicBox.Image = bitmap;
                    });
                }
                else
                {
                    // �ͷž�ͼ����Դ
                    miniPicBox.Image?.Dispose();

                    // ���� PictureBox ��ͼ��
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
                MessageBox.Show("��ѡ��һ����Ƶ�豸��");
                return;
            }

            if (videoSource != null)
            {
                if (videoSource.IsRunning)
                {
                    // �������ͷ�������У�ֹͣ����
                    videoSource.SignalToStop();
                    videoSource.WaitForStop();
                    captureBtn.Text = "��ʼ����";
                }
                else
                {
                    // �������ͷû�����У���ʼ����
                    videoSource.Start();
                    captureBtn.Text = "ֹͣ����";
                }
            }
            else
            {
                // ��ȡ���п��õ���Ƶ�豸
                FilterInfoCollection videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

                if (videoDevices.Count > 0)
                {
                    // ���� ComboBox ��ѡ�е��豸���� VideoCaptureDevice
                    videoSource = new VideoCaptureDevice(videoDevices[comboBox1.SelectedIndex].MonikerString);
                    videoSource.NewFrame += new NewFrameEventHandler(VideoNewFrame);

                    // ��ʼ����
                    videoSource.Start();
                    captureBtn.Text = "ֹͣ����";
                }
                else
                {
                    MessageBox.Show("δ�ҵ�����ͷ�豸��");
                }
            }
        }

        private void ComBtnClick(object sender, EventArgs e)
        {
            if (portDeviceBox.SelectedIndex < 0)
            {
                MessageBox.Show("δѡ�񴮿��豸��");
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
                serialPortLabel.Text = $"�����Ѵ� {portName}";
            }
            serialPortHandler.SendCommand(BuildCommandPacket(0x00, 0x01, []));
            mouseTracker = InitMouseListener(serialPortHandler);
            mouseTracker.StartTracking();
        }

        void OnCh9329Response(object? sender, Ch9329ResponseEventArgs e)
        {
            if (!e.IsValid)
            {
                Console.WriteLine("�յ���Ч����");
                return;
            }
            Console.WriteLine($"�յ���Ч������ַ {e.Address}, ���� {e.Command}, ���� {BitConverter.ToString(e.Data)}");

        }

        private byte[] BuildMousePackat(int deltaX, int deltaY)
        {
            byte[] data =
            [
                // ��һ���ֽڹ̶�Ϊ 0x01
                0x01,
                // �ڶ����ֽڣ���갴��ֵ
                key, // ��ȡ��� 3 λ����ʾ�м����Ҽ������
                // �������ֽڣ�X �����ƶ�����
                (byte)(deltaX >= 0 ? deltaX & 0x7F : (0x80 | (deltaX & 0x7F))),
                // ���ĸ��ֽڣ�Y �����ƶ�����
                (byte)(deltaY >= 0 ? deltaY & 0x7F : (0x80 | (deltaY & 0x7F))),
                // ������ֽڣ����ֹ���ֵ��Ĭ��Ϊ 0���޹�����
                0x00, // �ɸ���������ӹ����߼�
            ];

            // �������������
            return BuildCommandPacket(0x00, 0x05, data);
        }
        private byte[] BuildMouseWheelPackat(byte w)
        {
            byte[] data =
            [
                // ��һ���ֽڹ̶�Ϊ 0x01
                0x01,
                // �ڶ����ֽڣ���갴��ֵ
                0x00, // ��ȡ��� 3 λ����ʾ�м����Ҽ������
                // �������ֽڣ�X �����ƶ�����
                0x00,
                // ���ĸ��ֽڣ�Y �����ƶ�����
                0x00,
                // ������ֽڣ����ֹ���ֵ��Ĭ��Ϊ 0���޹�����
                w, // �ɸ���������ӹ����߼�
            ];

            // �������������
            return BuildCommandPacket(0x00, 0x05, data);
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            fullScreenForm = new FullScreenForm();
            fullScreenForm.FormClosed += FullScreenForm_FormClosed;
            // ��ʾȫ����
            fullScreenForm.Show();
        }
        private void FullScreenForm_FormClosed(object? sender, FormClosedEventArgs e)
        {
            // �� FullScreenForm �ر�ʱ����
            fullScreenForm?.Dispose();
            fullScreenForm = null;
        }
    }
}
