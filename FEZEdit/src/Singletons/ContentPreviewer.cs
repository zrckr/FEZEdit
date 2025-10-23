using System;
using System.Collections.Generic;
using FEZRepacker.Core.Definitions.Game.ArtObject;
using FEZRepacker.Core.Definitions.Game.TrileSet;
using Godot;
using Serilog;
using Environment = Godot.Environment;

namespace FEZEdit.Singletons;

using AnimatedTexture = FEZRepacker.Core.Definitions.Game.Graphics.AnimatedTexture;
using Texture2D = FEZRepacker.Core.Definitions.Game.XNA.Texture2D;

public static class ContentPreviewer
{
    private static readonly ILogger Logger = LoggerFactory.Create(nameof(ContentPreviewer));

    private const float CameraRotationX = -Mathf.Pi / 8f;

    private const float CameraRotationY = -Mathf.Pi / 4f;

    public delegate void ReceivePreviewHandler(string path, ImageTexture preview, bool finished, params object[] data);

    public static int PreviewSize { get; set; } = 64;

    private static bool s_isGenerating;

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
                        MeshesToPreview.Enqueue(new PreviewGenerationState(path, mesh, handler, data));
                        break;
                    }

                case AnimatedTexture animatedTexture:
                    {
                        var mesh = ContentConversion.ConvertToMesh(animatedTexture);
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

    private static async void GeneratePreview(SceneTree tree)
    {
        s_isGenerating = true;
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
            Environment = new Environment { BackgroundMode = Environment.BGMode.ClearColor },
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
        
        while (MeshesToPreview.TryDequeue(out var state))
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
            camera.Transform = camera.Transform.TranslatedLocal(Vector3.Back * cameraSize);
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

            var finished = MeshesToPreview.Count < 1;
            var preview = ImageTexture.CreateFromImage(texture.GetImage());
            preview.ResourceName = state.Mesh?.ResourceName ?? state.Path;
            state.Handler?.Invoke(state.Path, preview, finished, state.Args);
        }
        
        viewport.QueueFree();
        s_isGenerating = false;
    }

    private record struct PreviewGenerationState(
        string Path,
        Mesh Mesh,
        ReceivePreviewHandler Handler,
        params object[] Args
    );
}