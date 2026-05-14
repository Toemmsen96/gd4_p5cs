using Godot;
using System.Collections.Generic;

public partial class Gametest : GodotP5
{
    private sealed class NoteState
    {
        public float X;
        public float Y;
        public bool Hit;
        public Color NoteColor;
    }

    private readonly List<NoteState> _notes = new();
    private float _speed = 2.5f;
    private int _score;
    private int _combo;
    private int _missed;
    private const float LaneX = 100f;

    // Hit flash: lerps back to dim over time
    private Color _hitFlash = Colors.Transparent;
    private float _hitFlashTimer;

    public override void Setup()
    {
        _notes.Clear();
        _score = 0;
        _combo = 0;
        _missed = 0;
        _speed = 2.5f;
        _hitFlash = Colors.Transparent;

        SetTitle("Gametest");
        SetViewportMode(ViewportMode.Always);
        CreateCanvas(700, 450);

        for (int i = 0; i < 25; i++)
            SpawnNote(Width + i * 110f);
    }

    private void SpawnNote(float x)
    {
        float hue = Random(0f, 1f);
        _notes.Add(new NoteState
        {
            X = x,
            Y = Random(60f, Height - 60f),
            Hit = false,
            NoteColor = Color.FromHsv(hue, 0.9f, 1f),
        });
    }

    public override void KeyPressed()
    {
        if (Key == "R") Restart();
    }

    public override void DrawSketch()
    {
        // Scroll speed climbs with score
        _speed = Map(_score, 0, 500, 2.5f, 7f);
        _speed = Constrain(_speed, 2.5f, 7f);

        // Background
        Background(new Color(0.08f, 0.08f, 0.12f));

        // Lane background
        NoStroke();
        Fill(new Color(1f, 1f, 1f, 0.04f));
        Rect(0, 0, LaneX + 20f, Height);

        // Hit-zone glow
        if (_hitFlashTimer > 0f)
        {
            _hitFlashTimer -= DeltaTime;
            Fill(LerpColor(_hitFlash, Colors.Transparent, 1f - _hitFlashTimer / 0.15f));
            Rect(LaneX - 18f, 0, 36f, Height);
        }

        // Lane line
        Stroke(new Color(0f, 1f, 0.5f, 0.8f));
        StrokeWeight(2f);
        Line(LaneX, 0, LaneX, Height);

        // Cursor (player)
        float cursorY = Constrain(MouseY, 20f, Height - 20f);
        NoStroke();
        Fill(new Color(0f, 1f, 0.5f));
        Circle(LaneX, cursorY, 12);
        Fill(new Color(0f, 1f, 0.5f, 0.25f));
        Circle(LaneX, cursorY, 28);

        // Notes
        foreach (NoteState note in _notes)
        {
            note.X -= _speed;

            if (note.Hit) continue;

            // Check hit
            if (!note.Hit
                && Dist(note.X, note.Y, LaneX, cursorY) < 22f)
            {
                note.Hit = true;
                _score += 10 + _combo * 2;
                _combo++;
                _hitFlash = note.NoteColor;
                _hitFlashTimer = 0.15f;
            }

            // Draw shadow trail
            Fill(new Color(note.NoteColor, 0.15f));
            for (int t = 1; t <= 4; t++)
                Circle(note.X + t * 8f, note.Y, Lerp(14f, 4f, t / 4f));

            // Draw note
            Fill(note.NoteColor);
            Circle(note.X, note.Y, 14);
            Fill(new Color(1f, 1f, 1f, 0.5f));
            Circle(note.X - 4f, note.Y - 4f, 4);
        }

        // Remove notes that passed the lane without being hit
        int before = _notes.Count;
        _notes.RemoveAll(n => n.X < -30f);
        int removed = before - _notes.Count;
        for (int i = 0; i < removed; i++)
        {
            _notes.RemoveAll(n => !n.Hit && n.X < -30f);
            _missed++;
            _combo = 0;
        }

        // Refill note queue
        while (_notes.Count < 25)
            SpawnNote(_notes.Count > 0 ? _notes[^1].X + Random(80f, 160f) : Width + 80f);

        DrawHud(cursorY);
    }

    private void DrawHud(float cursorY)
    {
        // Score
        TextSize(22);
        TextAlign(HorizontalAlignment.Left);
        Fill(Colors.White);
        Text($"Score  {_score}", 14, 18);

        // Combo
        if (_combo > 1)
        {
            float pulse = 1f + Mathf.Sin(FrameCount * 0.2f) * 0.15f;
            TextSize((int)(16 * pulse));
            Color comboColor = Color.FromHsv(Map(_combo, 0, 30, 0.33f, 0f), 1f, 1f);
            Fill(comboColor);
            Text($"x{_combo} combo", 14, 46);
        }

        // Speed indicator
        TextSize(13);
        Fill(new Color(1f, 1f, 1f, 0.5f));
        Text($"Speed  {_speed:F1}", 14, Height - 40f);

        // Missed counter
        Color missColor = LerpColor(Colors.White, Colors.OrangeRed, Constrain(_missed / 10f, 0f, 1f));
        Fill(missColor);
        Text($"Missed {_missed}", 14, Height - 20f);

        // Accuracy guide line from cursor
        Stroke(new Color(0f, 1f, 0.5f, 0.12f));
        StrokeWeight(1f);
        Line(LaneX, cursorY, Width, cursorY);

        // Controls hint
        TextSize(12);
        TextAlign(HorizontalAlignment.Right);
        Fill(new Color(1f, 1f, 1f, 0.35f));
        Text("R — restart", Width - 10f, Height - 18f);
    }
}
