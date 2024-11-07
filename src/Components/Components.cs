using System.Numerics;

namespace Ball;

public readonly record struct Position(Vector2 value);
public readonly record struct Velocity(Vector2 value);
public readonly record struct Orientation(float value);
public readonly record struct Scale(float value);
public readonly record struct Model(int ID);
public readonly record struct BoundingBox(float X, float Y, float Width, float Height);
public readonly record struct Circle(float Radius);
public readonly record struct SolidCollision();
public readonly record struct HitBall();
public readonly record struct Player();
public readonly record struct Bounce();
public readonly record struct CanBeHit();
public readonly record struct HasGravity();
public readonly record struct ResetBallOnHit();
public readonly record struct DestroyOnContactWithBall();
public readonly record struct WorldOffset(float Y);
public readonly record struct IgnoreWorldOffset();