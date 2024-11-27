using System.Numerics;
using MoonTools.ECS;
using MoonWorks;
using MoonWorks.Math;

namespace Ball;

public class XPMeter : MoonTools.ECS.System
{
    public XPMeter(World world) : base(world)
    {
    }

    public override void Update(TimeSpan delta)
    {
        if (!Some<XP>())
            return;

        var xpEntity = GetSingletonEntity<XP>();
        var xp = Get<XP>(xpEntity);

        Set(xpEntity, new Scale(new Vector2(float.Lerp(0f, Dimensions.GameWidth - 16, (float)xp.Current / xp.Target), 2f)));
    }
}