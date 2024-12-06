using System.Numerics;
using MoonTools.ECS;
using MoonWorks;
using MoonWorks.Math;

namespace Ball;

public class Stars : MoonTools.ECS.System
{
    Filter BallFilter;

    public Stars(World world) : base(world)
    {
        BallFilter = FilterBuilder.Include<DamageMultiplier>().Build();
    }

    public Entity SpawnStar()
    {
        var entity = CreateEntity();

        Set(entity, new Model(Content.Models.Star.ID));
        Set(entity, new Position(Vector2.Zero));
        Set(entity, new Orientation(Rando.Range(0f, MathF.PI * 2f)));
        Set(entity, new Scale(new Vector2(8, 8)));
        Set(entity, new DestroyOnStartGame());
        Set(entity, new Highlight());
        Set(entity, new AngularVelocity(Rando.Range(-5f, 5f)));
        Set(entity, new Depth(0.01f));
        Set(entity, new Timer(0.2f));
        return entity;
    }


    public override void Update(TimeSpan delta)
    {
        foreach (var entity in BallFilter.Entities)
        {
            var star = SpawnStar();
            Set(star, new Position(Get<Position>(entity).Value + Rando.InsideUnitCircle() * 8.0f));
        }
    }
}