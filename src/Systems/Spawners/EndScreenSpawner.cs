using System.Numerics;
using MoonTools.ECS;

namespace Ball;

public class EndScreenSpawner : Manipulator
{
    Filter DestroyFilter;
    Filter DontDestroyFilter;
    MarqueeSpawner MarqueeSpawner;


    public EndScreenSpawner(World world) : base(world)
    {
        DestroyFilter = FilterBuilder.Include<DestroyOnStateTransition>().Exclude<DontDestroyOnNextTransition>().Build();
        DontDestroyFilter = FilterBuilder
            .Include<DestroyOnStateTransition>()
            .Exclude<Player>()
            .Exclude<Block>()
            .Exclude<GivesXP>()
            .Exclude<CanDealDamageToBlock>()
            .Exclude<CanTakeDamage>()
            .Exclude<ComboText>()
            .Exclude<Power>()
            .Exclude<HasGravity>()
            .Exclude<DontDestroyOnNextTransition>()
            .Build();

        MarqueeSpawner = new MarqueeSpawner(world);
    }

    public void OpenEndScreen()
    {
        foreach (var entity in DontDestroyFilter.Entities)
        {
            Set(entity, new DontDestroyOnNextTransition());
        }

        foreach (var entity in DestroyFilter.Entities)
        {
            Destroy(entity);
        }

        var es = CreateEntity();
        Set(es, new EndScreen());
        Set(es, new DestroyOnStateTransition());

        MarqueeSpawner.SpawnMarquee("WINNER!", Fonts.HeaderFont, Fonts.HeaderSize, 2, 100f, Dimensions.GameHeight * 0.4f);

        MarqueeSpawner.SpawnMarquee("PRESS START TO QUIT", Fonts.BodyFont, Fonts.PromptSize, 2, -100f, Dimensions.GameHeight * 0.5f);

    }
}