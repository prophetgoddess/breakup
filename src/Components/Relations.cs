using System.Numerics;

public enum CollisionDirection
{
    X, Y
}

public readonly record struct Dashing();
public readonly record struct Spinning();
public readonly record struct Colliding(CollisionDirection Direction, bool Solid);
public readonly record struct IgnoreSolidCollision();
public readonly record struct NextLife();
public readonly record struct HeldBy(Vector2 offset);
public readonly record struct ChildOf(Vector2 offset);
public readonly record struct DontMoveTowardsPlayer();
public readonly record struct LockMeter();
public readonly record struct FlickerTimer();
public readonly record struct HPDisplay();
public readonly record struct TrailTimer();