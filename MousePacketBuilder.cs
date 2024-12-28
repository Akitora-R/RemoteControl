namespace RemoteControl
{

    class PacketBuilder
    {
        public static byte[] BuildCommandPacket(byte address, byte command, byte[] data)
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
    }
    internal class MousePacketBuilder
    {
        private byte key = 0x00;

        public byte[] OnKeyPress(int i)
        {
            key |= (byte)(1 << (i - 513) / 3);
            return BuildMousePackat(0, 0);
        }
        public byte[] OnKeyUp(int i)
        {
            key &= (byte)~(1 << (i - 514) / 3);
            return BuildMousePackat(0, 0);
        }

        public byte[] BuildMousePackat(int deltaX, int deltaY)
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
            return PacketBuilder.BuildCommandPacket(0x00, 0x05, data);
        }

        public byte[] BuildMouseWheelPackat(int i)
        {
            i /= 12;
            byte b;
            if (i >= 0)
            {
                b = (byte)(i & 0x7F);
            }
            else
            {
                b = (byte)(i & 0x7F | 0x80);
            }
            byte[] data =
            [
                // 第一个字节固定为 0x01
                0x01,
                // 第二个字节：鼠标按键值
                key, // 仅取最低 3 位，表示中键、右键、左键
                // 第三个字节：X 方向移动距离
                0x00,
                // 第四个字节：Y 方向移动距离
                0x00,
                // 第五个字节：滚轮滚动值，默认为 0（无滚动）
                b, // 可根据需求添加滚动逻辑
            ];

            // 构建最终命令包
            return PacketBuilder.BuildCommandPacket(0x00, 0x05, data);
        }
    }
}
