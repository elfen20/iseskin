using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cave;
using Cave.Media;
using Cave.Media.OpenGL;
using Cave.Media.Video;

namespace IseSkin
{
    class Program
    {
        Glfw3Renderer renderer;
        IseSkin iseSkin;
        bool exit = false;
        bool initDone = false;
        bool needsReload = false;
        bool needsReFit = false;
        IniReader config;

        static void Main(string[] args)
        {
            var listener = new ConsoleTraceListener();
            listener.Filter = new EventTypeFilter(SourceLevels.Warning);
            Trace.Listeners.Add(listener);
            new Program().Run();
        }

        private void Run()
        {
            Initialize();
            RenderLoop();
        }

        private void RenderLoop()
        {
            using (FileSystemWatcher watcher = new FileSystemWatcher())
            {
                watcher.Filter = Path.GetFileName(config.Name);
                watcher.Path = Path.GetDirectoryName(config.Name);
                watcher.NotifyFilter = NotifyFilters.LastWrite;
                watcher.Changed += OnConfigChanged;
                watcher.EnableRaisingEvents = true;

                int fCounter = 0;

                while (!exit)
                {
                    if (needsReload)
                    {
                        UpdateConfig();
                        needsReload = false;
                    }
                    if (needsReFit)
                    {
                        Trace.TraceInformation($"new FrameBuffer size: ({renderer.Resolution.X},{renderer.Resolution.Y})");
                        renderer.SetWindowTitle($"ISE Skin [{renderer.Resolution.X},{renderer.Resolution.Y}@{iseSkin?.GetCurrentSizeName()}]");
                        iseSkin?.ReadSpriteValues();
                        needsReFit = false;
                    }
                    iseSkin?.SetText("FCounter", fCounter.ToString());
                    renderer.Clear(iseSkin.BGColor);
                    iseSkin?.Render();
                    renderer.Present();
                    fCounter++;
                    Thread.Sleep(10);
                }
            }
        }


        private void OnConfigChanged(object sender, FileSystemEventArgs e)
        {
            needsReload = true;
        }

        private void Initialize()
        {
            //Bitmap32.Loader = new SkiaBitmap32Loader();

            string configFile = FileSystem.Combine(FileSystem.ProgramDirectory, "../../../config", "IseSkin.ini");
            
            config = IniReader.FromFile(configFile);

            renderer = new Glfw3Renderer();
            if (!renderer.IsAvailable) throw new Exception("renderer is not available!");
            IRenderDevice[] devs = renderer.GetDevices();
            if (devs.Length < 1) throw new Exception("no devices found");
            renderer.Closed += WindowClosed;
            renderer.MouseButtonChanged += MBChanged;
            renderer.FrameBufferChanged += FBChanged;
            renderer.Initialize(devs[0], RendererMode.Window, RendererFlags.WaitRetrace, 1024, 768, "OpenGL Test");
            iseSkin = new IseSkin(renderer);
            UpdateConfig();
            initDone = true;
        }

        private bool readSize(IniReader reader, string section, string name, out Size size)
        {
            size = Size.Empty;
            string sizeStr = reader.ReadString(section, name, "");
            string[] parts = sizeStr.Split(';');
            if (parts.Length == 2)
            {
                int w, h;
                if (int.TryParse(parts[0], out w) && int.TryParse(parts[1], out h))
                {
                    size = new Size(w, h);
                    return true;
                }
            }
            return false;
        }

        private bool readSizeF(IniReader reader, string section, string name, out SizeF size)
        {
            size = Size.Empty;
            string sizeStr = reader.ReadString(section, name, "");
            string[] parts = sizeStr.Split(';');
            if (parts.Length == 2)
            {
                float w, h;
                if (float.TryParse(parts[0], out w) && float.TryParse(parts[1], out h))
                {
                    size = new SizeF(w, h);
                    return true;
                }
            }
            return false;
        }


        private void UpdateConfig()
        {
            try
            {
                config.Reload();
                if (!initDone)
                {
                    // has to be in main till renderer interface supports window setttings               
                    if (readSize(config, "Init", "CreateWindowSize", out Size windowSize))
                    {
                        renderer.SetWindowSize(windowSize.Width, windowSize.Height);
                    }
                }
                iseSkin?.LoadFromConfigFile(config);
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.Message);
            }

        }

        private void FBChanged(object sender, glfw3.SizeEventArgs e)
        {
            needsReFit = true;
        }

        private void MBChanged(object sender, glfw3.MouseButtonEventArgs e)
        {
            if (e.Button == glfw3.MouseButton.ButtonLeft)
            {
                Vector3 pos = IseSkin.PositionTransform(Vector3.Create(e.PositionNorm.X, e.PositionNorm.Y, 0));
                //iseSkin.Sprites["3"].Sprite.Position = pos;
            }
            if (e.Button == glfw3.MouseButton.ButtonRight)
            {
                UpdateConfig();
            }

        }

        private void WindowClosed(object sender, EventArgs e)
        {
            exit = true;
        }
    }
}
