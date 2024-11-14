using System.Numerics;

public enum CollisionDirection
{
    X, Y
}

public readonly record struct Bouncing();
public readonly record struct Colliding(CollisionDirection Direction, bool Solid);
public readonly record struct IgnoreSolidCollision();
public readonly record struct NextLife();
public readonly record struct HeldBy(Vector2 offset);
public readonly record struct ChildOf(Vector2 offset);