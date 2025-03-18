using System;
using Jellyfish.Render.Lighting;

namespace Jellyfish.Render.Shaders;

public class Shadow : Shader
{
    private readonly ILightSource _light;
    private readonly int _shadowNum;

    public Shadow(ILightSource light, int shadowNum) : 
        base("shaders/Shadow.vert", null, "shaders/Shadow.frag")
    {
        _light = light;
        _shadowNum = shadowNum;
    }

    public override void Bind()
    {
        base.Bind();
        SetMatrix4("lightSpaceMatrix", _light.Projections[_shadowNum]);
    }
}