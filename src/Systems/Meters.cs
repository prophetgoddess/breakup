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
            if (!HasOutRelation<LockMeter>(entity))
            {
                if (Has<Flicker>(entity))
                {
                    Remove<Flicker>(entity);
                }
                value -= (float)delta.TotalSeconds * meter.Decay;

                if (value >= 1.0f)
                {
                    var timer = CreateEntity();
                    Set(timer, new Timer(2.0f));
                    Set(entity, new Flicker(0.05f));
                    Relate(entity, timer, new LockMeter());
                }
            }
            else if (value <= 0)
            {
                Remove<Flicker>(entity);
                Remove<Invisible>(entity);
                UnrelateAll<LockMeter>(entity);
            }

            value = Math.Clamp(value, 0, 1f);
            var scale = Get<Scale>(entity).Value;

            Set(entity, new Scale(Vector2.One * float.Lerp(0f, meter.Scale, meter.Value)));
            Set(entity, new Meter(value, meter.Decay, meter.Scale));


        }
    }
}