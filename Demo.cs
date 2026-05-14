using Godot;
using System;
using System.IO;

public partial class Demo : SubViewportContainer
{
	[Export]
	public Script Sketch { get; set; } = null!;

	private SubViewport sketchViewport = null!;
	private ColorRect viewportBg = null!;
	private Node2D canvas = null!;
	private CanvasLayer canvasLayer = null!;
	private Label labelWarningMsg = null!;
	private TextureButton btMenu = null!;
	private Panel panel = null!;
	private Label lbFps = null!;
	private ColorPickerButton btCurrentColor = null!;
	private FileDialog fileDialog = null!;
	private FileDialog sketchFileDialog = null!;
	private Image? imgSave;
	private bool sketchIsGd;

	private string currentSketchPath = string.Empty;
	private FileSystemWatcher? fileWatcher;
	private bool reloadPending = false;

	public override void _Ready()
	{
		sketchViewport = GetNode<SubViewport>("SketchViewport");
		viewportBg = GetNode<ColorRect>("SketchViewport/ViewportBg");
		canvas = GetNode<Node2D>("SketchViewport/Canvas");
		canvasLayer = GetNode<CanvasLayer>("CanvasLayer");
		labelWarningMsg = GetNode<Label>("CanvasLayer/LabelWarningMsg");
		btMenu = GetNode<TextureButton>("CanvasLayer/BtMenu");
		panel = GetNode<Panel>("CanvasLayer/Panel");
		lbFps = GetNode<Label>("CanvasLayer/Panel/BoxButton/LabelFps");
		btCurrentColor = GetNode<ColorPickerButton>("CanvasLayer/Panel/BoxButton/BtCurrentColor");
		fileDialog = GetNode<FileDialog>("FileDialog");

		sketchFileDialog = new FileDialog
		{
			FileMode = FileDialog.FileModeEnum.OpenFile,
			Access = FileDialog.AccessEnum.Filesystem,
			Filters = ["*.cs,*.gd ; C# and GDScript sketches"]
		};
		sketchFileDialog.FileSelected += OnSketchFileSelected;
		AddChild(sketchFileDialog);

		if (Sketch == null)
		{
			labelWarningMsg.Visible = true;
		}
		else
		{
			currentSketchPath = Sketch.ResourcePath;
			LoadSketch();
			SetupHotReload(ProjectSettings.GlobalizePath(currentSketchPath));
			btMenu.Show();
		}

		sketchViewport.HandleInputLocally = true;
	}

	public override void _Process(double delta)
	{
		if (reloadPending)
		{
			reloadPending = false;
			ReloadGdScript();
		}
	}

	private void StartFileWatcher(string absolutePath)
	{
		fileWatcher?.Dispose();
		if (!File.Exists(absolutePath))
			return;

		fileWatcher = new FileSystemWatcher(Path.GetDirectoryName(absolutePath)!, Path.GetFileName(absolutePath))
		{
			NotifyFilter = NotifyFilters.LastWrite,
			EnableRaisingEvents = true
		};
		fileWatcher.Changed += (_, _) => reloadPending = true;
	}

	private void WatchCanvasScriptChanged()
	{
		// Godot fires NOTIFICATION_SCRIPT_CHANGED on the canvas node after a C# assembly hot-reload.
		// We subscribe once; it gets cleared automatically when SetScript replaces the node's script.
		if (!canvas.IsConnected(Node.SignalName.ScriptChanged, new Callable(this, nameof(OnCanvasScriptChanged))))
			canvas.Connect(Node.SignalName.ScriptChanged, new Callable(this, nameof(OnCanvasScriptChanged)), (uint)ConnectFlags.OneShot);
	}

	private void OnCanvasScriptChanged()
	{
		if (string.IsNullOrEmpty(currentSketchPath))
			return;
		var script = ResourceLoader.Load<Script>(currentSketchPath, cacheMode: ResourceLoader.CacheMode.IgnoreDeep);
		if (script == null)
			return;
		Sketch = script;
		sketchViewport.Set("size", Vector2I.Zero);
		LoadSketch();
		// Re-subscribe for the next save.
		WatchCanvasScriptChanged();
	}

	private void OnSketchFileSelected(string absolutePath)
	{
		string resPath = ProjectSettings.LocalizePath(absolutePath);
		if (string.IsNullOrEmpty(resPath))
			resPath = absolutePath;

		bool isCSharp = absolutePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);

		// For .cs: IgnoreDeep returns the already-loaded in-memory script without re-parsing from disk
		// (disk is source code, not bytecode — only Godot's build system can produce a new assembly).
		// For .gd: Replace forces a re-read and re-parse of the script file.
		var cacheMode = isCSharp ? ResourceLoader.CacheMode.IgnoreDeep : ResourceLoader.CacheMode.Replace;
		var script = ResourceLoader.Load<Script>(resPath, cacheMode: cacheMode);
		if (script == null)
		{
			GD.PushError($"Could not load sketch: {absolutePath}");
			return;
		}

		Sketch = script;
		currentSketchPath = resPath;

		sketchViewport.Set("size", Vector2I.Zero);
		LoadSketch();
		SetupHotReload(absolutePath);
		labelWarningMsg.Visible = false;
		btMenu.Show();
	}

	private void SetupHotReload(string absolutePath)
	{
		fileWatcher?.Dispose();
		fileWatcher = null;

		if (sketchIsGd)
			StartFileWatcher(absolutePath);
		else
			WatchCanvasScriptChanged();
	}

	private void ReloadGdScript()
	{
		if (string.IsNullOrEmpty(currentSketchPath))
			return;

		var script = ResourceLoader.Load<Script>(currentSketchPath, cacheMode: ResourceLoader.CacheMode.Replace);
		if (script == null)
		{
			GD.PushError($"Could not reload sketch: {currentSketchPath}");
			return;
		}

		Sketch = script;
		sketchViewport.Set("size", Vector2I.Zero);
		LoadSketch();
	}

	private void _OnBtLoadSketchPressed()
	{
		sketchFileDialog.CurrentDir = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
		sketchFileDialog.Show();
	}

	private void _on_bt_load_sketch_pressed()
	{
		_OnBtLoadSketchPressed();
	}

	private void DisconnectSketchSignals()
	{
		var bgCallable = new Callable(this, nameof(SetBackgroundColor));
		var vpCallable = new Callable(this, nameof(SetViewportSize));
		var colorCallable = new Callable(this, nameof(SetCurrentColor));

		if (sketchIsGd)
		{
			if (canvas.IsConnected("set_background_color", bgCallable)) canvas.Disconnect("set_background_color", bgCallable);
			if (canvas.IsConnected("set_viewport_size", vpCallable)) canvas.Disconnect("set_viewport_size", vpCallable);
			if (canvas.IsConnected("set_current_color", colorCallable)) canvas.Disconnect("set_current_color", colorCallable);
		}
		else if (canvas is GodotP5 p5Canvas)
		{
			if (p5Canvas.IsConnected(GodotP5.SignalName.SetBackgroundColor, bgCallable)) p5Canvas.Disconnect(GodotP5.SignalName.SetBackgroundColor, bgCallable);
			if (p5Canvas.IsConnected(GodotP5.SignalName.SetViewportSize, vpCallable)) p5Canvas.Disconnect(GodotP5.SignalName.SetViewportSize, vpCallable);
			if (p5Canvas.IsConnected(GodotP5.SignalName.SetCurrentColor, colorCallable)) p5Canvas.Disconnect(GodotP5.SignalName.SetCurrentColor, colorCallable);
		}
	}

	private void LoadSketch()
	{
		DisconnectSketchSignals();
		canvas.SetScript(Sketch);

		canvas = GetNode<Node2D>("SketchViewport/Canvas");
		sketchIsGd = Sketch.ResourcePath.EndsWith(".gd", StringComparison.OrdinalIgnoreCase);

		if (sketchIsGd)
		{
			canvas.Connect("set_background_color", new Callable(this, nameof(SetBackgroundColor)));
			canvas.Connect("set_viewport_size", new Callable(this, nameof(SetViewportSize)));
			canvas.Connect("set_current_color", new Callable(this, nameof(SetCurrentColor)));
			canvas.Set("sub_viewport", sketchViewport);

			if (canvas.HasMethod("_init_from_main_scene"))
			{
				canvas.Call("_init_from_main_scene");
			}
			else
			{
				GD.PushError("GDScript sketch must be compatible with godotp5_class.gd and implement _init_from_main_scene().");
			}
			return;
		}

		if (canvas is not GodotP5 p5Canvas)
		{
			GD.PushError("Sketch script must inherit from GodotP5.");
			return;
		}

		p5Canvas.Connect(GodotP5.SignalName.SetBackgroundColor, new Callable(this, nameof(SetBackgroundColor)));
		p5Canvas.Connect(GodotP5.SignalName.SetViewportSize, new Callable(this, nameof(SetViewportSize)));
		p5Canvas.Connect(GodotP5.SignalName.SetCurrentColor, new Callable(this, nameof(SetCurrentColor)));

		p5Canvas.SubViewport = sketchViewport;
		p5Canvas.InitFromMainScene();
	}

	private void SetBackgroundColor(Color color)
	{
		viewportBg.Color = color;
	}

	private void SetViewportSize(Vector2I viewportSize)
	{
		sketchViewport.Set("size", viewportSize);
		sketchViewport.Set("size_2d_override", viewportSize);
		viewportBg.Set("size", new Vector2(viewportSize.X, viewportSize.Y));
		DisplayServer.WindowSetSize(viewportSize);
	}

	private void SetCurrentColor(Color color)
	{
		btCurrentColor.Color = color;
	}

	private void OnWindowSizeChanged()
	{
		GD.Print($"window size changed : {GetViewportRect().Size}");
	}

	private void _OnBtMenuPressed()
	{
		panel.Show();
		btMenu.Hide();
	}

	private void _on_bt_menu_pressed()
	{
		_OnBtMenuPressed();
	}

	private void _OnBtHidePressed()
	{
		panel.Hide();
		btMenu.Show();
	}

	private void _on_bt_hide_pressed()
	{
		_OnBtHidePressed();
	}

	private void _OnBtPausePressed()
	{
		GD.Print("on Button pause pressed ..");
		if (sketchIsGd)
		{
			if (canvas.HasMethod("pause"))
			{
				canvas.Call("pause");
			}
			return;
		}

		if (canvas is GodotP5 p5Canvas)
		{
			p5Canvas.Pause();
		}
	}

	private void _on_bt_pause_pressed()
	{
		_OnBtPausePressed();
	}

	private void _OnBtRestartPressed()
	{
		sketchViewport.Set("size", Vector2I.Zero);
		if (sketchIsGd)
		{
			if (canvas.HasMethod("restart"))
			{
				canvas.Call("restart");
			}
			return;
		}

		if (canvas is GodotP5 p5Canvas)
		{
			p5Canvas.Restart();
		}
	}

	private void _on_bt_restart_pressed()
	{
		_OnBtRestartPressed();
	}

	private void _OnColorBtCurrentColorChanged(Color color)
	{
		if (sketchIsGd)
		{
			canvas.Set("_current_color", color);
			return;
		}

		if (canvas is GodotP5 p5Canvas)
		{
			p5Canvas.CurrentColor = color;
		}
	}

	private void _on_color_BtCurrentColor_changed(Color color)
	{
		_OnColorBtCurrentColorChanged(color);
	}

	private void _OnBtSaveImagePressed()
	{
		imgSave = sketchViewport.GetTexture().GetImage();
		fileDialog.Show();
	}

	private void _on_bt_save_image_pressed()
	{
		_OnBtSaveImagePressed();
	}

	private void _OnFileDialogFileSelected(string path)
	{
		imgSave?.SavePng(path);
	}

	private void _on_file_dialog_file_selected(string path)
	{
		_OnFileDialogFileSelected(path);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (Sketch == null)
		{
			return;
		}

		canvas.Call("_unhandled_input", @event);
	}

	public override void _ExitTree()
	{
		fileWatcher?.Dispose();
	}
}
