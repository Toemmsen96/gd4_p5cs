using Godot;

// Base class for C# sketches with hot-reload support.
// Use this instead of GodotP5. All drawing calls proxy through the shell node.
public abstract class HotSketch
{
    internal GodotP5 Shell { get; set; } = null!;

    // ── Lifecycle ─────────────────────────────────────────────────
    public virtual void Setup()        { }
    public virtual void DrawSketch()   { }
    public virtual void MousePressed() { }
    public virtual void MouseReleased(){ }
    public virtual void MouseClicked() { }
    public virtual void MouseMoved()   { }
    public virtual void MouseDragged() { }
    public virtual void KeyPressed()   { }
    public virtual void KeyReleased()  { }

    // ── State ─────────────────────────────────────────────────────
    protected float  Width         => Shell.Width;
    protected float  Height        => Shell.Height;
    protected int    MouseX        => Shell.MouseX;
    protected int    MouseY        => Shell.MouseY;
    protected int    PMouseX       => Shell.PMouseX;
    protected int    PMouseY       => Shell.PMouseY;
    protected int    MovedX        => Shell.MovedX;
    protected int    MovedY        => Shell.MovedY;
    protected bool   MouseIsPressed=> Shell.MouseIsPressed;
    protected string?MouseButton   => Shell.MouseButton;
    protected bool   KeyIsPressed  => Shell.KeyIsPressed;
    protected string?Key           => Shell.Key;
    protected int    FrameCount    => Shell.FrameCount;
    protected float  DeltaTime     => Shell.DeltaTime;

    // ── Canvas / loop ─────────────────────────────────────────────
    protected void CreateCanvas(int w, int h)                      => Shell.CreateCanvas(w, h);
    protected void SetTitle(string title)                          => Shell.SetTitle(title);
    protected void SetViewportMode(GodotP5.ViewportMode mode)      => Shell.SetViewportMode(mode);
    protected void NoLoop()                                        => Shell.NoLoop();
    protected void Loop()                                          => Shell.Loop();

    // ── Style ─────────────────────────────────────────────────────
    protected void Background(Color c, float alpha = -1f)          => Shell.Background(c, alpha);
    protected void Fill(Color c)                                   => Shell.Fill(c);
    protected void NoFill()                                        => Shell.NoFill();
    protected void Stroke(Color c)                                 => Shell.Stroke(c);
    protected void NoStroke()                                      => Shell.NoStroke();
    protected void StrokeWeight(float w)                           => Shell.StrokeWeight(w);
    protected void Smooth()                                        => Shell.Smooth();
    protected void NoSmooth()                                      => Shell.NoSmooth();

    // ── Transform ─────────────────────────────────────────────────
    protected void Push()                                          => Shell.Push();
    protected void Pop()                                           => Shell.Pop();
    protected void Translate(float x, float y)                     => Shell.Translate(x, y);
    protected void Rotate(float angle)                             => Shell.Rotate(angle);
    protected void Scale(float x, float y)                         => Shell.Scale(x, y);
    protected void ResetMatrix()                                   => Shell.ResetMatrix();

    // ── Shapes ────────────────────────────────────────────────────
    protected void Circle(float x, float y, float r, int pts = 32)    => Shell.Circle(x, y, r, pts);
    protected void Ellipse(float x, float y, float w, float h, int pts = 32) => Shell.Ellipse(x, y, w, h, pts);
    protected void Arc(float x, float y, float w, float h, float start, float stop, int pts = 32) => Shell.Arc(x, y, w, h, start, stop, pts);
    protected void Point(float x, float y)                         => Shell.Point(x, y);
    protected void Line(float x0, float y0, float x1, float y1)   => Shell.Line(x0, y0, x1, y1);
    protected void Triangle(float x1, float y1, float x2, float y2, float x3, float y3) => Shell.Triangle(x1, y1, x2, y2, x3, y3);
    protected void Quad(float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4) => Shell.Quad(x1, y1, x2, y2, x3, y3, x4, y4);
    protected void Rect(float x, float y, float w, float h)        => Shell.Rect(x, y, w, h);
    protected void Square(float x, float y, float s)               => Shell.Square(x, y, s);
    protected void BeginShape()                                    => Shell.BeginShape();
    protected void Vertex(float x, float y)                        => Shell.Vertex(x, y);
    protected void EndShape(bool close = false)                    => Shell.EndShape(close);
    protected void Bezier(float x1, float y1, float cx1, float cy1, float cx2, float cy2, float x2, float y2, int steps = 32)
        => Shell.Bezier(x1, y1, cx1, cy1, cx2, cy2, x2, y2, steps);

    // ── Text ──────────────────────────────────────────────────────
    protected void Text(string s, float x, float y)                => Shell.Text(s, x, y);
    protected void TextSize(int size)                              => Shell.TextSize(size);
    protected void TextAlign(HorizontalAlignment h)                => Shell.TextAlign(h);

    // ── Image ─────────────────────────────────────────────────────
    protected static Texture2D? LoadImage(string path)             => GodotP5.LoadImage(path);
    protected void Image(Texture2D tex, float x, float y, float w = -1, float h = -1) => Shell.Image(tex, x, y, w, h);

    // ── Math ──────────────────────────────────────────────────────
    protected static float Map(float value, float start1, float stop1, float start2, float stop2) => GodotP5.Map(value, start1, stop1, start2, stop2);
    protected static float Lerp(float a, float b, float t)         => GodotP5.Lerp(a, b, t);
    protected static Vector2 LerpV(Vector2 a, Vector2 b, float t)  => GodotP5.LerpV(a, b, t);
    protected static float Constrain(float n, float lo, float hi)  => GodotP5.Constrain(n, lo, hi);
    protected static float Dist(float x1, float y1, float x2, float y2) => GodotP5.Dist(x1, y1, x2, y2);
    protected static float Sq(float n)                             => GodotP5.Sq(n);
    protected static float Degrees(float r)                        => GodotP5.Degrees(r);
    protected static float Radians(float d)                        => GodotP5.Radians(d);
    protected float Random(float max)                              => Shell.Random(max);
    protected float Random(float min, float max)                   => Shell.Random(min, max);
    protected int   Random(int max)                                => Shell.Random(max);
    protected int   Random(int min, int max)                       => Shell.Random(min, max);
    protected float RandomGaussian(float mean = 0f, float sd = 1f) => Shell.RandomGaussian(mean, sd);
    protected float Noise(float x)                                 => Shell.Noise(x);
    protected float Noise(float x, float y)                        => Shell.Noise(x, y);
    protected float Noise(float x, float y, float z)               => Shell.Noise(x, y, z);
    protected void  NoiseSeed(int seed)                            => Shell.NoiseSeed(seed);

    // ── Color ─────────────────────────────────────────────────────
    protected static Color LerpColor(Color c1, Color c2, float t)  => GodotP5.LerpColor(c1, c2, t);

    // ── Constants ─────────────────────────────────────────────────
    protected const float PI         = Mathf.Pi;
    protected const float TWO_PI     = Mathf.Tau;
    protected const float TAU        = Mathf.Tau;
    protected const float HALF_PI    = Mathf.Pi / 2f;
    protected const float QUARTER_PI = Mathf.Pi / 4f;
    protected const float E          = Mathf.E;
}
