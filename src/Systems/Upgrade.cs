
using System.Numerics;
using MoonTools.ECS;
using MoonWorks;

namespace Ball;

public class Upgrade : MoonTools.ECS.System
{
    Filter CleanupFilter;
    UpgradeMenuSpawner UpgradeMenuSpawner;
    GiveUpgrade GiveUpgrade;

    public Upgrade(World world) : base(world)
    {
        CleanupFilter = FilterBuilder.Include<DestroyOnStateTransition>().Exclude<DontDestroyOnNextTransition>().Build();
        UpgradeMenuSpawner = new UpgradeMenuSpawner(world);
        GiveUpgrade = new GiveUpgrade(world);

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
        Set(selector, new DestroyOnStateTransition());

        return selector;
    }

    public override void Update(TimeSpan delta)
    {
        if (Some<Selected>() && Some<UpgradeOption>())
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
            if (inputState.Launch.IsPressed && !HasOutRelation<CantSelectUpgrade>(selected))
            {
                if (GiveUpgrade.Upgrade(Get<UpgradeOption>(selected).Upgrade))
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