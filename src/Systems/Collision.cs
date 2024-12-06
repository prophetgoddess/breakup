
using System.Numerics;
using MoonTools.ECS;

namespace Ball;

public class Collision : MoonTools.ECS.System
{
    Filter CollidingFilter;

    public Collision(World world) : base(world)
    {
        CollidingFilter = FilterBuilder.Include<SolidCollision>().Include<Position>().Include<BoundingBox>().Build();
    }

    public override void Update(TimeSpan delta)
    {
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

            var xpEntity = GetSingletonEntity<XP>();
            var xp = Get<XP>(xpEntity);
            var currentXP = xp.Current;
            var targetXP = xp.Target;

            currentXP += Get<GivesXP>(other).Amount;
            if (currentXP >= xp.Target)
            {
                targetXP *= 2;
                currentXP = 0;
                var levelEntity = GetSingletonEntity<Level>();
                var level = Get<Level>(levelEntity);
                Set(levelEntity, new Level(level.Value + 1));
            }
            Set(xpEntity, new XP(currentXP, targetXP));

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
                var hitPoints = Get<HitPoints>(other);
                Set(other, new HitPoints(hitPoints.Value - damage, hitPoints.Max));
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
            var meterValue = GetSingleton<Power>().Value * 200.0f;
            var otherPos = Get<Position>(other).Value;
            var dir = Vector2.Normalize(otherPos - position);
            velocity = dir * -velocity.Length();

            if (HasOutRelation<Spinning>(other))
                velocity.Y -= 200.0f + meterValue;

            Set(entity, new Velocity(velocity));
        }
    }
}