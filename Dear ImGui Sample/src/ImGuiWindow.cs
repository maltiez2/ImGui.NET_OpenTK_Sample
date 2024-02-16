using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using System;
using System.Runtime.InteropServices;

namespace Dear_ImGui_Sample;



public class ImGuiWindow : GameWindow, IWindowRenderer
{
    private readonly ImGuiViewportPtr mViewport;

    public NativeWindow Native => this;

    public ImGuiWindow(ImGuiViewportPtr viewport) : base(GameWindowSettings.Default, new NativeWindowSettings()
    {
        WindowBorder = GetBorderSettings(viewport),
        Location = new Vector2i((int)viewport.Pos.X, (int)viewport.Pos.Y),
        Size = new Vector2i((int)viewport.Size.X, (int)viewport.Size.Y),
        APIVersion = new Version(3, 3)
    })
    {
        mViewport = viewport;
        GCHandle gcHandle = GCHandle.Alloc(this);
        mViewport.PlatformUserData = (IntPtr)gcHandle;

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

    public void OnRender(float deltaSeconds)
    {
        base.OnRenderFrame(new FrameEventArgs(deltaSeconds));

        GL.ClearColor(new Color4(0, 32, 48, 255));
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
    }
    public void OnUpdate(float deltaSeconds)
    {
        NewInputFrame();
        ProcessWindowEvents(IsEventDriven);
        UpdateTime = deltaSeconds;
    }
    public void OnDraw(float deltaSeconds)
    {

    }
}
