using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using KeyStats.Helpers;

namespace KeyStats.Services;

public class InputMonitorService : IDisposable
{
    private static InputMonitorService? _instance;
    public static InputMonitorService Instance => _instance ??= new InputMonitorService();

    private IntPtr _keyboardHookId = IntPtr.Zero;
    private IntPtr _mouseHookId = IntPtr.Zero;
    private NativeInterop.LowLevelKeyboardProc? _keyboardProc;
    private NativeInterop.LowLevelMouseProc? _mouseProc;

    private bool _isMonitoring;
    private readonly HashSet<int> _pressedKeys = new(); // 跟踪当前按下的键，防止长按时重复计数
    private readonly double _mouseSampleInterval = 1.0 / 30.0; // 30 FPS
    private DateTime _lastMouseSampleTime = DateTime.MinValue;
    private System.Drawing.Point? _lastMousePosition;
    private double _accumulatedDistance = 0.0;

    public event Action<string, string, string>? KeyPressed;
    public event Action<string, string>? LeftMouseClicked;
    public event Action<string, string>? RightMouseClicked;
    public event Action<string, string>? MiddleMouseClicked;
    public event Action<string, string>? SideBackMouseClicked;
    public event Action<string, string>? SideForwardMouseClicked;
    public event Action<double>? MouseMoved;
    public event Action<double, string, string>? MouseScrolled;

    private InputMonitorService() { }

    public void StartMonitoring()
    {
        if (_isMonitoring) return;

        _keyboardProc = KeyboardHookCallback;
        _mouseProc = MouseHookCallback;

        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule;

        if (curModule != null)
        {
            var moduleHandle = NativeInterop.GetModuleHandle(curModule.ModuleName);
            _keyboardHookId = NativeInterop.SetWindowsHookEx(
                NativeInterop.WH_KEYBOARD_LL,
                _keyboardProc,
                moduleHandle,
                0);

            if (_keyboardHookId == IntPtr.Zero)
            {
                var error = Marshal.GetLastWin32Error();
                Debug.WriteLine($"Failed to install keyboard hook. Error code: {error}");
                throw new System.ComponentModel.Win32Exception(error, "Failed to install keyboard hook");
            }

            _mouseHookId = NativeInterop.SetWindowsHookEx(
                NativeInterop.WH_MOUSE_LL,
                _mouseProc,
                moduleHandle,
                0);

            if (_mouseHookId == IntPtr.Zero)
            {
                var error = Marshal.GetLastWin32Error();
                Debug.WriteLine($"Failed to install mouse hook. Error code: {error}");
                // Clean up keyboard hook before throwing
                if (_keyboardHookId != IntPtr.Zero)
                {
                    NativeInterop.UnhookWindowsHookEx(_keyboardHookId);
                    _keyboardHookId = IntPtr.Zero;
                }
                throw new System.ComponentModel.Win32Exception(error, "Failed to install mouse hook");
            }
        }

        _isMonitoring = true;
        Debug.WriteLine("Input monitoring started successfully");
    }

    public void StopMonitoring()
    {
        if (!_isMonitoring) return;

        if (_keyboardHookId != IntPtr.Zero)
        {
            NativeInterop.UnhookWindowsHookEx(_keyboardHookId);
            _keyboardHookId = IntPtr.Zero;
        }

        if (_mouseHookId != IntPtr.Zero)
        {
            NativeInterop.UnhookWindowsHookEx(_mouseHookId);
            _mouseHookId = IntPtr.Zero;
        }

        // 清空按下的键集合
        _pressedKeys.Clear();

        _isMonitoring = false;
        Debug.WriteLine("Input monitoring stopped");
    }

    private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var message = (int)wParam;
            var hookStruct = Marshal.PtrToStructure<NativeInterop.KBDLLHOOKSTRUCT>(lParam);
            var vkCode = (int)hookStruct.vkCode;

            if (message == NativeInterop.WM_KEYDOWN || message == NativeInterop.WM_SYSKEYDOWN)
            {
                // 只在键第一次按下时记录，忽略长按时的重复按下事件
                if (!_pressedKeys.Contains(vkCode))
                {
                    _pressedKeys.Add(vkCode);
                    var keyName = KeyNameMapper.GetKeyName(vkCode);
                    var activeApp = ActiveWindowManager.GetActiveAppInfo();
                    // 异步触发事件，避免阻塞低级钩子回调
                    ThreadPool.QueueUserWorkItem(_ => KeyPressed?.Invoke(keyName, activeApp.AppName, activeApp.DisplayName));
                }
            }
            else if (message == NativeInterop.WM_KEYUP || message == NativeInterop.WM_SYSKEYUP)
            {
                // 键释放时，从集合中移除
                _pressedKeys.Remove(vkCode);
            }
        }

        return NativeInterop.CallNextHookEx(_keyboardHookId, nCode, wParam, lParam);
    }

    private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var message = (int)wParam;
            var hookStruct = Marshal.PtrToStructure<NativeInterop.MSLLHOOKSTRUCT>(lParam);

            switch (message)
            {
                case NativeInterop.WM_LBUTTONDOWN:
                    {
                        // 在钩子回调中获取进程名，然后异步触发事件
                        var activeApp = ActiveWindowManager.GetActiveAppInfo();
                        ThreadPool.QueueUserWorkItem(_ => LeftMouseClicked?.Invoke(activeApp.AppName, activeApp.DisplayName));
                    }
                    break;

                case NativeInterop.WM_RBUTTONDOWN:
                    {
                        var activeApp = ActiveWindowManager.GetActiveAppInfo();
                        ThreadPool.QueueUserWorkItem(_ => RightMouseClicked?.Invoke(activeApp.AppName, activeApp.DisplayName));
                    }
                    break;

                case NativeInterop.WM_MBUTTONDOWN:
                    {
                        var activeApp = ActiveWindowManager.GetActiveAppInfo();
                        ThreadPool.QueueUserWorkItem(_ => MiddleMouseClicked?.Invoke(activeApp.AppName, activeApp.DisplayName));
                    }
                    break;

                case NativeInterop.WM_XBUTTONDOWN:
                    {
                        var activeApp = ActiveWindowManager.GetActiveAppInfo();
                        var button = NativeInterop.HiWord((int)hookStruct.mouseData);
                        if (button == NativeInterop.XBUTTON2)
                        {
                            ThreadPool.QueueUserWorkItem(_ => SideForwardMouseClicked?.Invoke(activeApp.AppName, activeApp.DisplayName));
                        }
                        else
                        {
                            // Default unknown/legacy side buttons to back.
                            ThreadPool.QueueUserWorkItem(_ => SideBackMouseClicked?.Invoke(activeApp.AppName, activeApp.DisplayName));
                        }
                    }
                    break;

                case NativeInterop.WM_MOUSEMOVE:
                    HandleMouseMove(hookStruct.pt);
                    break;

                case NativeInterop.WM_MOUSEWHEEL:
                case NativeInterop.WM_MOUSEHWHEEL:
                    {
                        var activeApp = ActiveWindowManager.GetActiveAppInfo();
                        var mouseData = hookStruct.mouseData;
                        ThreadPool.QueueUserWorkItem(_ => HandleScroll(mouseData, activeApp.AppName, activeApp.DisplayName));
                    }
                    break;
            }
        }

        return NativeInterop.CallNextHookEx(_mouseHookId, nCode, wParam, lParam);
    }

    private void HandleMouseMove(NativeInterop.POINT pt)
    {
        var now = DateTime.Now;
        var currentPosition = new System.Drawing.Point(pt.x, pt.y);

        // 初始化位置
        if (!_lastMousePosition.HasValue)
        {
            _lastMousePosition = currentPosition;
            _lastMouseSampleTime = now;
            return;
        }

        // 计算本次移动的距离
        var dx = currentPosition.X - _lastMousePosition.Value.X;
        var dy = currentPosition.Y - _lastMousePosition.Value.Y;
        var segmentDistance = Math.Sqrt(dx * dx + dy * dy);

        // 过滤异常大的单次移动（可能是鼠标跳跃或系统事件）
        // 保留真实路径累计，但仍丢弃明显不合理的跳点，避免污染统计。
        const double maxSegmentDistance = 250.0;
        if (segmentDistance > maxSegmentDistance)
        {
            _accumulatedDistance = 0.0;
            _lastMousePosition = currentPosition;
            _lastMouseSampleTime = now;
            return;
        }

        // 直接累计每一小段位移，统计真实走过的路径长度。
        _accumulatedDistance += segmentDistance;
        _lastMousePosition = currentPosition;

        var elapsed = (now - _lastMouseSampleTime).TotalSeconds;
        if (elapsed < _mouseSampleInterval)
        {
            return;
        }

        var reportedDistance = _accumulatedDistance;
        _accumulatedDistance = 0.0;
        _lastMouseSampleTime = now;

        if (reportedDistance <= 0)
        {
            return;
        }

        var distance = reportedDistance;
        ThreadPool.QueueUserWorkItem(_ => MouseMoved?.Invoke(distance));
    }

    private void HandleScroll(uint mouseData, string appName, string displayName)
    {
        // mouseData contains the scroll delta in the high-order word
        // WHEEL_DELTA is 120, so divide by 120 to get wheel ticks
        var delta = NativeInterop.HiWord((int)mouseData);
        var scrollDistance = Math.Abs(delta) / 120.0;
        MouseScrolled?.Invoke(scrollDistance, appName, displayName);
    }

    public void ResetLastMousePosition()
    {
        _lastMousePosition = null;
        _accumulatedDistance = 0.0;
    }

    public void Dispose()
    {
        StopMonitoring();
        _instance = null;
    }
}
