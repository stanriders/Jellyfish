using Jellyfish.Entities;
using Jellyfish.Render.Lighting;
using OpenTK.Graphics.OpenGL;
using Serilog;

namespace Jellyfish.Render.Shaders;

public class Shadow : Shader
{
    private readonly ILightSource _light;

    public Shadow(ILightSource light) : 
        base("shaders/Shadow.vert", null, "shaders/Shadow.frag")
    {
        _light = light;
    }

    public override void Bind()
    {
        base.Bind();
        SetMatrix4("lightSpaceMatrix", _light.Projection());
    }
}