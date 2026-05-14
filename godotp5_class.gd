extends Node2D
class_name GodotP5

###	Godot Doc : class CanvasItem
#	https://docs.godotengine.org/en/stable/classes/class_canvasitem.html
###

###
# 	Link : https://p5js.org
# 	GodotP5 is based on p5js's original goals, but I didn't tie to p5js's syntax.
###

signal set_background_color
signal set_viewport_size
signal set_current_color

### Class variable
enum VIEWPORT_MODE {ALWAYS, NEVER, ONCE}
var _is_loaded : bool = false
var sub_viewport: SubViewport
var _current_bg_color: Color = Color.BLACK
var _current_color: Color = Color.WHITE

### VAR P5JS
var width : float = 100
var height : float = 100

var displayWidth: float:
	get: return DisplayServer.screen_get_size().x
var displayHeight: float:
	get: return DisplayServer.screen_get_size().y
var windowWidth: float:
	get: return DisplayServer.window_get_size().x
var windowHeight: float:
	get: return DisplayServer.window_get_size().y

var frameCount: int = 0
var deltaTime: float

var mouseX : int = 0
var mouseY : int = 0
var pmouseX : int = 0
var pmouseY : int = 0
var movedX : int = 0
var movedY : int = 0
var mouseIsPressed : bool = false
var mouseButton
var keyIsPressed : bool = false
var key : String = ""
var keyCode : int = 0

const TWO_PI = TAU
const HALF_PI = PI/2.0
const QUARTER_PI = PI/4.0
const E = 2.718281828459045


### P5JS FUNC / VAR
var _fill_color: Color = Color.WHITE
var _stroke_color: Color = Color.GRAY
var _stroke_weight: float = 1.0
var _no_stroke: bool = false
var _no_fill: bool = false
var _is_looping: bool = true
var _point_count: int = 32
var _antialiased: bool = true

var _text_size: int = 14
var _text_font: Font
var _text_h_align: HorizontalAlignment = HORIZONTAL_ALIGNMENT_LEFT
var _text_v_align: VerticalAlignment = VERTICAL_ALIGNMENT_TOP

# beginShape/endShape vertex buffer
var _shape_array: Array[Vector2] = []

# Transform stack (used inside _draw via draw_set_transform_matrix)
var _current_transform: Transform2D = Transform2D.IDENTITY
var _transform_stack: Array[Transform2D] = []

# Style stack for push/pop
var _style_stack: Array[Dictionary] = []

# Noise
var _noise: FastNoiseLite

# Legacy node-transform (kept for backward compat with m_translate / m_rotate)
var _matrix_transform := Transform2D()


func _init_from_main_scene():
	setup()
	_is_loaded = true
	if _is_looping == false:
		queue_redraw()
	else:
		loop()

func set_title(title: String) -> void:
	DisplayServer.window_set_title(title)

func _input(_event: InputEvent) -> void:
	if Input.is_action_just_pressed("ui_cancel"):
		get_tree().quit()

func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventKey:
		if event.is_pressed():
			keyIsPressed = true
			keyCode = event.physical_keycode
			key = event.as_text_physical_keycode()
			keyPressed()
		else:
			keyIsPressed = false
			key = ""
			keyCode = 0
			keyReleased()

	if event is InputEventMouseButton:
		if event.is_pressed():
			mouseIsPressed = true
			match event.button_index:
				1: mouseButton = "LEFT"
				2: mouseButton = "CENTER"
				3: mouseButton = "RIGHT"
			mouseClicked()
		else:
			mouseIsPressed = false
			mouseReleased()

	if event is InputEventMouseMotion:
		if mouseIsPressed:
			mouseDragged()
		else:
			mouseMoved()

func _on_viewport_size_changed() -> void:
	width  = get_viewport_rect().size.x
	height = get_viewport_rect().size.y

func set_viewport_mode(mode: VIEWPORT_MODE) -> void:
	match mode:
		VIEWPORT_MODE.ALWAYS:
			sub_viewport.render_target_clear_mode = SubViewport.CLEAR_MODE_ALWAYS
		VIEWPORT_MODE.NEVER:
			sub_viewport.render_target_clear_mode = SubViewport.CLEAR_MODE_NEVER
		VIEWPORT_MODE.ONCE:
			sub_viewport.render_target_clear_mode = SubViewport.CLEAR_MODE_ONCE

func _process(delta: float) -> void:
	if not _is_loaded:
		return

	if _is_looping:
		queue_redraw()
		frameCount += 1
		deltaTime = delta

	if mouseIsPressed:
		mousePressed()

	pmouseX = mouseX
	pmouseY = mouseY
	var mouse_pos = get_local_mouse_position()
	mouseX = int(mouse_pos.x)
	mouseY = int(mouse_pos.y)
	movedX = mouseX - pmouseX
	movedY = mouseY - pmouseY

	# Reset draw-transform tracking each frame (actual draw transform resets in Godot at start of _draw)
	_current_transform = Transform2D.IDENTITY
	_transform_stack.clear()
	_style_stack.clear()

	# Reset legacy node-transform
	_matrix_transform = Transform2D.IDENTITY
	self.transform = _matrix_transform


### Structure
func setup() -> void: pass
func _draw() -> void: pass

func noLoop() -> void:
	_is_looping = false
	set_process(false)

func loop() -> void:
	_is_looping = true
	set_process(true)

func isLooping() -> bool:
	return _is_looping

func pause() -> void:
	set_process(not is_processing())

func restart() -> void:
	setup()


### Rendering
func createCanvas(w: int, h: int) -> void:
	width = w
	height = h
	set_viewport_size.emit(Vector2i(w, h))

func resizeCanvas(): pass
func noCanvas(): pass


### Settings
func background(color: Color, alpha: float = -1.0) -> void:
	if alpha >= 0.0:
		color = Color(color, alpha)
	_current_bg_color = color
	set_background_color.emit(color)
	RenderingServer.set_default_clear_color(color)

func clear() -> void:
	draw_rect(Rect2(0, 0, width, height), _current_bg_color, true)

func noStroke() -> void:
	_no_stroke = true

func noFill() -> void:
	_no_fill = true

func fill(color: Color) -> void:
	_fill_color = color
	_no_fill = false

func stroke(color: Color) -> void:
	_stroke_color = color
	_no_stroke = false

func set_color(color: Color) -> void:
	_current_color = color
	_stroke_color = color
	_fill_color = color
	_no_fill = false
	_no_stroke = false

func strokeWeight(w: float) -> void:
	_stroke_weight = w

func setPointCount(point_count: int) -> void:
	_point_count = point_count

func smooth() -> void:
	_antialiased = true

func noSmooth() -> void:
	_antialiased = false


### Environment
func frameRate(fps: int) -> void:
	Engine.max_fps = fps

func getTargetFrameRate() -> int:
	return Engine.max_fps

func cursor(): pass
func noCursor(): pass
func windowResized(): pass
func fullscreen(): pass


### Transform (draw-space, call inside _draw)
func push() -> void:
	_transform_stack.push_back(_current_transform)
	_style_stack.push_back({
		"fill_color":    _fill_color,
		"stroke_color":  _stroke_color,
		"stroke_weight": _stroke_weight,
		"no_fill":       _no_fill,
		"no_stroke":     _no_stroke,
		"antialiased":   _antialiased,
		"point_count":   _point_count,
		"text_size":     _text_size,
		"text_h_align":  _text_h_align,
	})

func pop() -> void:
	if _transform_stack.size() > 0:
		_current_transform = _transform_stack.pop_back()
		draw_set_transform_matrix(_current_transform)
	if _style_stack.size() > 0:
		var s = _style_stack.pop_back()
		_fill_color    = s["fill_color"]
		_stroke_color  = s["stroke_color"]
		_stroke_weight = s["stroke_weight"]
		_no_fill       = s["no_fill"]
		_no_stroke     = s["no_stroke"]
		_antialiased   = s["antialiased"]
		_point_count   = s["point_count"]
		_text_size     = s["text_size"]
		_text_h_align  = s["text_h_align"]

func draw_translate(x: float, y: float) -> void:
	_current_transform = _current_transform.translated(Vector2(x, y))
	draw_set_transform_matrix(_current_transform)

func draw_rotate(angle: float) -> void:
	_current_transform = _current_transform.rotated(angle)
	draw_set_transform_matrix(_current_transform)

func draw_scale(x: float, y: float) -> void:
	_current_transform = _current_transform.scaled(Vector2(x, y))
	draw_set_transform_matrix(_current_transform)

func resetMatrix() -> void:
	_current_transform = Transform2D.IDENTITY
	draw_set_transform_matrix(_current_transform)

func applyMatrix(): pass

# Legacy: modify node transform directly (for sketches using m_translate/m_rotate)
func m_rotate(angle: float) -> void:
	_matrix_transform.x.x =  cos(angle)
	_matrix_transform.y.y =  cos(angle)
	_matrix_transform.x.y =  sin(angle)
	_matrix_transform.y.x = -sin(angle)
	self.transform = _matrix_transform

func m_translate(x: float, y: float) -> void:
	_matrix_transform.origin = Vector2(x, y)
	self.transform = _matrix_transform

func shearX(): pass
func shearY(): pass


### Shape
func circle(x: float, y: float, radius: float, point_count: int = 32) -> void:
	if not _no_fill:
		draw_circle(Vector2(x, y), radius, _fill_color)
	if not _no_stroke:
		draw_arc(Vector2(x, y), radius, 0, TAU, point_count, _stroke_color, _stroke_weight, _antialiased)

func ellipse(x: float, y: float, w: float, h: float, point_count: int = 32) -> void:
	var rx := w * 0.5
	var ry := h * 0.5
	var pts := PackedVector2Array()
	for i in range(point_count):
		var a := TAU * i / float(point_count)
		pts.append(Vector2(x + rx * cos(a), y + ry * sin(a)))
	if not _no_fill:
		draw_polygon(pts, PackedColorArray([_fill_color]))
	if not _no_stroke:
		pts.append(pts[0])
		draw_polyline(pts, _stroke_color, _stroke_weight, _antialiased)

func arc(x: float, y: float, w: float, h: float, start: float, stop: float, point_count: int = 32) -> void:
	var rx := w * 0.5
	var ry := h * 0.5
	if abs(rx - ry) < 0.01:
		if not _no_stroke:
			draw_arc(Vector2(x, y), rx, start, stop, point_count, _stroke_color, _stroke_weight, _antialiased)
		return
	var pts := PackedVector2Array()
	for i in range(point_count + 1):
		var a := start + (stop - start) * i / float(point_count)
		pts.append(Vector2(x + rx * cos(a), y + ry * sin(a)))
	if not _no_stroke:
		draw_polyline(pts, _stroke_color, _stroke_weight, _antialiased)

func point(x: float, y: float) -> void:
	if not _no_stroke:
		draw_circle(Vector2(x, y), _stroke_weight * 0.5, _stroke_color)

func line(x0: float, y0: float, x1: float, y1: float) -> void:
	if _no_stroke:
		return
	draw_line(Vector2(x0, y0), Vector2(x1, y1), _stroke_color, _stroke_weight, _antialiased)

func triangle(x1: float, y1: float, x2: float, y2: float, x3: float, y3: float) -> void:
	var pts := PackedVector2Array([Vector2(x1, y1), Vector2(x2, y2), Vector2(x3, y3)])
	if not _no_fill:
		draw_polygon(pts, PackedColorArray([_fill_color]))
	if not _no_stroke:
		pts.append(Vector2(x1, y1))
		draw_polyline(pts, _stroke_color, _stroke_weight, _antialiased)

func quad(x1: float, y1: float, x2: float, y2: float, x3: float, y3: float, x4: float, y4: float) -> void:
	var pts := PackedVector2Array([Vector2(x1, y1), Vector2(x2, y2), Vector2(x3, y3), Vector2(x4, y4)])
	if not _no_fill:
		draw_polygon(pts, PackedColorArray([_fill_color]))
	if not _no_stroke:
		pts.append(Vector2(x1, y1))
		draw_polyline(pts, _stroke_color, _stroke_weight, _antialiased)

func rect(x: float, y: float, w: float, h: float) -> void:
	quad(x, y, x + w, y, x + w, y + h, x, y + h)

func square(x: float, y: float, s: float) -> void:
	rect(x, y, s, s)

func beginShape(_shape_mode = -1) -> void:
	_shape_array.clear()

func vertex(x: float, y: float) -> void:
	_shape_array.append(Vector2(x, y))

func endShape(close: bool = false) -> void:
	if _shape_array.size() < 2:
		return
	var pca := PackedVector2Array(_shape_array)
	if not _no_fill:
		draw_polygon(pca, PackedColorArray([_fill_color]))
	if not _no_stroke:
		if close:
			pca.append(pca[0])
		draw_polyline(pca, _stroke_color, _stroke_weight, _antialiased)

func bezier(x1: float, y1: float, cx1: float, cy1: float, cx2: float, cy2: float, x2: float, y2: float, steps: int = 32) -> void:
	if _no_stroke:
		return
	var p0 := Vector2(x1, y1)
	var p1 := Vector2(cx1, cy1)
	var p2 := Vector2(cx2, cy2)
	var p3 := Vector2(x2, y2)
	var pts := PackedVector2Array()
	for i in range(steps + 1):
		var t := float(i) / float(steps)
		var u := 1.0 - t
		pts.append(u*u*u*p0 + 3.0*u*u*t*p1 + 3.0*u*t*t*p2 + t*t*t*p3)
	draw_polyline(pts, _stroke_color, _stroke_weight, _antialiased)

func curve(x1: float, y1: float, x2: float, y2: float, x3: float, y3: float, x4: float, y4: float, steps: int = 32) -> void:
	# Catmull-Rom spline through p1..p4 with p0/p5 as phantom control points
	if _no_stroke:
		return
	var p0 := Vector2(x1, y1)
	var p1 := Vector2(x2, y2)
	var p2 := Vector2(x3, y3)
	var p3 := Vector2(x4, y4)
	var pts := PackedVector2Array()
	for i in range(steps + 1):
		var t := float(i) / float(steps)
		var t2 := t * t
		var t3 := t2 * t
		var v : Vector2 = 0.5 * (
			(-p0 + 3.0*p1 - 3.0*p2 + p3) * t3
			+ (2.0*p0 - 5.0*p1 + 4.0*p2 - p3) * t2
			+ (-p0 + p2) * t
			+ 2.0 * p1
		)
		pts.append(v)
	draw_polyline(pts, _stroke_color, _stroke_weight, _antialiased)

func ellipseMode(): pass
func rectMode(): pass
func strokeCap(): pass
func strokeJoin(): pass
func bezierDetail(): pass
func bezierPoint(): pass
func bezierTangent(): pass
func curveDetail(): pass
func curveTightness(): pass
func curvePoint(): pass
func curveTangent(): pass


### Text
func textSize(size: int) -> void:
	_text_size = size

func textFont(font: Font) -> void:
	_text_font = font

func textAlign(h: HorizontalAlignment, v: VerticalAlignment = VERTICAL_ALIGNMENT_TOP) -> void:
	_text_h_align = h
	_text_v_align = v

func text(str: String, x: float, y: float) -> void:
	var font := _text_font if _text_font else ThemeDB.fallback_font
	draw_string(font, Vector2(x, y), str, _text_h_align, -1, _text_size, _fill_color)


### Image
func loadImage(path: String) -> Texture2D:
	return ResourceLoader.load(path) as Texture2D

func image(texture: Texture2D, x: float, y: float, w: float = -1, h: float = -1) -> void:
	if w < 0 or h < 0:
		draw_texture(texture, Vector2(x, y))
	else:
		draw_texture_rect(texture, Rect2(x, y, w, h), false)

func createImage(): pass
func saveCanvas(): pass
func saveFrames(): pass


### Math helpers (p5.js API names)
func map(value: float, start1: float, stop1: float, start2: float, stop2: float) -> float:
	return remap(value, start1, stop1, start2, stop2)

func constrain(n: float, lo: float, hi: float) -> float:
	return clamp(n, lo, hi)

func dist(x1: float, y1: float, x2: float, y2: float) -> float:
	return Vector2(x1, y1).distance_to(Vector2(x2, y2))

func dist3(x1: float, y1: float, z1: float, x2: float, y2: float, z2: float) -> float:
	return Vector3(x1, y1, z1).distance_to(Vector3(x2, y2, z2))

func mag(a: float, b: float) -> float:
	return Vector2(a, b).length()

func norm(value: float, start: float, stop: float) -> float:
	return (value - start) / (stop - start)

func sq(n: float) -> float:
	return n * n

func degrees(r: float) -> float:
	return rad_to_deg(r)

func radians(d: float) -> float:
	return deg_to_rad(d)

func random_val(max_val: float) -> float:
	return randf() * max_val

func random_range(min_val: float, max_val: float) -> float:
	return randf_range(min_val, max_val)

func random_int(min_val: int, max_val: int) -> int:
	return randi_range(min_val, max_val)

func randomGaussian(mean: float = 0.0, sd: float = 1.0) -> float:
	# Box-Muller transform
	var u1 := randf()
	var u2 := randf()
	while u1 == 0.0:
		u1 = randf()
	var z := sqrt(-2.0 * log(u1)) * cos(TAU * u2)
	return mean + sd * z

func noise_val(x: float) -> float:
	if not _noise:
		_noise = FastNoiseLite.new()
	return (_noise.get_noise_1d(x) + 1.0) * 0.5

func noise_2d(x: float, y: float) -> float:
	if not _noise:
		_noise = FastNoiseLite.new()
	return (_noise.get_noise_2d(x, y) + 1.0) * 0.5

func noise_3d(x: float, y: float, z: float) -> float:
	if not _noise:
		_noise = FastNoiseLite.new()
	return (_noise.get_noise_3d(x, y, z) + 1.0) * 0.5

func noiseSeed(seed: int) -> void:
	if not _noise:
		_noise = FastNoiseLite.new()
	_noise.seed = seed


### Color helpers
func lerpColor(c1: Color, c2: Color, amt: float) -> Color:
	return c1.lerp(c2, amt)

func red(c: Color) -> float:   return c.r
func green(c: Color) -> float: return c.g
func blue(c: Color) -> float:  return c.b
func alpha(c: Color) -> float: return c.a

func colorFromHSB(h: float, s: float, b: float, a: float = 1.0) -> Color:
	return Color.from_hsv(h, s, b, a)


### Vector
func createVector(x: float = 0.0, y: float = 0.0) -> Vector2:
	return Vector2(x, y)


### Time & Date
func hour() -> int:
	return Time.get_time_dict_from_system()["hour"]

func minute() -> int:
	return Time.get_time_dict_from_system()["minute"]

func second() -> int:
	return Time.get_time_dict_from_system()["second"]

func day() -> int:
	return Time.get_date_dict_from_system()["day"]

func month() -> int:
	return Time.get_date_dict_from_system()["month"]

func year() -> int:
	return Time.get_date_dict_from_system()["year"]

func millis() -> int:
	return Time.get_ticks_msec()


### Mouse events (override in sketch)
func mousePressed(): pass
func mouseReleased(): pass
func mouseClicked(): pass
func mouseMoved(): pass
func mouseDragged(): pass
func mouseWheel(): pass
func doubleClicked(): pass
func requestPointerLock(): pass
func exitPointerLock(): pass


### Keyboard events (override in sketch)
func keyPressed(): pass
func keyReleased(): pass
func keyTyped(): pass
func keyIsDown(k): pass
