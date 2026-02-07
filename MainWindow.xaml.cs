using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Forms = System.Windows.Forms;

namespace FocusMask
{
    public partial class MainWindow : Window
    {
        private double _targetOpacity = 0.75;
        private double _currentOpacity = 0.75;
        private Rect _targetRect = new Rect(0, 0, 0, 0);
        private Rect _currentRect = new Rect(0, 0, 0, 0);
        private bool _isActive = true;
        private bool _isDesktop = false;

        private Forms.NotifyIcon _notifyIcon;
        private DispatcherTimer _animTimer;

        private IntPtr _hWinEventHook;
        private IntPtr _hKeyboardHook;
        private IntPtr _hMouseHook;

        private WinEventDelegate _winEventDel;
        private LowLevelProc _keyboardDel;
        private LowLevelProc _mouseDel;

        private long _lastAltTime = 0;
        private bool _isAdjustingOpacity = false;

        public MainWindow()
        {
            InitializeComponent();
            InitializeTray();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.Left = SystemParameters.VirtualScreenLeft;
            this.Top = SystemParameters.VirtualScreenTop;
            this.Width = SystemParameters.VirtualScreenWidth;
            this.Height = SystemParameters.VirtualScreenHeight;

            SetWindowExTransparent();

            _winEventDel = new WinEventDelegate(WinEventProc);
            _hWinEventHook = SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_OBJECT_LOCATIONCHANGE, IntPtr.Zero, _winEventDel, 0, 0, WINEVENT_OUTOFCONTEXT | WINEVENT_SKIPOWNPROCESS);

            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                _keyboardDel = HookKeyboardCallback;
                _hKeyboardHook = SetWindowsHookEx(WH_KEYBOARD_LL, _keyboardDel, GetModuleHandle(curModule.ModuleName), 0);

                _mouseDel = HookMouseCallback;
                _hMouseHook = SetWindowsHookEx(WH_MOUSE_LL, _mouseDel, GetModuleHandle(curModule.ModuleName), 0);
            }

            _animTimer = new DispatcherTimer(DispatcherPriority.Render);
            _animTimer.Interval = TimeSpan.FromMilliseconds(16);
            _animTimer.Tick += AnimationLoop;
            _animTimer.Start();

            UpdateTarget(GetForegroundWindow());
        }

        private void AnimationLoop(object sender, EventArgs e)
        {
            double goalAlpha = (_isActive && !_isDesktop) ? _targetOpacity : 0.0;

            if (Math.Abs(_currentOpacity - goalAlpha) > 0.01)
                _currentOpacity += (goalAlpha - _currentOpacity) * 0.2;
            else
                _currentOpacity = goalAlpha;

            MaskBrush.Opacity = _currentOpacity;

            if (_isActive && !_isDesktop)
            {
                double speed = 0.25;
                _currentRect = new Rect(
                    _currentRect.X + (_targetRect.X - _currentRect.X) * speed,
                    _currentRect.Y + (_targetRect.Y - _currentRect.Y) * speed,
                    _currentRect.Width + (_targetRect.Width - _currentRect.Width) * speed,
                    _currentRect.Height + (_targetRect.Height - _currentRect.Height) * speed
                );

                if (_currentRect.Width < 1 || _currentRect.Height < 1)
                    HoleGeometry.Rect = new Rect(0, 0, 0, 0);
                else
                    HoleGeometry.Rect = _currentRect;
            }
        }

        private IntPtr HookKeyboardCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && ((int)wParam == WM_KEYUP || (int)wParam == WM_SYSKEYUP))
            {
                int vkCode = Marshal.ReadInt32(lParam);
                if (vkCode == VK_LMENU || vkCode == VK_RMENU)
                {
                    if (_isAdjustingOpacity)
                    {
                        _isAdjustingOpacity = false;
                        _lastAltTime = 0;
                    }
                    else
                    {
                        long now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                        if (now - _lastAltTime < 400)
                        {
                            ToggleSwitch();
                            _lastAltTime = 0;
                        }
                        else
                        {
                            _lastAltTime = now;
                        }
                    }
                }
            }
            return CallNextHookEx(_hKeyboardHook, nCode, wParam, lParam);
        }

        private IntPtr HookMouseCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (int)wParam == WM_MOUSEWHEEL)
            {
                if ((GetAsyncKeyState(VK_MENU) & 0x8000) != 0)
                {
                    MSLLHOOKSTRUCT hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                    short delta = (short)((hookStruct.mouseData >> 16) & 0xFFFF);

                    if (delta > 0) _targetOpacity = Math.Max(0.1, _targetOpacity - 0.05);
                    else _targetOpacity = Math.Min(0.95, _targetOpacity + 0.05);

                    _isAdjustingOpacity = true;
                    return (IntPtr)1;
                }
            }
            return CallNextHookEx(_hMouseHook, nCode, wParam, lParam);
        }

        private void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (eventType == EVENT_SYSTEM_FOREGROUND || eventType == EVENT_OBJECT_LOCATIONCHANGE)
            {
                if (idObject == 0 && hwnd == GetForegroundWindow())
                {
                    UpdateTarget(hwnd);
                }
            }
        }

        private void UpdateTarget(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero) return;
            if (IsExcluded(hwnd))
            {
                _isDesktop = true;
                return;
            }

            _isDesktop = false;

            RECT rect;
            int hr = DwmGetWindowAttribute(hwnd, DWMWA_EXTENDED_FRAME_BOUNDS, out rect, Marshal.SizeOf<RECT>());
            if (hr != 0) GetWindowRect(hwnd, out rect);

            var source = PresentationSource.FromVisual(this);
            if (source?.CompositionTarget != null)
            {
                var dpiX = source.CompositionTarget.TransformToDevice.M11;
                var dpiY = source.CompositionTarget.TransformToDevice.M22;

                double l = rect.Left / dpiX;
                double t = rect.Top / dpiY;
                double w = (rect.Right - rect.Left) / dpiX;
                double h = (rect.Bottom - rect.Top) / dpiY;

                l -= this.Left;
                t -= this.Top;

                _targetRect = new Rect(l, t, w, h);
            }
        }

        private bool IsExcluded(IntPtr hwnd)
        {
            if (hwnd == new WindowInteropHelper(this).Handle) return true;

            System.Text.StringBuilder sb = new System.Text.StringBuilder(256);
            GetClassName(hwnd, sb, 256);
            string className = sb.ToString();

            if (className == "Shell_TrayWnd" || className == "Progman" || className == "WorkerW") return true;

            return false;
        }

        private void ToggleSwitch()
        {
            _isActive = !_isActive;
            UpdateTarget(GetForegroundWindow());
        }

        private void InitializeTray()
        {
            _notifyIcon = new Forms.NotifyIcon();
            _notifyIcon.Icon = System.Drawing.SystemIcons.Shield;
            _notifyIcon.Visible = true;
            _notifyIcon.Text = "Focus Mask (双击 Alt 开关)";

            var menu = new Forms.ContextMenuStrip();
            menu.Items.Add("退出", null, (s, e) => Close());
            _notifyIcon.ContextMenuStrip = menu;
        }

        private void SetWindowExTransparent()
        {
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_hWinEventHook != IntPtr.Zero) UnhookWinEvent(_hWinEventHook);
            if (_hKeyboardHook != IntPtr.Zero) UnhookWindowsHookEx(_hKeyboardHook);
            if (_hMouseHook != IntPtr.Zero) UnhookWindowsHookEx(_hMouseHook);
            if (_notifyIcon != null) _notifyIcon.Dispose();
        }

        private delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);
        private delegate IntPtr LowLevelProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")] static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);
        [DllImport("user32.dll")] static extern bool UnhookWinEvent(IntPtr hWinEventHook);
        [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
        [DllImport("user32.dll")] static extern int GetWindowLong(IntPtr hwnd, int index);
        [DllImport("user32.dll")] static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);
        [DllImport("user32.dll")] static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)] static extern IntPtr SetWindowsHookEx(int idHook, LowLevelProc lpfn, IntPtr hMod, uint dwThreadId);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)][return: MarshalAs(UnmanagedType.Bool)] static extern bool UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)] static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)] static extern IntPtr GetModuleHandle(string lpModuleName);
        [DllImport("user32.dll")] static extern short GetAsyncKeyState(int vKey);
        [DllImport("dwmapi.dll")] static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out RECT pvAttribute, int cbAttribute);

        [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
        [StructLayout(LayoutKind.Sequential)] private struct MSLLHOOKSTRUCT { public System.Drawing.Point pt; public uint mouseData; public uint flags; public uint time; public IntPtr dwExtraInfo; }

        const int GWL_EXSTYLE = -20;
        const int WS_EX_TRANSPARENT = 0x00000020;
        const int WS_EX_TOOLWINDOW = 0x00000080;
        const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
        const uint EVENT_OBJECT_LOCATIONCHANGE = 0x800B;
        const uint WINEVENT_OUTOFCONTEXT = 0x0000;
        const uint WINEVENT_SKIPOWNPROCESS = 0x0002;
        const int WH_KEYBOARD_LL = 13;
        const int WH_MOUSE_LL = 14;
        const int WM_KEYUP = 0x0101;
        const int WM_SYSKEYUP = 0x0105;
        const int WM_MOUSEWHEEL = 0x020A;
        const int VK_LMENU = 0xA4;
        const int VK_RMENU = 0xA5;
        const int VK_MENU = 0x12;
        const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;
    }
}