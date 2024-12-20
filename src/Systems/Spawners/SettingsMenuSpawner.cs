using System.Numerics;
using Ball;
using MoonTools.ECS;

public class SettingsMenuSpawner : Manipulator
{
    MainMenuSpawner MainMenuSpawner;
    Filter DestroyFilter;
    Filter DontDestroyFilter;

    public SettingsMenuSpawner(World world) : base(world)
    {
        MainMenuSpawner = new MainMenuSpawner(world);

        DestroyFilter = FilterBuilder.Include<DestroyOnStateTransition>().Exclude<DontDestroyOnNextTransition>().Build();
        DontDestroyFilter = FilterBuilder.Include<DestroyOnStateTransition>().Include<DontDestroyOnNextTransition>().Build();

    }
    public void CloseSettingsMenu()
    {
        foreach (var entity in DestroyFilter.Entities)
        {
            Destroy(entity);
        }

        foreach (var entity in DontDestroyFilter.Entities)
        {
            Remove<DontDestroyOnNextTransition>(entity);
        }
    }

    public void OpenSettingsMenu()
    {
        foreach (var entity in DestroyFilter.Entities)
        {
            Destroy(entity);
        }

        var promptEntity = CreateEntity();
        Set(promptEntity,
        new Position(new Vector2(Dimensions.GameWidth * 0.5f, Dimensions.GameHeight * 0.25f)));
        Set(promptEntity,
         new Text(
            Fonts.HeaderFont,
            Fonts.HeaderSize,
            Stores.TextStorage.GetID("SETTINGS"),
            MoonWorks.Graphics.Font.HorizontalAlignment.Center,
            MoonWorks.Graphics.Font.VerticalAlignment.Middle));
        Set(promptEntity, new KeepOpacityWhenPaused());
        Set(promptEntity, new Pause());
        Set(promptEntity, new Depth(0.1f));
        Set(promptEntity, new Marquee(100f));
        Set(promptEntity, new FollowsCamera(Dimensions.GameHeight * 0.25f));
        Set(promptEntity, new DestroyOnStateTransition());
        Set(promptEntity, new SettingsMenu());

        var text = Get<Text>(promptEntity);
        var font = Stores.FontStorage.Get(text.FontID);
        var str = Stores.TextStorage.Get(text.TextID);
        WellspringCS.Wellspring.Rectangle rect;
        font.TextBounds(str, text.Size, text.HorizontalAlignment, text.VerticalAlignment, out rect);

        var promptDouble = CreateEntity();
        Set(promptDouble,
            new Position(new Vector2(Dimensions.GameWidth * 0.5f - rect.W - text.Size, Dimensions.GameHeight * 0.25f)));
        Set(promptDouble,
         new Text(
            Fonts.HeaderFont,
            Fonts.HeaderSize,
            Stores.TextStorage.GetID("SETTINGS"),
            MoonWorks.Graphics.Font.HorizontalAlignment.Center,
            MoonWorks.Graphics.Font.VerticalAlignment.Middle));
        Set(promptDouble, new KeepOpacityWhenPaused());
        Set(promptDouble, new Depth(0.1f));
        Set(promptDouble, new Marquee(100f));
        Set(promptDouble, new FollowsCamera(Dimensions.GameHeight * 0.25f));
        Set(promptDouble, new DestroyOnStateTransition());

    }

}