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
        new Position(new Vector2(Dimensions.GameWidth * 0.5f, Dimensions.GameHeight * 0.1f)));
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
        Set(promptEntity, new FollowsCamera(Dimensions.GameHeight * 0.1f));
        Set(promptEntity, new DestroyOnStateTransition());

        var text = Get<Text>(promptEntity);
        var font = Stores.FontStorage.Get(text.FontID);
        var str = Stores.TextStorage.Get(text.TextID);
        WellspringCS.Wellspring.Rectangle rect;
        font.TextBounds(str, text.Size, text.HorizontalAlignment, text.VerticalAlignment, out rect);

        var promptDouble = CreateEntity();
        Set(promptDouble,
            new Position(new Vector2(Dimensions.GameWidth * 0.5f - rect.W - text.Size, Dimensions.GameHeight * 0.1f)));
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
        Set(promptDouble, new FollowsCamera(Dimensions.GameHeight * 0.1f));
        Set(promptDouble, new DestroyOnStateTransition());

        var startY = Dimensions.GameHeight * 0.3f;
        var x = 20;

        var musicVolume = GetSingleton<MusicVolume>().Value;

        var musicVolumeLabel = CreateEntity();
        Set(musicVolumeLabel,
            new Position(new Vector2(x, startY)));
        Set(musicVolumeLabel,
         new Text(
            Fonts.HeaderFont,
            Fonts.PromptSize,
            Stores.TextStorage.GetID("music"),
            MoonWorks.Graphics.Font.HorizontalAlignment.Left,
            MoonWorks.Graphics.Font.VerticalAlignment.Middle));
        Set(musicVolumeLabel, new KeepOpacityWhenPaused());
        Set(musicVolumeLabel, new Depth(0.1f));
        Set(musicVolumeLabel, new FollowsCamera(startY));
        Set(musicVolumeLabel, new DestroyOnStateTransition());
        Set(musicVolumeLabel, new Setting());
        Set(musicVolumeLabel, new Selected());

        var musicVolumeDisplay = CreateEntity();
        Set(musicVolumeDisplay,
            new Position(new Vector2(x + 150, startY)));
        Set(musicVolumeDisplay,
         new Text(
            Fonts.HeaderFont,
            Fonts.PromptSize,
            Stores.TextStorage.GetID(new string('|', (int)(musicVolume * Audio.MaxVolume))),
            MoonWorks.Graphics.Font.HorizontalAlignment.Left,
            MoonWorks.Graphics.Font.VerticalAlignment.Middle));
        Set(musicVolumeDisplay, new KeepOpacityWhenPaused());
        Set(musicVolumeDisplay, new Depth(0.1f));
        Set(musicVolumeDisplay, new FollowsCamera(startY));
        Set(musicVolumeDisplay, new DestroyOnStateTransition());

        Relate(musicVolumeLabel, musicVolumeDisplay, new SettingDisplay());
        Relate(musicVolumeLabel, GetSingletonEntity<MusicVolume>(), new SettingControls());

        startY += 30f;

        var sfxVolume = GetSingleton<SFXVolume>().Value;

        var sfxVolumeLabel = CreateEntity();
        Set(sfxVolumeLabel,
            new Position(new Vector2(x, startY)));
        Set(sfxVolumeLabel,
         new Text(
            Fonts.HeaderFont,
            Fonts.PromptSize,
            Stores.TextStorage.GetID("sfx"),
            MoonWorks.Graphics.Font.HorizontalAlignment.Left,
            MoonWorks.Graphics.Font.VerticalAlignment.Middle));
        Set(sfxVolumeLabel, new KeepOpacityWhenPaused());
        Set(sfxVolumeLabel, new Depth(0.1f));
        Set(sfxVolumeLabel, new FollowsCamera(startY));
        Set(sfxVolumeLabel, new DestroyOnStateTransition());
        Set(sfxVolumeLabel, new Setting());
        Relate(sfxVolumeLabel, GetSingletonEntity<SFXVolume>(), new SettingControls());


        var sfxVolumeDisplay = CreateEntity();
        Set(sfxVolumeDisplay,
            new Position(new Vector2(x + 150, startY)));
        Set(sfxVolumeDisplay,
         new Text(
            Fonts.HeaderFont,
            Fonts.PromptSize,
            Stores.TextStorage.GetID(new string('|', (int)(sfxVolume * Audio.MaxVolume))),
            MoonWorks.Graphics.Font.HorizontalAlignment.Left,
            MoonWorks.Graphics.Font.VerticalAlignment.Middle));
        Set(sfxVolumeDisplay, new KeepOpacityWhenPaused());
        Set(sfxVolumeDisplay, new Depth(0.1f));
        Set(sfxVolumeDisplay, new FollowsCamera(startY));
        Set(sfxVolumeDisplay, new DestroyOnStateTransition());

        Relate(sfxVolumeLabel, sfxVolumeDisplay, new SettingDisplay());

        Relate(musicVolumeLabel, sfxVolumeLabel, new VerticalConnection());
    }

}