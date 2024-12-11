
using System.Numerics;
using MoonTools.ECS;
using MoonWorks.Math;

namespace Ball;

public class Collision : MoonTools.ECS.System
{
    Filter CollidingFilter;

    XPAndLevel XPAndLevel;

    public Collision(World world) : base(world)
    {
        CollidingFilter = FilterBuilder.Include<SolidCollision>().Include<Position>().Include<BoundingBox>().Build();
        XPAndLevel = new XPAndLevel(world);
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

                HandleBounce(entity, other, collision, xCollision, yCollision);

                HandleDestroyBall(entity, other);

                HandleHitBall(entity, other);

                HandleGems(entity, other);

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

            Destroy(other);
        }

    }

    void HandleBounce(Entity entity, Entity other, Colliding collision, bool xCollision, bool yCollision)
    {
        var velocity = Get<Velocity>(entity).Value;
        var position = Get<Position>(entity).Value;

        if (Has<Bounce>(entity) && collision.Solid)
        {
            if (Has<CanTakeDamageFromBall>(other) && Has<HitPoints>(other) && Has<CanDealDamageToBlock>(entity))
            {
                var damage = Get<CanDealDamageToBlock>(entity).Amount;
                if (Has<DamageMultiplier>(entity))
                {
                    damage *= Get<DamageMultiplier>(entity).Multiplier;
                    Remove<DamageMultiplier>(entity);
                }
                var hitPoints = Get<HitPoints>(other);
                Set(other, new HitPoints(hitPoints.Value - damage, hitPoints.Max));
            }
            else if (Has<CanDealDamageToBlock>(entity) && !Has<HitBall>(other))
            {
                Set(CreateEntity(), new PlayOnce(Stores.SFXStorage.GetID(Content.SFX.clink)));
            }

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

    void HandleDestroyBall(Entity entity, Entity other)
    {
        if (Has<Bounce>(entity) && Has<ResetBallOnHit>(other))
        {
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
            }

            Set(entity, new Velocity(velocity));
        }
    }
}