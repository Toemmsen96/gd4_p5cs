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
- Transform stack: `push()` / `pop()`, `translate`, `rotate`
- Mouse input: `mouseX`, `mouseY`, `mouseIsPressed`, `mousePressed()`
- Time helpers: `hour()`, `minute()`, `second()`
- Constants: `TWO_PI`, `HALF_PI`, `QUARTER_PI`

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
<summary>Drawing</summary>

| GDScript | C# | Description |
|---|---|---|
| `circle(x, y, r)` | `Circle(x, y, r)` | Draw a circle |
| `line(x1, y1, x2, y2)` | `Line(x1, y1, x2, y2)` | Draw a line |
| `arc(x, y, w, h, start, stop)` | — | Draw an arc |
| `point(x, y)` | — | Draw a point |
| `rect(x, y, w, h)` | — | Draw a rectangle |
| `square(x, y, s)` | — | Draw a square |
| `triangle(x1,y1, x2,y2, x3,y3)` | — | Draw a triangle |
| `quad(x1,y1, x2,y2, x3,y3, x4,y4)` | — | Draw a quadrilateral |
| `beginShape()` / `vertex(x,y)` / `endShape()` | — | Draw a custom polygon |

</details>

<details>
<summary>Color & Style</summary>

| GDScript | C# | Description |
|---|---|---|
| `fill(color)` | `Fill(color)` | Set fill color |
| `noFill()` | `NoFill()` | Disable fill |
| `stroke(color)` | `Stroke(color)` | Set stroke color |
| `noStroke()` | `NoStroke()` | Disable stroke |
| `strokeWeight(w)` | `StrokeWeightSet(w)` | Set stroke width |
| `background(color)` | `Background(color)` | Clear canvas with color |

</details>

<details>
<summary>Canvas & Control</summary>

| GDScript | C# | Description |
|---|---|---|
| `createCanvas(w, h)` | `CreateCanvas(w, h)` | Set canvas size |
| `loop()` | `Loop()` | Resume draw loop |
| `noLoop()` | `NoLoop()` | Stop draw loop |
| `pause()` | `Pause()` | Toggle pause |
| `restart()` | `Restart()` | Restart sketch |
| `clear()` | — | Clear the canvas |

</details>

<details>
<summary>Transforms</summary>

| GDScript | Description |
|---|---|
| `push()` | Save transform state |
| `pop()` | Restore transform state |
| `m_translate(x, y)` | Translate origin |
| `m_rotate(angle)` | Rotate around origin |
| `resetMatrix()` | Reset transforms |

</details>

<details>
<summary>Input & Time</summary>

| GDScript / C# | Description |
|---|---|
| `mouseX`, `mouseY` | Current mouse position |
| `pmouseX`, `pmouseY` | Previous mouse position (GDScript) |
| `mouseIsPressed` | Whether a mouse button is held |
| `mouseButton` | Which mouse button is pressed |
| `mousePressed()` | Override to handle mouse clicks |
| `hour()`, `minute()`, `second()` | Current time |
| `frameCount` | Frames elapsed since start |
| `deltaTime` | Time since last frame |

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
