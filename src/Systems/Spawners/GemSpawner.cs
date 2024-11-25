using MoonWorks.Graphics;
using System.Numerics;
using Ball;
using MoonTools.ECS;

public class GemSpawner : Manipulator
{
    public GemSpawner(World world) : base(world)
    {
    }

    public Entity SpawnGem()
    {
        var entity = CreateEntity();

        Set(entity, new Model(Content.Models.Triangle.ID));
        Set(entity, new Position(new Vector2(
                Dimensions.GameWidth * 0.5f,
                Dimensions.GameHeight * 0.9f
            )));
        Set(entity, new Orientation(Rando.Range(0f, MathF.PI * 2f)));
        Set(entity, new Velocity(Rando.InsideUnitCircle() * 100.0f));
        Set(entity, new BoundingBox(0, 0, 8, 8));
        Set(entity, new Scale(new Vector2(1, 1)));
        Set(entity, new DestroyOnStartGame());
        Set(entity, new MoveTowardsPlayer(500.0f, 500.0f));
        Set(entity, new FillMeter(0.066f));
        Set(entity, new Highlight());
        Set(entity, new AngularVelocity(Rando.Range(-5f, 5f)));
        Set(entity, new AddGems(1));

        var timer = CreateEntity();
        Set(timer, new Timer(Rando.Range(1f, 2f)));
        Relate(entity, timer, new DontMoveTowardsPlayer());

        return entity;
    }

    public void SpawnGems(int numGems, Vector2 position)
    {
        for (var i = 0; i < numGems; i++)
        {
            var gem = SpawnGem();
            Set(gem, new Position(position + Rando.InsideUnitCircle() * 10.0f));
        }
    }
}