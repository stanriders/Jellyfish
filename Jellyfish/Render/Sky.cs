using Jellyfish.Debug;
using Jellyfish.Render.Shaders;
using Jellyfish.Utils;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System;
using Jellyfish.Entities;

namespace Jellyfish.Render;

public class HosekWilkieParams
{
    public Vector3 A;
    public Vector3 B;
    public Vector3 C;
    public Vector3 D;
    public Vector3 E;
    public Vector3 F;
    public Vector3 G;
    public Vector3 H;
    public Vector3 I;
    public Vector3 Z;
}

public class Sky
{
    private readonly Skybox _shader;
    private readonly HosekWilkieParams _params;

    private float _normalizedSunYValue = 0.1f;

    public Sky()
    {
        _params = new HosekWilkieParams();
        _shader = new Skybox(_params);
    }

    public void Draw()
    {
        if (Engine.LightManager.Sun == null)
            return;

        bool normalizedSunY = true;

        var sun = (Sun)Engine.LightManager.Sun!.Source;
        var sunDirection = Vector3.Transform(Vector3.UnitY, sun.Rotation);
        var sunTheta = MathF.Acos(Math.Clamp(sunDirection.Y, 0.0f, 1.0f));

        CalculateSkyParams(sun.Turbidity, sun.Albedo, sunTheta);

        if (normalizedSunY)
        {
            var s = HosekWilkie(MathF.Cos(sunTheta), 0.0f, 1.0f, _params) * _params.Z;

            var luminance = Vector3.Dot(s, new Vector3(0.2126f, 0.7152f, 0.0722f));
            if (luminance > 0.0f)
            {
                _params.Z = _params.Z / luminance * _normalizedSunYValue;
            }
        }

        _shader.Bind();
        CommonShapes.CubeVertexArray?.Bind();

        GL.DepthFunc(DepthFunction.Lequal);
        GL.DrawArrays(PrimitiveType.Triangles, 0, CommonShapes.Cube.Length);
        PerformanceMeasurment.Increment("DrawCalls");
        GL.DepthFunc(DepthFunction.Less);

        _shader.Unbind();
        CommonShapes.CubeVertexArray?.Unbind();
    }

    public void Unload()
    {
        _shader.Unload();
    }

    private void CalculateSkyParams(float turbidity, float albedo, float sunTheta)
    {
        _params.A = new Vector3(
            (float)Evaluate(HosekWilkieDataset.DatasetRgb1, 0, 9, turbidity, albedo, sunTheta),
            (float)Evaluate(HosekWilkieDataset.DatasetRgb2, 0, 9, turbidity, albedo, sunTheta),
            (float)Evaluate(HosekWilkieDataset.DatasetRgb3, 0, 9, turbidity, albedo, sunTheta)
        );

        _params.B = new Vector3(
            (float)Evaluate(HosekWilkieDataset.DatasetRgb1, 1, 9, turbidity, albedo, sunTheta),
            (float)Evaluate(HosekWilkieDataset.DatasetRgb2, 1, 9, turbidity, albedo, sunTheta),
            (float)Evaluate(HosekWilkieDataset.DatasetRgb3, 1, 9, turbidity, albedo, sunTheta)
        );

        _params.C = new Vector3(
            (float)Evaluate(HosekWilkieDataset.DatasetRgb1, 2, 9, turbidity, albedo, sunTheta),
            (float)Evaluate(HosekWilkieDataset.DatasetRgb2, 2, 9, turbidity, albedo, sunTheta),
            (float)Evaluate(HosekWilkieDataset.DatasetRgb3, 2, 9, turbidity, albedo, sunTheta)
        );

        _params.D = new Vector3(
            (float)Evaluate(HosekWilkieDataset.DatasetRgb1, 3, 9, turbidity, albedo, sunTheta),
            (float)Evaluate(HosekWilkieDataset.DatasetRgb2, 3, 9, turbidity, albedo, sunTheta),
            (float)Evaluate(HosekWilkieDataset.DatasetRgb3, 3, 9, turbidity, albedo, sunTheta)
        );

        _params.E = new Vector3(
            (float)Evaluate(HosekWilkieDataset.DatasetRgb1, 4, 9, turbidity, albedo, sunTheta),
            (float)Evaluate(HosekWilkieDataset.DatasetRgb2, 4, 9, turbidity, albedo, sunTheta),
            (float)Evaluate(HosekWilkieDataset.DatasetRgb3, 4, 9, turbidity, albedo, sunTheta)
        );

        _params.F = new Vector3(
            (float)Evaluate(HosekWilkieDataset.DatasetRgb1, 5, 9, turbidity, albedo, sunTheta),
            (float)Evaluate(HosekWilkieDataset.DatasetRgb2, 5, 9, turbidity, albedo, sunTheta),
            (float)Evaluate(HosekWilkieDataset.DatasetRgb3, 5, 9, turbidity, albedo, sunTheta)
        );

        _params.G = new Vector3(
            (float)Evaluate(HosekWilkieDataset.DatasetRgb1, 6, 9, turbidity, albedo, sunTheta),
            (float)Evaluate(HosekWilkieDataset.DatasetRgb2, 6, 9, turbidity, albedo, sunTheta),
            (float)Evaluate(HosekWilkieDataset.DatasetRgb3, 6, 9, turbidity, albedo, sunTheta)
        );

        // H and I are intentionally swapped.
        _params.H = new Vector3(
            (float)Evaluate(HosekWilkieDataset.DatasetRgb1, 8, 9, turbidity, albedo, sunTheta),
            (float)Evaluate(HosekWilkieDataset.DatasetRgb2, 8, 9, turbidity, albedo, sunTheta),
            (float)Evaluate(HosekWilkieDataset.DatasetRgb3, 8, 9, turbidity, albedo, sunTheta)
        );

        _params.I = new Vector3(
            (float)Evaluate(HosekWilkieDataset.DatasetRgb1, 7, 9, turbidity, albedo, sunTheta),
            (float)Evaluate(HosekWilkieDataset.DatasetRgb2, 7, 9, turbidity, albedo, sunTheta),
            (float)Evaluate(HosekWilkieDataset.DatasetRgb3, 7, 9, turbidity, albedo, sunTheta)
        );

        _params.Z = new Vector3(
            (float)Evaluate(HosekWilkieDataset.DatasetRgbRad1, 0, 1, turbidity, albedo, sunTheta),
            (float)Evaluate(HosekWilkieDataset.DatasetRgbRad2, 0, 1, turbidity, albedo, sunTheta),
            (float)Evaluate(HosekWilkieDataset.DatasetRgbRad3, 0, 1, turbidity, albedo, sunTheta)
        );
    }


    private static double EvaluateSpline(double[] spline, int offset, int stride, double value)
    {
        var inv = 1.0 - value;

        return Math.Pow(inv, 5.0) * spline[offset + 0 * stride]
               +
               5.0 * Math.Pow(inv, 4.0) * value * spline[offset + 1 * stride]
               +
               10.0 * Math.Pow(inv, 3.0) * Math.Pow(value, 2.0) * spline[offset + 2 * stride]
               +
               10.0 * Math.Pow(inv, 2.0) * Math.Pow(value, 3.0) * spline[offset + 3 * stride]
               +
               5.0 * inv * Math.Pow(value, 4.0) * spline[offset + 4 * stride]
               +
               Math.Pow(value, 5.0) * spline[offset + 5 * stride];
    }

    private static double Evaluate(double[] dataset, int offset, int stride, float turbidity, float albedo, float sunTheta)
    {
        // Splines are functions of elevation^1/3.
        var elevationK = Math.Pow(Math.Max(0.0, 1.0 - sunTheta / (Math.PI / 2.0)), 1.0 / 3.0);

        // Turbidity table is 1..10.
        var turbidity0 = Math.Clamp((int)turbidity, 1, 10);
        var turbidity1 = Math.Min(turbidity0 + 1, 10);
        var turbidityK = Math.Clamp(turbidity - turbidity0, 0.0, 1.0);

        // First 10 turbidity sets = albedo 0.
        //
        // Second 10 turbidity sets = albedo 1.
        //
        // Each set contains 6 spline coefficients.

        var datasetA0 = offset;
        var datasetA1 = offset + stride * 6 * 10;

        var t0Offset = stride * 6 * (turbidity0 - 1);
        var t1Offset = stride * 6 * (turbidity1 - 1);

        var a0t0 = EvaluateSpline(
            dataset,
            datasetA0 + t0Offset,
            stride,
            elevationK
        );

        var a1t0 = EvaluateSpline(
            dataset,
            datasetA1 + t0Offset,
            stride,
            elevationK
        );

        var a0t1 = EvaluateSpline(
            dataset,
            datasetA0 + t1Offset,
            stride,
            elevationK
        );

        var a1t1 = EvaluateSpline(
            dataset,
            datasetA1 + t1Offset,
            stride,
            elevationK
        );

        return
            a0t0 * (1.0 - albedo) * (1.0 - turbidityK)
            +
            a1t0 * albedo * (1.0 - turbidityK)
            +
            a0t1 * (1.0 - albedo) * turbidityK
            +
            a1t1 * albedo * turbidityK;
    }

    private static Vector3 HosekWilkie(float cosTheta, float gamma, float cosGamma, HosekWilkieParams skyParams)
    {
        var h2 = skyParams.H * skyParams.H;

        var denominator = Vector3.One + h2 -
                          2.0f * cosGamma * skyParams.H;

        var chi = (1.0f + cosGamma * cosGamma)
                  *
                  new Vector3(
                      1.0f / MathF.Pow(denominator.X, 1.5f),
                      1.0f / MathF.Pow(denominator.Y, 1.5f),
                      1.0f / MathF.Pow(denominator.Z, 1.5f)
                  );

        var expB = 
            new Vector3(
                MathF.Exp(skyParams.B.X / (cosTheta + 0.01f)),
                MathF.Exp(skyParams.B.Y / (cosTheta + 0.01f)),
                MathF.Exp(skyParams.B.Z / (cosTheta + 0.01f))
            );

        var expE =
            new Vector3(
                MathF.Exp(skyParams.E.X * gamma),
                MathF.Exp(skyParams.E.Y * gamma),
                MathF.Exp(skyParams.E.Z * gamma)
            );

        var result = (Vector3.One + skyParams.A * expB)
                     *
                     (skyParams.C +
                      skyParams.D * expE +
                      skyParams.F * (cosGamma * cosGamma) +
                      skyParams.G * chi +
                      skyParams.I * MathF.Sqrt(MathF.Max(0.0f, cosTheta)));

        return result;
    }

    public Vector3 GetSkyRadiance(Vector3 viewDirection)
    {
        viewDirection = Vector3.Normalize(viewDirection);

        var sunDirection = Vector3.Transform(Vector3.UnitY, Engine.LightManager.Sun!.Source.Rotation);

        // cos(theta)
        var cosTheta = Math.Clamp(viewDirection.Y, 0.0f, 1.0f);

        // Angle between sun and view.
        var cosGamma = Math.Clamp(Vector3.Dot(sunDirection, viewDirection), -1.0f, 1.0f);

        var sky = HosekWilkie(cosTheta, MathF.Acos(cosGamma), cosGamma, _params);

        // Apply RGB radiance coefficients.
        return sky * _params.Z;
    }
}