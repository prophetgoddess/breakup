using System.Numerics;
using System.Runtime.InteropServices;
using MoonWorks.Graphics;

namespace Ball;
public struct TransformVertexUniform
{
    public Matrix4x4 ViewProjection;

    public TransformVertexUniform(Matrix4x4 viewProjection)
    {
        ViewProjection = viewProjection;
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct PositionColorVertex : IVertexType
{
    public Vector3 Position;
    public Color Color;

    public PositionColorVertex(Vector3 position, Color color)
    {
        Position = position;
        Color = color;
    }

    public static VertexElementFormat[] Formats { get; } =
    [
        VertexElementFormat.Float3,
        VertexElementFormat.Ubyte4Norm
    ];

    public static uint[] Offsets { get; } =
    [
        0,
        12
    ];

    public override string ToString()
    {
        return Position + " | " + Color;
    }
}