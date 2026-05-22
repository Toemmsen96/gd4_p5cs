extends GodotP5
# Smoke Particles based on https://p5js.org/examples/math-and-physics-smoke-particle-system/
var particle_system: ParticleSystemP5

func setup() -> void:
	createCanvas(720, 400)
	particle_system = ParticleSystemP5.new(0, createVector(width / 2.0, height - 60.0))

func _draw() -> void:
	if particle_system == null:
		return
	background(Color(20.0 / 255, 20.0 / 255, 20.0 / 255))

	var dx = map(mouseX, 0, width, -0.2, 0.2)
	var wind = createVector(dx, 0)

	particle_system.applyForce(wind)
	particle_system.run(self)
	for i in range(2):
		particle_system.addParticle(frameCount)

	_drawVector(wind, createVector(width / 2.0, 50.0), 500.0)

func _drawVector(v: Vector2, loc: Vector2, scale: float) -> void:
	push()
	var arrow_size := 4.0
	draw_translate(loc.x, loc.y)
	stroke(Color.WHITE)
	strokeWeight(3)
	draw_rotate(v.angle())
	var length := v.length() * scale
	line(0, 0, length, 0)
	line(length, 0, length - arrow_size, arrow_size / 2.0)
	line(length, 0, length - arrow_size, -arrow_size / 2.0)
	pop()


class ParticleSystemP5:
	var particles: Array = []
	var origin: Vector2

	func _init(particle_count: int, orig: Vector2) -> void:
		origin = orig
		for i in range(particle_count):
			particles.append(ParticleP5.new(origin, 0))

	func run(p5: GodotP5) -> void:
		for i in range(particles.size() - 1, -1, -1):
			var particle: ParticleP5 = particles[i]
			particle.update()
			particle.render(p5)
			if particle.is_dead():
				particles.remove_at(i)

	func applyForce(dir: Vector2) -> void:
		for particle in particles:
			particle.applyForce(dir)

	func addParticle(frame_count: int) -> void:
		particles.append(ParticleP5.new(origin, frame_count))


class ParticleP5:
	var loc: Vector2
	var velocity: Vector2
	var acceleration: Vector2
	var lifespan: float
	var color: Color

	func _init(pos: Vector2, frame_count: int) -> void:
		loc = pos
		velocity = Vector2(_gaussian() * 0.3, _gaussian() * 0.3 - 1.0)
		acceleration = Vector2.ZERO
		lifespan = 100.0
		color = Color.from_hsv((frame_count % 256) / 255.0, 1.0, 1.0)

	func _gaussian() -> float:
		var u1 := randf()
		var u2 := randf()
		while u1 == 0.0:
			u1 = randf()
		return sqrt(-2.0 * log(u1)) * cos(TAU * u2)

	func render(p5: GodotP5) -> void:
		var c := Color(color.r, color.g, color.b, lifespan / 100.0)
		p5.push()
		p5.noStroke()
		p5.fill(c)
		p5.circle(loc.x, loc.y, 20)
		p5.pop()

	func applyForce(f: Vector2) -> void:
		acceleration += f

	func is_dead() -> bool:
		return lifespan <= 0.0

	func update() -> void:
		velocity += acceleration
		loc += velocity
		lifespan -= 2.5
		acceleration = Vector2.ZERO
