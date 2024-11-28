using System.Numerics;
using MoonTools.ECS;
using MoonWorks.Math;

namespace Ball;

public class PowerMeter : MoonTools.ECS.System
{
    Filter MeterFilter;

    public PowerMeter(World world) : base(world)
    {
        MeterFilter = FilterBuilder.Include<Power>().Include<Scale>().Build();
    }

    public override void Update(TimeSpan delta)
    {
        foreach (var entity in MeterFilter.Entities)
        {
            var meter = Get<Power>(entity);
            var value = meter.Value;
            if (!HasOutRelation<LockMeter>(entity))
            {
                if (Has<Flicker>(entity))
                {
                    Remove<Flicker>(entity);
                    Remove<Invisible>(entity);
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

            Set(entity, new Scale(new Vector2(float.Lerp(0f, meter.Scale, meter.Value), 2f)));
            Set(entity, new Power(value, meter.Decay, meter.Scale));
        }
    }
}