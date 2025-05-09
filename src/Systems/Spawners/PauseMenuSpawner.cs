using System.Numerics;
using MoonTools.ECS;
using MoonWorks.Input;

namespace Ball;

public class PauseMenuSpawner : Manipulator
{
    Filter DestroyFilter;
    MarqueeSpawner MarqueeSpawner;


    public PauseMenuSpawner(World world) : base(world)
    {
        DestroyFilter = FilterBuilder.Include<DestroyOnStateTransition>().Exclude<DontDestroyOnNextTransition>().Build();
        MarqueeSpawner = new MarqueeSpawner(world);
    }

    public void OpenPauseMenu()
    {
        foreach (var entity in DestroyFilter.Entities)
        {
            Set(entity, new DontDestroyOnNextTransition());
        }

        var pauseEntity = MarqueeSpawner.SpawnMarquee("PAUSED", Fonts.HeaderFont, Fonts.HeaderSize, 2, 100f, Dimensions.GameHeight * 0.5f);
        Set(pauseEntity, new Pause());
        MarqueeSpawner.SpawnMarquee($"settings: {Input.GetButtonName(Actions.Launch)} / quit: hold {Input.GetButtonName(Actions.Cancel)}", Fonts.BodyFont, Fonts.InfoSize, 2.0f, -100f, Dimensions.GameHeight * 0.6f);


    }
}