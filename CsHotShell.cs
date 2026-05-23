using Godot;
#nullable enable
// Permanent GodotP5 shell node. A dynamically compiled HotSketch is swapped in/out
// without touching the scene tree. The shell delegates all virtual calls to the sketch.
public partial class CsHotShell : GodotP5
{
    private HotSketch? _sketch;

    public void LoadHotSketch(HotSketch? sketch)
    {
        _sketch = sketch;
        if (_sketch != null)
            _sketch.Shell = this;
    }

    public override void Setup()         => _sketch?.Setup();
    public override void DrawSketch()    => _sketch?.DrawSketch();
    public override void MousePressed()  => _sketch?.MousePressed();
    public override void MouseReleased() => _sketch?.MouseReleased();
    public override void MouseClicked()  => _sketch?.MouseClicked();
    public override void MouseMoved()    => _sketch?.MouseMoved();
    public override void MouseDragged()  => _sketch?.MouseDragged();
    public override void KeyPressed()    => _sketch?.KeyPressed();
    public override void KeyReleased()   => _sketch?.KeyReleased();
}
