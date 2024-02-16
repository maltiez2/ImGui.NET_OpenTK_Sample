using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Dear_ImGui_Sample;



public class ImGuiWindow : GameWindow, IWindowRenderer
{
    private readonly ImGuiViewportPtr mViewport;

    public NativeWindow Native => this;

    public NotSharedDeviceResourced DeviceResources { get; }

    public ImGuiWindow(ImGuiViewportPtr viewport, GameWindow mainWindow) : base(GameWindowSettings.Default, new NativeWindowSettings()
    {
        SharedContext = mainWindow.Context,
        WindowBorder = GetBorderSettings(viewport),
        Location = new Vector2i((int)viewport.Pos.X, (int)viewport.Pos.Y),
        Size = new Vector2i((int)viewport.Size.X, (int)viewport.Size.Y),
        APIVersion = new Version(3, 3)
    })
    {
        mViewport = viewport;
        GCHandle gcHandle = GCHandle.Alloc(this);
        mViewport.PlatformUserData = (IntPtr)gcHandle;
        DeviceResources = new(Context);

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

public struct NotSharedDeviceResourced
{
    public int VertexArray;
    public int VertexBuffer;
    public int VertexBufferSize;
    public int IndexBuffer;
    public int IndexBufferSize;

    public NotSharedDeviceResourced(IGLFWGraphicsContext context)
    {
        context.MakeCurrent();

        VertexBufferSize = 10000;
        IndexBufferSize = 2000;

        int prevVAO = GL.GetInteger(GetPName.VertexArrayBinding);
        int prevArrayBuffer = GL.GetInteger(GetPName.ArrayBufferBinding);

        VertexArray = GL.GenVertexArray();
        GL.BindVertexArray(VertexArray);

        VertexBuffer = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBuffer);
        GL.BufferData(BufferTarget.ArrayBuffer, VertexBufferSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);

        IndexBuffer = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, IndexBuffer);
        GL.BufferData(BufferTarget.ElementArrayBuffer, IndexBufferSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);

        int stride = Unsafe.SizeOf<ImDrawVert>();
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, stride, 0);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, 8);
        GL.VertexAttribPointer(2, 4, VertexAttribPointerType.UnsignedByte, true, stride, 16);

        GL.EnableVertexAttribArray(0);
        GL.EnableVertexAttribArray(1);
        GL.EnableVertexAttribArray(2);

        GL.BindVertexArray(prevVAO);
        GL.BindBuffer(BufferTarget.ArrayBuffer, prevArrayBuffer);
    }
}
