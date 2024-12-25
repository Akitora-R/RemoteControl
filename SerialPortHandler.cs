using System;
using System.IO.Ports;

namespace RemoteControl
{
    public class Ch9329ResponseEventArgs : EventArgs
    {
        public byte Address { get; }
        public byte Command { get; }
        public byte[] Data { get; }
        public byte Checksum { get; }
        public bool IsValid { get; }

        public Ch9329ResponseEventArgs(byte address, byte command, byte[] data, byte checksum, bool isValid)
        {
            Address = address;
            Command = command;
            Data = data;
            Checksum = checksum;
            IsValid = isValid;
        }
    }

    public class SerialPortHandler
    {
        private readonly SerialPort serialPort;
        private readonly int Timeout = 500;
        private readonly EventHandler<Ch9329ResponseEventArgs> responseHandler;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="portName">串口名称（如 COM3）</param>
        /// <param name="baudRate">波特率</param>
        /// <param name="responseHandler">自定义的响应处理事件</param>
        public SerialPortHandler(string portName, int baudRate, EventHandler<Ch9329ResponseEventArgs> responseHandler)
        {
            this.responseHandler = responseHandler ?? throw new ArgumentNullException(nameof(responseHandler));

            Console.WriteLine($"初始化串口 {portName} {baudRate}");
            serialPort = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One)
            {
                ReadTimeout = Timeout,
                WriteTimeout = Timeout
            };
            serialPort.DataReceived += OnDataReceived;
        }

        /// <summary>
        /// 打开串口
        /// </summary>
        public void OpenPort()
        {
            try
            {
                serialPort.Open();
            }
            catch (Exception ex)
            {

            }
        }

        public bool IsOpen()
        {
            return serialPort.IsOpen;
        }

        public string PortName()
        {
            return serialPort.PortName;
        }

        /// <summary>
        /// 处理串口接收数据事件
        /// </summary>
        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                int bytesToRead = serialPort.BytesToRead;
                if (bytesToRead > 0)
                {
                    byte[] buffer = new byte[bytesToRead];
                    serialPort.Read(buffer, 0, bytesToRead);

                    // 检查数据包是否有效
                    if (buffer.Length >= 6 && buffer[0] == 0x57 && buffer[1] == 0xAB)
                    {
                        byte address = buffer[2];
                        byte command = buffer[3];
                        byte length = buffer[4];
                        byte[] data = new byte[length];
                        Array.Copy(buffer, 5, data, 0, length);
                        byte checksum = buffer[5 + length];

                        // 验证累加和
                        byte calculatedChecksum = (byte)(0x57 + 0xAB + address + command + length);
                        foreach (byte b in data)
                        {
                            calculatedChecksum += b;
                        }

                        bool isValid = calculatedChecksum == checksum;

                        // 触发事件
                        responseHandler?.Invoke(this, new Ch9329ResponseEventArgs(address, command, data, checksum, isValid));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in OnDataReceived: {ex.Message}");
            }
        }

        /// <summary>
        /// 发送命令包
        /// </summary>
        /// <param name="packet">命令包</param>
        public void SendCommand(byte[] packet)
        {
            if (serialPort != null && serialPort.IsOpen)
            {
                serialPort.Write(packet, 0, packet.Length);
            }
            else
            {
                throw new InvalidOperationException("Serial port is not open.");
            }
        }

        /// <summary>
        /// 关闭串口
        /// </summary>
        public void ClosePort()
        {
            if (serialPort != null && serialPort.IsOpen)
            {
                serialPort.Close();
            }
        }
    }
}
