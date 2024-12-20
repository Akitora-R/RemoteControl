using AForge.Video.DirectShow;
using AForge.Video;
using System.Windows.Forms;

namespace RemoteControl
{
    public partial class Form1 : Form
    {
        private VideoCaptureDevice videoSource;
        public Form1()
        {
            InitializeComponent();
            this.Text = "USB摄像头捕获";
            //this.Size = new Size(1600, 900);

            // 创建PictureBox来显示摄像头内容
            //pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;

            //pictureBox1 = new PictureBox
            //{
            //    SizeMode = PictureBoxSizeMode.StretchImage,
            //    Size = this.ClientSize / 2,
            //    Location = new Point(0, 0)
            //};
            //this.Controls.Add(pictureBox1);

            // 获取摄像头设备
            FilterInfoCollection videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            if (videoDevices.Count > 0)
            {
                videoSource = new VideoCaptureDevice(videoDevices[0].MonikerString);
                videoSource.NewFrame += new NewFrameEventHandler(Video_NewFrame);
                videoSource.Start();
            }
            else
            {
                MessageBox.Show("未找到摄像头设备！");
            }
        }

        private void Video_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            // 释放旧图像资源
            pictureBox1.Image?.Dispose();

            // 在PictureBox中显示新的帧
            pictureBox1.Image = (Bitmap)eventArgs.Frame.Clone();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            if (videoSource != null && videoSource.IsRunning)
            {
                videoSource.SignalToStop();
                videoSource = null;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }
    }
}
