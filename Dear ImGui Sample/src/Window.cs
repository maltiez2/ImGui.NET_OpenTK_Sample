using ImGuiNET;
using ImGuizmoNET;
using imnodesNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using System;

namespace ImGuiController_OpenTK;

public class Window : GameWindow, IWindow
{
    private ImGuiController? _controller;

    public NativeWindow Native => this;

    public IImGuiRenderer ImGuiRenderer => throw new NotImplementedException();

    public ImGuiViewportPtr Viewport => throw new NotImplementedException();

    public Window() : base(GameWindowSettings.Default, new NativeWindowSettings() { Size = new Vector2i(1600, 900), APIVersion = new Version(3, 3) })
    {

    }

    protected override void OnLoad()
    {
        base.OnLoad();
        Title += ": OpenGL Version: " + GL.GetString(StringName.Version);
        _controller = new ImGuiController(this);
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        _controller?.Render((float)args.Time);

        SwapBuffers();
    }

    private static void TestWindow()
    {
        ImGui.ShowDemoWindow();

        ImGui.Begin("T_1");
        ImGui.End();

        ImGui.Begin("T_2");
        ImGui.End();


        ImGui.Begin("TEST");

        imnodes.BeginNodeEditor();

        imnodes.BeginNode(1);
        ImGui.Dummy(new System.Numerics.Vector2(80, 45));
        imnodes.EndNode();

        imnodes.EndNodeEditor();

        ImGui.End();
    }

    public void OnUpdate(float deltaSeconds)
    {
        // All updates are done before calling controller rendering
    }
    public void OnRender(float deltaSeconds)
    {
        GL.ClearColor(new Color4(0, 32, 48, 255));
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
    }

    public void OnDraw(float deltaSeconds)
    {
        ImGui.DockSpaceOverViewport();
        ImGuizmo.BeginFrame();
        TestWindow();
    }

    void IWindow.SwapBuffers()
    {
        // Swapping buffer will be handled on its own
    }
}
