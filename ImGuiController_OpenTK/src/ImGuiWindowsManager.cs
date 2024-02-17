using ImGuiNET;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace ImGuiController_OpenTK;

public interface IWindowsManager : IDisposable
{
    IImGuiWindow? GetWindow(ImGuiViewportPtr viewport);
}

public sealed class ImGuiWindowsManager : IWindowsManager
{
    public ImGuiWindowsManager(CreateNewWindow windowsMaker, ImGuiViewportPtr mainViewport, IImGuiWindow mainWindow)
    {
        mWindowsMaker = windowsMaker;
        mMainViewport = mainViewport;
        mMainWindow = mainWindow;

        ImGuiPlatformIOPtr platformIO = ImGui.GetPlatformIO();
        mMainViewport = platformIO.Viewports[0];

        mCreateWindow = CreateWindow;
        mDestroyWindow = DestroyWindow;
        mShowWindow = ShowWindow;
        mSetWindowPos = SetWindowPos;
        mSetWindowSize = SetWindowSize;
        mSetWindowFocus = SetWindowFocus;
        mGetWindowFocus = GetWindowFocus;
        mGetWindowMinimized = GetWindowMinimized;
        mSetWindowTitle = SetWindowTitle;
        mSetClipboardText = SetClipboardTextFn;
        mGetClipboardText = GetClipboardTextFn;

        platformIO.Platform_CreateWindow = Marshal.GetFunctionPointerForDelegate(mCreateWindow);
        platformIO.Platform_DestroyWindow = Marshal.GetFunctionPointerForDelegate(mDestroyWindow);
        platformIO.Platform_ShowWindow = Marshal.GetFunctionPointerForDelegate(mShowWindow);
        platformIO.Platform_SetWindowPos = Marshal.GetFunctionPointerForDelegate(mSetWindowPos);
        platformIO.Platform_SetWindowSize = Marshal.GetFunctionPointerForDelegate(mSetWindowSize);
        platformIO.Platform_SetWindowFocus = Marshal.GetFunctionPointerForDelegate(mSetWindowFocus);
        platformIO.Platform_GetWindowFocus = Marshal.GetFunctionPointerForDelegate(mGetWindowFocus);
        platformIO.Platform_GetWindowMinimized = Marshal.GetFunctionPointerForDelegate(mGetWindowMinimized);
        platformIO.Platform_SetWindowTitle = Marshal.GetFunctionPointerForDelegate(mSetWindowTitle);

        unsafe
        {
            mGetWindowPos = GetWindowPos;
            mGetWindowSize = GetWindowSize;
            ImGuiNative.ImGuiPlatformIO_Set_Platform_GetWindowPos(platformIO.NativePtr, Marshal.GetFunctionPointerForDelegate(mGetWindowPos));
            ImGuiNative.ImGuiPlatformIO_Set_Platform_GetWindowSize(platformIO.NativePtr, Marshal.GetFunctionPointerForDelegate(mGetWindowSize));
        }

        ImGuiIOPtr io = ImGui.GetIO();
        io.SetClipboardTextFn = Marshal.GetFunctionPointerForDelegate(mSetClipboardText);
        io.GetClipboardTextFn = Marshal.GetFunctionPointerForDelegate(mGetClipboardText);
        io.ClipboardUserData = IntPtr.Zero;
    }

    public IImGuiWindow? GetWindow(ImGuiViewportPtr viewport)
    {
        unsafe
        {
            if (viewport.NativePtr == mMainViewport.NativePtr) return mMainWindow;
        }

        if (viewport.PlatformUserData == IntPtr.Zero) return null;

        return (ImGuiWindow?)GCHandle.FromIntPtr(viewport.PlatformUserData).Target;
    }

    public delegate IImGuiWindow CreateNewWindow(ImGuiViewportPtr viewport);

    public event Action<NativeWindow>? OnWindowCreated;
    public event Action<NativeWindow>? OnWindowDestroyed;

    private readonly HashSet<IImGuiWindow> mWindows = new();
    private readonly ImGuiViewportPtr mMainViewport;
    private readonly CreateNewWindow mWindowsMaker;
    private readonly IImGuiWindow mMainWindow;

    #region Delegates for ImGui
    private readonly Platform_CreateWindow mCreateWindow;
    private readonly Platform_DestroyWindow mDestroyWindow;
    private readonly Platform_GetWindowPos mGetWindowPos;
    private readonly Platform_ShowWindow mShowWindow;
    private readonly Platform_SetWindowPos mSetWindowPos;
    private readonly Platform_SetWindowSize mSetWindowSize;
    private readonly Platform_GetWindowSize mGetWindowSize;
    private readonly Platform_SetWindowFocus mSetWindowFocus;
    private readonly Platform_GetWindowFocus mGetWindowFocus;
    private readonly Platform_GetWindowMinimized mGetWindowMinimized;
    private readonly Platform_SetWindowTitle mSetWindowTitle;
    private readonly SetClipboardTextDlg mSetClipboardText;
    private readonly GetClipboardTextDlg mGetClipboardText;
    #endregion

    private NativeWindow GetWindowImpl(ImGuiViewportPtr viewport)
    {
        unsafe
        {
            if (viewport.NativePtr == mMainViewport.NativePtr) return mMainWindow.Native;
            if (viewport.PlatformUserData == IntPtr.Zero) return mMainWindow.Native;
        }

        return (ImGuiWindow?)GCHandle.FromIntPtr(viewport.PlatformUserData).Target ?? mMainWindow.Native;
    }
    private NativeWindow GetWindowImpl()
    {
        foreach (IImGuiWindow window in mWindows)
        {
            if (window.Native.IsFocused) return window.Native;
        }

        return mMainWindow.Native;
    }

    #region Delegates' values for ImGui

    private delegate void SetClipboardTextDlg(IntPtr userData, string text);
    private delegate IntPtr GetClipboardTextDlg(IntPtr userData);

    private unsafe void SetClipboardTextFn(IntPtr userData, string text) => GLFW.SetClipboardString(GetWindowImpl().WindowPtr, text);
    private unsafe IntPtr GetClipboardTextFn(IntPtr userData) => Marshal.StringToCoTaskMemUTF8(GLFW.GetClipboardString(GetWindowImpl().WindowPtr));

    private void CreateWindow(ImGuiViewportPtr viewport)
    {
        IImGuiWindow window = mWindowsMaker.Invoke(viewport);
        mWindows.Add(window);
        if (window != null) OnWindowCreated?.Invoke(window.Native);
    }
    private void DestroyWindow(ImGuiViewportPtr viewport)
    {
        if (viewport.PlatformUserData == IntPtr.Zero) return;

        ImGuiWindow? window = (ImGuiWindow?)GCHandle.FromIntPtr(viewport.PlatformUserData).Target;
        window?.Close();
        window?.Dispose();
        viewport.PlatformUserData = IntPtr.Zero;
        if (window != null && mWindows.Contains(window)) mWindows.Remove(window);
        if (window != null) OnWindowDestroyed?.Invoke(window);
    }
    private void ShowWindow(ImGuiViewportPtr viewport)
    {
        NativeWindow window = GetWindowImpl(viewport);
        window.IsVisible = true;
    }

    private unsafe void GetWindowPos(ImGuiViewportPtr viewport, System.Numerics.Vector2* outPos)
    {
        NativeWindow window = GetWindowImpl(viewport);
        *outPos = new System.Numerics.Vector2(window.ClientLocation.X, window.ClientLocation.Y);
    }
    private void SetWindowPos(ImGuiViewportPtr viewport, System.Numerics.Vector2 pos)
    {
        NativeWindow window = GetWindowImpl(viewport);
        window.Location = new((int)pos.X, (int)pos.Y);
    }
    private void SetWindowSize(ImGuiViewportPtr viewport, System.Numerics.Vector2 size)
    {
        NativeWindow window = GetWindowImpl(viewport);
        window.Size = new((int)size.X, (int)size.Y);
    }
    private unsafe void GetWindowSize(ImGuiViewportPtr viewport, System.Numerics.Vector2* outSize)
    {
        NativeWindow window = GetWindowImpl(viewport);
        *outSize = new System.Numerics.Vector2(window.Size.X, window.Size.Y);
        Console.WriteLine($"GetWindowSize: {window.Size}");
    }

    private void SetWindowFocus(ImGuiViewportPtr viewport)
    {
        NativeWindow window = GetWindowImpl(viewport);
        window.Focus();
    }
    private byte GetWindowFocus(ImGuiViewportPtr viewport)
    {
        NativeWindow window = GetWindowImpl(viewport);
        return window.IsFocused ? (byte)1 : (byte)0;
    }
    private byte GetWindowMinimized(ImGuiViewportPtr viewport)
    {
        NativeWindow window = GetWindowImpl(viewport);
        return window.WindowState == WindowState.Minimized ? (byte)1 : (byte)0;
    }
    private unsafe void SetWindowTitle(ImGuiViewportPtr viewport, IntPtr title)
    {
        NativeWindow window = GetWindowImpl(viewport);
        byte* titlePtr = (byte*)title;
        int count = 0;
        while (titlePtr[count] != 0)
        {
            count += 1;
        }
        window.Title = System.Text.Encoding.ASCII.GetString(titlePtr, count);
    }
    #endregion

    #region Disposing
    private bool mDisposed = false;
    public void Dispose()
    {
        if (mDisposed) return;
        mDisposed = true;

        foreach (IImGuiWindow window in mWindows.Where(window => window != mMainWindow))
        {
            window.Dispose();
        }
    }
    #endregion
}
