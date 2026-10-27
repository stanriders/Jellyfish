using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Hexa.NET.ImGui;
using Hexa.NET.ImGuizmo;
using Jellyfish.Console;
using Jellyfish.Entities;
using Jellyfish.Render;
using Jellyfish.Utils;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Jellyfish.UI.Editor.Tools;

public class FaceTool : IEditorTool
{
    public EditorToolType Type => EditorToolType.Faces;
    public string Name => "Faces";
    public bool Selected { get; set; }

    private readonly List<int> _selectedFaces = new();

    private Ray? _mouseRay;

    private const uint face_hover_color = 0xFF00DCFF;
    private const uint face_selected_color = 0xFF0050FF;

    public void DrawToolbar(EditorToolType? selectedTool, Action selectToolAction)
    {
        if (ImGui.RadioButton(Name, selectedTool == Type))
            selectToolAction();
    }

    public void DrawSidebar()
    {
        if (!Selected)
            return;

        if (Editor.Instance.SelectedEntity is WorldMesh worldMesh)
        {
            DrawFaceControls(worldMesh);
        }
    }

    public void Draw()
    {
        if (!Selected)
            return;

        DrawFaceSelection();
        DrawPatchControls();
    }

    public void Input(KeyboardState keyboardState, MouseState mouseState, Ray ray, bool control, bool alt)
    {
        if (!Selected)
            return;

        _mouseRay = ray;

        if (keyboardState.IsKeyPressed(Keys.Delete) && Editor.Instance.SelectedEntity is WorldMesh worldMesh && _selectedFaces.Count > 0)
        {
            var mesh = worldMesh.GetPropertyValue<EditableMesh>("Mesh")!;
            worldMesh.SetPropertyValue("Mesh", mesh with { Faces = mesh.Faces.Where((_, i) => !_selectedFaces.Contains(i)).ToArray() });
            _selectedFaces.Clear();
        }

        if (!mouseState.IsButtonPressed(MouseButton.Left))
            return;

        var entity = Trace.IntersectsWorldMesh(ray, out var face, out _, out _);

        // eyedropper
        if (alt)
        {
            if (entity != null)
                MaterialBrowser.SelectedMaterial = entity.GetPropertyValue<EditableMesh>("Mesh")!.Faces[face].Material;

            return;
        }

        if (entity != Editor.Instance.SelectedEntity || entity == null)
        {
            Editor.Instance.SelectedEntity = entity;
            _selectedFaces.Clear();
        }

        if (entity == null)
            return;

        if (!control)
        {
            _selectedFaces.Clear();
            _selectedFaces.Add(face);
        }
        else if (!_selectedFaces.Remove(face))
        {
            _selectedFaces.Add(face);
        }
    }

    public void Reset()
    {
        _selectedFaces.Clear();
    }

    private void DrawFaceSelection()
    {
        if (_mouseRay != null && Trace.IntersectsWorldMesh(_mouseRay.Value, out var hoveredFace, out _, out _) is { } hoveredEntity)
            DrawFaceOutline(hoveredEntity, hoveredFace, face_hover_color);

        if (Editor.Instance.SelectedEntity is not WorldMesh worldMesh)
            return;

        var faceCount = worldMesh.GetPropertyValue<EditableMesh>("Mesh")!.Faces.Length;
        _selectedFaces.RemoveAll(x => x >= faceCount);

        foreach (var face in _selectedFaces)
            DrawFaceOutline(worldMesh, face, face_selected_color);
    }

    private unsafe void DrawPatchControls()
    {
        if (Editor.Instance.SelectedEntity is not WorldMesh worldMesh || _selectedFaces.Count != 1)
            return;

        var mesh = worldMesh.GetPropertyValue<EditableMesh>("Mesh")!;
        var face = mesh.Faces[_selectedFaces[0]];
        if (face.Subdivisions == 0)
            return;

        var transform = worldMesh.Transform;
        var gridSize = (float)ConVarStorage.Get<int>("edt_gridsize");
        var snapping = ConVarStorage.Get<bool>("edt_snap");

        fixed (float* view = Engine.MainViewport.GetViewMatrix().ToFloatArray())
        fixed (float* proj = Engine.MainViewport.GetProjectionMatrix().ToFloatArray())
        fixed (float* snap = new[] { gridSize, gridSize, gridSize })
        {
            for (var i = 0; i < face.Indices.Length; i++)
            {
                var index = face.Indices[i];
                var pointTransform = Matrix4.CreateTranslation(Vector3.TransformPosition(mesh.Vertices[index], transform)).ToFloatArray();

                fixed (float* pointTransformPinned = pointTransform)
                {
                    ImGuizmo.SetID(i);

                    ImGuizmo.DrawCubes(ref Unsafe.AsRef<float>(view), ref Unsafe.AsRef<float>(proj),
                        ref Unsafe.AsRef<float>(pointTransformPinned), 1);

                    if (ImGuizmo.Manipulate(ref Unsafe.AsRef<float>(view), ref Unsafe.AsRef<float>(proj),
                            ImGuizmoOperation.Translate, ImGuizmoMode.World,
                            ref Unsafe.AsRef<float>(pointTransformPinned),
                            null,
                            snapping ? snap : null))
                    {
                        //_usingGizmo = true; // TODO?

                        var vertices = mesh.Vertices.ToArray();
                        vertices[index] = Vector3.TransformPosition(pointTransform.ToMatrix().ExtractTranslation(), transform.Inverted());

                        mesh = mesh with { Vertices = vertices };
                        worldMesh.SetPropertyValue("Mesh", mesh);
                    }
                }
            }
        }
    }

    private static void DrawFaceOutline(WorldMesh entity, int faceIndex, uint color)
    {
        var mesh = entity.GetPropertyValue<EditableMesh>("Mesh")!;
        var transform = entity.Transform;
        var face = mesh.Faces[faceIndex];
        var indices = face.Indices;

        if (face.Subdivisions > 0)
        {
            // outline the curved edges of the patch
            var grid = WorldMesh.TessellatePatch(mesh, face);
            var size = face.Subdivisions;
            for (var i = 0; i < size; i++)
            {
                DebugRender.DrawLine(Vector3.TransformPosition(grid[i, 0], transform), Vector3.TransformPosition(grid[i + 1, 0], transform), color);
                DebugRender.DrawLine(Vector3.TransformPosition(grid[i, size], transform), Vector3.TransformPosition(grid[i + 1, size], transform), color);
                DebugRender.DrawLine(Vector3.TransformPosition(grid[0, i], transform), Vector3.TransformPosition(grid[0, i + 1], transform), color);
                DebugRender.DrawLine(Vector3.TransformPosition(grid[size, i], transform), Vector3.TransformPosition(grid[size, i + 1], transform), color);
            }

            return;
        }

        for (var i = 0; i < indices.Length; i++)
        {
            DebugRender.DrawLine(Vector3.TransformPosition(mesh.Vertices[indices[i]], transform),
                Vector3.TransformPosition(mesh.Vertices[indices[(i + 1) % indices.Length]], transform), color);
        }
    }

    private void DrawFaceControls(WorldMesh entity)
    {
        var mesh = entity.GetPropertyValue<EditableMesh>("Mesh")!;
        _selectedFaces.RemoveAll(x => x >= mesh.Faces.Length);

        ImGui.SeparatorText("Faces");

        if (_selectedFaces.Count == 0)
        {
            ImGui.TextWrapped("Click to select faces, Ctrl+click to select multiple, Alt+click to pick a material, Delete to remove selected faces. Select a single curved face to move its control points");
            return;
        }

        var face = mesh.Faces[_selectedFaces[0]];

        ImGui.Text($"{_selectedFaces.Count} selected");
        ImGui.TextWrapped($"Material: {face.Material}");

        var offset = (System.Numerics.Vector2)face.TextureOffset;
        if (ImGui.DragFloat2("Texture offset", ref offset, 0.01f))
            ModifySelectedFaces(x => x with { TextureOffset = (Vector2)offset });

        var scale = (System.Numerics.Vector2)face.TextureScale;
        if (ImGui.DragFloat2("Texture scale", ref scale, 0.01f, 0.01f, 100.0f))
            ModifySelectedFaces(x => x with { TextureScale = (Vector2)scale });

        var rotation = face.TextureRotation;
        if (ImGui.DragFloat("Texture rotation", ref rotation, 1.0f))
            ModifySelectedFaces(x => x with { TextureRotation = rotation });

        if (ImGui.Button("Apply selected material"))
            ModifySelectedFaces(x => x with { Material = MaterialBrowser.SelectedMaterial });

        ImGui.SameLine();
        if (ImGui.Button("Reset mapping"))
            ModifySelectedFaces(x => x with { TextureOffset = Vector2.Zero, TextureScale = Vector2.One, TextureRotation = 0 });

        // flipping a patch swaps its u and v directions, reversing the control points would scramble the rows
        ImGui.SameLine();
        if (ImGui.Button("Flip"))
            ModifySelectedFaces(x => x with
            {
                Indices = x.Subdivisions > 0
                    ? Enumerable.Range(0, 16).Select(i => x.Indices[i % 4 * 4 + i / 4]).ToArray()
                    : x.Indices.Reverse().ToArray()
            });

        var firstPatch = _selectedFaces.Select(x => mesh.Faces[x]).FirstOrDefault(x => x.Subdivisions > 0);
        if (firstPatch != null)
        {
            var subdivisions = firstPatch.Subdivisions;
            if (ImGui.DragInt("Subdivisions", ref subdivisions, 0.1f, 1, 64))
                ModifySelectedFaces(x => x.Subdivisions > 0 ? x with { Subdivisions = subdivisions } : x);

            if (ImGui.Button("Flatten"))
                ModifySelectedFaces(x => x.Subdivisions > 0 ? x with { Indices = [x.Indices[0], x.Indices[3], x.Indices[15], x.Indices[12]], Subdivisions = 0 } : x);
        }

        if (_selectedFaces.Any(x => mesh.Faces[x].Subdivisions == 0 && mesh.Faces[x].Indices.Length == 4))
        {
            if (firstPatch != null)
                ImGui.SameLine();

            if (ImGui.Button("Make curved"))
            {
                var vertices = mesh.Vertices.ToList();
                var faces = mesh.Faces.ToArray();

                foreach (var i in _selectedFaces.Where(x => faces[x].Subdivisions == 0 && faces[x].Indices.Length == 4))
                {
                    // 4x4 control points spread over the quad, corners stay shared with the neighbouring faces
                    var corners = faces[i].Indices;
                    var controlPoints = new int[16];
                    for (var row = 0; row < 4; row++)
                    {
                        for (var column = 0; column < 4; column++)
                        {
                            var index = (row, column) switch
                            {
                                (0, 0) => corners[0],
                                (0, 3) => corners[1],
                                (3, 3) => corners[2],
                                (3, 0) => corners[3],
                                _ => -1
                            };

                            if (index == -1)
                            {
                                var top = Vector3.Lerp(vertices[corners[0]], vertices[corners[1]], column / 3.0f);
                                var bottom = Vector3.Lerp(vertices[corners[3]], vertices[corners[2]], column / 3.0f);
                                vertices.Add(Vector3.Lerp(top, bottom, row / 3.0f));
                                index = vertices.Count - 1;
                            }

                            controlPoints[row * 4 + column] = index;
                        }
                    }

                    faces[i] = faces[i] with { Indices = controlPoints, Subdivisions = 8 };
                }

                entity.SetPropertyValue("Mesh", mesh with { Vertices = vertices.ToArray(), Faces = faces });
            }
        }

        return;

        void ModifySelectedFaces(Func<EditableFace, EditableFace> modify)
        {
            var currentMesh = entity.GetPropertyValue<EditableMesh>("Mesh")!;
            var faces = currentMesh.Faces.ToArray();
            foreach (var i in _selectedFaces)
                faces[i] = modify(faces[i]);

            entity.SetPropertyValue("Mesh", currentMesh with { Faces = faces });
        }
    }
}