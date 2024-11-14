using System.Numerics;
using System.Runtime.InteropServices;
using MoonWorks.Graphics;

namespace Ball;

[StructLayout(LayoutKind.Sequential)]
public struct TransformVertexUniform
{
    public Matrix4x4 ViewProjection;
    public Vector4 Color;

    public TransformVertexUniform(Matrix4x4 viewProjection, Color color)
    {
        Color = color.ToVector4();
        ViewProjection = viewProjection;
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct PositionVertex : IVertexType
{
    public Vector3 Position;

    public PositionVertex(Vector3 position)
    {
        Position = position;
    }

    public static VertexElementFormat[] Formats { get; } =
    [
        VertexElementFormat.Float3
    ];

    public static uint[] Offsets { get; } = [0];

    public override string ToString()
    {
        return Position.ToString();
    }
}
