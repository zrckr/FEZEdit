using System.Threading.Tasks;
using Godot;
using Godot.Collections;

namespace FEZEdit.Tools;

public static class GodotUtil
{
    private const float CameraRotationX = -Mathf.Pi / 8f;

    private const float CameraRotationY = -Mathf.Pi / 4f;

    public static async Task<Array<Texture2D>> GeneratePreviews(
        SceneTree tree,
        Array<Mesh> meshes,
        int previewSize)
    {
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);

        var viewport = new SubViewport
        {
            Name = nameof(SubViewport),
            RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
            Size = Vector2I.One * previewSize,
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
        meshInstance.Basis = Basis.Identity.Rotated(Vector3.Up, -Mathf.Pi / 2f);

        var previews = new Array<Texture2D>();
        foreach (var mesh in meshes)
        {
            var meshAabb = mesh.GetAabb();
            var meshCenter = meshAabb.GetCenter();
            var cameraSize = meshAabb.GetLongestAxisSize();

            camera.SetOrthogonal(cameraSize * 1.5f, 0.01f, 1000f);
            camera.Transform = new Transform3D
            {
                Origin = meshCenter, Basis = Basis.FromEuler(new Vector3(CameraRotationX, CameraRotationY, 0))
            };
            camera.Transform = camera.Transform.TranslatedLocal(Vector3.Back * cameraSize);

            meshInstance.Mesh = mesh;
            DisplayServer.ProcessEvents();
            await tree.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            await tree.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);

            var texture = viewport.GetTexture();
            if (texture == null)
            {
                GD.PushError($"Failed to get preview for mesh: {mesh.ResourceName}");
                continue;
            }

            var preview = ImageTexture.CreateFromImage(texture.GetImage());
            preview.ResourceName = mesh.ResourceName;
            previews.Add(preview);
        }

        viewport.QueueFree();
        return previews;
    }
}