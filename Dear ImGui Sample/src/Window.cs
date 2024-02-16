﻿using ImGuiNET;
using ImGuizmoNET;
using imnodesNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using System;

namespace Dear_ImGui_Sample;

public interface IWindowRenderer
{
    NativeWindow Native { get; }
    void OnRender(float deltaSeconds);
    void OnDraw(float deltaSeconds);
    void OnUpdate(float deltaSeconds);
    void SwapBuffers();
}

public class Window : GameWindow, IWindowRenderer
{
    ImGuiController _controller;

    public NativeWindow Native => this;

    public Window() : base(GameWindowSettings.Default, new NativeWindowSettings() { Size = new Vector2i(1600, 900), APIVersion = new Version(3, 3) })
    { }

    protected override void OnLoad()
    {
        base.OnLoad();
        Title += ": OpenGL Version: " + GL.GetString(StringName.Version);
        _controller = new ImGuiController(ClientSize.X, ClientSize.Y, this);
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);
        _controller.WindowResized(ClientSize.X, ClientSize.Y);
    }

    protected override void OnRenderFrame(FrameEventArgs e)
    {
        base.OnRenderFrame(e);

        _controller.Draw((float)e.Time);

        /*Context.MakeCurrent();
        
        base.OnRenderFrame(e);

        _controller.Update(this, (float)e.Time);

        TestWindow();

        _controller.Render((float)e.Time);
        
        //SwapBuffers();
        //_controller.SwapExtraWindows();*/
    }

    private void TestWindow()
    {
        //ImPlotNET.ImPlot.ShowDemoWindow();
        ImGui.ShowDemoWindow();

        ImGui.Begin("TEST");

        imnodes.BeginNodeEditor();

        imnodes.BeginNode(1);
        ImGui.Dummy(new System.Numerics.Vector2(80, 45));
        imnodes.EndNode();

        imnodes.EndNodeEditor();

        ImGui.End();
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);
        _controller.PressChar((char)e.Unicode);
    }
    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        _controller.MouseScroll(e.Offset);
    }

    public void OnUpdate(float deltaSeconds)
    {
        /*NewInputFrame();
        ProcessWindowEvents(IsEventDriven);
        UpdateTime = deltaSeconds;*/
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
}