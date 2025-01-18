

using System.Numerics;
using MoonTools.ECS;
using MoonWorks.Math;

namespace Ball;

public class PlayerAttractor : MoonTools.ECS.System
{

    Filter PlayerAttractionFilter;
    XPAndLevel XPAndLevel;

    public PlayerAttractor(World world) : base(world)
    {
        PlayerAttractionFilter = FilterBuilder.Include<Position>().Include<MoveTowardsPlayer>().Build();
        XPAndLevel = new(world);
    }

    public override void Update(TimeSpan delta)
    {
        if (Some<Pause>())
            return;

        if (!Some<Player>())
            return;

        var player = GetSingletonEntity<Player>();

        var playerPosition = Get<Position>(player).Value;

        foreach (var entity in PlayerAttractionFilter.Entities)
        {
            if (HasOutRelation<DontMoveTowardsPlayer>(entity))
                continue;

            if (!HasOutRelation<MoveTowardsPlayerTimer>(entity))
            {
                if (!Has<MovingTowardsPlayer>(entity))
                {
                    var timerEntity = CreateEntity();
                    Set(timerEntity, new Timer(Rando.Range(0.5f, 1.0f)));
                    Relate(entity, timerEntity, new MoveTowardsPlayerTimer());
                    Set(entity, new MovingTowardsPlayer(Get<Position>(entity).Value));
                    Remove<Velocity>(entity);

                }
                else
                {
                    var meterEntity = GetSingletonEntity<Power>();
                    var meter = Get<Power>(meterEntity);
                    var value = meter.Value;
                    value += Get<FillMeter>(entity).Amount;

                    Set(meterEntity, new Power(value, meter.Decay, meter.Scale));

                    var gemsEntity = GetSingletonEntity<Gems>();
                    var gems = Get<Gems>(gemsEntity);
                    var total = gems.Total;
                    total += Get<AddGems>(entity).Amount;
                    var current = gems.Current;
                    current += Get<AddGems>(entity).Amount;
                    Set(gemsEntity, new Gems(current, total));

                    XPAndLevel.GiveXP(Get<GivesXP>(entity).Amount);
                    Set(CreateEntity(), new PlayOnce(Stores.SFXStorage.GetID(Content.SFX.gemcollect)));

                    Destroy(entity);
                }
                continue;
            }

            var timer = Get<Timer>(OutRelationSingleton<MoveTowardsPlayerTimer>(entity));
            var t = Easing.InQuad(1.0f - timer.Remaining);

            Set(entity, new Position(Vector2.Lerp(Get<MovingTowardsPlayer>(entity).startPosition, playerPosition, t)));
        }
    }
}