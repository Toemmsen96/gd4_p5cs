#nullable enable
using Godot;

// Drop into any scene, set the Sketch export to a class that extends GodotP5.
// The node self-assembles its SubViewport tree and wires all signals.
[Tool]
public partial class GodotP5Node : SubViewportContainer
{
    [Export] public Script? Sketch { get; set; }

    private SubViewport _viewport = null!;
    private ColorRect   _bg       = null!;
    private Node2D      _canvas   = null!;
    private GodotP5?    _p5;

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

        _canvas = new Node2D { Name = "Canvas" };
        _viewport.AddChild(_canvas);

        SetProcessInput(true);

        if (Sketch != null)
            LoadSketch();
        else
            GD.PushWarning("GodotP5Node: no Sketch assigned — set the Sketch export in the Inspector.");
    }

    private void LoadSketch()
    {
        _canvas.SetScript(Sketch);
        _canvas = _viewport.GetNode<Node2D>("Canvas");

        if (_canvas is not GodotP5 p5)
        {
            GD.PushError("GodotP5Node: Sketch must extend GodotP5.");
            return;
        }

        _p5 = p5;
        p5.Connect(GodotP5.SignalName.SetBackgroundColor, new Callable(this, nameof(OnSetBackground)));
        p5.Connect(GodotP5.SignalName.SetViewportSize,    new Callable(this, nameof(OnSetViewportSize)));
        p5.SubViewport = _viewport;
        p5.InitFromMainScene();
    }

    public override void _Input(InputEvent @event)
    {
        // SubViewportContainer only forwards mouse events to the SubViewport.
        // Call HandleKeyEvent directly on the sketch to bypass SubViewport routing entirely.
        if (@event is InputEventKey keyEvent && _p5 != null)
        {
            _p5.HandleKeyEvent(keyEvent);
            GetViewport().SetInputAsHandled();
        }
    }

    private void OnSetBackground(Color color)
    {
        _bg.Color = color;
    }

    private void OnSetViewportSize(Vector2I size)
    {
        _viewport.Size           = size;
        _viewport.Size2DOverride = size;
        _bg.Size                 = new Vector2(size.X, size.Y);
        CustomMinimumSize        = new Vector2(size.X, size.Y);
        DisplayServer.WindowSetSize(size);
    }
}
