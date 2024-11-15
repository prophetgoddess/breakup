using System.Numerics;
using MoonTools.ECS;
using MoonWorks.Math;

namespace Ball;

public class Meters : MoonTools.ECS.System
{
    Filter MeterFilter;

    public Meters(World world) : base(world)
    {
        MeterFilter = FilterBuilder.Include<Meter>().Include<Scale>().Build();
    }

    public override void Update(TimeSpan delta)
    {
        foreach (var entity in MeterFilter.Entities)
        {
            var meter = Get<Meter>(entity);
            var value = meter.Value;
            value -= (float)delta.TotalSeconds * meter.Decay;
            value = Math.Clamp(value, 0, 1f);
            var scale = Get<Scale>(entity).Value;

            Set(entity, new Scale(Vector2.One * float.Lerp(0f, meter.Scale, meter.Value)));
            Set(entity, new Meter(value, meter.Decay, meter.Scale));
        }
    }
}