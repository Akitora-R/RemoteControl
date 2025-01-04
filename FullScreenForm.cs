

namespace RemoteControl
{
    internal class FullScreenForm : Form
    {
        private readonly PictureBox pictureBox;
        private readonly Button exitButton;

        public FullScreenForm()
        {
            // 设置窗口属性
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.TopMost = true;
            this.StartPosition = FormStartPosition.Manual;
            this.Bounds = Screen.PrimaryScreen!.Bounds;
            this.KeyPreview = true;
            this.KeyDown += FullScreenForm_KeyDown;

            // 初始化PictureBox
            pictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black,
            };
            this.Controls.Add(pictureBox);

            // 初始化退出按钮
            exitButton = new Button
            {
                Text = "退出",
                Size = new Size(100, 50),
                Location = new Point(Screen.PrimaryScreen.WorkingArea.Width - 110, 10),
                Visible = false,
                BackColor = Color.White,
            };
            exitButton.Click += (s, e) => this.Close();
            pictureBox.Controls.Add(exitButton);
            pictureBox.MouseMove += FullScreenForm_MouseMove;
        }

        private void FullScreenForm_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                this.Close();
            }
        }

        private void FullScreenForm_MouseMove(object? sender, MouseEventArgs e)
        {
            // 检测鼠标是否在屏幕边缘（例如，左边缘）
            int edgeThreshold = 200; // 边缘宽度
            if (Screen.PrimaryScreen!.WorkingArea.Width - e.X < edgeThreshold)
            {
                exitButton.Visible = true;
            }
            else
            {
                exitButton.Visible = false;
            }
        }

        public void UpdateFrame(Bitmap bitmap)
        {
            if (pictureBox.InvokeRequired)
            {
                pictureBox.Invoke(() =>
                {
                    pictureBox.Image?.Dispose();
                    pictureBox.Image = bitmap;
                });
            }
            else
            {
                pictureBox.Image?.Dispose();
                pictureBox.Image = bitmap;
            }
        }
    }
}
