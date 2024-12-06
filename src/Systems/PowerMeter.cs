using System.Diagnostics.Metrics;
using System.Numerics;
using MoonTools.ECS;
using MoonWorks.Math;

namespace Ball;

public class PowerMeter : MoonTools.ECS.System
{

    public PowerMeter(World world) : base(world)
    {
    }

    public override void Update(TimeSpan delta)
    {
        if (Some<Pause>())
            return;

        if (!Some<Power>())
            return;

        var entity = GetSingletonEntity<Power>();
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
                Set(timer, new Timer(10.0f));
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
        Set(entity, new Power(value, meter.Decay, meter.Scale));
    }
}