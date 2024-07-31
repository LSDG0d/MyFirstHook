using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

class Program
{
    //Insert - Start
    //Delete - Stop
    //Home - Exit
    private static void Main()
    {
        nint hookid;
        hookid = SetHook();
        Console.WriteLine("App start. Main process = " + MainProcess.ToString());
        Application.Run();
        Console.WriteLine("Unhook");
        UnhookWindowsHookEx(hookid);
    }
    #region DllImports
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProcDelegate lpfn, IntPtr lMod, int dwThreadId);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hHook);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
    [DllImport("Kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetModuleHandle(IntPtr lpModuleName);
    [DllImport("user32.dll")]
    private static extern IntPtr MessageBox(IntPtr hWnd, string text, string caption, int options);
    [DllImport("user32.dll")]
    private static extern void SetWindowText(IntPtr hWnd, string text);
    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(ref Point lpPoint);
    [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
    private static extern void mouse_event(uint dwFlags, int dx, int dy, int dwData, int dwExtraInfo);
    #endregion
    #region MouseEventData
    //Absolute coordinates
    private const int MOUSEEVENTF_ABSOLUTE = 0x8000;
    //LKM
    private const int MOUSEEVENTF_LEFTDOWN = 0x0002;
    //LKMUP
    private const int MOUSEEVENTF_LEFTUP = 0x0004;
    //PKM
    private const int MOUSEEVENTF_RIGHTDOWN = 0x0008;
    //PKMUP
    private const int MOUSEEVENTF_RIGHTUP = 0x0010;
    //MouseMove
    private const int MOUSEEVENTF_MOVE = 0x0001;
    #endregion
    #region KeyBoardData
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private static LowLevelKeyboardProcDelegate m_callback = LowLevelKeyboardHookProc;
    private static IntPtr m_hHook;
    #endregion
    #region LowLevelKeyboardProcDelegate
    private delegate IntPtr LowLevelKeyboardProcDelegate(int nCode, IntPtr wParam, IntPtr lParam);
    #endregion
    #region LowLevelKeyboardHookProc
    private static IntPtr LowLevelKeyboardHookProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var khs = ((KeyboardHookStruct)Marshal.PtrToStructure(lParam, typeof(KeyboardHookStruct)));
            if (khs.VirtualKeyCode == 45) //Insert
            {
                if (!IsStart)
                {
                    StartClicks();
                    Console.WriteLine("Start = " + IsStart.ToString());
                }
            }
            if (khs.VirtualKeyCode == 46) //Delete
            {
                if (IsStart)
                {
                    StopClicks();
                    Console.WriteLine("Start = " + IsStart.ToString());
                }
            }
            if(khs.VirtualKeyCode == 36) //Home
            {
                CloseApp();
            }
        }
        return CallNextHookEx(m_hHook, nCode, wParam, lParam);
    }
    #endregion
    #region SetHook
    public static nint SetHook()
    {
        try
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, m_callback, GetModuleHandle(IntPtr.Zero), 0);
            }
        }
        catch
        {
            return nint.Zero;
        }
    }
    #endregion
    #region KeyboardHookStruct
    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardHookStruct
    {
        public readonly int VirtualKeyCode;
        public readonly int ScanCode;
        public readonly int Flags;
        public readonly int Time;
        public readonly IntPtr ExtraInfo;
    }
    #endregion
    #region Params
    private static bool IsStart { get; set; } = false;
    private static IntPtr descriptor { get; set; } = 0;
    private static Point MousePosition { get; set; }
    private static Point MouseLastIterationPosition { get; set; }
    private static bool MainProcessIsActive { get; set; } = false;
    private static Task MainProcess
    {
        get
        {
            lock (StartProcess())
                return Task.FromResult(Process.GetCurrentProcess());
        }
    }
    #endregion
    #region MainLogic
    private static async Task StartProcess()
    {
        if (MainProcessIsActive) { return; }
        MainProcessIsActive = true;
        while (true)
        {
            if (IsStart)
            {
                Click();
                await Task.Delay(10);
            }
            await Task.Delay(10);
        }
    }
    #endregion
    #region CursorPositionToParam
    private static async void SetCursorPosition()
    {
        Point point = new Point();
        GetCursorPos(ref point);
        MousePosition = point;
    }
    private static async void SetLastIterationCursorPosition()
    {
        Point point = new Point();
        GetCursorPos(ref point);
        MouseLastIterationPosition = point;
    }
    #endregion
    #region CloseApp
    private static void CloseApp()
    {
        Environment.Exit(0);
    }
    #endregion
    #region Start/Stop clicker
    private static async void StartClicks()
    {
        IsStart = true;
    }
    private static async void StopClicks()
    {
        IsStart = false;
    }
    #endregion
    #region ClickEvent
    private static async void Click()
    {
        Size resolution = Screen.PrimaryScreen.Bounds.Size;
        int X = 65535 / resolution.Width * Convert.ToInt32(MousePosition.X.ToString());
        int Y = 65535 / resolution.Height * Convert.ToInt32(MousePosition.Y.ToString());
        mouse_event(MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, X, Y, 0, 0);
    }
    #endregion
    #region MoveMouse
    private static async void MoveMouse()
    {
        SetLastIterationCursorPosition();
        Size resolution = Screen.PrimaryScreen.Bounds.Size;
        int X = 65535 / resolution.Width * Convert.ToInt32(MousePosition.X.ToString());
        int Y = 65535 / resolution.Height * Convert.ToInt32(MousePosition.Y.ToString());
        mouse_event(MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_MOVE, X, Y, 0, 0);
    }
    private static async void MoveMouseToLastIteration()
    {
        Size resolution = Screen.PrimaryScreen.Bounds.Size;
        int X = 65535 / resolution.Width * Convert.ToInt32(MouseLastIterationPosition.X.ToString());
        int Y = 65535 / resolution.Height * Convert.ToInt32(MouseLastIterationPosition.Y.ToString());
        mouse_event(MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_MOVE, X, Y, 0, 0);
    }
    #endregion
}
