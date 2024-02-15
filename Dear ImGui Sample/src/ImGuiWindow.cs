using ImGuiNET;
using ImGuizmoNET;
using imnodesNET;
using OpenTK.Core;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Dear_ImGui_Sample;

public class ImGuiWindow : GameWindow
{
    private ImGuiController mController;
    private ImGuiViewportPtr mViewport;
    private readonly GCHandle mGcHandle;

    public ImGuiWindow(ImGuiViewportPtr viewport, ImGuiController controller) : base(GameWindowSettings.Default, new NativeWindowSettings()
    {
        WindowBorder = GetBorderSettings(viewport),
        Location = new Vector2i((int)viewport.Pos.X, (int)viewport.Pos.Y),
        Size = new Vector2i((int)viewport.Size.X, (int)viewport.Size.Y),
        APIVersion = new Version(3, 3)
    })
    {
        mViewport = viewport;
        mGcHandle = GCHandle.Alloc(this);
        mViewport.PlatformUserData = (IntPtr)mGcHandle;
        mController = controller;

        Resize += _ => mViewport.PlatformRequestResize = true;
        Move += _ => mViewport.PlatformRequestMove = true;
        Closing += _ => mViewport.PlatformRequestClose = true;
    }

    protected static WindowBorder GetBorderSettings(ImGuiViewportPtr viewport)
    {
        if ((viewport.Flags & ImGuiViewportFlags.NoDecoration) != 0)
        {
            return WindowBorder.Hidden;
        }

        return WindowBorder.Resizable;
    }

    public void Update(float totalSeconds)
    {
        NewInputFrame();
        ProcessWindowEvents(IsEventDriven);
        UpdateTime = totalSeconds;
        OnUpdateFrame(new FrameEventArgs(totalSeconds));
    }

    public void Render(float totalSeconds)
    {
        OnRenderFrame(new FrameEventArgs(totalSeconds));
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        GL.ClearColor(new Color4(0, 32, 48, 255));
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
    }
}
