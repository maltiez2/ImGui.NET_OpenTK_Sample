﻿using ImGuiNET;
using ImGuizmoNET;
using ImPlotNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using ErrorCode = OpenTK.Graphics.OpenGL4.ErrorCode;
using Vector2 = OpenTK.Mathematics.Vector2;

namespace Dear_ImGui_Sample;

public class ImGuiController : IDisposable
{
    private bool _frameBegun;

    private int _vertexArray;
    private int _vertexBuffer;
    private int _vertexBufferSize;
    private int _indexBuffer;
    private int _indexBufferSize;

    //private Texture _fontTexture;

    private int _fontTexture;

    private int _shader;
    private int _shaderFontTextureLocation;
    private int _shaderProjectionMatrixLocation;

    private int _windowWidth;
    private int _windowHeight;

    private System.Numerics.Vector2 _scaleFactor = System.Numerics.Vector2.One;

    private static bool KHRDebugAvailable = false;

    private int GLVersion;
    private bool CompatibilityProfile;

    // Image trackers

    private ImGuiViewportPtr _mainViewport;
    private Window _mainWindow;

    private readonly Platform_CreateWindow _createWindow;
    private readonly Platform_DestroyWindow _destroyWindow;
    private readonly Platform_GetWindowPos _getWindowPos;
    private readonly Platform_ShowWindow _showWindow;
    private readonly Platform_SetWindowPos _setWindowPos;
    private readonly Platform_SetWindowSize _setWindowSize;
    private readonly Platform_GetWindowSize _getWindowSize;
    private readonly Platform_SetWindowFocus _setWindowFocus;
    private readonly Platform_GetWindowFocus _getWindowFocus;
    private readonly Platform_GetWindowMinimized _getWindowMinimized;
    private readonly Platform_SetWindowTitle _setWindowTitle;
    private int _lastAssignedID = 100;

    /// <summary>
    /// Constructs a new ImGuiController.
    /// </summary>
    public ImGuiController(int width, int height, Window window)
    {
        _windowWidth = width;
        _windowHeight = height;
        _mainWindow = window;

        int major = GL.GetInteger(GetPName.MajorVersion);
        int minor = GL.GetInteger(GetPName.MinorVersion);

        GLVersion = major * 100 + minor * 10;

        KHRDebugAvailable = (major == 4 && minor >= 3) || IsExtensionSupported("KHR_debug");

        CompatibilityProfile = (GL.GetInteger((GetPName)All.ContextProfileMask) & (int)All.ContextCompatibilityProfileBit) != 0;

        IntPtr context = ImGui.CreateContext();
        ImGui.SetCurrentContext(context);

        IntPtr plotContext = ImPlot.CreateContext();

        ImPlot.SetCurrentContext(plotContext);
        ImPlot.SetImGuiContext(context);

        ImGuizmo.SetImGuiContext(context);

        IntPtr imnodesContext = imnodesNET.imnodes.CreateContext();
        imnodesNET.imnodes.SetCurrentContext(imnodesContext);
        imnodesNET.imnodes.SetImGuiContext(context);


        ImGuiIOPtr io = ImGui.GetIO();
        io.Fonts.AddFontDefault();

        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
        io.ConfigFlags |= ImGuiConfigFlags.ViewportsEnable;

        ImGuiPlatformIOPtr platformIO = ImGui.GetPlatformIO();
        _mainViewport = platformIO.Viewports[0];

        _createWindow = CreateWindow;
        _destroyWindow = DestroyWindow;
        _showWindow = ShowWindow;
        unsafe
        {
            _getWindowPos = GetWindowPos;
            _getWindowSize = GetWindowSize;
        }
        _setWindowPos = SetWindowPos;
        _setWindowSize = SetWindowSize;
        _setWindowFocus = SetWindowFocus;
        _getWindowFocus = GetWindowFocus;
        _getWindowMinimized = GetWindowMinimized;
        _setWindowTitle = SetWindowTitle;

        platformIO.Platform_CreateWindow = Marshal.GetFunctionPointerForDelegate(_createWindow);
        platformIO.Platform_DestroyWindow = Marshal.GetFunctionPointerForDelegate(_destroyWindow);
        platformIO.Platform_ShowWindow = Marshal.GetFunctionPointerForDelegate(_showWindow);
        platformIO.Platform_SetWindowPos = Marshal.GetFunctionPointerForDelegate(_setWindowPos);
        platformIO.Platform_SetWindowSize = Marshal.GetFunctionPointerForDelegate(_setWindowSize);
        platformIO.Platform_SetWindowFocus = Marshal.GetFunctionPointerForDelegate(_setWindowFocus);
        platformIO.Platform_GetWindowFocus = Marshal.GetFunctionPointerForDelegate(_getWindowFocus);
        platformIO.Platform_GetWindowMinimized = Marshal.GetFunctionPointerForDelegate(_getWindowMinimized);
        platformIO.Platform_SetWindowTitle = Marshal.GetFunctionPointerForDelegate(_setWindowTitle);

        unsafe
        {
            ImGuiNative.ImGuiPlatformIO_Set_Platform_GetWindowPos(platformIO.NativePtr, Marshal.GetFunctionPointerForDelegate(_getWindowPos));
            ImGuiNative.ImGuiPlatformIO_Set_Platform_GetWindowSize(platformIO.NativePtr, Marshal.GetFunctionPointerForDelegate(_getWindowSize));
        }

        /*unsafe
        {
            io.NativePtr->BackendPlatformName = (byte*)new FixedAsciiString("Veldrid.SDL2 Backend").DataPtr;
        }*/
        io.BackendFlags |= ImGuiBackendFlags.HasMouseCursors;
        io.BackendFlags |= ImGuiBackendFlags.HasSetMousePos;
        io.BackendFlags |= ImGuiBackendFlags.PlatformHasViewports;
        io.BackendFlags |= ImGuiBackendFlags.RendererHasViewports;
        ImGui.GetIO().BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;

        CreateDeviceResources();

        SetPerFrameImGuiData(1f / 60f, window);
        UpdateMonitors();

        ImGui.NewFrame();
        _frameBegun = true;
    }

    // ***************************************

    private NativeWindow GetWindow(ImGuiViewportPtr viewport)
    {
        unsafe
        {
            if (viewport.NativePtr == _mainViewport.NativePtr) return _mainWindow;
        }

        return (ImGuiWindow)GCHandle.FromIntPtr(viewport.PlatformUserData).Target;
    }
    private IWindowRenderer GetWindowRenderer(ImGuiViewportPtr viewport)
    {
        unsafe
        {
            if (viewport.NativePtr == _mainViewport.NativePtr) return _mainWindow;
        }

        return (ImGuiWindow)GCHandle.FromIntPtr(viewport.PlatformUserData).Target;
    }

    private const int maxWindows = 25;
    private int windowsCount = 0;
    private void CreateWindow(ImGuiViewportPtr viewport)
    {
        if (windowsCount > maxWindows)
        {
            _mainWindow.Close();
            return;
        }
        Console.WriteLine("CreateWindow");
        _ = new ImGuiWindow(viewport);
        windowsCount++;
    }
    private void DestroyWindow(ImGuiViewportPtr viewport)
    {
        Console.WriteLine("DestroyWindow");
        if (viewport.PlatformUserData == IntPtr.Zero) return;

        ImGuiWindow window = (ImGuiWindow)GCHandle.FromIntPtr(viewport.PlatformUserData).Target;
        window.Close();
        window.Dispose();
        viewport.PlatformUserData = IntPtr.Zero;

    }
    private void ShowWindow(ImGuiViewportPtr vp)
    {
        Console.WriteLine("ShowWindow");
        ImGuiWindow window = GetWindow(vp) as ImGuiWindow;
        //window.Run();
        /*Task.Run(() =>
        {
            Console.WriteLine("Run"); 
            window.Run();
        });*/
    }

    private unsafe void GetWindowPos(ImGuiViewportPtr vp, System.Numerics.Vector2* outPos)
    {
        NativeWindow window = GetWindow(vp);
        *outPos = new System.Numerics.Vector2(window.ClientLocation.X, window.ClientLocation.Y);
    }
    private void SetWindowPos(ImGuiViewportPtr vp, System.Numerics.Vector2 pos)
    {
        Console.WriteLine($"SetWindowPos: {pos}");
        NativeWindow window = GetWindow(vp);
        window.Location = new((int)pos.X, (int)pos.Y);
    }
    private void SetWindowSize(ImGuiViewportPtr vp, System.Numerics.Vector2 size)
    {
        Console.WriteLine("SetWindowSize");
        NativeWindow window = GetWindow(vp);
        window.Size = new((int)size.X, (int)size.Y);
    }
    private unsafe void GetWindowSize(ImGuiViewportPtr vp, System.Numerics.Vector2* outSize)
    {
        if (vp.NativePtr == _mainViewport.NativePtr)
        {
            *outSize = new System.Numerics.Vector2(_mainWindow.Size.X, _mainWindow.Size.Y);
            return;
        }

        NativeWindow window = GetWindow(vp);
        *outSize = new System.Numerics.Vector2(window.Size.X, window.Size.Y);
    }

    private void SetWindowFocus(ImGuiViewportPtr vp)
    {
        Console.WriteLine("SetWindowFocus");
        NativeWindow window = GetWindow(vp);
        window.Focus();
    }
    private byte GetWindowFocus(ImGuiViewportPtr vp)
    {
        NativeWindow window = GetWindow(vp);
        return window.IsFocused ? (byte)1 : (byte)0;
    }
    private byte GetWindowMinimized(ImGuiViewportPtr vp)
    {
        NativeWindow window = GetWindow(vp);
        return window.WindowState == WindowState.Minimized ? (byte)1 : (byte)0;
    }
    private unsafe void SetWindowTitle(ImGuiViewportPtr vp, IntPtr title)
    {
        Console.WriteLine("SetWindowTitle");
        NativeWindow window = GetWindow(vp);
        byte* titlePtr = (byte*)title;
        int count = 0;
        while (titlePtr[count] != 0)
        {
            count += 1;
        }
        window.Title = System.Text.Encoding.ASCII.GetString(titlePtr, count);
    }

    // ***************************************

    private unsafe void UpdateMonitors()
    {
        ImGuiPlatformIOPtr platformIO = ImGui.GetPlatformIO();
        Marshal.FreeHGlobal(platformIO.NativePtr->Monitors.Data);
        List<MonitorInfo> monitors = Monitors.GetMonitors();
        IntPtr data = Marshal.AllocHGlobal(Unsafe.SizeOf<ImGuiPlatformMonitor>() * monitors.Count);
        platformIO.NativePtr->Monitors = new ImVector(monitors.Count, monitors.Count, data);
        for (int i = 0; i < monitors.Count; i++)
        {
            Box2i clientArea = monitors[i].ClientArea;
            ImGuiPlatformMonitorPtr monitor = platformIO.Monitors[i];

            monitor.DpiScale = 1f;
            monitor.MainPos = new System.Numerics.Vector2(clientArea.Min.X, clientArea.Min.Y);
            monitor.MainSize = new System.Numerics.Vector2(clientArea.Size.X, clientArea.Size.Y);
            monitor.WorkPos = new System.Numerics.Vector2(clientArea.Min.X, clientArea.Min.Y);
            monitor.WorkSize = new System.Numerics.Vector2(clientArea.Size.X, clientArea.Size.Y);
        }
    }

    public void SwapExtraWindows()
    {
        ImGuiPlatformIOPtr platformIO = ImGui.GetPlatformIO();
        for (int i = 1; i < platformIO.Viewports.Size; i++)
        {
            ImGuiViewportPtr vp = platformIO.Viewports[i];
            ImGuiWindow window = (ImGuiWindow)GCHandle.FromIntPtr(vp.PlatformUserData).Target;
            window.SwapBuffers();
        }
    }


    // ***************************************

    public void WindowResized(int width, int height)
    {
        _windowWidth = width;
        _windowHeight = height;
    }

    public void DestroyDeviceObjects()
    {
        Dispose();
    }

    public void CreateDeviceResources()
    {
        _vertexBufferSize = 10000;
        _indexBufferSize = 2000;

        int prevVAO = GL.GetInteger(GetPName.VertexArrayBinding);
        int prevArrayBuffer = GL.GetInteger(GetPName.ArrayBufferBinding);

        _vertexArray = GL.GenVertexArray();
        GL.BindVertexArray(_vertexArray);
        LabelObject(ObjectLabelIdentifier.VertexArray, _vertexArray, "ImGui");

        _vertexBuffer = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
        LabelObject(ObjectLabelIdentifier.Buffer, _vertexBuffer, "VBO: ImGui");
        GL.BufferData(BufferTarget.ArrayBuffer, _vertexBufferSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);

        _indexBuffer = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBuffer);
        LabelObject(ObjectLabelIdentifier.Buffer, _indexBuffer, "EBO: ImGui");
        GL.BufferData(BufferTarget.ElementArrayBuffer, _indexBufferSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);

        RecreateFontDeviceTexture();

        string VertexSource = @"#version 330 core

uniform mat4 projection_matrix;

layout(location = 0) in vec2 in_position;
layout(location = 1) in vec2 in_texCoord;
layout(location = 2) in vec4 in_color;

out vec4 color;
out vec2 texCoord;

void main()
{
    gl_Position = projection_matrix * vec4(in_position, 0, 1);
    color = in_color;
    texCoord = in_texCoord;
}";
        string FragmentSource = @"#version 330 core

uniform sampler2D in_fontTexture;

in vec4 color;
in vec2 texCoord;

out vec4 outputColor;

void main()
{
    outputColor = color * texture(in_fontTexture, texCoord);
}";

        _shader = CreateProgram("ImGui", VertexSource, FragmentSource);
        _shaderProjectionMatrixLocation = GL.GetUniformLocation(_shader, "projection_matrix");
        _shaderFontTextureLocation = GL.GetUniformLocation(_shader, "in_fontTexture");

        int stride = Unsafe.SizeOf<ImDrawVert>();
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, stride, 0);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, 8);
        GL.VertexAttribPointer(2, 4, VertexAttribPointerType.UnsignedByte, true, stride, 16);

        GL.EnableVertexAttribArray(0);
        GL.EnableVertexAttribArray(1);
        GL.EnableVertexAttribArray(2);

        GL.BindVertexArray(prevVAO);
        GL.BindBuffer(BufferTarget.ArrayBuffer, prevArrayBuffer);

        CheckGLError("End of ImGui setup");
    }

    /// <summary>
    /// Recreates the device texture used to render text.
    /// </summary>
    public void RecreateFontDeviceTexture()
    {
        ImGuiIOPtr io = ImGui.GetIO();
        io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out int width, out int height, out int bytesPerPixel);

        int mips = (int)Math.Floor(Math.Log(Math.Max(width, height), 2));

        int prevActiveTexture = GL.GetInteger(GetPName.ActiveTexture);
        GL.ActiveTexture(TextureUnit.Texture0);
        int prevTexture2D = GL.GetInteger(GetPName.TextureBinding2D);

        _fontTexture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, _fontTexture);
        GL.TexStorage2D(TextureTarget2d.Texture2D, mips, SizedInternalFormat.Rgba8, width, height);
        LabelObject(ObjectLabelIdentifier.Texture, _fontTexture, "ImGui Text Atlas");

        GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, width, height, PixelFormat.Bgra, PixelType.UnsignedByte, pixels);

        GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, mips - 1);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);

        // Restore state
        GL.BindTexture(TextureTarget.Texture2D, prevTexture2D);
        GL.ActiveTexture((TextureUnit)prevActiveTexture);

        io.Fonts.SetTexID((IntPtr)_fontTexture);

        io.Fonts.ClearTexData();
    }

    public void Draw(float deltaSeconds)
    {
        ImGui.UpdatePlatformWindows();

        UpdateImGuiInput(deltaSeconds);

        ImVector<ImGuiViewportPtr> viewports = ImGui.GetPlatformIO().Viewports;
        for (int index = 0; index < Math.Min(2, viewports.Size); index++)
        {
            ImGuiViewportPtr viewport = viewports[index];
            IWindowRenderer window = GetWindowRenderer(viewport);

            MonitorInfo monitor = Monitors.GetMonitorFromWindow(window.Native);
            viewport.Pos = new System.Numerics.Vector2(monitor.ClientArea.Min.X, monitor.ClientArea.Min.Y);
            viewport.Size = new System.Numerics.Vector2(monitor.ClientArea.Size.X, monitor.ClientArea.Size.Y);

            
            window.OnUpdate(deltaSeconds);
        }

        SetPerFrameImGuiData(deltaSeconds, _mainWindow);

        UpdateMonitors();
        ImGui.NewFrame();

        for (int index = 0; index < Math.Min(2, viewports.Size); index++)
        {
            ImGuiViewportPtr viewport = viewports[index];
            IWindowRenderer window = GetWindowRenderer(viewport);

            window.OnDraw(deltaSeconds);
        }

        ImGui.Render();

        for (int index = 0; index < Math.Min(2, viewports.Size); index++)
        {
            ImGuiViewportPtr viewport = viewports[index];
            IWindowRenderer window = GetWindowRenderer(viewport);

            window.Native.MakeCurrent();
            window.OnRender(deltaSeconds);
            using (new SaveGLState(GLVersion, CompatibilityProfile))
            {
                RenderImDrawData(viewport.DrawData, window.Native);
            }
            window.SwapBuffers();
        }
    }

    private void SetPerFrameImGuiData(float deltaSeconds, NativeWindow window)
    {
        MonitorInfo monitor = Monitors.GetMonitorFromWindow(window);

        ImGuiIOPtr io = ImGui.GetIO();
        io.DisplaySize = new System.Numerics.Vector2(
                window.ClientSize.X / _scaleFactor.X,
                window.ClientSize.Y / _scaleFactor.Y);
        io.DisplayFramebufferScale = new System.Numerics.Vector2(monitor.HorizontalScale, monitor.VerticalScale);
        io.DeltaTime = deltaSeconds; // DeltaTime is in seconds.
    }

    readonly List<char> PressedChars = new();

    private void UpdateImGuiInput(float deltaSeconds)
    {
        ImGuiIOPtr io = ImGui.GetIO();

        bool mouseLeft = false;
        bool mouseRight= false;
        bool mouseMiddle = false;
        bool mouseButton4 = false;
        bool mouseButton5 = false;
        bool keyCtrl = false;
        bool keyAlt = false;
        bool keyShift = false;
        bool keySuper = false;

        ImVector<ImGuiViewportPtr> viewports = ImGui.GetPlatformIO().Viewports;
        for (int index = 0; index < Math.Min(2, viewports.Size); index++)
        {
            ImGuiViewportPtr viewport = viewports[index];
            IWindowRenderer window = GetWindowRenderer(viewport);
            MonitorInfo monitor = Monitors.GetMonitorFromWindow(window.Native);

            MouseState mouseState = window.Native.MouseState;
            KeyboardState keyboardState = window.Native.KeyboardState;

            mouseLeft |= mouseState[MouseButton.Left];
            mouseRight |= mouseState[MouseButton.Right];
            mouseMiddle |= mouseState[MouseButton.Middle];
            mouseButton4 |= mouseState[MouseButton.Button4];
            mouseButton5 |= mouseState[MouseButton.Button5];
            keyCtrl |= keyboardState.IsKeyDown(Keys.LeftControl) || keyboardState.IsKeyDown(Keys.RightControl);
            keyAlt |= keyboardState.IsKeyDown(Keys.LeftAlt) || keyboardState.IsKeyDown(Keys.RightAlt);
            keyShift |= keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift);
            keySuper |= keyboardState.IsKeyDown(Keys.LeftSuper) || keyboardState.IsKeyDown(Keys.RightSuper);

            foreach (Keys key in Enum.GetValues(typeof(Keys)))
            {
                if (key == Keys.Unknown)
                {
                    continue;
                }
                io.AddKeyEvent(TranslateKey(key), keyboardState.IsKeyDown(key));
            }
        }

        io.MouseDown[0] = mouseLeft;
        io.MouseDown[1] = mouseRight;
        io.MouseDown[2] = mouseMiddle;
        io.MouseDown[3] = mouseButton4;
        io.MouseDown[4] = mouseButton5;
        io.KeyCtrl = keyCtrl;
        io.KeyAlt = keyAlt;
        io.KeyShift = keyShift;
        io.KeySuper = keySuper;

        Vector2i screenPoint = new((int)_mainWindow.MouseState.X, (int)_mainWindow.MouseState.Y);
        Vector2i point = _mainWindow.ClientLocation + screenPoint;
        io.MousePos = new System.Numerics.Vector2(point.X, point.Y);

        foreach (char c in PressedChars)
        {
            io.AddInputCharacter(c);
        }
        PressedChars.Clear();
    }

    internal void PressChar(char keyChar)
    {
        PressedChars.Add(keyChar);
    }

    internal void MouseScroll(Vector2 offset)
    {
        ImGuiIOPtr io = ImGui.GetIO();

        io.MouseWheel = offset.Y;
        io.MouseWheelH = offset.X;
    }

    public sealed class SaveGLState : IDisposable
    {
        private bool disposedValue;

        private readonly int prevVAO = GL.GetInteger(GetPName.VertexArrayBinding);
        private readonly int prevArrayBuffer = GL.GetInteger(GetPName.ArrayBufferBinding);
        private readonly int prevProgram = GL.GetInteger(GetPName.CurrentProgram);
        private readonly bool prevBlendEnabled = GL.GetBoolean(GetPName.Blend);
        private readonly bool prevScissorTestEnabled = GL.GetBoolean(GetPName.ScissorTest);
        private readonly int prevBlendEquationRgb = GL.GetInteger(GetPName.BlendEquationRgb);
        private readonly int prevBlendEquationAlpha = GL.GetInteger(GetPName.BlendEquationAlpha);
        private readonly int prevBlendFuncSrcRgb = GL.GetInteger(GetPName.BlendSrcRgb);
        private readonly int prevBlendFuncSrcAlpha = GL.GetInteger(GetPName.BlendSrcAlpha);
        private readonly int prevBlendFuncDstRgb = GL.GetInteger(GetPName.BlendDstRgb);
        private readonly int prevBlendFuncDstAlpha = GL.GetInteger(GetPName.BlendDstAlpha);
        private readonly bool prevCullFaceEnabled = GL.GetBoolean(GetPName.CullFace);
        private readonly bool prevDepthTestEnabled = GL.GetBoolean(GetPName.DepthTest);
        private readonly int prevActiveTexture = GL.GetInteger(GetPName.ActiveTexture);
        private readonly int prevTexture2D = GL.GetInteger(GetPName.TextureBinding2D);
        private readonly int[] prevScissorBox = GetScissorBox();
        private readonly int[] prevPolygonMode = GetPolygonMode();

        private readonly int GLVersion;
        private readonly bool CompatibilityProfile;

        public SaveGLState(int GLVersion, bool CompatibilityProfile)
        {
            this.GLVersion = GLVersion;
            this.CompatibilityProfile = CompatibilityProfile;

            GL.ActiveTexture(TextureUnit.Texture0);

            if (GLVersion <= 310 || CompatibilityProfile)
            {
                GL.PolygonMode(MaterialFace.Front, PolygonMode.Fill);
                GL.PolygonMode(MaterialFace.Back, PolygonMode.Fill);
            }
            else
            {
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            }
        }

        private static int[] GetScissorBox()
        {
            Span<int> prevScissorBoxPtr = stackalloc int[4];
            unsafe
            {
                fixed (int* iptr = &prevScissorBoxPtr[0])
                {
                    GL.GetInteger(GetPName.ScissorBox, iptr);
                }
            }
            return prevScissorBoxPtr.ToArray();
        }
        private static int[] GetPolygonMode()
        {
            Span<int> prevPolygonModePtr = stackalloc int[2];
            unsafe
            {
                fixed (int* iptr = &prevPolygonModePtr[0])
                {
                    GL.GetInteger(GetPName.PolygonMode, iptr);
                }
            }
            return prevPolygonModePtr.ToArray();
        }

        public void Dispose()
        {
            if (disposedValue) return;
            disposedValue = true;
            GC.SuppressFinalize(this);

            GL.BindTexture(TextureTarget.Texture2D, prevTexture2D);
            GL.ActiveTexture((TextureUnit)prevActiveTexture);
            GL.UseProgram(prevProgram);
            GL.BindVertexArray(prevVAO);
            GL.Scissor(prevScissorBox[0], prevScissorBox[1], prevScissorBox[2], prevScissorBox[3]);
            GL.BindBuffer(BufferTarget.ArrayBuffer, prevArrayBuffer);
            GL.BlendEquationSeparate((BlendEquationMode)prevBlendEquationRgb, (BlendEquationMode)prevBlendEquationAlpha);
            GL.BlendFuncSeparate(
                (BlendingFactorSrc)prevBlendFuncSrcRgb,
                (BlendingFactorDest)prevBlendFuncDstRgb,
                (BlendingFactorSrc)prevBlendFuncSrcAlpha,
                (BlendingFactorDest)prevBlendFuncDstAlpha);
            if (prevBlendEnabled) GL.Enable(EnableCap.Blend); else GL.Disable(EnableCap.Blend);
            if (prevDepthTestEnabled) GL.Enable(EnableCap.DepthTest); else GL.Disable(EnableCap.DepthTest);
            if (prevCullFaceEnabled) GL.Enable(EnableCap.CullFace); else GL.Disable(EnableCap.CullFace);
            if (prevScissorTestEnabled) GL.Enable(EnableCap.ScissorTest); else GL.Disable(EnableCap.ScissorTest);

            if (GLVersion <= 310 || CompatibilityProfile)
            {
                GL.PolygonMode(MaterialFace.Front, (PolygonMode)prevPolygonMode[0]);
                GL.PolygonMode(MaterialFace.Back, (PolygonMode)prevPolygonMode[1]);
            }
            else
            {
                GL.PolygonMode(MaterialFace.FrontAndBack, (PolygonMode)prevPolygonMode[0]);
            }
        }
    }

    private void RenderImDrawData(ImDrawDataPtr draw_data, NativeWindow window)
    {
        if (draw_data.CmdListsCount == 0)
        {
            return;
        }

        // Bind the element buffer (thru the VAO) so that we can resize it.
        GL.BindVertexArray(_vertexArray);
        // Bind the vertex buffer so that we can resize it.
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);

        for (int i = 0; i < draw_data.CmdListsCount; i++)
        {
            ImDrawListPtr cmd_list = draw_data.CmdLists[i];

            int vertexSize = cmd_list.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>();
            if (vertexSize > _vertexBufferSize)
            {
                int newSize = (int)Math.Max(_vertexBufferSize * 1.5f, vertexSize);

                GL.BufferData(BufferTarget.ArrayBuffer, newSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);
                _vertexBufferSize = newSize;

                Console.WriteLine($"Resized dear imgui vertex buffer to new size {_vertexBufferSize}");
            }

            int indexSize = cmd_list.IdxBuffer.Size * sizeof(ushort);
            if (indexSize > _indexBufferSize)
            {
                int newSize = (int)Math.Max(_indexBufferSize * 1.5f, indexSize);
                GL.BufferData(BufferTarget.ElementArrayBuffer, newSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);
                _indexBufferSize = newSize;

                Console.WriteLine($"Resized dear imgui index buffer to new size {_indexBufferSize}");
            }
        }

        // Setup orthographic projection matrix into our constant buffer
        ImGuiIOPtr io = ImGui.GetIO();

        Console.WriteLine(window.ClientRectangle);

        Matrix4 mvp = Matrix4.CreateOrthographicOffCenter(
            window.ClientRectangle.Min.X,
            window.ClientRectangle.Max.X,
            window.ClientRectangle.Max.Y,
            window.ClientRectangle.Min.Y,
            -1.0f,
            1.0f);

        GL.UseProgram(_shader);
        GL.UniformMatrix4(_shaderProjectionMatrixLocation, false, ref mvp);
        GL.Uniform1(_shaderFontTextureLocation, 0);
        CheckGLError("Projection");

        GL.BindVertexArray(_vertexArray);
        CheckGLError("VAO");

        draw_data.ScaleClipRects(io.DisplayFramebufferScale);

        GL.Enable(EnableCap.Blend);
        GL.Enable(EnableCap.ScissorTest);
        GL.BlendEquation(BlendEquationMode.FuncAdd);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.Disable(EnableCap.CullFace);
        GL.Disable(EnableCap.DepthTest);

        // Render command lists
        for (int n = 0; n < draw_data.CmdListsCount; n++)
        {
            ImDrawListPtr cmd_list = draw_data.CmdLists[n];

            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, cmd_list.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>(), cmd_list.VtxBuffer.Data);
            CheckGLError($"Data Vert {n}");

            GL.BufferSubData(BufferTarget.ElementArrayBuffer, IntPtr.Zero, cmd_list.IdxBuffer.Size * sizeof(ushort), cmd_list.IdxBuffer.Data);
            CheckGLError($"Data Idx {n}");

            for (int cmd_i = 0; cmd_i < cmd_list.CmdBuffer.Size; cmd_i++)
            {
                ImDrawCmdPtr pcmd = cmd_list.CmdBuffer[cmd_i];
                if (pcmd.UserCallback != IntPtr.Zero)
                {
                    throw new NotImplementedException();
                }
                else
                {
                    GL.ActiveTexture(TextureUnit.Texture0);
                    GL.BindTexture(TextureTarget.Texture2D, (int)pcmd.TextureId);
                    CheckGLError("Texture");

                    System.Numerics.Vector4 clip = pcmd.ClipRect;
                    Console.WriteLine(clip);
                    GL.Scissor(
                        -window.ClientRectangle.Min.X + (int)clip.X,
                        window.ClientRectangle.Max.Y - (int)clip.W,
                        (int)(clip.Z - clip.X),
                        (int)(clip.W - clip.Y));
                    CheckGLError("Scissor");

                    if ((io.BackendFlags & ImGuiBackendFlags.RendererHasVtxOffset) != 0)
                    {
                        GL.DrawElementsBaseVertex(PrimitiveType.Triangles, (int)pcmd.ElemCount, DrawElementsType.UnsignedShort, (IntPtr)(pcmd.IdxOffset * sizeof(ushort)), unchecked((int)pcmd.VtxOffset));
                    }
                    else
                    {
                        GL.DrawElements(BeginMode.Triangles, (int)pcmd.ElemCount, DrawElementsType.UnsignedShort, (int)pcmd.IdxOffset * sizeof(ushort));
                    }
                    CheckGLError("Draw");
                }
            }
        }

        GL.Disable(EnableCap.Blend);
        GL.Disable(EnableCap.ScissorTest);
    }

    /// <summary>
    /// Frees all graphics resources used by the renderer.
    /// </summary>
    public void Dispose()
    {
        GL.DeleteVertexArray(_vertexArray);
        GL.DeleteBuffer(_vertexBuffer);
        GL.DeleteBuffer(_indexBuffer);

        GL.DeleteTexture(_fontTexture);
        GL.DeleteProgram(_shader);
    }

    public static void LabelObject(ObjectLabelIdentifier objLabelIdent, int glObject, string name)
    {
        if (KHRDebugAvailable)
            GL.ObjectLabel(objLabelIdent, glObject, name.Length, name);
    }

    static bool IsExtensionSupported(string name)
    {
        int n = GL.GetInteger(GetPName.NumExtensions);
        for (int i = 0; i < n; i++)
        {
            string extension = GL.GetString(StringNameIndexed.Extensions, i);
            if (extension == name) return true;
        }

        return false;
    }

    public static int CreateProgram(string name, string vertexSource, string fragmentSoruce)
    {
        int program = GL.CreateProgram();
        LabelObject(ObjectLabelIdentifier.Program, program, $"Program: {name}");

        int vertex = CompileShader(name, ShaderType.VertexShader, vertexSource);
        int fragment = CompileShader(name, ShaderType.FragmentShader, fragmentSoruce);

        GL.AttachShader(program, vertex);
        GL.AttachShader(program, fragment);

        GL.LinkProgram(program);

        GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int success);
        if (success == 0)
        {
            string info = GL.GetProgramInfoLog(program);
            Debug.WriteLine($"GL.LinkProgram had info log [{name}]:\n{info}");
        }

        GL.DetachShader(program, vertex);
        GL.DetachShader(program, fragment);

        GL.DeleteShader(vertex);
        GL.DeleteShader(fragment);

        return program;
    }

    private static int CompileShader(string name, ShaderType type, string source)
    {
        int shader = GL.CreateShader(type);
        LabelObject(ObjectLabelIdentifier.Shader, shader, $"Shader: {name}");

        GL.ShaderSource(shader, source);
        GL.CompileShader(shader);

        GL.GetShader(shader, ShaderParameter.CompileStatus, out int success);
        if (success == 0)
        {
            string info = GL.GetShaderInfoLog(shader);
            Debug.WriteLine($"GL.CompileShader for shader '{name}' [{type}] had info log:\n{info}");
        }

        return shader;
    }

    public static void CheckGLError(string title)
    {
        ErrorCode error;
        int i = 1;
        while ((error = GL.GetError()) != ErrorCode.NoError)
        {
            Debug.Print($"{title} ({i++}): {error}");
        }
    }

    public static ImGuiKey TranslateKey(Keys key)
    {
        if (key >= Keys.D0 && key <= Keys.D9)
            return key - Keys.D0 + ImGuiKey._0;

        if (key >= Keys.A && key <= Keys.Z)
            return key - Keys.A + ImGuiKey.A;

        if (key >= Keys.KeyPad0 && key <= Keys.KeyPad9)
            return key - Keys.KeyPad0 + ImGuiKey.Keypad0;

        if (key >= Keys.F1 && key <= Keys.F12)
            return key - Keys.F1 + ImGuiKey.F12;

        switch (key)
        {
            case Keys.Tab: return ImGuiKey.Tab;
            case Keys.Left: return ImGuiKey.LeftArrow;
            case Keys.Right: return ImGuiKey.RightArrow;
            case Keys.Up: return ImGuiKey.UpArrow;
            case Keys.Down: return ImGuiKey.DownArrow;
            case Keys.PageUp: return ImGuiKey.PageUp;
            case Keys.PageDown: return ImGuiKey.PageDown;
            case Keys.Home: return ImGuiKey.Home;
            case Keys.End: return ImGuiKey.End;
            case Keys.Insert: return ImGuiKey.Insert;
            case Keys.Delete: return ImGuiKey.Delete;
            case Keys.Backspace: return ImGuiKey.Backspace;
            case Keys.Space: return ImGuiKey.Space;
            case Keys.Enter: return ImGuiKey.Enter;
            case Keys.Escape: return ImGuiKey.Escape;
            case Keys.Apostrophe: return ImGuiKey.Apostrophe;
            case Keys.Comma: return ImGuiKey.Comma;
            case Keys.Minus: return ImGuiKey.Minus;
            case Keys.Period: return ImGuiKey.Period;
            case Keys.Slash: return ImGuiKey.Slash;
            case Keys.Semicolon: return ImGuiKey.Semicolon;
            case Keys.Equal: return ImGuiKey.Equal;
            case Keys.LeftBracket: return ImGuiKey.LeftBracket;
            case Keys.Backslash: return ImGuiKey.Backslash;
            case Keys.RightBracket: return ImGuiKey.RightBracket;
            case Keys.GraveAccent: return ImGuiKey.GraveAccent;
            case Keys.CapsLock: return ImGuiKey.CapsLock;
            case Keys.ScrollLock: return ImGuiKey.ScrollLock;
            case Keys.NumLock: return ImGuiKey.NumLock;
            case Keys.PrintScreen: return ImGuiKey.PrintScreen;
            case Keys.Pause: return ImGuiKey.Pause;
            case Keys.KeyPadDecimal: return ImGuiKey.KeypadDecimal;
            case Keys.KeyPadDivide: return ImGuiKey.KeypadDivide;
            case Keys.KeyPadMultiply: return ImGuiKey.KeypadMultiply;
            case Keys.KeyPadSubtract: return ImGuiKey.KeypadSubtract;
            case Keys.KeyPadAdd: return ImGuiKey.KeypadAdd;
            case Keys.KeyPadEnter: return ImGuiKey.KeypadEnter;
            case Keys.KeyPadEqual: return ImGuiKey.KeypadEqual;
            case Keys.LeftShift: return ImGuiKey.LeftShift;
            case Keys.LeftControl: return ImGuiKey.LeftCtrl;
            case Keys.LeftAlt: return ImGuiKey.LeftAlt;
            case Keys.LeftSuper: return ImGuiKey.LeftSuper;
            case Keys.RightShift: return ImGuiKey.RightShift;
            case Keys.RightControl: return ImGuiKey.RightCtrl;
            case Keys.RightAlt: return ImGuiKey.RightAlt;
            case Keys.RightSuper: return ImGuiKey.RightSuper;
            case Keys.Menu: return ImGuiKey.Menu;
            default: return ImGuiKey.None;
        }
    }
}