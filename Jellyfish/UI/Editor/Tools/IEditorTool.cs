using System;
using Jellyfish.Utils;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Jellyfish.UI.Editor.Tools;

public interface IEditorTool
{
    EditorToolType Type { get; }
    string Name { get; }
    bool Selected { get; set; }

    void Draw();
    void DrawToolbar(EditorToolType? selectedTool, Action selectToolAction);
    void DrawSidebar();
    void Input(KeyboardState keyboardState, MouseState mouseState, Ray ray, bool control, bool alt);
    void Reset();
}