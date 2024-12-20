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
            this.Text = "USB����ͷ����";
            //this.Size = new Size(1600, 900);

            // ����PictureBox����ʾ����ͷ����
            //pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;

            //pictureBox1 = new PictureBox
            //{
            //    SizeMode = PictureBoxSizeMode.StretchImage,
            //    Size = this.ClientSize / 2,
            //    Location = new Point(0, 0)
            //};
            //this.Controls.Add(pictureBox1);

            // ��ȡ����ͷ�豸
            FilterInfoCollection videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            if (videoDevices.Count > 0)
            {
                videoSource = new VideoCaptureDevice(videoDevices[0].MonikerString);
                videoSource.NewFrame += new NewFrameEventHandler(Video_NewFrame);
                videoSource.Start();
            }
            else
            {
                MessageBox.Show("δ�ҵ�����ͷ�豸��");
            }
        }

        private void Video_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            // �ͷž�ͼ����Դ
            pictureBox1.Image?.Dispose();

            // ��PictureBox����ʾ�µ�֡
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
