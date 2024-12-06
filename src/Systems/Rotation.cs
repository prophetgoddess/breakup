using Ball;
using MoonTools.ECS;

public class Rotation : MoonTools.ECS.System
{
    Filter RotationFilter;

    public Rotation(World world) : base(world)
    {
        RotationFilter = FilterBuilder.Include<Orientation>().Include<AngularVelocity>().Build();
    }

    public override void Update(TimeSpan delta)
    {
        if (Some<Pause>())
            return;

        foreach (var entity in RotationFilter.Entities)
        {
            var orientation = Get<Orientation>(entity).Value;
            var angularVelocity = Get<AngularVelocity>(entity).Value;

            orientation += angularVelocity * (float)delta.TotalSeconds;

            Set(entity, new Orientation(orientation));
        }
    }
}