using System.Numerics;

namespace Ball;

public readonly record struct Position(Vector2 value);
public readonly record struct Velocity(Vector2 value);
public readonly record struct Orientation(float value);
public readonly record struct Scale(float value);
public readonly record struct Sprite();
public readonly record struct BoundingBox(float X, float Y, float Width, float Height);
public readonly record struct Circle(float Radius);
public readonly record struct SolidCollision();