namespace RemoteControl
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            groupBox1 = new GroupBox();
            miniPicBox = new PictureBox();
            captureBtn = new Button();
            comboBox1 = new ComboBox();
            portDeviceBox = new ComboBox();
            comBtn = new Button();
            serialPortLabel = new Label();
            kbLabel = new Label();
            mousePosLabel = new Label();
            groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)miniPicBox).BeginInit();
            SuspendLayout();
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(miniPicBox);
            groupBox1.Location = new Point(12, 34);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(650, 387);
            groupBox1.TabIndex = 1;
            groupBox1.TabStop = false;
            groupBox1.Text = "Screen";
            // 
            // pictureBox1
            // 
            miniPicBox.Location = new Point(6, 22);
            miniPicBox.Name = "pictureBox1";
            miniPicBox.Size = new Size(640, 360);
            miniPicBox.SizeMode = PictureBoxSizeMode.StretchImage;
            miniPicBox.TabIndex = 0;
            miniPicBox.TabStop = false;
            miniPicBox.Click += pictureBox1_Click;
            // 
            // captureBtn
            // 
            captureBtn.Location = new Point(160, 12);
            captureBtn.Name = "captureBtn";
            captureBtn.Size = new Size(142, 23);
            captureBtn.TabIndex = 2;
            captureBtn.Text = "开始捕获";
            captureBtn.UseVisualStyleBackColor = true;
            captureBtn.Click += CaptureBtnClick;
            // 
            // comboBox1
            // 
            comboBox1.FormattingEnabled = true;
            comboBox1.Location = new Point(12, 12);
            comboBox1.Name = "comboBox1";
            comboBox1.Size = new Size(142, 25);
            comboBox1.TabIndex = 3;
            // 
            // portDeviceBox
            // 
            portDeviceBox.FormattingEnabled = true;
            portDeviceBox.Location = new Point(308, 12);
            portDeviceBox.Name = "portDeviceBox";
            portDeviceBox.Size = new Size(142, 25);
            portDeviceBox.TabIndex = 4;
            // 
            // comBtn
            // 
            comBtn.Location = new Point(456, 14);
            comBtn.Name = "comBtn";
            comBtn.Size = new Size(142, 23);
            comBtn.TabIndex = 5;
            comBtn.Text = "打开串口";
            comBtn.UseVisualStyleBackColor = true;
            comBtn.Click += ComBtnClick;
            // 
            // serialPortLabel
            // 
            serialPortLabel.AutoSize = true;
            serialPortLabel.Location = new Point(604, 20);
            serialPortLabel.Name = "serialPortLabel";
            serialPortLabel.Size = new Size(44, 17);
            serialPortLabel.TabIndex = 6;
            serialPortLabel.Text = "未连接";
            // 
            // kbLabel
            // 
            kbLabel.AutoSize = true;
            kbLabel.Location = new Point(789, 15);
            kbLabel.Name = "kbLabel";
            kbLabel.Size = new Size(32, 17);
            kbLabel.TabIndex = 9;
            kbLabel.Text = "键盘";
            // 
            // mousePosLabel
            // 
            mousePosLabel.AutoSize = true;
            mousePosLabel.Location = new Point(689, 15);
            mousePosLabel.Name = "mousePosLabel";
            mousePosLabel.Size = new Size(32, 17);
            mousePosLabel.TabIndex = 10;
            mousePosLabel.Text = "鼠标";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(858, 443);
            Controls.Add(mousePosLabel);
            Controls.Add(kbLabel);
            Controls.Add(serialPortLabel);
            Controls.Add(comBtn);
            Controls.Add(portDeviceBox);
            Controls.Add(comboBox1);
            Controls.Add(captureBtn);
            Controls.Add(groupBox1);
            Name = "Form1";
            Text = "Form1";
            Load += Form1_Load;
            groupBox1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)miniPicBox).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private GroupBox groupBox1;
        private PictureBox miniPicBox;
        private Button captureBtn;
        private ComboBox comboBox1;
        private ComboBox portDeviceBox;
        private Button comBtn;
        private Label serialPortLabel;
        private Label kbLabel;
        private Label mousePosLabel;
    }
}
