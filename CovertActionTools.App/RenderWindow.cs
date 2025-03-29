using System.Numerics;
using System.Runtime.InteropServices;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace CovertActionTools.App;

public class RenderWindow : IDisposable
{
    public enum RenderType
    {
        Unknown = 0,
        Primitive = 1,
        Image = 2,
    }
    private static readonly Vector3 ClearColor = new Vector3(0.3f, 0.45f, 0.7f);

    private readonly string _title;
    private readonly int _w;
    private readonly int _h;

    private bool _windowOpen;
    private Sdl2Window _window = null!;
    private GraphicsDevice _graphicsDevice = null!;
    private ImGuiController _controller = null!;
    private CommandList _commandList = null!;

    private readonly Dictionary<string, Texture> _textures = new Dictionary<string, Texture>();
    
    private DateTime _lastUpdate = DateTime.MinValue;
    
    public RenderWindow(string title, int w, int h)
    {
        _title = title;
        _w = w;
        _h = h;

        CreateWindow();
    }

    public bool IsOpen()
    {
        return _windowOpen;
    }

    public void ProcessInput()
    {
        InputSnapshot snapshot = _window.PumpEvents();
        if (!_window.Exists)
        {
            _windowOpen = false;
        }

        var delta = NextFrameDelta();
        _controller.Update((float)delta, snapshot);
    }

    public void Render()
    {
        _commandList.Begin();
        _commandList.SetFramebuffer(_graphicsDevice.MainSwapchain.Framebuffer);
        _commandList.ClearColorTarget(0, new RgbaFloat(ClearColor.X, ClearColor.Y, ClearColor.Z, 1f));
        _controller.Render(_graphicsDevice, _commandList);
        _commandList.End();
        _graphicsDevice.SubmitCommands(_commandList);
        _graphicsDevice.SwapBuffers(_graphicsDevice.MainSwapchain);
    }

    private bool DoesTextureExist(RenderType type, string id)
    {
        var fullId = $"{type:G}_{id}";
        return _textures.ContainsKey(fullId);
    }

    private void SaveTexture(RenderType type, string id, Texture texture)
    {
        var fullId = $"{type:G}_{id}";
        _textures.Add(fullId, texture);
    }

    private Texture GetTexture(RenderType type, string id)
    {
        var fullId = $"{type:G}_{id}";
        return _textures[fullId];
    }

    public IntPtr RenderImage(RenderType type, string id, int width, int height, byte[] rawPixels, bool force = false)
    {
        if (force || !DoesTextureExist(type, id))
        {
            var newTexture = _graphicsDevice.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
                (uint)width,
                (uint)height,
                1,
                1,
                PixelFormat.R8_G8_B8_A8_UNorm,
                TextureUsage.Sampled));
            newTexture.Name = id;
            SaveTexture(type, id, newTexture);
        }

        var texture = GetTexture(type, id);
        
        IntPtr pixels = Marshal.AllocHGlobal(rawPixels.Length);
        Marshal.Copy(rawPixels, 0, pixels, rawPixels.Length);
        try
        {
            _graphicsDevice.UpdateTexture(
                texture,
                pixels,
                (uint)(4 * width * height),
                0,
                0,
                0,
                (uint)width,
                (uint)height,
                1,
                0,
                0);
        }
        finally
        {
            Marshal.FreeHGlobal(pixels);
        }
        
        return _controller.GetOrCreateImGuiBinding(_graphicsDevice.ResourceFactory, texture);
    }
    
    public IntPtr RenderCheckerboardRectangle(int checkerWidth, int w, int h, (byte, byte, byte, byte) fillCol, (byte, byte, byte, byte) fillCol2)
    {
        var fillColId = $"{fillCol.Item1}-{fillCol.Item2}-{fillCol.Item3}-{fillCol.Item4}";
        var fillCol2Id = $"{fillCol2.Item1}-{fillCol2.Item2}-{fillCol2.Item3}-{fillCol2.Item4}";
        var id = $"checker_{checkerWidth}_{w}_{h}_{fillColId}_{fillCol2Id}";
        if (DoesTextureExist(RenderType.Primitive, id))
        {
            var texture = GetTexture(RenderType.Primitive, id);
            return _controller.GetOrCreateImGuiBinding(_graphicsDevice.ResourceFactory, texture);
        }
        var bytes = new byte[w * h * 4];
        for (var i = 0; i < w; i++)
        {
            for (var j = 0; j < h; j++)
            {
                var x = i % (checkerWidth * 2);
                var y = j % (checkerWidth * 2);
                
                var (r, g, b, a) = fillCol;
                if (x < checkerWidth && y < checkerWidth ||
                    (x >= checkerWidth && y >= checkerWidth))
                {
                    (r, g, b, a) = fillCol2;
                }
                
                bytes[(j * w + i) * 4 + 0] = r;
                bytes[(j * w + i) * 4 + 1] = g;
                bytes[(j * w + i) * 4 + 2] = b;
                bytes[(j * w + i) * 4 + 3] = a;
            }
        }

        return RenderImage(RenderType.Primitive, id, w, h, bytes);
    }
    
    private void CreateWindow()
    {
        if (_windowOpen)
        {
            throw new Exception("Window already open");
        }
        VeldridStartup.CreateWindowAndGraphicsDevice(
            new WindowCreateInfo(50, 50, _w, _h, WindowState.Normal, _title),
            new GraphicsDeviceOptions(true, null, true, ResourceBindingModel.Improved, true, true),
            out var window,
            out var gd);
        if (window == null || gd == null)
        {
            throw new Exception("Failed to create window");
        }

        _window = window;
        _graphicsDevice = gd;
        
        _controller = new ImGuiController(_graphicsDevice, _graphicsDevice.MainSwapchain.Framebuffer.OutputDescription, _window.Width, _window.Height);
        _window.Resized += WindowOnResize;
        _window.Closing += WindowOnClose;
        _commandList = _graphicsDevice.ResourceFactory.CreateCommandList();
        _windowOpen = true;
    }

    private void WindowOnResize()
    {
        if (!_windowOpen)
        {
            throw new Exception("Window not open");
        }
        if (_controller == null)
        {
            throw new Exception("Missing controller");
        }

        _graphicsDevice.MainSwapchain.Resize((uint)_window.Width, (uint)_window.Height);
        _controller.WindowResized(_window.Width, _window.Height);
    }

    private void WindowOnClose()
    {
        _windowOpen = false;
    }
    
    private double NextFrameDelta()
    {
        var prevUpdate = _lastUpdate;
        _lastUpdate = DateTime.UtcNow;
        return (_lastUpdate - prevUpdate).TotalSeconds;
    }

    #region Dispose
    public void Dispose()
    {
        try
        {
            _graphicsDevice.WaitForIdle();
            _graphicsDevice.Dispose();
            _controller.Dispose();
            _commandList.Dispose();
        }
        catch (Exception)
        {
            //non-issue
        }
    }
    #endregion
}