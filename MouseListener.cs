using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace RemoteControl
{
    internal class MouseListener : IDisposable
    {
        private IntPtr hookID = IntPtr.Zero;
        private LowLevelMouseProc proc;

        private int lastX = 0;
        private int lastY = 0;

        private bool isTracking = false; // 跟踪状态

        private Action<int, int, int, int> mouseMoveCallback;
        private Action<int> mouseWheelCallback;
        private Action<int> mouseButtonDownCallback;
        private Action<int> mouseButtonUpCallback;

        // 新增：记录上次鼠标移动回调时间
        private DateTime lastMouseMoveTime = DateTime.MinValue;
        // 设置回调最小间隔毫秒
        private readonly TimeSpan mouseMoveInterval = TimeSpan.FromMilliseconds(10);

        // 可选：使用一个锁来保证线程安全
        private readonly object mouseMoveLock = new object();

        public MouseListener(
            Action<int, int, int, int> mouseMoveCallback,
            Action<int> mouseWheelCallback,
            Action<int> mouseButtonDownCallback,
            Action<int> mouseButtonUpCallback)
        {
            this.mouseMoveCallback = mouseMoveCallback ?? throw new ArgumentNullException(nameof(mouseMoveCallback));
            this.mouseWheelCallback = mouseWheelCallback ?? throw new ArgumentNullException(nameof(mouseWheelCallback));
            this.mouseButtonDownCallback = mouseButtonDownCallback ?? throw new ArgumentNullException(nameof(mouseButtonDownCallback));
            this.mouseButtonUpCallback = mouseButtonUpCallback ?? throw new ArgumentNullException(nameof(mouseButtonUpCallback));

            proc = HookCallback;
        }

        public void Dispose()
        {
            Stop();
        }

        /// <summary>
        /// 启动鼠标跟踪
        /// </summary>
        public void Start()
        {
            if (isTracking)
            {
                Console.WriteLine("Mouse tracking is already running.");
                return;
            }

            hookID = SetHook(proc);
            isTracking = true;
            Console.WriteLine("Mouse tracking started.");
        }

        /// <summary>
        /// 停止鼠标跟踪
        /// </summary>
        public void Stop()
        {
            if (!isTracking)
            {
                Console.WriteLine("Mouse tracking is not running.");
                return;
            }

            if (hookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(hookID);
                hookID = IntPtr.Zero;
            }

            isTracking = false;
            Console.WriteLine("Mouse tracking stopped.");
        }

        private IntPtr SetHook(LowLevelMouseProc proc)
        {
            using var curProcess = Process.GetCurrentProcess();
            using var curModule = curProcess.MainModule;
            return SetWindowsHookEx(WH_MOUSE_LL, proc,
                GetModuleHandle(curModule.ModuleName), 0);
        }

        private const int WH_MOUSE_LL = 14;

        private IntPtr HookCallback(
            int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                MSLLHOOKSTRUCT mouseStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);

                switch ((int)wParam)
                {
                    case WM_MOUSEMOVE:
                        HandleMouseMove(mouseStruct);
                        break;

                    case WM_MOUSEWHEEL:
                        HandleMouseWheel(mouseStruct);
                        break;

                    case WM_LBUTTONDOWN:
                    case WM_RBUTTONDOWN:
                    case WM_MBUTTONDOWN:
                        mouseButtonDownCallback((int)wParam);
                        break;

                    case WM_LBUTTONUP:
                    case WM_RBUTTONUP:
                    case WM_MBUTTONUP:
                        mouseButtonUpCallback((int)wParam);
                        break;
                }
            }

            return CallNextHookEx(hookID, nCode, wParam, lParam);
        }

        private void HandleMouseMove(MSLLHOOKSTRUCT mouseStruct)
        {

            // 节流处理
            DateTime now = DateTime.UtcNow;
            bool shouldCallback = false;

            lock (mouseMoveLock)
            {
                if (now - lastMouseMoveTime >= mouseMoveInterval)
                {
                    shouldCallback = true;
                    lastMouseMoveTime = now;
                }
            }

            if (shouldCallback)
            {
                // 获取鼠标当前位置
                int currentX = mouseStruct.pt.x;
                int currentY = mouseStruct.pt.y;

                // 计算 Delta 值
                int deltaX = currentX - lastX;
                int deltaY = currentY - lastY;

                // 更新最后位置
                lastX = currentX;
                lastY = currentY;
                // 异步执行回调，避免阻塞钩子线程
                ThreadPool.QueueUserWorkItem(_ => mouseMoveCallback(currentX, currentY, deltaX, deltaY));
            }
        }

        private void HandleMouseWheel(MSLLHOOKSTRUCT mouseStruct)
        {
            // 获取滚轮滚动方向
            int wheelDelta = (short)((mouseStruct.mouseData >> 16) & 0xFFFF);
            // 异步执行回调，避免阻塞钩子线程
            ThreadPool.QueueUserWorkItem(_ => mouseWheelCallback(wheelDelta));
        }

        private const int WM_MOUSEMOVE = 0x0200;
        private const int WM_MOUSEWHEEL = 0x020A;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_RBUTTONUP = 0x0205;
        private const int WM_MBUTTONDOWN = 0x0207;
        private const int WM_MBUTTONUP = 0x0208;

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}