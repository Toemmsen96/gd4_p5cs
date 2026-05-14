#nullable enable
using Godot;
using System;
using System.Collections.Generic;

public partial class GodotP5 : Node2D
{
    [Signal] public delegate void SetBackgroundColorEventHandler(Color color);
    [Signal] public delegate void SetViewportSizeEventHandler(Vector2I viewportSize);
    [Signal] public delegate void SetCurrentColorEventHandler(Color color);

    public enum ViewportMode { Always, Never, Once }

    public SubViewport SubViewport { get; set; } = null!;

    // Colors
    public Color CurrentBackgroundColor = Colors.Black;
    public Color CurrentColor = Colors.White;

    // Canvas / window
    public float Width { get; protected set; } = 100;
    public float Height { get; protected set; } = 100;
    public static float DisplayWidth => DisplayServer.ScreenGetSize().X;
    public static float DisplayHeight => DisplayServer.ScreenGetSize().Y;
    public static float WindowWidth => DisplayServer.WindowGetSize().X;
    public static float WindowHeight => DisplayServer.WindowGetSize().Y;

    // Frame
    public int FrameCount { get; protected set; }
    public float DeltaTime { get; protected set; }

    // Mouse
    public int MouseX { get; protected set; }
    public int MouseY { get; protected set; }
    public int PMouseX { get; protected set; }
    public int PMouseY { get; protected set; }
    public int MovedX { get; protected set; }
    public int MovedY { get; protected set; }
    public bool MouseIsPressed { get; protected set; }
    public string? MouseButton { get; protected set; }

    // Keyboard
    public bool KeyIsPressed { get; protected set; }
    public string? Key { get; protected set; }

    // Constants
    public const float PI = Mathf.Pi;
    public const float TWO_PI = Mathf.Tau;
    public const float TAU = Mathf.Tau;
    public const float HALF_PI = Mathf.Pi / 2.0f;
    public const float QUARTER_PI = Mathf.Pi / 4.0f;
    public const float E = Mathf.E;

    // Style state
    protected Color FillColor = Colors.White;
    protected Color StrokeColor = Colors.Gray;
    protected float _strokeWeight = 1.0f;
    protected bool NoStrokeEnabled;
    protected bool NoFillEnabled;
    protected bool Antialiased = true;
    protected int _pointCount = 32;
    protected int _textSize = 14;
    protected HorizontalAlignment _textHAlign = HorizontalAlignment.Left;

    // Loop / init state
    protected bool IsLoaded;
    protected bool IsLooping = true;

    // Transform tracking (only valid during _Draw)
    private Transform2D _currentTransform = Transform2D.Identity;

    private struct StyleState
    {
        public Color FillColor, StrokeColor;
        public float StrokeWeight;
        public bool NoFill, NoStroke, Antialiased;
        public int PointCount, TextSize;
        public HorizontalAlignment TextHAlign;
        public Transform2D Transform;
    }
    private readonly Stack<StyleState> _stateStack = new();

    // BeginShape / EndShape vertex buffer
    private readonly List<Vector2> _shapeVertices = [];

    // Noise (lazy-init)
    private FastNoiseLite? _noise;

    // RNG
    private readonly Random _rng = new();

    // ── Lifecycle ─────────────────────────────────────────────────
    public void InitFromMainScene()
    {
        Setup();
        IsLoaded = true;
        if (!IsLooping) QueueRedraw();
        else Loop();
    }

    public override void _Process(double delta)
    {
        if (!IsLoaded) return;

        if (IsLooping)
        {
            FrameCount++;
            DeltaTime = (float)delta;
            QueueRedraw();
        }

        if (MouseIsPressed) MousePressed();

        PMouseX = MouseX;
        PMouseY = MouseY;
        Vector2 pos = GetLocalMousePosition();
        MouseX = (int)pos.X;
        MouseY = (int)pos.Y;
        MovedX = MouseX - PMouseX;
        MovedY = MouseY - PMouseY;
    }

    public override void _Draw()
    {
        _currentTransform = Transform2D.Identity;
        _stateStack.Clear();
        DrawSketch();
    }

    public override void _Input(InputEvent @event)
    {
        if (Input.IsActionJustPressed("ui_cancel"))
            GetTree().Quit();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb)
        {
            if (mb.Pressed)
            {
                MouseIsPressed = true;
                MouseButton = mb.ButtonIndex switch
                {
                    Godot.MouseButton.Left   => "LEFT",
                    Godot.MouseButton.Middle => "CENTER",
                    Godot.MouseButton.Right  => "RIGHT",
                    _ => MouseButton,
                };
                MouseClicked();
            }
            else
            {
                MouseIsPressed = false;
                MouseReleased();
            }
        }
        else if (@event is InputEventMouseMotion)
        {
            if (MouseIsPressed) MouseDragged();
            else MouseMoved();
        }
        else if (@event is InputEventKey keyEvent)
        {
            if (keyEvent.Pressed)
            {
                KeyIsPressed = true;
                Key = keyEvent.AsTextPhysicalKeycode();
                KeyPressed();
            }
            else
            {
                KeyIsPressed = false;
                Key = null;
                KeyReleased();
            }
        }
    }

    // ── Structure ─────────────────────────────────────────────────
    public void Pause() => SetProcess(!IsProcessing());
    public void Restart() => Setup();

    public void Loop()
    {
        IsLooping = true;
        SetProcess(true);
    }

    public void NoLoop()
    {
        IsLooping = false;
        SetProcess(false);
    }

    public bool IsLoopingState => IsLooping;

    // ── Canvas ────────────────────────────────────────────────────
    public void SetTitle(string title) => DisplayServer.WindowSetTitle(title);

    public void CreateCanvas(int width, int height)
    {
        Width = width;
        Height = height;
        EmitSignal(SignalName.SetViewportSize, new Vector2I(width, height));
    }

    public void SetViewportMode(ViewportMode mode)
    {
        if (SubViewport == null) return;
        int clearMode = mode switch
        {
            ViewportMode.Always => 0,
            ViewportMode.Never  => 1,
            ViewportMode.Once   => 2,
            _ => 0,
        };
        SubViewport.Set("render_target_clear_mode", clearMode);
    }

    // ── Transform (call inside DrawSketch) ────────────────────────
    public void Push()
    {
        _stateStack.Push(new StyleState
        {
            FillColor  = FillColor,
            StrokeColor = StrokeColor,
            StrokeWeight = _strokeWeight,
            NoFill     = NoFillEnabled,
            NoStroke   = NoStrokeEnabled,
            Antialiased = Antialiased,
            PointCount = _pointCount,
            TextSize   = _textSize,
            TextHAlign = _textHAlign,
            Transform  = _currentTransform,
        });
    }

    public void Pop()
    {
        if (_stateStack.Count == 0) return;
        StyleState s = _stateStack.Pop();
        FillColor     = s.FillColor;
        StrokeColor   = s.StrokeColor;
        _strokeWeight = s.StrokeWeight;
        NoFillEnabled   = s.NoFill;
        NoStrokeEnabled = s.NoStroke;
        Antialiased   = s.Antialiased;
        _pointCount   = s.PointCount;
        _textSize     = s.TextSize;
        _textHAlign   = s.TextHAlign;
        _currentTransform = s.Transform;
        DrawSetTransformMatrix(_currentTransform);
    }

    public void Translate(float x, float y)
    {
        _currentTransform = _currentTransform.Translated(new Vector2(x, y));
        DrawSetTransformMatrix(_currentTransform);
    }

    public new void Rotate(float angle)
    {
        _currentTransform = _currentTransform.Rotated(angle);
        DrawSetTransformMatrix(_currentTransform);
    }

    public new void Scale(float x, float y)
    {
        _currentTransform = _currentTransform.Scaled(new Vector2(x, y));
        DrawSetTransformMatrix(_currentTransform);
    }

    public void ResetMatrix()
    {
        _currentTransform = Transform2D.Identity;
        DrawSetTransformMatrix(_currentTransform);
    }

    // ── Settings ─────────────────────────────────────────────────
    public void Background(Color color, float alpha = -1)
    {
        CurrentBackgroundColor = color;
        EmitSignal(SignalName.SetBackgroundColor, color);
    }

    public void NoStroke() => NoStrokeEnabled = true;
    public void NoFill()   => NoFillEnabled = true;

    public void Fill(Color color)
    {
        FillColor = color;
        NoFillEnabled = false;
    }

    public void Stroke(Color color)
    {
        StrokeColor = color;
        NoStrokeEnabled = false;
    }

    public void SetColor(Color color)
    {
        CurrentColor = color;
        FillColor = color;
        StrokeColor = color;
        NoFillEnabled = false;
        NoStrokeEnabled = false;
        EmitSignal(SignalName.SetCurrentColor, color);
    }

    public void StrokeWeight(float weight) => _strokeWeight = weight;
    // Backward-compat alias
    public void StrokeWeightSet(float weight) => _strokeWeight = weight;

    public void SetPointCount(int count) => _pointCount = count;
    public void Smooth()   => Antialiased = true;
    public void NoSmooth() => Antialiased = false;

    public static void FrameRate(int fps) => Engine.MaxFps = fps;
    public static int  GetTargetFrameRate() => Engine.MaxFps;

    // ── Text ─────────────────────────────────────────────────────
    public void TextSize(int size) => _textSize = size;
    public void TextAlign(HorizontalAlignment h) => _textHAlign = h;

    public void Text(string str, float x, float y)
    {
        DrawString(ThemeDB.FallbackFont, new Vector2(x, y), str, _textHAlign, -1, _textSize, FillColor);
    }

    // Legacy helper kept for existing sketches
    public void DrawSketchString(Font font, Vector2 position, string text,
        HorizontalAlignment alignment, int width, int fontSize, Color modulate)
    {
        DrawString(font, position, text, alignment, width, fontSize, modulate);
    }

    // ── Shapes ───────────────────────────────────────────────────
    public void Circle(float x, float y, float radius, int pointCount = 32)
    {
        if (!NoFillEnabled)
            DrawCircle(new Vector2(x, y), radius, FillColor);
        if (!NoStrokeEnabled)
            DrawArc(new Vector2(x, y), radius, 0, Mathf.Tau, pointCount, StrokeColor, _strokeWeight, Antialiased);
    }

    public void Ellipse(float x, float y, float w, float h, int pointCount = 32)
    {
        float rx = w * 0.5f;
        float ry = h * 0.5f;
        var pts = new Vector2[pointCount + 1];
        for (int i = 0; i < pointCount; i++)
        {
            float a = Mathf.Tau * i / pointCount;
            pts[i] = new Vector2(x + rx * Mathf.Cos(a), y + ry * Mathf.Sin(a));
        }
        pts[pointCount] = pts[0];

        if (!NoFillEnabled)
        {
            var fill = new Vector2[pointCount];
            Array.Copy(pts, fill, pointCount);
            DrawPolygon(fill, [FillColor]);
        }
        if (!NoStrokeEnabled)
            DrawPolyline(pts, StrokeColor, _strokeWeight, Antialiased);
    }

    public void Arc(float x, float y, float w, float h, float start, float stop, int pointCount = 32)
    {
        float rx = w * 0.5f;
        float ry = h * 0.5f;
        if (Mathf.Abs(rx - ry) < 0.01f)
        {
            if (!NoStrokeEnabled)
                DrawArc(new Vector2(x, y), rx, start, stop, pointCount, StrokeColor, _strokeWeight, Antialiased);
            return;
        }
        var pts = new Vector2[pointCount + 1];
        for (int i = 0; i <= pointCount; i++)
        {
            float a = start + (stop - start) * i / pointCount;
            pts[i] = new Vector2(x + rx * Mathf.Cos(a), y + ry * Mathf.Sin(a));
        }
        if (!NoStrokeEnabled)
            DrawPolyline(pts, StrokeColor, _strokeWeight, Antialiased);
    }

    public void Point(float x, float y)
    {
        if (!NoStrokeEnabled)
            DrawCircle(new Vector2(x, y), _strokeWeight * 0.5f, StrokeColor);
    }

    public void Line(float x0, float y0, float x1, float y1)
    {
        if (!NoStrokeEnabled)
            DrawLine(new Vector2(x0, y0), new Vector2(x1, y1), StrokeColor, _strokeWeight, Antialiased);
    }

    public void Triangle(float x1, float y1, float x2, float y2, float x3, float y3)
    {
        Vector2[] pts = [new(x1, y1), new(x2, y2), new(x3, y3)];
        if (!NoFillEnabled)
            DrawPolygon(pts, [FillColor]);
        if (!NoStrokeEnabled)
            DrawPolyline([new(x1, y1), new(x2, y2), new(x3, y3), new(x1, y1)],
                StrokeColor, _strokeWeight, Antialiased);
    }

    public void Quad(float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4)
    {
        Vector2[] pts = [new(x1, y1), new(x2, y2), new(x3, y3), new(x4, y4)];
        if (!NoFillEnabled)
            DrawPolygon(pts, [FillColor]);
        if (!NoStrokeEnabled)
            DrawPolyline([new(x1, y1), new(x2, y2), new(x3, y3), new(x4, y4), new(x1, y1)],
                StrokeColor, _strokeWeight, Antialiased);
    }

    public void Rect(float x, float y, float w, float h)
        => Quad(x, y, x + w, y, x + w, y + h, x, y + h);

    public void Square(float x, float y, float s) => Rect(x, y, s, s);

    public void BeginShape() => _shapeVertices.Clear();

    public void Vertex(float x, float y) => _shapeVertices.Add(new Vector2(x, y));

    public void EndShape(bool close = false)
    {
        if (_shapeVertices.Count < 2) return;
        var poly = _shapeVertices.ToArray();
        if (!NoFillEnabled)
            DrawPolygon(poly, [FillColor]);
        if (!NoStrokeEnabled)
        {
            var line = new List<Vector2>(poly);
            if (close) line.Add(poly[0]);
            DrawPolyline([.. line], StrokeColor, _strokeWeight, Antialiased);
        }
        _shapeVertices.Clear();
    }

    public void Bezier(float x1, float y1, float cx1, float cy1, float cx2, float cy2, float x2, float y2, int steps = 32)
    {
        if (NoStrokeEnabled) return;
        var p0 = new Vector2(x1, y1);
        var p1 = new Vector2(cx1, cy1);
        var p2 = new Vector2(cx2, cy2);
        var p3 = new Vector2(x2, y2);
        var pts = new Vector2[steps + 1];
        for (int i = 0; i <= steps; i++)
        {
            float t = (float)i / steps;
            float u = 1 - t;
            pts[i] = u*u*u*p0 + 3*u*u*t*p1 + 3*u*t*t*p2 + t*t*t*p3;
        }
        DrawPolyline(pts, StrokeColor, _strokeWeight, Antialiased);
    }

    // ── Image ─────────────────────────────────────────────────────
    public static Texture2D? LoadImage(string path)
        => ResourceLoader.Load<Texture2D>(path);

    public void Image(Texture2D texture, float x, float y, float w = -1, float h = -1)
    {
        if (w < 0 || h < 0)
            DrawTexture(texture, new Vector2(x, y));
        else
            DrawTextureRect(texture, new Rect2(x, y, w, h), false);
    }

    // ── Math ─────────────────────────────────────────────────────
    public static float Map(float value, float start1, float stop1, float start2, float stop2)
        => start2 + (stop2 - start2) * ((value - start1) / (stop1 - start1));

    public static float Lerp(float start, float stop, float amt)
        => Mathf.Lerp(start, stop, amt);

    public static Vector2 LerpV(Vector2 start, Vector2 stop, float amt)
        => start.Lerp(stop, amt);

    public static float Constrain(float n, float low, float high)
        => Mathf.Clamp(n, low, high);

    public static float Dist(float x1, float y1, float x2, float y2)
        => new Vector2(x2 - x1, y2 - y1).Length();

    public static float Dist(float x1, float y1, float z1, float x2, float y2, float z2)
        => new Vector3(x2 - x1, y2 - y1, z2 - z1).Length();

    public static float Mag(float a, float b) => new Vector2(a, b).Length();

    public static float Norm(float value, float start, float stop)
        => (value - start) / (stop - start);

    public static float Sq(float n) => n * n;

    public static float Degrees(float radians) => Mathf.RadToDeg(radians);
    public static float Radians(float degrees) => Mathf.DegToRad(degrees);

    public float Random(float max)
        => (float)(_rng.NextDouble() * max);
    public float Random(float min, float max)
        => (float)(_rng.NextDouble() * (max - min) + min);
    public int Random(int max) => _rng.Next(max);
    public int Random(int min, int max) => _rng.Next(min, max);

    public float RandomGaussian(float mean = 0f, float sd = 1f)
    {
        // Box-Muller transform
        double u1 = 1.0 - _rng.NextDouble();
        double u2 = 1.0 - _rng.NextDouble();
        double z  = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        return (float)(mean + sd * z);
    }

    public float Noise(float x)
    {
        _noise ??= new FastNoiseLite();
        return (_noise.GetNoise1D(x) + 1.0f) * 0.5f;
    }

    public float Noise(float x, float y)
    {
        _noise ??= new FastNoiseLite();
        return (_noise.GetNoise2D(x, y) + 1.0f) * 0.5f;
    }

    public float Noise(float x, float y, float z)
    {
        _noise ??= new FastNoiseLite();
        return (_noise.GetNoise3D(x, y, z) + 1.0f) * 0.5f;
    }

    public void NoiseSeed(int seed)
    {
        _noise ??= new FastNoiseLite();
        _noise.Seed = seed;
    }

    // ── Color ────────────────────────────────────────────────────
    public static Color LerpColor(Color c1, Color c2, float amt) => c1.Lerp(c2, amt);
    public static float Red(Color c)   => c.R;
    public static float Green(Color c) => c.G;
    public static float Blue(Color c)  => c.B;
    public static float Alpha(Color c) => c.A;

    // ── Time ─────────────────────────────────────────────────────
    public static int  Hour()   => Time.GetTimeDictFromSystem()["hour"].AsInt32();
    public static int  Minute() => Time.GetTimeDictFromSystem()["minute"].AsInt32();
    public static int  Second() => Time.GetTimeDictFromSystem()["second"].AsInt32();
    public static int  Day()    => Time.GetDateDictFromSystem()["day"].AsInt32();
    public static int  Month()  => Time.GetDateDictFromSystem()["month"].AsInt32();
    public static int  Year()   => Time.GetDateDictFromSystem()["year"].AsInt32();
    public static long Millis() => (long)Time.GetTicksMsec();

    // ── Virtual hooks (override in sketches) ──────────────────────
    public virtual void Setup()      { }
    public virtual void DrawSketch() { }
    public virtual void MousePressed()  { }
    public virtual void MouseReleased() { }
    public virtual void MouseClicked()  { }
    public virtual void MouseMoved()    { }
    public virtual void MouseDragged()  { }
    public virtual void KeyPressed()    { }
    public virtual void KeyReleased()   { }
}
