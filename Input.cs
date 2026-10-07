using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace PixelTrek
{
    internal static class Native
    {
        internal delegate IntPtr HookProc(int code, IntPtr message, IntPtr data);
        [StructLayout(LayoutKind.Sequential)] internal struct Point { internal int X, Y; }
        [StructLayout(LayoutKind.Sequential)] internal struct MouseData { internal Point Point; internal uint MouseDataValue, Flags, Time; internal UIntPtr Extra; }
        [StructLayout(LayoutKind.Sequential)] internal struct RawDevice { internal ushort Page, Usage; internal uint Flags; internal IntPtr Target; }
        [DllImport("user32.dll", SetLastError = true)] internal static extern IntPtr SetWindowsHookEx(int id, HookProc callback, IntPtr module, uint thread);
        [DllImport("user32.dll")] internal static extern bool UnhookWindowsHookEx(IntPtr hook);
        [DllImport("user32.dll")] internal static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr message, IntPtr data);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] internal static extern IntPtr GetModuleHandle(string module);
        [DllImport("user32.dll", SetLastError = true)] internal static extern bool RegisterRawInputDevices([In] RawDevice[] devices, uint count, uint size);
        [DllImport("user32.dll")] internal static extern uint GetRawInputData(IntPtr raw, uint command, IntPtr data, ref uint size, uint headerSize);
        [DllImport("user32.dll")] internal static extern bool PostMessage(IntPtr handle, int message, IntPtr a, IntPtr b);
        [DllImport("user32.dll")] internal static extern bool GetCursorPos(out Point point);
        [DllImport("wtsapi32.dll")] internal static extern bool WTSRegisterSessionNotification(IntPtr handle, uint flags);
        [DllImport("wtsapi32.dll")] internal static extern bool WTSUnRegisterSessionNotification(IntPtr handle);
    }
    public sealed class InputEngine : IDisposable
    {
        readonly Tracker tracker;
        readonly Thread thread;
        readonly ManualResetEvent ready = new ManualResetEvent(false);
        readonly MotionCounter motion = new MotionCounter();
        readonly KeyCounter keys = new KeyCounter();
        public readonly Kinematics Live = new Kinematics();
        static double Clock { get { return Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency; } }
        Native.HookProc mouseCallback;
        IntPtr mouseHook, rawWindow;
        ApplicationContext context;
        System.Windows.Forms.Timer sampleTimer;
        volatile bool suspended, disposed;
        public string Error { get; private set; }
        public InputEngine(Tracker tracker)
        {
            this.tracker = tracker;
            thread = new Thread(Run) { IsBackground = true, Name = "PixelTrek input" };
            thread.SetApartmentState(ApartmentState.STA);
        }
        public bool Start() { thread.Start(); return ready.WaitOne(5000) && Error == null; }
        void Run()
        {
            InputWindow window = null;
            System.Windows.Forms.Timer sample = null;
            try
            {
                context = new ApplicationContext();
                window = new InputWindow(this); rawWindow = window.Handle;
                var device = new Native.RawDevice { Page = 1, Usage = 6, Flags = 0x2100, Target = rawWindow }; // INPUTSINK + DEVNOTIFY
                if (!Native.RegisterRawInputDevices(new [] { device }, 1, (uint)Marshal.SizeOf(typeof(Native.RawDevice)))) throw new Win32Exception(Marshal.GetLastWin32Error());
                mouseCallback = Mouse;
                InstallMouse(); ready.Set();
                sample = new System.Windows.Forms.Timer { Interval = 100 };
                sampleTimer = sample;
                sample.Tick += delegate
                {
                    if (suspended || tracker.Paused) { Live.Reset(Clock); sample.Stop(); return; }
                    LiveRates rates = Live.Sample(Clock);
                    tracker.ObserveRates(rates.Speed, rates.Acceleration, DateTime.UtcNow);
                    if (rates.Speed == 0 && rates.Acceleration == 0) sample.Stop();
                };
                Application.Run(context);
            }
            catch (Exception ex) { Error = "Input tracking is unavailable: " + ex.Message; ready.Set(); }
            finally
            {
                if (sample != null) sample.Dispose(); sampleTimer = null;
                if (mouseHook != IntPtr.Zero) Native.UnhookWindowsHookEx(mouseHook);
                if (window != null) window.CloseWindow();
                rawWindow = IntPtr.Zero;
            }
        }
        void InstallMouse()
        {
            if (mouseHook != IntPtr.Zero) Native.UnhookWindowsHookEx(mouseHook);
            motion.Reset(); Native.Point current;
            Live.Reset(Clock);
            if (sampleTimer != null) sampleTimer.Stop();
            if (Native.GetCursorPos(out current)) motion.Move(current.X, current.Y, true);
            mouseHook = Native.SetWindowsHookEx(14, mouseCallback, Native.GetModuleHandle(null), 0);
            if (mouseHook == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error());
        }
        IntPtr Mouse(int code, IntPtr message, IntPtr data)
        {
            if (code >= 0)
            {
                var m = (Native.MouseData)Marshal.PtrToStructure(data, typeof(Native.MouseData));
                ProcessMouse(message.ToInt32(), m.Point.X, m.Point.Y, (m.Flags & 1) != 0, DateTime.UtcNow);
            }
            return Native.CallNextHookEx(mouseHook, code, message, data);
        }
        internal void ProcessMouse(int message, int x, int y, bool injected, DateTime utc)
        {
            bool ignore = suspended || tracker.Paused || injected;
            if (message == 0x200)
            {
                double distance = motion.Move(x, y, ignore);
                if (ignore) { Live.Reset(Clock); }
                else if (distance > 0)
                {
                    if (sampleTimer != null && !sampleTimer.Enabled) { Live.Reset(Clock); sampleTimer.Start(); }
                    Live.Add(motion.DeltaX, motion.DeltaY, distance); tracker.Add(distance, 0, 0, utc);
                }
            }
            else if (!ignore && (message == 0x201 || message == 0x204 || message == 0x207 || message == 0x20B))
            {
                tracker.Add(0,0,1,utc);
            }
        }
        internal void ProcessKey(IntPtr device, int scan, int flags, int virtualKey, DateTime utc)
        {
            if (virtualKey == 255 || suspended) return;
            if (scan == 0) scan = virtualKey | 0x8000;
            bool fresh = keys.Change(device, scan, flags & 6, (flags & 1) != 0);
            if (fresh && !tracker.Paused) { tracker.Add(0, 1, 0, utc); }
            if (virtualKey == 0x13) keys.Reset();
        }
        public void Suspend(bool value) { suspended = value; Live.Reset(Clock); Reset(); }
        public void Reset() { if (rawWindow != IntPtr.Zero) Native.PostMessage(rawWindow, 0x8001, IntPtr.Zero, IntPtr.Zero); }
        public void Dispose()
        {
            if (disposed) return; disposed = true;
            if (rawWindow != IntPtr.Zero) Native.PostMessage(rawWindow, 0x8002, IntPtr.Zero, IntPtr.Zero);
            if (thread.IsAlive) thread.Join(2000);
            ready.Dispose();
        }
        sealed class InputWindow : NativeWindow
        {
            readonly InputEngine owner;
            readonly IntPtr buffer = Marshal.AllocHGlobal(512);
            readonly uint headerSize = (uint)(8 + 2 * IntPtr.Size);
            public InputWindow(InputEngine owner) { this.owner = owner; CreateHandle(new CreateParams { Caption = "PixelTrek input sink", Parent = new IntPtr(-3) }); }
            public void CloseWindow() { DestroyHandle(); Marshal.FreeHGlobal(buffer); }
            protected override void WndProc(ref Message m)
            {
                if (m.Msg == 0xFF)
                {
                    uint size = 512;
                    uint result = Native.GetRawInputData(m.LParam, 0x10000003, buffer, ref size, headerSize);
                    if (result != UInt32.MaxValue && result >= headerSize + 16 && Marshal.ReadInt32(buffer) == 1)
                    {
                        int offset = (int)headerSize;
                        int scan = (ushort)Marshal.ReadInt16(buffer, offset);
                        int flags = (ushort)Marshal.ReadInt16(buffer, offset + 2);
                        int virtualKey = (ushort)Marshal.ReadInt16(buffer, offset + 6);
                        IntPtr device = Marshal.ReadIntPtr(buffer, 8);
                        owner.ProcessKey(device, scan, flags, virtualKey, DateTime.UtcNow);
                    }
                }
                else if (m.Msg == 0xFE) owner.keys.Reset(); // A keyboard was connected/disconnected.
                else if (m.Msg == 0x8001) { owner.motion.Reset(); owner.keys.Reset(); try { owner.InstallMouse(); } catch (Exception ex) { owner.Error = ex.Message; } }
                else if (m.Msg == 0x8002) owner.context.ExitThread();
                base.WndProc(ref m);
            }
        }
    }
}
