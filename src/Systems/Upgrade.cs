
using System.Numerics;
using MoonTools.ECS;

namespace Ball;

public class Upgrade : MoonTools.ECS.System
{
    Filter CleanupFilter;
    UpgradeMenuSpawner UpgradeMenuSpawner;

    public Upgrade(World world) : base(world)
    {
        CleanupFilter = FilterBuilder.Include<DestroyWhenLeavingUpgradeMenu>().Build();
        UpgradeMenuSpawner = new UpgradeMenuSpawner(world);
    }

    Entity CreateSelector()
    {
        var selector = CreateEntity();
        Set(selector, new Position(Vector2.Zero));
        Set(selector, new Model(Content.Models.Triangle.ID));
        Set(selector, new Depth(0.1f));
        Set(selector, new FollowsCamera(0f));
        Set(selector, new Selector());
        Set(selector, new KeepOpacityWhenPaused());
        Set(selector, new DestroyWhenLeavingUpgradeMenu());

        return selector;
    }

    bool GiveUpgrade(Entity upgrade)
    {
        var type = Get<UpgradeOption>(upgrade).Upgrade;

        if (type == Upgrades.ChainReaction)
        {
            Set(GetSingletonEntity<Player>(), new DestroyedBlocksDamageNeighbors());
        }
        else if (type == Upgrades.Combo)
        {
            Set(GetSingletonEntity<Player>(), new ComboBuilder());

        }
        else if (type == Upgrades.Confidence)
        {
            Set(GetSingletonEntity<Player>(), new BlocksSpawnWithLessHealth());

        }
        else if (type == Upgrades.Invictus)
        {
            Set(GetSingletonEntity<Player>(), new ReviveWithOneHealth(true));
        }
        else if (type == Upgrades.MedSchool)
        {

        }
        else if (type == Upgrades.OptimalHealth)
        {
            Set(GetSingletonEntity<Player>(), new DoubleDamageOnOneLife());
        }
        else if (type == Upgrades.Piercing)
        {
            Set(GetSingletonEntity<Player>(), new PiercingBalls());
        }
        else if (type == Upgrades.Emergency)
        {
            var entity = GetSingletonEntity<Lives>();
            var lives = Get<Lives>(entity);
            Set(entity, new Lives(lives.Value + 3));
        }
        else if (type == Upgrades.Refresh)
        {
            foreach (var entity in CleanupFilter.Entities)
            {
                Destroy(entity);
            }

            UpgradeMenuSpawner.OpenUpgradeMenu();
            return false;
        }
        else if (type == Upgrades.Revenge)
        {
            Set(GetSingletonEntity<Player>(), new DamageBlocksOnLostLife());
        }
        else if (type == Upgrades.Safety)
        {
            Set(GetSingletonEntity<Player>(), new BarrierTakesExtraHit(true));

        }
        else if (type == Upgrades.Bonus)
        {
            Set(GetSingletonEntity<Player>(), new BlocksSpawnBonusBalls());
        }

        return true;
    }

    public override void Update(TimeSpan delta)
    {
        if (Some<Selected>())
        {
            var inputState = GetSingleton<InputState>();

            var selected = GetSingletonEntity<Selected>();
            var selector = Some<Selector>() ? GetSingletonEntity<Selector>() : CreateSelector();

            Set(selector, new Position(new Vector2(Get<Position>(selected).Value.X, Get<Position>(selected).Value.Y - 20f)));
            Set(selector, new FollowsCamera(Get<Position>(selected).Value.Y - 20));

            if (inputState.Right.IsPressed)
            {
                if (HasOutRelation<HorizontalConnection>(selected))
                {
                    Remove<Flicker>(selected);
                    Remove<Selected>(selected);
                    Set(OutRelationSingleton<HorizontalConnection>(selected), new Selected());
                }
            }
            if (inputState.Left.IsPressed)
            {
                if (HasInRelation<HorizontalConnection>(selected))
                {
                    Remove<Flicker>(selected);
                    Remove<Selected>(selected);
                    Set(InRelationSingleton<HorizontalConnection>(selected), new Selected());
                }
            }
            if (inputState.Swing.IsPressed)
            {
                if (GiveUpgrade(selected))
                {
                    foreach (var entity in CleanupFilter.Entities)
                    {
                        Destroy(entity);
                    }
                }
            }
        }
    }
}