#nullable enable
using Godot;
using System.IO;

// Drop into any scene. Set either:
//   Sketch       — a compiled GodotP5 subclass script (.cs or .gd)
//   HotSketchPath — path to a HotSketch .cs file (auto-detected if you assign it to Sketch too)
/// <summary>
/// A SubViewportContainer that self-assembles a SubViewport and runs a GodotP5 sketch.
/// Assign a compiled <c>GodotP5</c> subclass script to <c>Sketch</c>, or a HotSketch
/// <c>.cs</c> file to <c>HotSketchPath</c> (or to <c>Sketch</c> — auto-detected).
/// GDScript sketches extending <c>GodotP5</c> are also supported via <c>Sketch</c>.
/// </summary>
[Tool]
[GlobalClass]
public partial class GodotP5Node : SubViewportContainer
{
    [Export] public Script? Sketch { get; set; }

    /// <summary>Path to a .cs HotSketch file, e.g. "res://hotreload_sketches/MySketch.cs".
    /// Alternatively, just assign the file to Sketch — it is detected automatically.</summary>
    [Export] public string HotSketchPath { get; set; } = "";

    private SubViewport _viewport = null!;
    private ColorRect   _bg       = null!;
    private Node2D      _canvas   = null!;
    private GodotP5?    _p5;       // set for C# and hot-reload sketches
    private Node2D?     _gdSketch; // set for GDScript sketches

    private CsHotReload?       _hotReload;
    private FileSystemWatcher? _watcher;

    public override string[] _GetConfigurationWarnings() => [];

    public override void _Ready()
    {
        Stretch = false;

        _viewport = new SubViewport
        {
            Name = "Viewport",
            HandleInputLocally = true,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
        };
        AddChild(_viewport);

        _bg = new ColorRect
        {
            Name        = "Bg",
            Color       = Colors.Black,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _viewport.AddChild(_bg);

        SetProcessInput(true);

        // Auto-detect: if Sketch is a .cs file that doesn't extend a Godot node
        // (GetInstanceBaseType is empty), it must be a HotSketch.
        if (string.IsNullOrEmpty(HotSketchPath)
            && Sketch != null
            && IsHotSketchScript(Sketch))
        {
            HotSketchPath = Sketch.ResourcePath;
            Sketch = null;
        }

        bool useHotReload = !string.IsNullOrEmpty(HotSketchPath);

        if (useHotReload)
        {
            var shell = new CsHotShell { Name = "Canvas" };
            _viewport.AddChild(shell);
            _canvas = shell;
            LoadHotSketch(setup: true);
        }
        else if (Sketch != null)
        {
            _canvas = new Node2D { Name = "Canvas" };
            _viewport.AddChild(_canvas);
            LoadCompiledSketch();
        }
        else
        {
            _canvas = new Node2D { Name = "Canvas" };
            _viewport.AddChild(_canvas);
            GD.PushWarning("GodotP5Node: assign Sketch or HotSketchPath in the Inspector.");
        }
    }

    // A C# script extending a plain class (HotSketch) has no Godot base type.
    private static bool IsHotSketchScript(Script script) =>
        script.ResourcePath.EndsWith(".cs") && string.IsNullOrEmpty(script.GetInstanceBaseType());

    // ── Compiled sketch (C# GodotP5 subclass or GDScript) ────────────────────

    private void LoadCompiledSketch()
    {
        _canvas.SetScript(Sketch!);
        _canvas = _viewport.GetNode<Node2D>("Canvas");

        if (_canvas is GodotP5 p5)
        {
            _p5 = p5;
            p5.Connect(GodotP5.SignalName.SetBackgroundColor, new Callable(this, nameof(OnSetBackground)));
            p5.Connect(GodotP5.SignalName.SetViewportSize,    new Callable(this, nameof(OnSetViewportSize)));
            p5.SubViewport = _viewport;
            p5.InitFromMainScene();
        }
        else if (_canvas.HasSignal("set_background_color"))
        {
            // GDScript sketch extending the GDScript GodotP5 base class
            _gdSketch = _canvas;
            _canvas.Connect("set_background_color", new Callable(this, nameof(OnSetBackground)));
            _canvas.Connect("set_viewport_size",    new Callable(this, nameof(OnSetViewportSize)));
            _canvas.Set("sub_viewport", _viewport);
            _canvas.Call("_init_from_main_scene");
        }
        else
        {
            GD.PushError("GodotP5Node: Sketch must extend GodotP5.");
        }
    }

    // ── Hot-reload sketch ─────────────────────────────────────────────────────

    private void LoadHotSketch(bool setup)
    {
        string absPath = ProjectSettings.GlobalizePath(HotSketchPath);
        if (!File.Exists(absPath))
        {
            GD.PushError($"GodotP5Node: HotSketch file not found: {absPath}");
            return;
        }

        _hotReload ??= new CsHotReload();
        var (sketch, error) = _hotReload.CompileAndLoad(absPath);
        if (error != null)
        {
            GD.PushError($"GodotP5Node HotSketch compile error:\n{error}");
            return;
        }

        var shell = (CsHotShell)_canvas;
        shell.LoadHotSketch(sketch);

        if (setup)
        {
            _p5 = shell;
            shell.Connect(GodotP5.SignalName.SetBackgroundColor, new Callable(this, nameof(OnSetBackground)));
            shell.Connect(GodotP5.SignalName.SetViewportSize,    new Callable(this, nameof(OnSetViewportSize)));
            shell.SubViewport = _viewport;
            shell.InitFromMainScene();

            _watcher = new FileSystemWatcher(
                Path.GetDirectoryName(absPath)!,
                Path.GetFileName(absPath))
            {
                NotifyFilter        = NotifyFilters.LastWrite,
                EnableRaisingEvents = true,
            };
            _watcher.Changed += (_, _) =>
                Callable.From(() => LoadHotSketch(setup: false)).CallDeferred();
        }
        else
        {
            shell.Restart();
        }
    }

    public override void _ExitTree()
    {
        _watcher?.Dispose();
        _hotReload?.Dispose();
    }

    // ── Input ─────────────────────────────────────────────────────────────────

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventKey keyEvent) return;

        if (_p5 != null)
        {
            _p5.HandleKeyEvent(keyEvent);
            GetViewport().SetInputAsHandled();
        }
        else if (_gdSketch != null)
        {
            _gdSketch.Call("_unhandled_input", keyEvent);
            GetViewport().SetInputAsHandled();
        }
    }

    // ── Callbacks ─────────────────────────────────────────────────────────────

    private void OnSetBackground(Color color) => _bg.Color = color;

    private void OnSetViewportSize(Vector2I size)
    {
        _viewport.Size           = size;
        _viewport.Size2DOverride = size;
        _bg.Size                 = new Vector2(size.X, size.Y);
        CustomMinimumSize        = new Vector2(size.X, size.Y);
        DisplayServer.WindowSetSize(size);
    }
}
