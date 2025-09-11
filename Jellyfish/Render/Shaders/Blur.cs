using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Jellyfish.Render.Shaders;

public class Blur : Shader
{
    public enum Direction
    {
        Horizontal,
        Vertical
    }

    public enum Size
    {
        Blur5,
        Blur9,
        Blur13
    }

    private readonly Direction _direction;
    private readonly Size _size;
    private readonly Texture _rtSource;

    public Blur(string source, Direction direction, Size size) : base("shaders/Screenspace.vert", null, "shaders/Blur.frag")
    {
        _direction = direction;
        _size = size;
        _rtSource = Engine.TextureManager.GetTexture(source)!;
    }
    public override void Bind()
    {
        base.Bind();
        _rtSource.Bind(0);

        SetVector2("screenSize", new Vector2(Engine.MainViewport.Size.X, Engine.MainViewport.Size.Y));
        SetInt("direction", (int)_direction);
        SetInt("size", (int)_size); 
    }

    public override void Unbind()
    {
        GL.BindTextureUnit(0, 0);
        base.Unbind();
    }

    public override void Unload()
    {
        _rtSource.Unload();
        base.Unload();
    }
}