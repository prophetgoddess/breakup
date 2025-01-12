using System.Numerics;
using MoonTools.ECS;
using MoonWorks;
using MoonWorks.Math;

namespace Ball;

public class Trail : MoonTools.ECS.System
{
    Filter BallFilter;

    public Trail(World world) : base(world)
    {
        BallFilter = FilterBuilder.Include<DamageMultiplier>().Build();
    }

    public Entity SpawnTrail(Vector4 uv)
    {
        var entity = CreateEntity();

        Set(entity, new SDFGraphic(uv));
        Set(entity, new Position(Vector2.Zero));
        Set(entity, new Scale(new Vector2(8, 8)));
        Set(entity, new DestroyOnStateTransition());
        Set(entity, new Highlight());
        Set(entity, new AngularVelocity(Rando.Range(-5f, 5f)));
        Set(entity, new Depth(0.99f));
        Set(entity, new Timer(0.33f));
        Set(entity, new Alpha(128));
        return entity;
    }

    public override void Update(TimeSpan delta)
    {
        if (Some<Pause>())
            return;

        foreach (var entity in BallFilter.Entities)
        {
            if (!HasOutRelation<TrailTimer>(entity))
            {
                var t = SpawnTrail(Get<SDFGraphic>(entity).UV);
                Set(t, new Scale(Get<Scale>(entity).Value));
                Set(t, new Position(Get<Position>(entity).Value));

                var timer = CreateEntity();
                Set(timer, new Timer(0.05f));
                Relate(entity, timer, new TrailTimer());
            }
        }
    }
}