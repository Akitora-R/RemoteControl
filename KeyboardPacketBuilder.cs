namespace RemoteControl
{
    internal class KeyboardPacketBuilder
    {
        public static readonly Dictionary<Keys, byte> KeysToHidUsage = new()
        {
            // 主键盘区（字母）
            { Keys.A, 0x04 },
            { Keys.B, 0x05 },
            { Keys.C, 0x06 },
            { Keys.D, 0x07 },
            { Keys.E, 0x08 },
            { Keys.F, 0x09 },
            { Keys.G, 0x0A },
            { Keys.H, 0x0B },
            { Keys.I, 0x0C },
            { Keys.J, 0x0D },
            { Keys.K, 0x0E },
            { Keys.L, 0x0F },
            { Keys.M, 0x10 },
            { Keys.N, 0x11 },
            { Keys.O, 0x12 },
            { Keys.P, 0x13 },
            { Keys.Q, 0x14 },
            { Keys.R, 0x15 },
            { Keys.S, 0x16 },
            { Keys.T, 0x17 },
            { Keys.U, 0x18 },
            { Keys.V, 0x19 },
            { Keys.W, 0x1A },
            { Keys.X, 0x1B },
            { Keys.Y, 0x1C },
            { Keys.Z, 0x1D },

            // 主键盘区（数字行）
            { Keys.D1, 0x1E },
            { Keys.D2, 0x1F },
            { Keys.D3, 0x20 },
            { Keys.D4, 0x21 },
            { Keys.D5, 0x22 },
            { Keys.D6, 0x23 },
            { Keys.D7, 0x24 },
            { Keys.D8, 0x25 },
            { Keys.D9, 0x26 },
            { Keys.D0, 0x27 },

            // 主键盘区（符号）
            { Keys.Enter,      0x28 },
            { Keys.Escape,     0x29 },
            { Keys.Back,       0x2A }, // Backspace
            { Keys.Tab,        0x2B },
            { Keys.Space,      0x2C },
            { Keys.OemMinus,   0x2D }, // '-' (横杠)
            { Keys.Oemplus,    0x2E }, // '=' (前提是不分数字小键盘)
            { Keys.OemOpenBrackets,  0x2F }, // '['
            { Keys.OemCloseBrackets, 0x30 }, // ']'
            { Keys.OemPipe,    0x31 }, // '\\'
            { Keys.OemSemicolon, 0x33 }, // ';'
            { Keys.OemQuotes,  0x34 }, // '\''
            { Keys.Oemtilde,   0x35 }, // '`~'
            { Keys.Oemcomma,   0x36 }, // ','
            { Keys.OemPeriod,  0x37 }, // '.'
            { Keys.OemQuestion,0x38 }, // '/?'
            { Keys.CapsLock,   0x39 },

            // 功能键 F1~F12
            { Keys.F1,  0x3A },
            { Keys.F2,  0x3B },
            { Keys.F3,  0x3C },
            { Keys.F4,  0x3D },
            { Keys.F5,  0x3E },
            { Keys.F6,  0x3F },
            { Keys.F7,  0x40 },
            { Keys.F8,  0x41 },
            { Keys.F9,  0x42 },
            { Keys.F10, 0x43 },
            { Keys.F11, 0x44 },
            { Keys.F12, 0x45 },

            // 一些系统/控制键
            { Keys.PrintScreen, 0x46 },
            { Keys.Scroll,      0x47 }, // Scroll Lock
            { Keys.Pause,       0x48 },
            { Keys.Insert,      0x49 },
            { Keys.Home,        0x4A },
            { Keys.PageUp,      0x4B },
            { Keys.Delete,      0x4C },
            { Keys.End,         0x4D },
            { Keys.PageDown,    0x4E },
            { Keys.Right,       0x4F },
            { Keys.Left,        0x50 },
            { Keys.Down,        0x51 },
            { Keys.Up,          0x52 },
            { Keys.NumLock,     0x53 },

            // 小键盘区（若需要可继续补充）
            { Keys.Divide,   0x54 }, // '/' (num pad)
            { Keys.Multiply, 0x55 }, // '*' (num pad)
            { Keys.Subtract, 0x56 }, // '-' (num pad)
            { Keys.Add,      0x57 }, // '+' (num pad)
            // Keypad Enter 是 0x58，但在 .NET 中往往和普通 Enter 同一个 Keys 枚举，实际开发可视情况处理
            { Keys.NumPad1,  0x59 },
            { Keys.NumPad2,  0x5A },
            { Keys.NumPad3,  0x5B },
            { Keys.NumPad4,  0x5C },
            { Keys.NumPad5,  0x5D },
            { Keys.NumPad6,  0x5E },
            { Keys.NumPad7,  0x5F },
            { Keys.NumPad8,  0x60 },
            { Keys.NumPad9,  0x61 },
            { Keys.NumPad0,  0x62 },
            { Keys.Decimal,   0x63 },

            // 修饰键（左右 Shift / Ctrl / Alt / Win 等），CH9329中往往在 0xE0~0xE7
            // 这里仅示例左 Shift 与右 Shift，可按需添加
            { Keys.LShiftKey, 0xE1 },
            { Keys.RShiftKey, 0xE5 },
            { Keys.LControlKey, 0xE0 },
            { Keys.RControlKey, 0xE4 },
            { Keys.LMenu, 0xE2 },  // 左 Alt
            { Keys.RMenu, 0xE6 },  // 右 Alt
            { Keys.LWin, 0xE3 },
            { Keys.RWin, 0xE7 },
        };

        public static readonly Dictionary<Keys, byte> ControlKeyMask = new()
        {
            {Keys.LControlKey,1 },
            {Keys.LShiftKey,1<<1 },
            {Keys.LMenu,1<<2 },
            {Keys.LWin,1<<3 },
            {Keys.RControlKey,1<<4 },
            {Keys.RShiftKey,1<<5 },
            {Keys.RMenu,1<<6 },
            {Keys.RWin,1<<7 },
        };

        private byte controls = 0x00;
        private readonly LinkedList<byte> normKeys = new([0x00, 0x00, 0x00, 0x00, 0x00, 0x00]);
        private readonly object l = new();

        public byte[] Reset()
        {
            lock (l)
            {
                controls = 0x00;
                normKeys.Clear();
                for (var i = 0; i < 6; i++)
                {
                    normKeys.AddLast(0x00);
                }
                return PacketBuilder.BuildCommandPacket(0x00, 0x82, [0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]);
            }
        }

        public byte[] OnKeyPress(Keys key)
        {
            lock (l)
            {
                if (ControlKeyMask.TryGetValue(key, out byte value))
                {
                    controls |= value;
                }
                else if (KeysToHidUsage.TryGetValue(key, out byte v))
                {
                    if (normKeys.Remove(v))
                    {

                        normKeys.AddLast(v);
                    }
                    else
                    {
                        normKeys.RemoveFirst();
                        normKeys.AddLast(v);
                    }
                }
                return PacketBuilder.BuildCommandPacket(0x00, 0x02, [controls, 0x00, .. normKeys]);
            }
        }

        public byte[] OnKeyUp(Keys key)
        {
            lock (l)
            {
                if (ControlKeyMask.TryGetValue(key, out byte value))
                {
                    controls &= (byte)~value;
                }
                else if (KeysToHidUsage.TryGetValue(key, out byte v))
                {
                    normKeys.Remove(v);
                    for (var i = 0; i < 6 - normKeys.Count; i++)
                    {
                        normKeys.AddLast(0x00);
                    }
                }
                return PacketBuilder.BuildCommandPacket(0x00, 0x02, [controls, 0x00, .. normKeys]);
            }
        }
    }
}
