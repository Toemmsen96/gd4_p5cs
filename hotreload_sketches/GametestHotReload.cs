using Godot;
using System.Collections.Generic;

public class GametestHotReload : HotSketch
{
    private sealed class NoteState
    {
        public float X;
        public float Y;
        public bool Hit;
        public Color NoteColor;
    }

    private readonly List<NoteState> _notes = new();
    private float _speed = 1.0f;
    private int _score;
    private int _combo;
    private int _missed;
    private int _consecutiveMisses;
    private int _notesHit;
    private bool _gameOver;
    private const float LaneX = 100f;

    private Color _hitFlash = Colors.Transparent;
    private float _hitFlashTimer;

    public override void Setup()
    {
        _notes.Clear();
        _score = 0;
        _combo = 0;
        _missed = 0;
        _consecutiveMisses = 0;
        _notesHit = 0;
        _gameOver = false;
        _speed = 1.0f;
        _hitFlash = Colors.Transparent;

        SetTitle("Gametest");
        SetViewportMode(GodotP5.ViewportMode.Always);
        CreateCanvas(700, 450);

        for (int i = 0; i < 4; i++)
            SpawnNote(Width + i * 160f);
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
        Background(new Color(0.08f, 0.08f, 0.12f));

        if (_gameOver)
        {
            DrawGameOver();
            return;
        }

        _speed = Map(_notesHit, 0, 150, 1.0f, 7f);
        _speed = Constrain(_speed, 1.0f, 7f);

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

            if (Dist(note.X, note.Y, LaneX, cursorY) < 22f)
            {
                note.Hit = true;
                _score += 10 + _combo * 2;
                _combo++;
                _notesHit++;
                _consecutiveMisses = 0;
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

        // Count missed notes before removing
        int missedNow = 0;
        foreach (NoteState n in _notes)
            if (!n.Hit && n.X < -30f) missedNow++;
        _notes.RemoveAll(n => n.X < -30f);
        if (missedNow > 0)
        {
            _missed += missedNow;
            _consecutiveMisses += missedNow;
            _combo = 0;
            if (_consecutiveMisses >= 5)
                _gameOver = true;
        }

        // Refill note queue
        while (_notes.Count < 8)
            SpawnNote(_notes.Count > 0 ? _notes[^1].X + Random(100f, 200f) : Width + 100f);

        DrawHud(cursorY);
    }

    private void DrawHud(float cursorY)
    {
        TextSize(22);
        TextAlign(HorizontalAlignment.Left);
        Fill(Colors.White);
        Text($"Score  {_score}", 14, 18);

        if (_combo > 1)
        {
            float pulse = 1f + Mathf.Sin(FrameCount * 0.2f) * 0.15f;
            TextSize((int)(16 * pulse));
            Color comboColor = Color.FromHsv(Map(_combo, 0, 30, 0.33f, 0f), 1f, 1f);
            Fill(comboColor);
            Text($"x{_combo} combo", 14, 46);
        }

        TextSize(13);
        Fill(new Color(1f, 1f, 1f, 0.5f));
        Text($"Speed  {_speed:F1}", 14, Height - 40f);

        Color missColor = LerpColor(Colors.White, Colors.OrangeRed, Constrain(_consecutiveMisses / 5f, 0f, 1f));
        Fill(missColor);
        Text($"Missed {_consecutiveMisses}/5", 14, Height - 20f);

        Stroke(new Color(0f, 1f, 0.5f, 0.12f));
        StrokeWeight(1f);
        Line(LaneX, cursorY, Width, cursorY);

        TextSize(12);
        TextAlign(HorizontalAlignment.Right);
        Fill(new Color(1f, 1f, 1f, 0.35f));
        Text("R — restart", Width - 10f, Height - 18f);
    }

    private void DrawGameOver()
    {
        TextAlign(HorizontalAlignment.Center);
        Fill(Colors.OrangeRed);
        TextSize(48);
        Text("GAME OVER", Width / 2f, Height / 2f - 40f);

        Fill(Colors.White);
        TextSize(22);
        Text($"Score: {_score}", Width / 2f, Height / 2f + 10f);

        TextSize(16);
        Fill(new Color(1f, 1f, 1f, 0.6f));
        Text("R — restart", Width / 2f, Height / 2f + 50f);
    }
}
