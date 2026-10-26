using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using System.Text;
using Godot;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
#nullable enable

// Compiles a single HotSketch .cs file in-process using Roslyn and loads the
// result into a collectible AssemblyLoadContext so it can be fully unloaded
// before the next compile.
public sealed class CsHotReload : IDisposable
{
    private AssemblyLoadContext? _ctx;

    // Returns (sketch instance, null) on success or (null, error message) on failure.
    public (HotSketch? Sketch, string? Error) CompileAndLoad(string absolutePath)
    {
        string source;
        try { source = File.ReadAllText(absolutePath); }
        catch (Exception e) { return (null, e.Message); }

        // Build the reference set from loaded assemblies, with an explicit fallback for
        // assemblies whose Location is empty (Godot can load the game DLL from a stream).
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (!a.IsDynamic && !string.IsNullOrEmpty(a.Location) && File.Exists(a.Location))
                paths.Add(a.Location);
        }

        // Pin the two assemblies the sketch always needs, with a Godot-path fallback.
        ResolveAssembly(typeof(HotSketch).Assembly, paths);  // p5_Godot4.dll
        ResolveAssembly(typeof(Godot.Node).Assembly, paths); // GodotSharp.dll

        var refs = paths
            .Select(p => (MetadataReference)MetadataReference.CreateFromFile(p))
            .ToArray();

        var parseOpts = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp12);
        var compileOpts = new CSharpCompilationOptions(
            OutputKind.DynamicallyLinkedLibrary,
            nullableContextOptions: NullableContextOptions.Enable,
            optimizationLevel: OptimizationLevel.Debug
        );

        // Unique name so the ALC/debugger can distinguish repeated compilations.
        string asmName = $"HotSketch_{System.IO.Path.GetFileNameWithoutExtension(absolutePath)}_{DateTime.UtcNow.Ticks}";

        // SourceText with explicit encoding avoids CS8055 "cannot emit debug info without encoding".
        var sourceText = SourceText.From(source, Encoding.UTF8);
        var compilation = CSharpCompilation.Create(
            asmName,
            [CSharpSyntaxTree.ParseText(sourceText, parseOpts, absolutePath)],
            refs,
            compileOpts
        );

        using var dllStream = new MemoryStream();
        using var pdbStream = new MemoryStream();
        var emitResult = compilation.Emit(dllStream, pdbStream);

        if (!emitResult.Success)
        {
            var errors = emitResult.Diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(d => d.ToString());
            return (null, string.Join("\n", errors));
        }

        // Unload previous context before creating a new one.
        Unload();

        dllStream.Seek(0, SeekOrigin.Begin);
        pdbStream.Seek(0, SeekOrigin.Begin);

        // SketchLoadContext delegates all dependency resolution (p5_Godot4, GodotSharp, …)
        // back to the default ALC so we reuse the already-loaded versions instead of
        // trying to load them from disk into the collectible context.
        _ctx = new SketchLoadContext(asmName);
        var assembly = _ctx.LoadFromStream(dllStream, pdbStream);

        // GetTypes can still throw ReflectionTypeLoadException for unresolvable types
        // in other assemblies; grab whatever types did load successfully.
        Type[] types;
        try { types = assembly.GetTypes(); }
        catch (System.Reflection.ReflectionTypeLoadException ex)
        { types = ex.Types.Where(t => t != null).ToArray()!; }

        var sketchType = types
            .FirstOrDefault(t => typeof(HotSketch).IsAssignableFrom(t) && !t.IsAbstract);

        if (sketchType == null)
            return (null, "File compiled successfully but contains no concrete HotSketch subclass.");

        try
        {
            var sketch = (HotSketch)Activator.CreateInstance(sketchType)!;
            return (sketch, null);
        }
        catch (Exception e)
        {
            return (null, $"Could not instantiate {sketchType.Name}: {e.Message}");
        }
    }

    // Adds the assembly's path to the set. When Location is empty (Godot loaded the DLL
    // from a stream), searches the project's Godot build-output directories as a fallback.
    private static void ResolveAssembly(System.Reflection.Assembly asm, HashSet<string> paths)
    {
        string loc = asm.Location;
        if (!string.IsNullOrEmpty(loc) && File.Exists(loc))
        {
            paths.Add(loc);
            return;
        }

        string asmName = asm.GetName().Name!;
        string projectDir = ProjectSettings.GlobalizePath("res://");
        foreach (string cfg in new[] { "Debug", "Release", "ExportDebug", "ExportRelease" })
        {
            string candidate = Path.Combine(projectDir, ".godot", "mono", "temp", "bin", cfg, asmName + ".dll");
            if (File.Exists(candidate))
            {
                paths.Add(candidate);
                return;
            }
        }

        GD.PushWarning($"[HotReload] Could not locate assembly on disk: {asmName} (Location='{loc}')");
    }

    // Custom ALC that delegates dependency resolution to the already-running assemblies
    // by object reference. Returning by name via Default.LoadFromAssemblyName can silently
    // load a second copy from disk (when Godot loaded the DLL from a stream), producing a
    // different HotSketch type object → IsAssignableFrom fails even though the sketch
    // clearly extends HotSketch.
    private sealed class SketchLoadContext : AssemblyLoadContext
    {
        // Map assembly name → the live assembly object we want to reuse.
        private readonly Dictionary<string, System.Reflection.Assembly> _pinned;

        public SketchLoadContext(string name) : base(name, isCollectible: true)
        {
            _pinned = new Dictionary<string, System.Reflection.Assembly>(StringComparer.OrdinalIgnoreCase);
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                string? n = a.GetName().Name;
                if (n != null) _pinned.TryAdd(n, a);
            }
        }

        protected override System.Reflection.Assembly? Load(System.Reflection.AssemblyName assemblyName)
        {
            // Return the live in-memory assembly so the sketch's HotSketch reference
            // is the exact same type object as typeof(HotSketch) in the main process.
            if (assemblyName.Name != null && _pinned.TryGetValue(assemblyName.Name, out var live))
                return live;
            return null;
        }
    }

    public void Unload()
    {
        _ctx?.Unload();
        _ctx = null;
    }

    public void Dispose() => Unload();
}
