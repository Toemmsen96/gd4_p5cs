// Smoke Particles based on https://p5js.org/examples/math-and-physics-smoke-particle-system/
using Godot;
using System.Collections.Generic;

public partial class SmokeParticles : GodotP5
{
    private ParticleSystem _particleSystem = null!;

    public override void Setup()
    {
        CreateCanvas(720, 400);
        _particleSystem = new ParticleSystem(0, new Vector2(Width / 2f, Height - 60f));
    }

    public override void DrawSketch()
    {
        if (_particleSystem == null) return;
        Background(new Color(20f / 255f, 20f / 255f, 20f / 255f));

        float dx = Map(MouseX, 0, Width, -0.2f, 0.2f);
        var wind = new Vector2(dx, 0);

        _particleSystem.ApplyForce(wind);
        _particleSystem.Run(this);
        for (int i = 0; i < 2; i++)
            _particleSystem.AddParticle(FrameCount);

        DrawWindVector(wind, new Vector2(Width / 2f, 50f), 500f);
    }

    private void DrawWindVector(Vector2 v, Vector2 loc, float scale)
    {
        Push();
        float arrowSize = 4f;
        Rotate(v.Angle());
        Translate(loc.X, loc.Y);
        Stroke(Colors.White);
        StrokeWeight(3f);
        float length = v.Length() * scale;
        Line(0, 0, length, 0);
        Line(length, 0, length - arrowSize, arrowSize / 2f);
        Line(length, 0, length - arrowSize, -arrowSize / 2f);
        Pop();
    }

    private sealed class ParticleSystem
    {
        private readonly List<Particle> _particles = new();
        private readonly Vector2 _origin;

        public ParticleSystem(int count, Vector2 origin)
        {
            _origin = origin;
            for (int i = 0; i < count; i++)
                _particles.Add(new Particle(_origin, 0));
        }

        public void Run(SmokeParticles p5)
        {
            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                _particles[i].Update();
                _particles[i].Render(p5);
                if (_particles[i].IsDead())
                    _particles.RemoveAt(i);
            }
        }

        public void ApplyForce(Vector2 force)
        {
            foreach (Particle p in _particles)
                p.ApplyForce(force);
        }

        public void AddParticle(int frameCount)
            => _particles.Add(new Particle(_origin, frameCount));
    }

    private sealed class Particle
    {
        private Vector2 _loc;
        private Vector2 _velocity;
        private Vector2 _acceleration;
        private float _lifespan;
        private readonly Color _color;

        public Particle(Vector2 pos, int frameCount)
        {
            _loc = pos;
            _velocity = new Vector2(Gaussian() * 0.3f, Gaussian() * 0.3f - 1.0f);
            _acceleration = Vector2.Zero;
            _lifespan = 100f;
            _color = Color.FromHsv((frameCount % 256) / 255f, 1f, 1f);
        }

        private static float Gaussian()
        {
            // Box-Muller transform
            double u1 = GD.Randf();
            double u2 = GD.Randf();
            while (u1 == 0.0) u1 = GD.Randf();
            return (float)(System.Math.Sqrt(-2.0 * System.Math.Log(u1)) * System.Math.Cos(System.Math.PI * 2.0 * u2));
        }

        public void Render(SmokeParticles p5)
        {
            var c = new Color(_color.R, _color.G, _color.B, _lifespan / 100f);
            p5.Push();
            p5.NoStroke();
            p5.Fill(c);
            p5.Circle(_loc.X, _loc.Y, 20);
            p5.Pop();
        }

        public void ApplyForce(Vector2 f) => _acceleration += f;

        public bool IsDead() => _lifespan <= 0f;

        public void Update()
        {
            _velocity += _acceleration;
            _loc += _velocity;
            _lifespan -= 2.5f;
            _acceleration = Vector2.Zero;
        }
    }
}
