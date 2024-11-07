public enum CollisionDirection
{
    X, Y
}

public readonly record struct Spinning();
public readonly record struct Colliding(CollisionDirection Direction, bool Solid);
public readonly record struct IgnoreSolidCollision();
public readonly record struct NextLife();