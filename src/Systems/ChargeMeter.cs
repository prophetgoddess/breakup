using System.Numerics;
using MoonTools.ECS;

namespace Ball;

public class ChargeMeter : MoonTools.ECS.System
{

    public ChargeMeter(World world) : base(world)
    {
    }

    public override void Update(TimeSpan delta)
    {
        if (!Some<Charge>())
            return;

        var entity = GetSingletonEntity<Charge>();

        var meter = Get<Charge>(entity);
        var value = meter.Value;

        if (value >= 1.0f && !Has<Flicker>(entity))
        {
            Set(entity, new Flicker(0.05f));
        }
        else if (value <= 0)
        {
            Remove<Flicker>(entity);
            Remove<Invisible>(entity);
        }

        Set(entity, new Scale(Vector2.One * float.Lerp(0f, meter.Scale, meter.Value)));

    }
}