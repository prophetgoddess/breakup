using System.Numerics;
using MoonTools.ECS;

namespace Ball;

public class GiveUpgrade : Manipulator
{
    Filter CleanupFilter;

    UpgradeMenuSpawner UpgradeMenuSpawner;

    public GiveUpgrade(World world) : base(world)
    {
        UpgradeMenuSpawner = new UpgradeMenuSpawner(world);
        CleanupFilter = FilterBuilder.Include<DestroyOnStateTransition>().Exclude<DontDestroyOnNextTransition>().Build();

    }

    public bool Upgrade(Upgrades type)
    {
        if (type == Upgrades.ChainReaction)
        {
            Set(GetSingletonEntity<Player>(), new DestroyedBlocksDamageNeighbors());
        }
        else if (type == Upgrades.Combo)
        {
            Set(GetSingletonEntity<Player>(), new ComboAddedToDamage());

        }
        else if (type == Upgrades.Confidence)
        {
            Set(GetSingletonEntity<Player>(), new BlocksSpawnWithLessHealth());

        }
        else if (type == Upgrades.Invictus)
        {
            Set(GetSingletonEntity<Player>(), new ReviveWithOneHealth(true));
            Set(GetSingletonEntity<LivesLabel>(), new HighlightFlicker());
            Set(GetSingletonEntity<LivesLabel>(), new Flicker(0.33f));

        }
        else if (type == Upgrades.MedSchool)
        {
            Set(GetSingletonEntity<Player>(), new BonusLives());
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
            Remove<Invisible>(GetSingletonEntity<DestroysBall>());
        }
        else if (type == Upgrades.Bonus)
        {
            Set(GetSingletonEntity<Player>(), new BlocksSpawnBonusBalls());
        }
        else if (type == Upgrades.Buddy)
        {
            var buddy = CreateEntity();
            Set(buddy, new Model(Content.Models.Square.ID));
            Set(buddy, new Position(new Vector2(
                    Dimensions.GameWidth * 0.5f,
                    Dimensions.GameHeight * 0.75f
                )));
            Set(buddy, new Orientation(0f));
            Set(buddy, new Velocity(Vector2.Zero));
            Set(buddy, new BoundingBox(0, 10, 32, 4));
            Set(buddy, new SolidCollision());
            Set(buddy, new Scale(new Vector2(32, 4)));
            Set(buddy, new FollowsCamera(Dimensions.GameHeight * 0.75f));
            Set(buddy, new DestroyOnStateTransition());
            Set(buddy, new MoveBackAndForth(64, Dimensions.GameWidth - 64, 100f));
        }

        return true;
    }
}