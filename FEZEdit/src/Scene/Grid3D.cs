using Godot;
using Godot.Collections;

namespace FEZEdit.Scene;

public partial class Grid3D : Node3D
{
    private const string GridShader = "res://src/Shaders/EditorGrid.gdshader";

    private const string OriginShader = "res://src/Shaders/EditorOrigin.gdshader";

    private const float ExtraCullMargin = 16384f;

    private static readonly Vector3[] OriginPoints =
    [
        new(0.0f, -0.5f, 0.0f),
        new(0.0f, -0.5f, 1.0f),
        new(0.0f, 0.5f, 1.0f),
        new(0.0f, -0.5f, 0.0f),
        new(0.0f, 0.5f, 1.0f),
        new(0.0f, 0.5f, 0.0f)
    ];

    private static readonly float[] Distances =
    [
        -1000000.0f,
        -1000.0f,
        0.0f,
        1000.0f,
        1000000.0f
    ];

    [Export] public bool GridEnabled { get; set; } = true;

    [Export] public bool OriginEnabled { get; set; } = true;

    [Export] public Color PrimaryGridColor { get; set; } = new(0.5f, 0.5f, 0.5f);

    [Export] public Color SecondaryGridColor { get; set; } = new(0.3f, 0.3f, 0.3f, 0.8f);

    [Export] public int GridSize { get; set; } = 100;

    [Export] public int PrimaryGridSteps { get; set; } = 10;

    [Export] public float DivisionLevelBias { get; set; } = -0.2f;

    [Export] public int DivisionLevelMax { get; set; } = 2;

    [Export] public int DivisionLevelMin { get; set; }

    [Export] public Color AxisXColor { get; set; } = new(0.96f, 0.20f, 0.32f);

    [Export] public Color AxisYColor { get; set; } = new(0.53f, 0.84f, 0.01f);

    [Export] public Color AxisZColor { get; set; } = new(0.16f, 0.55f, 0.96f);

    [Export] public Color AxisWColor { get; set; } = new(0.55f, 0.55f, 0.55f);

    private readonly bool[] _gridEnable = [false, false, true];

    private readonly bool[] _gridVisible = [false, false, true];

    private readonly MeshInstance3D[] _gridInstances = new MeshInstance3D[3];

    private readonly ShaderMaterial[] _gridMaterials = new ShaderMaterial[3];

    private MultiMeshInstance3D _originInstance;

    private ShaderMaterial _originMaterial;

    public override void _Ready()
    {
        for (int i = 0; i < 3; i++)
        {
            _gridMaterials[i] = new ShaderMaterial { Shader = ResourceLoader.Load<Shader>(GridShader) };
            _gridInstances[i] = new MeshInstance3D();
            _gridInstances[i].Name = $"GridPlane{i}";
            _gridInstances[i].CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
            _gridInstances[i].ExtraCullMargin = ExtraCullMargin;
            _gridInstances[i].GIMode = GeometryInstance3D.GIModeEnum.Disabled;
            AddChild(_gridInstances[i]);
        }

        InitIndicators();
        InitGrid();
    }

    /// <summary>
    /// https://github.com/godotengine/godot/blob/master/editor/scene/3d/node_3d_editor_plugin.cpp
    /// </summary>
    public void InitIndicators()
    {
        _originMaterial = new ShaderMaterial { Shader = ResourceLoader.Load<Shader>(OriginShader) };

        Array d = new();
        d.Resize((int)Mesh.ArrayType.Max);
        d[(int)Mesh.ArrayType.Vertex] = OriginPoints;

        var originMesh = new ArrayMesh();
        originMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, d);
        originMesh.SurfaceSetMaterial(0, _originMaterial);

        var multiMesh = new MultiMesh();
        multiMesh.Mesh = originMesh;
        multiMesh.TransformFormat = MultiMesh.TransformFormatEnum.Transform3D;
        multiMesh.UseColors = true;
        multiMesh.UseCustomData = false;
        multiMesh.InstanceCount = 12;

        for (int i = 0; i < 3; i++)
        {
            var originColor = (i) switch
            {
                0 => AxisXColor,
                1 => AxisYColor,
                2 => AxisZColor,
                _ => Colors.White
            };

            var axis = Vector3.Zero;
            axis[i] = 1;

            for (int j = 0; j < 4; j++)
            {
                var t = Transform3D.Identity;
                if (Distances[j] > 0.0)
                {
                    t = t.Scaled(axis * Distances[j + 1]);
                    t = t.Translated(axis * Distances[j]);
                }
                else
                {
                    t = t.Scaled(axis * Distances[j]);
                    t = t.Translated(axis * Distances[j + 1]);
                }

                multiMesh.SetInstanceTransform(i * 4 + j, t);
                multiMesh.SetInstanceColor(i * 4 + j, originColor);
            }
        }

        _originInstance = new MultiMeshInstance3D();
        _originInstance.Name = "OriginIndicators";
        _originInstance.Multimesh = multiMesh;
        _originInstance.MaterialOverride = _originMaterial;
        _originInstance.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
        _originInstance.ExtraCullMargin = ExtraCullMargin;
        _originInstance.GIMode = GeometryInstance3D.GIModeEnum.Disabled;
        _originInstance.Visible = OriginEnabled;
        AddChild(_originInstance);
    }

    public void InitGrid()
    {
        if (!GridEnabled)
        {
            return;
        }

        var camera = GetViewport().GetCamera3D();
        var cameraPosition = camera.GlobalPosition;
        if (cameraPosition == Vector3.Zero)
        {
            return; // Camera is invalid, don't draw the grid
        }

        var orthogonal = camera.Projection == Camera3D.ProjectionType.Orthogonal;

        for (int a = 0; a < 3; a++)
        {
            if (!_gridEnable[a]) continue;

            int b = (a + 1) % 3;
            int c = (a + 2) % 3;

            var normal = Vector3.Zero;
            normal[c] = 1.0f;

            float cameraDistance = Mathf.Abs(cameraPosition[c]);

            if (orthogonal)
            {
                cameraDistance = camera.Size / 2.0f;
                var cameraDirection = -camera.GlobalTransform.Basis.Z;
                var gridPlane = new Plane(normal, 0);
                var intersection = gridPlane.IntersectsRay(cameraPosition, cameraDirection) ?? cameraPosition;
                cameraPosition = intersection;
            }

            var divisionLevel =
                Mathf.Log(Mathf.Abs(cameraDistance)) / Mathf.Log(PrimaryGridSteps) + DivisionLevelBias;
            var clampedDivisionLevel = Mathf.Clamp(divisionLevel, DivisionLevelMin, DivisionLevelMax);
            var divisionLevelFloored = Mathf.Floor(clampedDivisionLevel);
            var divisionLevelDecimals = clampedDivisionLevel - divisionLevelFloored;

            var smallStepSize = Mathf.Pow(PrimaryGridSteps, divisionLevelFloored);
            var largeStepSize = smallStepSize * PrimaryGridSteps;
            var centerA = largeStepSize * Mathf.Floor(cameraPosition[a] / largeStepSize);
            var centerB = largeStepSize * Mathf.Floor(cameraPosition[b] / largeStepSize);

            var bgnA = centerA - GridSize * smallStepSize;
            var endA = centerA + GridSize * smallStepSize;
            var bgnB = centerB - GridSize * smallStepSize;
            var endB = centerB + GridSize * smallStepSize;

            var fadeSize = Mathf.Pow(PrimaryGridSteps, divisionLevel - 1.0f);
            var minFadeSize = Mathf.Pow(PrimaryGridSteps, DivisionLevelMin);
            var maxFadeSize = Mathf.Pow(PrimaryGridSteps, DivisionLevelMax);
            fadeSize = Mathf.Clamp(fadeSize, minFadeSize, maxFadeSize);

            var gridFadeSize = (GridSize - PrimaryGridSteps) * fadeSize;
            _gridMaterials[c].SetShaderParameter("grid_size", gridFadeSize);
            _gridMaterials[c].SetShaderParameter("orthogonal", orthogonal);

            // Generate grid points
            Vector3[] gridPoints;
            Vector3[] gridNormals;
            Color[] gridColors;

            // Generate grid lines
            {
                // Count expected size
                int expectedSize = 0;
                for (int i = -GridSize; i <= GridSize; i++)
                {
                    float positionA = centerA + i * smallStepSize;
                    float positionB = centerB + i * smallStepSize;

                    if (!(OriginEnabled && Mathf.IsZeroApprox(positionA)))
                    {
                        expectedSize += 2;
                    }

                    if (!(OriginEnabled && Mathf.IsZeroApprox(positionB)))
                    {
                        expectedSize += 2;
                    }
                }

                gridPoints = new Vector3[expectedSize];
                gridNormals = new Vector3[expectedSize];
                gridColors = new Color[expectedSize];

                int idx = 0;

                for (int i = -GridSize; i <= GridSize; i++)
                {
                    Color lineColor;
                    if (i % PrimaryGridSteps == 0)
                    {
                        lineColor = PrimaryGridColor.Lerp(SecondaryGridColor, divisionLevelDecimals);
                    }
                    else
                    {
                        lineColor = SecondaryGridColor;
                        lineColor.A *= (1 - divisionLevelDecimals);
                    }

                    float positionA = centerA + i * smallStepSize;
                    float positionB = centerB + i * smallStepSize;

                    // Don't draw lines over the origin if it's enabled
                    if (!(OriginEnabled && Mathf.IsZeroApprox(positionA)))
                    {
                        Vector3 lineBgn = Vector3.Zero;
                        Vector3 lineEnd = Vector3.Zero;

                        lineBgn[a] = positionA;
                        lineEnd[a] = positionA;
                        lineBgn[b] = bgnB;
                        lineEnd[b] = endB;

                        gridPoints[idx] = lineBgn;
                        gridPoints[idx + 1] = lineEnd;
                        gridColors[idx] = lineColor;
                        gridColors[idx + 1] = lineColor;
                        gridNormals[idx] = normal;
                        gridNormals[idx + 1] = normal;
                        idx += 2;
                    }

                    if (!(OriginEnabled && Mathf.IsZeroApprox(positionB)))
                    {
                        Vector3 lineBgn = Vector3.Zero;
                        Vector3 lineEnd = Vector3.Zero;

                        lineBgn[b] = positionB;
                        lineEnd[b] = positionB;
                        lineBgn[a] = bgnA;
                        lineEnd[a] = endA;

                        gridPoints[idx] = lineBgn;
                        gridPoints[idx + 1] = lineEnd;
                        gridColors[idx] = lineColor;
                        gridColors[idx + 1] = lineColor;
                        gridNormals[idx] = normal;
                        gridNormals[idx + 1] = normal;
                        idx += 2;
                    }
                }
            }

            Array meshArrays = new();
            meshArrays.Resize((int)Mesh.ArrayType.Max);
            meshArrays[(int)Mesh.ArrayType.Vertex] = gridPoints;
            meshArrays[(int)Mesh.ArrayType.Color] = gridColors;
            meshArrays[(int)Mesh.ArrayType.Normal] = gridNormals;

            var gridMesh = new ArrayMesh();
            gridMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Lines, meshArrays);
            gridMesh.SurfaceSetMaterial(0, _gridMaterials[c]);

            _gridInstances[c].Mesh = gridMesh;
            _gridInstances[c].Visible = _gridVisible[a];
        }
    }
}