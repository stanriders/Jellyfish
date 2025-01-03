﻿using System.Collections.Generic;
using Jellyfish.Console;
using Jellyfish.Render;
using JoltPhysicsSharp;
using OpenTK.Mathematics;
using Serilog;

namespace Jellyfish.Entities;

[Entity("plane_flat")]
public class Plane : BaseEntity
{
    private Mesh? _plane;
    private BodyID _physicsBodyId;

    public Plane()
    {
        AddProperty("Size", new Vector2(20, 20), false);
        AddProperty("Texture", "test.png", false);
        AddProperty("TextureScale", 1.0f, false);
    }

    public override void Load()
    {
        var size = GetPropertyValue<Vector2>("Size");
        var textureProperty = GetPropertyValue<string>("Texture");
        if (textureProperty == null)
        {
            EntityLog().Error("Texture not set!");
            return;
        }

        var a = new Vector3(-size.X / 2.0f, size.Y / 2.0f, 0);
        var b = new Vector3(size.X / 2.0f, size.Y / 2.0f, 0);
        var c = new Vector3(size.X / 2.0f, -size.Y / 2.0f, 0);
        var d = new Vector3(-size.X / 2.0f, -size.Y / 2.0f, 0);

        Vector3 u = b - a;
        Vector3 v = c - b;
        Vector3 normal = Vector3.Cross(u, v).Normalized();

        var texture = $"materials/{textureProperty}";
        var textureScale = GetPropertyValue<float>("TextureScale");

        _plane = new Mesh(new MeshPart
        {
            Name = "plane_flat",
            Vertices = new List<Vertex>
            {
                new()
                {
                    Coordinates = a,
                    Normal = normal,
                    UV = new(0, 0)
                },
                new()
                {
                    Coordinates = b,
                    Normal = normal,
                    UV = new(textureScale, 0)
                },
                new()
                {
                    Coordinates = c,
                    Normal = normal,
                    UV = new(textureScale, textureScale)
                },
                new()
                {
                    Coordinates = d,
                    Normal = normal,
                    UV = new(0, textureScale)
                },
                new()
                {
                    Coordinates = a,
                    Normal = normal,
                    UV = new(0, 0)
                },
                new()
                {
                    Coordinates = c,
                    Normal = normal,
                    UV = new(textureScale, textureScale)
                },
            },
            Texture = texture
        });

        MeshManager.AddMesh(_plane);
        _physicsBodyId = PhysicsManager.AddStaticObject(new []{ _plane.MeshPart }, this) ?? 0;
        base.Load();
    }

    public override void Think()
    {
        if (_plane != null)
        {
            _plane.Position = GetPropertyValue<Vector3>("Position");
            _plane.Rotation = GetPropertyValue<Quaternion>("Rotation");
        }

        base.Think();
    }

    public override void Unload()
    {
        if (_plane != null)
            MeshManager.RemoveMesh(_plane);

        PhysicsManager.RemoveObject(_physicsBodyId);

        base.Unload();
    }
}