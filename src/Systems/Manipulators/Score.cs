using MoonTools.ECS;

namespace Ball;

public class Scorer : Manipulator
{
    public Scorer(World world) : base(world)
    {
    }

    public readonly record struct AddScore(int Amount)
    {

    }
}