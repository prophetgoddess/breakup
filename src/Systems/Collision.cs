
using System.Numerics;
using MoonTools.ECS;
using MoonWorks.Math;

namespace Ball;

public class Collision : MoonTools.ECS.System
{
    Filter CollidingFilter;
    Filter BlocksFilter;

    XPAndLevel XPAndLevel;

    public Collision(World world) : base(world)
    {
        CollidingFilter = FilterBuilder.Include<Position>().Include<BoundingBox>().Build();
        XPAndLevel = new XPAndLevel(world);
        BlocksFilter = FilterBuilder
        .Include<Block>()
        .Include<HitPoints>()
        .Build();
    }

    public override void Update(TimeSpan delta)
    {
        if (Some<Pause>())
            return;

        foreach (var entity in CollidingFilter.Entities)
        {
            foreach (var other in OutRelations<Colliding>(entity))
            {
                var collision = GetRelationData<Colliding>(entity, other);
                var xCollision = collision.Direction == CollisionDirection.X;
                var yCollision = collision.Direction == CollisionDirection.Y;

                HandleDamage(entity, other);

                HandleBounce(entity, other, collision, xCollision, yCollision);

                HandleHitBall(entity, other);

                HandleGems(entity, other);

                HandleExtraLife(entity, other);

                HandleDestroyBall(entity, other);

                Unrelate<Colliding>(entity, other);

            }
        }
    }

    void HandleGems(Entity entity, Entity other)
    {
        if (Has<FillMeter>(other) && Has<AddGems>(other) && Has<Player>(entity))
        {
            var meterEntity = GetSingletonEntity<Power>();
            var meter = Get<Power>(meterEntity);
            var value = meter.Value;
            value += Get<FillMeter>(other).Amount;

            Set(meterEntity, new Power(value, meter.Decay, meter.Scale));

            var gemsEntity = GetSingletonEntity<Gems>();
            var gems = Get<Gems>(gemsEntity);
            var total = gems.Total;
            total += Get<AddGems>(other).Amount;
            var current = gems.Current;
            current += Get<AddGems>(other).Amount;
            Set(gemsEntity, new Gems(current, total));

            XPAndLevel.GiveXP(Get<GivesXP>(other).Amount);
            Set(CreateEntity(), new PlayOnce(Stores.SFXStorage.GetID(Content.SFX.gemcollect)));

            Destroy(other);
        }

    }

    void HandleExtraLife(Entity entity, Entity other)
    {
        if (Has<GivesExtraLife>(other) && Has<Player>(entity))
        {
            var l = GetSingletonEntity<Lives>();
            var lives = Get<Lives>(l);
            Set(l, new Lives(lives.Value + (Some<BonusLives>() ? 2 : 1)));
            Destroy(other);
        }
    }

    void HandleDamage(Entity entity, Entity other)
    {
        if (Has<CanTakeDamage>(other) && Has<HitPoints>(other) && Has<CanDealDamageToBlock>(entity))
        {
            var damage = Get<CanDealDamageToBlock>(entity).Amount;
            if (Has<DamageMultiplier>(entity))
            {
                damage *= Get<DamageMultiplier>(entity).Multiplier;
                Remove<DamageMultiplier>(entity);
            }

            if (Some<DoubleDamageOnOneLife>() && GetSingleton<Lives>().Value == 1 && Has<Bounce>(entity))
            {
                damage *= 2;
            }

            if (Has<ComboBuilder>(entity))
            {
                damage += Get<ComboBuilder>(entity).Combo;
            }

            var hitPoints = Get<HitPoints>(other);
            Set(other, new HitPoints(hitPoints.Value - damage, hitPoints.Max));

            var numberEntity = CreateEntity();
            Set(numberEntity, new Position(Get<Position>(other).Value));
            Set(numberEntity, new Velocity(
                new Vector2(
                    Rando.Range(-100f, 100f),
                    Rando.Range(20f, 100f)
                )
            ));
            Set(numberEntity, new Depth(0.01f));
            Set(numberEntity, new Text(Fonts.BodyFont, Fonts.InfoSize, Stores.TextStorage.GetID($"{damage}"), MoonWorks.Graphics.Font.HorizontalAlignment.Center, MoonWorks.Graphics.Font.VerticalAlignment.Middle));
            Set(numberEntity, new Timer(2f));
            Set(numberEntity, new Highlight());
            Set(numberEntity, new HasGravity(1f));

            if (hitPoints.Value - damage > 0)
            {
                Set(CreateEntity(), new PlayOnce(Stores.SFXStorage.GetID(Content.SFX.blockhit)));
            }
            else if (Has<ComboBuilder>(entity))
            {
                var combo = Get<ComboBuilder>(entity);
                Set(entity, new ComboBuilder(combo.Combo + 1));
            }
        }
    }

    void HandleBounce(Entity entity, Entity other, Colliding collision, bool xCollision, bool yCollision)
    {
        var velocity = Get<Velocity>(entity).Value;
        var position = Get<Position>(entity).Value;

        if (Has<Bounce>(entity) && collision.Solid)
        {
            if (Has<CanDealDamageToBlock>(entity) && !Has<HitBall>(other))
            {
                Set(CreateEntity(), new PlayOnce(Stores.SFXStorage.GetID(Content.SFX.clink), true));
            }

            if (Has<ComboBuilder>(other) && Has<Player>(other))
            {
                Set(entity, new ComboBuilder(0));
            }

            if (!(Some<PiercingBalls>() && Has<HitPoints>(other) && Get<HitPoints>(other).Value <= 0))
            {
                var newVelocity = velocity;

                if (xCollision && !yCollision)
                    newVelocity = new Vector2(-velocity.X, velocity.Y) * Get<Bounce>(entity).Coefficient;

                else if (yCollision && !xCollision)
                    newVelocity = new Vector2(velocity.X, -velocity.Y) * Get<Bounce>(entity).Coefficient;

                else if (yCollision && xCollision)
                    newVelocity = new Vector2(-velocity.X, -velocity.Y) * Get<Bounce>(entity).Coefficient;

                var otherPos = Get<Position>(other).Value;
                if (position.Y < otherPos.Y && !Has<Player>(other))
                {
                    newVelocity.Y += Rando.Range(-100f, 0f);
                    newVelocity.X += Rando.Range(-50f, 50f);
                }

                Set(entity, new Velocity(newVelocity));
            }
        }
    }

    void HandleDestroyBall(Entity entity, Entity other)
    {
        if (Has<Bounce>(entity) && Has<ResetBallOnHit>(other))
        {
            if (Has<DontLoseLife>(entity))
            {
                Set(entity, new Timer(0f));
            }
            else
            {
                if (Some<BarrierTakesExtraHit>() && GetSingleton<BarrierTakesExtraHit>().Active)
                {
                    Set(GetSingletonEntity<BarrierTakesExtraHit>(), new BarrierTakesExtraHit(false));
                    return;
                }
                else if (Some<BarrierTakesExtraHit>() && !GetSingleton<BarrierTakesExtraHit>().Active)
                {
                    Set(GetSingletonEntity<BarrierTakesExtraHit>(), new BarrierTakesExtraHit(true));
                }

                var player = GetSingletonEntity<Player>();
                Relate(entity, player, new HeldBy(new Vector2(0f, -32.0f)));
                Set(entity, new Velocity(Vector2.Zero));
                Unrelate<Colliding>(entity, other);
                Relate(entity, player, new IgnoreSolidCollision());

                var meterEntity = GetSingletonEntity<Power>();
                var meter = Get<Power>(meterEntity);
                Set(meterEntity, new Power(0f, meter.Decay, meter.Scale));

                var livesEntity = GetSingletonEntity<Lives>();
                var lives = Get<Lives>(livesEntity);

                Set(livesEntity, new Lives(lives.Value - 1));

                Set(CreateEntity(), new PlayOnce(Stores.SFXStorage.GetID(Content.SFX.fail)));

                if (Some<DamageBlocksOnLostLife>())
                {
                    var dmg = Get<CanDealDamageToBlock>(entity).Amount;
                    foreach (var block in BlocksFilter.Entities)
                    {
                        var hitPoints = Get<HitPoints>(block);
                        Set(block, new HitPoints(hitPoints.Value - dmg, hitPoints.Max));
                    }
                }
            }
        }
    }

    void HandleHitBall(Entity entity, Entity other)
    {
        var position = Get<Position>(entity).Value;
        var velocity = Get<Velocity>(entity).Value;

        if (Has<HitBall>(other) && Has<CanBeHit>(entity))
        {
            var otherPos = Get<Position>(other).Value;
            var dir = Vector2.Normalize(otherPos - position);
            velocity = dir * -velocity.Length();

            var meterEntity = GetSingletonEntity<Power>();
            var meter = Get<Power>(meterEntity);

            if (HasOutRelation<Spinning>(other))
            {
                velocity.Y -= 300.0f;
                if (meter.Value >= 1.0f)
                {
                    Set(entity, new DamageMultiplier(2));
                }
                Set(CreateEntity(), new PlayOnce(Stores.SFXStorage.GetID(Content.SFX.boing)));
            }

            Set(entity, new Velocity(velocity));
        }
    }
}