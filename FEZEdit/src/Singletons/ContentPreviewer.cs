using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FEZEdit.Core;
using FEZRepacker.Core.Definitions.Game.ArtObject;
using FEZRepacker.Core.Definitions.Game.TrileSet;
using Godot;
using Serilog;
using FileAccess = Godot.FileAccess;

namespace FEZEdit.Singletons;

using AnimatedTexture = FEZRepacker.Core.Definitions.Game.Graphics.AnimatedTexture;
using Texture2D = FEZRepacker.Core.Definitions.Game.XNA.Texture2D;

public static class ContentPreviewer
{
    private static readonly ILogger Logger = LoggerFactory.Create(nameof(ContentPreviewer));
    
    private const string PreviewsFolder = "user://previews/";

    private const float CameraRotationX = -Mathf.Pi / 8f;

    private const float CameraRotationY = -Mathf.Pi / 4f;

    public delegate void ReceivePreviewHandler(string path, ImageTexture preview, params object[] data);

    public static int PreviewSize { get; set; } = 64;

    private static bool s_isGenerating;
    
    private static Action s_finishCallback;

    private static readonly Queue<PreviewGenerationState> MeshesToPreview = [];

    static ContentPreviewer()
    {
        RenderingServer.Singleton.FramePostDraw += () =>
        {
            if (!s_isGenerating && MeshesToPreview.Count > 0)
            {
                var sceneTree = (SceneTree)Engine.GetMainLoop();
                GeneratePreview(sceneTree);
            }
        };
    }

    public static void GenerateContentPreviews()
    {
        var paths = ContentLoader.GetFiles("trile sets").ToList();
        paths.AddRange(ContentLoader.GetFiles("art objects"));
        paths.AddRange(ContentLoader.GetFiles("background planes"));
        paths.AddRange(ContentLoader.GetFiles("character animations"));

        var progress = new ProgressValue(0, 0, paths.Count, 1);
        EventBus.Progress(progress, "Generating previews...");
        
        foreach (var path in paths)
        {
            QueueContentPreview(path, (name, _, _) =>
            {
                progress.Next();
                EventBus.Progress(progress, "Preview generated: {0}", name);
            });
        }
        
        EventBus.Progress(ProgressValue.Complete);
    }

    public static void QueueContentPreview(string path, ReceivePreviewHandler handler, params object[] data)
    {
        try
        {
            var content = ContentLoader.Load<object>(path);
            switch (content)
            {
                case TrileSet trileSet:
                    {
                        var meshes = ContentConversion.ConvertToMesh(trileSet);
                        foreach ((string name, var mesh) in meshes)
                        {
                            MeshesToPreview.Enqueue(new PreviewGenerationState(name, mesh, handler, data));
                        }

                        break;
                    }

                case ArtObject artObject:
                    {
                        var mesh = ContentConversion.ConvertToMesh(artObject);
                        MeshesToPreview.Enqueue(new PreviewGenerationState(path, mesh, handler, data));
                        break;
                    }

                case Texture2D texture2D:
                    {
                        var mesh = ContentConversion.ConvertToMesh(texture2D);
                        mesh.ResourceName = Path.GetFileName(path);
                        MeshesToPreview.Enqueue(new PreviewGenerationState(path, mesh, handler, data));
                        break;
                    }

                case AnimatedTexture animatedTexture:
                    {
                        var mesh = ContentConversion.ConvertToMesh(animatedTexture);
                        mesh.ResourceName = path.StartsWith("character animations")
                            ? path.Split("\\")[1]
                            : Path.GetFileName(path);
                        MeshesToPreview.Enqueue(new PreviewGenerationState(path, mesh, handler, data));
                        break;
                    }

                default:
                    throw new ArgumentException($"Unknown content type to preview: {path}");
            }
        }
        catch (Exception exception)
        {
            Logger.Error(exception, "Error while queueing content preview");
        }
    }

    public static void FinishContentPreview(Action callback)
    {
        s_finishCallback = callback;
    }
    
    private static async void GeneratePreview(SceneTree tree)
    {
        s_isGenerating = true;
        
        var generatingQueue = new Queue<PreviewGenerationState>(); 
        while (MeshesToPreview.TryDequeue(out var state))
        {
            var cachedPreview = LoadPreview(state.Path);
            if (cachedPreview != null)
            {
                state.Handler?.Invoke(state.Path, cachedPreview, state.Args);
            }
            else
            {
                generatingQueue.Enqueue(state);
            }
        }

        if (generatingQueue.Count < 1)
        {
            s_isGenerating = false;
            s_finishCallback?.Invoke();
            s_finishCallback = null;
            return;
        }

        var viewport = new SubViewport
        {
            Name = nameof(SubViewport),
            RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
            Size = Vector2I.One * PreviewSize,
            TransparentBg = true,
            World3D = new World3D()
        };
        var node = new Node3D { Name = nameof(Node3D) };
        var camera = new Camera3D
        {
            Name = nameof(Camera3D),
            Environment = new Godot.Environment { BackgroundMode = Godot.Environment.BGMode.ClearColor },
            Attributes = new CameraAttributes(),
            Position = Vector3.Back * 3f
        };
        var light = new DirectionalLight3D { Name = nameof(DirectionalLight3D), LightColor = Colors.White };
        var light2 = new DirectionalLight3D { Name = nameof(DirectionalLight3D), LightColor = Colors.Gray };
        var meshInstance = new MeshInstance3D { Name = nameof(MeshInstance3D) };

        tree.Root.AddChild(viewport);
        viewport.AddChild(node);
        node.AddChild(camera);
        node.AddChild(light);
        node.AddChild(light2);
        node.AddChild(meshInstance);
        camera.SetCurrent(true);

        var xform = Transform3D.Identity;
        xform.Basis = Basis.Identity.Rotated(Vector3.Up, -Mathf.Pi / 6f);
        xform.Basis = Basis.Identity.Rotated(Vector3.Right, Mathf.Pi / 6f) * xform.Basis;
        light.Transform = xform * Transform3D.Identity.LookingAt(new Vector3(-2, -1, -1), Vector3.Up);
        light2.Transform = xform * Transform3D.Identity.LookingAt(new Vector3(+1, -1, -2), Vector3.Up);

        while (generatingQueue.TryDequeue(out var state))
        {
            var meshAabb = state.Mesh?.GetAabb() ?? new Aabb();
            var meshCenter = meshAabb.GetCenter();
            var cameraSize = meshAabb.GetLongestAxisSize();

            var cameraBasis = Basis.Identity;
            meshInstance.Basis = Basis.Identity;
            if (state.Mesh is not PlaneMesh)
            {
                cameraBasis = Basis.FromEuler(new Vector3(CameraRotationX, CameraRotationY, 0));
                meshInstance.Basis = Basis.Identity.Rotated(Vector3.Up, -Mathf.Pi / 2f);
            }

            camera.SetOrthogonal(cameraSize * 1.5f, 0.01f, 1000f);
            camera.Transform = new Transform3D { Origin = meshCenter, Basis = cameraBasis };
            camera.Transform = camera.Transform.TranslatedLocal(Vector3.Back * cameraSize * 2f);
            meshInstance.Mesh = state.Mesh;

            DisplayServer.ProcessEvents();
            await tree.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            await tree.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);

            var texture = viewport.GetTexture();
            if (texture == null)
            {
                Logger.Error("Failed to get preview for '{0}'", state.Path);
                return;
            }

            var preview = ImageTexture.CreateFromImage(texture.GetImage());
            preview.ResourceName = state.Mesh?.ResourceName ?? state.Path;
            state.Handler?.Invoke(state.Path, preview, state.Args);
            SavePreview(state.Path, preview);
        }

        viewport.QueueFree();
        s_isGenerating = false;
        s_finishCallback?.Invoke();
        s_finishCallback = null;
    }

    private static void SavePreview(string path, ImageTexture preview)
    {
        var error = DirAccess.Open("user://").MakeDir(PreviewsFolder);
        if (error != Error.Ok && error != Error.AlreadyExists)
        {
            throw new Exception($"Failed to get access to previews folder '{PreviewsFolder}'");
        }
        
        var previewPath = PreviewsFolder.PathJoin(path.Md5Text()) + ".res";
        error = ResourceSaver.Save(preview, previewPath, ResourceSaver.SaverFlags.BundleResources);
        if (error != Error.Ok)
        {
            throw new Exception($"Failed to save preview for file '{path}' ({error})");
        }
    }

    private static ImageTexture LoadPreview(string path)
    {
        var previewPath = PreviewsFolder.PathJoin(path.Md5Text()) + ".res";
        return FileAccess.FileExists(previewPath)
            ? ResourceLoader.Load<ImageTexture>(previewPath)
            : null;
    }
    
    public static void InvalidatePreview(string path)
    {
        var previewPath = PreviewsFolder.PathJoin(path.Md5Text()) + ".res";
        if (FileAccess.FileExists(previewPath))
        {
            var dirAccess = DirAccess.Open(PreviewsFolder);
            dirAccess.Remove(previewPath);
        }
    }

    private record struct PreviewGenerationState(
        string Path,
        Mesh Mesh,
        ReceivePreviewHandler Handler,
        params object[] Args
    );
}