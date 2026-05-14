# gd4_p5cs

gd4_p5cs is an interpretation of [p5.js](https://p5js.org) in Godot 4. It supports both **GDScript** and **C#** sketches, making it suitable for fast creative prototyping as well as more advanced work using Godot's .NET ecosystem.

Based on [adcomp/Godot4_p5](https://github.com/adcomp/Godot4_p5).

---

# Features

- Write sketches in **GDScript** or **C#** — both are fully supported
- **Hot-reloading**: changes to GDScript sketches reload automatically; C# sketches reload on assembly rebuild
- **Select sketches at runtime** from a menu without restarting the project
- Built-in UI: **pause**, **restart**, **screenshot/save**, **color picker**
- p5.js-inspired API: `setup()`, `draw()`, `background()`, `fill()`, `stroke()`, and more
- **Full shape set**: circle, ellipse, arc, rect, triangle, quad, point, bezier, curve, beginShape/endShape
- **Transform stack**: `push()` / `pop()` saves and restores transform **and** style state; `translate`, `rotate`, `scale`, `resetMatrix`
- **Math helpers**: `map`, `constrain`, `lerp`, `dist`, `noise` (1D/2D/3D), `random`, `randomGaussian`, and more
- **Text & images**: `text()`, `textSize()`, `textAlign()`, `loadImage()`, `image()`
- **Color helpers**: `lerpColor`, `red/green/blue/alpha` channel extraction
- Mouse input: `mouseX/Y`, `pmouseX/Y`, `movedX/Y`, `mouseIsPressed`, event callbacks
- Keyboard input: `keyIsPressed`, `key`, `keyCode`, `keyPressed()` / `keyReleased()` callbacks
- Time helpers: `hour()`, `minute()`, `second()`, `day()`, `month()`, `year()`, `millis()`
- Constants: `PI`, `TWO_PI`, `HALF_PI`, `QUARTER_PI`, `E`

---

# Usage

## Running a sketch

Open the project in Godot 4, press **Run**. Use the on-screen menu to select a sketch from the `sketch/` folder.

## Creating a GDScript sketch

Create a `.gd` file in `sketch/` that extends `godotp5_class`:

```gdscript
extends godotp5_class

func setup():
    createCanvas(800, 800)

func _draw():
    background(Color.BLACK)
    fill(Color.WHITE)
    circle(width / 2, height / 2, 100)
```

## Creating a C# sketch

Create a `.cs` file in `sketch/` that extends `GodotP5`:

```csharp
using Godot;

public partial class MySketch : GodotP5
{
    public override void Setup()
    {
        CreateCanvas(800, 800);
    }

    public override void DrawSketch()
    {
        Background(new Color(0, 0, 0));
        Fill(new Color(1, 1, 1));
        Circle(Width / 2, Height / 2, 100);
    }
}
```

---

# API Reference

<details>
<summary>Drawing — Shapes</summary>

| GDScript | C# | Description |
|---|---|---|
| `circle(x, y, r)` | `Circle(x, y, r)` | Filled circle with optional stroke |
| `ellipse(x, y, w, h)` | `Ellipse(x, y, w, h)` | Ellipse with independent width / height |
| `arc(x, y, w, h, start, stop)` | `Arc(x, y, w, h, start, stop)` | Elliptical arc (radians) |
| `line(x1, y1, x2, y2)` | `Line(x1, y1, x2, y2)` | Line segment |
| `point(x, y)` | `Point(x, y)` | Single point (sized by stroke weight) |
| `rect(x, y, w, h)` | `Rect(x, y, w, h)` | Rectangle |
| `square(x, y, s)` | `Square(x, y, s)` | Square |
| `triangle(x1,y1, x2,y2, x3,y3)` | `Triangle(...)` | Triangle |
| `quad(x1,y1, x2,y2, x3,y3, x4,y4)` | `Quad(...)` | Quadrilateral |
| `beginShape()` | `BeginShape()` | Start a custom polygon |
| `vertex(x, y)` | `Vertex(x, y)` | Add a vertex |
| `endShape([close])` | `EndShape([close])` | Finish and draw the polygon |
| `bezier(x1,y1, cx1,cy1, cx2,cy2, x2,y2)` | `Bezier(...)` | Cubic Bézier curve |
| `curve(x1,y1, x2,y2, x3,y3, x4,y4)` | — | Catmull-Rom spline |

</details>

<details>
<summary>Drawing — Text & Images</summary>

| GDScript | C# | Description |
|---|---|---|
| `text(str, x, y)` | `Text(str, x, y)` | Draw a string at position |
| `textSize(size)` | `TextSize(size)` | Set font size (pixels) |
| `textFont(font)` | — | Set font resource |
| `textAlign(h [, v])` | `TextAlign(h)` | Set text alignment |
| `loadImage(path)` | `LoadImage(path)` | Load a `Texture2D` from a path |
| `image(tex, x, y [, w, h])` | `Image(tex, x, y [, w, h])` | Draw a texture |

</details>

<details>
<summary>Color & Style</summary>

| GDScript | C# | Description |
|---|---|---|
| `fill(color)` | `Fill(color)` | Set fill color |
| `noFill()` | `NoFill()` | Disable fill |
| `stroke(color)` | `Stroke(color)` | Set stroke color |
| `noStroke()` | `NoStroke()` | Disable stroke |
| `strokeWeight(w)` | `StrokeWeight(w)` | Set stroke width |
| `smooth()` | `Smooth()` | Enable anti-aliasing (default) |
| `noSmooth()` | `NoSmooth()` | Disable anti-aliasing |
| `background(color [, alpha])` | `Background(color)` | Clear canvas with color |
| `clear()` | — | Redraw background color |
| `lerpColor(c1, c2, amt)` | `LerpColor(c1, c2, amt)` | Interpolate between two colors |
| `red(c)` / `green(c)` / `blue(c)` / `alpha(c)` | `Red(c)` / `Green(c)` / `Blue(c)` / `Alpha(c)` | Extract color channel (0–1) |
| `colorFromHSB(h, s, b [, a])` | — | Create color from HSB values |

</details>

<details>
<summary>Canvas & Control</summary>

| GDScript | C# | Description |
|---|---|---|
| `createCanvas(w, h)` | `CreateCanvas(w, h)` | Set canvas size |
| `loop()` | `Loop()` | Resume draw loop |
| `noLoop()` | `NoLoop()` | Stop draw loop |
| `pause()` | `Pause()` | Toggle pause |
| `restart()` | `Restart()` | Re-run `setup()` |
| `clear()` | — | Clear the canvas |
| `frameRate(fps)` | `FrameRate(fps)` | Set target frame rate |
| `getTargetFrameRate()` | `GetTargetFrameRate()` | Read target frame rate |
| `setPointCount(n)` | `SetPointCount(n)` | Default vertex count for arcs/circles |
| `set_title(title)` | `SetTitle(title)` | Set window title |

</details>

<details>
<summary>Transforms</summary>

`push()` / `pop()` save and restore **both** the draw transform **and** the full style state (fill, stroke, stroke weight, text size, etc.). `translate`, `rotate`, and `scale` operate in draw-space and must be called from inside `_draw()` / `DrawSketch()`.

| GDScript | C# | Description |
|---|---|---|
| `push()` | `Push()` | Save transform + style state |
| `pop()` | `Pop()` | Restore transform + style state |
| `draw_translate(x, y)` | `Translate(x, y)` | Translate draw origin |
| `draw_rotate(angle)` | `Rotate(angle)` | Rotate draw context (radians) |
| `draw_scale(x, y)` | `Scale(x, y)` | Scale draw context |
| `resetMatrix()` | `ResetMatrix()` | Reset draw transform to identity |
| `m_translate(x, y)` | — | Legacy: translate the node itself |
| `m_rotate(angle)` | — | Legacy: rotate the node itself |

</details>

<details>
<summary>Input</summary>

**Variables**

| GDScript | C# | Description |
|---|---|---|
| `mouseX`, `mouseY` | `MouseX`, `MouseY` | Current mouse position |
| `pmouseX`, `pmouseY` | `PMouseX`, `PMouseY` | Mouse position last frame |
| `movedX`, `movedY` | `MovedX`, `MovedY` | Mouse delta since last frame |
| `mouseIsPressed` | `MouseIsPressed` | Whether a button is held |
| `mouseButton` | `MouseButton` | `"LEFT"`, `"CENTER"`, or `"RIGHT"` |
| `keyIsPressed` | `KeyIsPressed` | Whether a key is held |
| `key` | `Key` | Name of the pressed key |
| `keyCode` | — | Physical keycode of the pressed key |

**Event callbacks** (override in your sketch)

| GDScript | C# | Triggered when |
|---|---|---|
| `mousePressed()` | `MousePressed()` | Mouse button held (every frame) |
| `mouseClicked()` | `MouseClicked()` | Mouse button first pressed |
| `mouseReleased()` | `MouseReleased()` | Mouse button released |
| `mouseMoved()` | `MouseMoved()` | Mouse moved (not dragging) |
| `mouseDragged()` | `MouseDragged()` | Mouse moved while button held |
| `keyPressed()` | `KeyPressed()` | Key first pressed |
| `keyReleased()` | `KeyReleased()` | Key released |

</details>

<details>
<summary>Math</summary>

| GDScript | C# | Description |
|---|---|---|
| `map(v, is, is2, os, os2)` | `Map(v, is, is2, os, os2)` | Re-map a value from one range to another |
| `constrain(n, lo, hi)` | `Constrain(n, lo, hi)` | Clamp a value |
| `lerp(a, b, t)` *(built-in)* | `Lerp(a, b, t)` | Linear interpolation |
| `dist(x1,y1, x2,y2)` | `Dist(x1,y1, x2,y2)` | Distance between two points |
| `mag(a, b)` | `Mag(a, b)` | Magnitude of a 2D vector |
| `norm(v, start, stop)` | `Norm(v, start, stop)` | Normalize to 0–1 |
| `sq(n)` | `Sq(n)` | Square (`n * n`) |
| `degrees(r)` | `Degrees(r)` | Radians → degrees |
| `radians(d)` | `Radians(d)` | Degrees → radians |
| `random_val(max)` | `Random(max)` | Random float 0–max |
| `random_range(min, max)` | `Random(min, max)` | Random float in range |
| `random_int(min, max)` | `Random(min, max)` | Random int in range |
| `randomGaussian([mean, sd])` | `RandomGaussian([mean, sd])` | Gaussian random number |
| `noise_val(x)` | `Noise(x)` | 1D noise (0–1) |
| `noise_2d(x, y)` | `Noise(x, y)` | 2D noise (0–1) |
| `noise_3d(x, y, z)` | `Noise(x, y, z)` | 3D noise (0–1) |
| `noiseSeed(seed)` | `NoiseSeed(seed)` | Set noise seed |
| `createVector(x, y)` | — | Create a `Vector2` |

</details>

<details>
<summary>Time & Date</summary>

| GDScript | C# | Description |
|---|---|---|
| `hour()` | `Hour()` | Current hour (0–23) |
| `minute()` | `Minute()` | Current minute (0–59) |
| `second()` | `Second()` | Current second (0–59) |
| `day()` | `Day()` | Current day of month |
| `month()` | `Month()` | Current month (1–12) |
| `year()` | `Year()` | Current year |
| `millis()` | `Millis()` | Milliseconds since engine start |
| `frameCount` | `FrameCount` | Frames elapsed since setup |
| `deltaTime` | `DeltaTime` | Seconds since last frame |

</details>

<details>
<summary>Constants</summary>

| Name | Value |
|---|---|
| `PI` | π ≈ 3.14159 |
| `TWO_PI` / `TAU` | 2π ≈ 6.28318 |
| `HALF_PI` | π/2 ≈ 1.5708 |
| `QUARTER_PI` | π/4 ≈ 0.7854 |
| `E` | e ≈ 2.71828 |

</details>

---

# Included Sketches

| Sketch | Language | Description |
|---|---|---|
| `AdditiveWaves` | GDScript | Additive wave synthesis (Coding Train #30) |
| `Circle_sin` | GDScript | Sine wave circles with animated lines |
| `Clock` | GDScript | Animated clock face (Coding Train #74) |
| `Lines_Dancing_Trigo` | GDScript | Trigonometric dancing lines |
| `MixWaves` | GDScript | Multiple sine waves mixed with color |
| `Phyllotaxis` | GDScript | Golden angle spiral / Fibonacci pattern |
| `Sincos_Trigo_Dance` | GDScript | Complex sine/cosine patterns |
| `blobby` | GDScript | Animated blob using rotated vectors |
| `blobby_polygon` | GDScript | Blob shape drawn as polygon |
| `circle_dance` | GDScript | Dancing circles with trig functions |
| `draw_with_mouse` | GDScript | Simple freehand mouse drawing |
| `heart_sin` | GDScript | Heart-shaped arc pattern |
| `lines_color` | GDScript | Animated lines with color gradients |
| `noise_circle_grid` | GDScript | Grid of circles driven by FastNoise |
| `space_circle` | GDScript | Particles moving in circular orbits |
| `spirograph` | GDScript | Spirograph mathematical visualization |
| `wrap_node` | GDScript | Wrapping particles with proximity connections |
| `test_func` | GDScript | Demonstrates various drawing functions |
| `empty_sketch` | GDScript | Blank template for new sketches |
| `Gametest` | C# | Interactive note-matching game |

---

# Status

Work in progress. Core drawing and control APIs are stable. C# support covers the most commonly used features.

---

# Credits

- [adcomp](https://github.com/adcomp/Godot4_p5) — original Godot4_p5 base project
- [Toemmsen96](https://github.com/Toemmsen96/gd4_p5cs) — C# support, hot-reload, sketch selector
