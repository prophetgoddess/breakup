using System.Numerics;
using Ball;
using MoonTools.ECS;
using MoonWorks.Input;

public class SettingsMenuSpawner : Manipulator
{
    MainMenuSpawner MainMenuSpawner;
    MarqueeSpawner MarqueeSpawner;
    Filter DestroyFilter;
    Filter DontDestroyFilter;

    public SettingsMenuSpawner(World world) : base(world)
    {
        MainMenuSpawner = new MainMenuSpawner(world);

        DestroyFilter = FilterBuilder.Include<DestroyOnStateTransition>().Exclude<DontDestroyOnNextTransition>().Build();
        DontDestroyFilter = FilterBuilder.Include<DestroyOnStateTransition>().Include<DontDestroyOnNextTransition>().Build();
        MarqueeSpawner = new MarqueeSpawner(world);
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

        var height = Some<Player>() ? Dimensions.GameHeight : Dimensions.UIHeight;

        Set(MarqueeSpawner.SpawnMarquee("Settings", Fonts.HeaderFont, Some<Player>() ? Fonts.HeaderSize : Fonts.TitleSize, Some<Player>() ? 2 : 3, 100f, height * 0.1f), new Pause());

        MarqueeSpawner.SpawnMarquee($"press {Input.GetButtonName(Actions.Cancel)} to go back", Fonts.BodyFont, Some<Player>() ? Fonts.InfoSize : Fonts.PromptSize, Some<Player>() ? 2 : 3, -100f, height * 0.2f);


        var startY = height * 0.3f;
        var x = Some<Player>() ? 20 : 40;

        var musicVolume = GetSingleton<MusicVolume>().Value;

        var musicVolumeLabel = CreateEntity();
        Set(musicVolumeLabel,
            new Position(new Vector2(x, startY)));
        Set(musicVolumeLabel,
         new Text(
            Fonts.HeaderFont,
            Some<Player>() ? Fonts.PromptSize : Fonts.BodySize,
            Stores.TextStorage.GetID("music"),
            MoonWorks.Graphics.Font.HorizontalAlignment.Left,
            MoonWorks.Graphics.Font.VerticalAlignment.Middle));
        Set(musicVolumeLabel, new KeepOpacityWhenPaused());
        Set(musicVolumeLabel, new Depth(0.1f));
        Set(musicVolumeLabel, new FollowsCamera(startY));
        Set(musicVolumeLabel, new DestroyOnStateTransition());
        Set(musicVolumeLabel, new Setting());
        Set(musicVolumeLabel, new Selected());
        if (!Some<Player>())
            Set(musicVolumeLabel, new UI());

        var musicVolumeDisplay = CreateEntity();
        Set(musicVolumeDisplay,
            new Position(new Vector2(x + 150, startY)));
        Set(musicVolumeDisplay,
         new Text(
            Fonts.HeaderFont,
            Some<Player>() ? Fonts.PromptSize : Fonts.BodySize,
            Stores.TextStorage.GetID(new string('|', (int)(musicVolume * Audio.MaxVolume))),
            MoonWorks.Graphics.Font.HorizontalAlignment.Left,
            MoonWorks.Graphics.Font.VerticalAlignment.Middle));
        Set(musicVolumeDisplay, new KeepOpacityWhenPaused());
        Set(musicVolumeDisplay, new Depth(0.1f));
        Set(musicVolumeDisplay, new FollowsCamera(startY));
        Set(musicVolumeDisplay, new Highlight());
        Set(musicVolumeDisplay, new DestroyOnStateTransition());
        if (!Some<Player>())
            Set(musicVolumeDisplay, new UI());

        Relate(musicVolumeLabel, musicVolumeDisplay, new SettingDisplay());
        Relate(musicVolumeLabel, GetSingletonEntity<MusicVolume>(), new SettingControls());

        startY += Some<Player>() ? 30f : 50f;

        var sfxVolume = GetSingleton<SFXVolume>().Value;

        var sfxVolumeLabel = CreateEntity();
        Set(sfxVolumeLabel,
            new Position(new Vector2(x, startY)));
        Set(sfxVolumeLabel,
         new Text(
            Fonts.HeaderFont,
            Some<Player>() ? Fonts.PromptSize : Fonts.BodySize,
            Stores.TextStorage.GetID("sfx"),
            MoonWorks.Graphics.Font.HorizontalAlignment.Left,
            MoonWorks.Graphics.Font.VerticalAlignment.Middle));
        Set(sfxVolumeLabel, new KeepOpacityWhenPaused());
        Set(sfxVolumeLabel, new Depth(0.1f));
        Set(sfxVolumeLabel, new FollowsCamera(startY));
        Set(sfxVolumeLabel, new DestroyOnStateTransition());
        Set(sfxVolumeLabel, new Setting());
        Relate(sfxVolumeLabel, GetSingletonEntity<SFXVolume>(), new SettingControls());
        if (!Some<Player>())
            Set(sfxVolumeLabel, new UI());

        var sfxVolumeDisplay = CreateEntity();
        Set(sfxVolumeDisplay,
            new Position(new Vector2(x + 150, startY)));
        Set(sfxVolumeDisplay,
         new Text(
            Fonts.HeaderFont,
            Some<Player>() ? Fonts.PromptSize : Fonts.BodySize,
            Stores.TextStorage.GetID(new string('|', (int)(sfxVolume * Audio.MaxVolume))),
            MoonWorks.Graphics.Font.HorizontalAlignment.Left,
            MoonWorks.Graphics.Font.VerticalAlignment.Middle));
        Set(sfxVolumeDisplay, new KeepOpacityWhenPaused());
        Set(sfxVolumeDisplay, new Depth(0.1f));
        Set(sfxVolumeDisplay, new FollowsCamera(startY));
        Set(sfxVolumeDisplay, new DestroyOnStateTransition());
        Set(sfxVolumeDisplay, new Highlight());
        if (!Some<Player>())
            Set(sfxVolumeDisplay, new UI());

        Relate(sfxVolumeLabel, sfxVolumeDisplay, new SettingDisplay());

        Relate(musicVolumeLabel, sfxVolumeLabel, new VerticalConnection());

        startY += Some<Player>() ? 30f : 50f;

        var fullscreen = GetSingleton<Fullscreen>().Value;

        var fullscreenLabel = CreateEntity();
        Set(fullscreenLabel,
            new Position(new Vector2(x, startY)));
        Set(fullscreenLabel,
         new Text(
            Fonts.HeaderFont,
            Some<Player>() ? Fonts.PromptSize : Fonts.BodySize,
            Stores.TextStorage.GetID("fullscreen"),
            MoonWorks.Graphics.Font.HorizontalAlignment.Left,
            MoonWorks.Graphics.Font.VerticalAlignment.Middle));
        Set(fullscreenLabel, new KeepOpacityWhenPaused());
        Set(fullscreenLabel, new Depth(0.1f));
        Set(fullscreenLabel, new FollowsCamera(startY));
        Set(fullscreenLabel, new DestroyOnStateTransition());
        Set(fullscreenLabel, new Setting());
        Relate(fullscreenLabel, GetSingletonEntity<Fullscreen>(), new SettingControls());
        if (!Some<Player>())
            Set(fullscreenLabel, new UI());

        var fullscreenDisplay = CreateEntity();
        Set(fullscreenDisplay,
            new Position(new Vector2(x + (Some<Player>() ? 250f : 340f), startY)));
        Set(fullscreenDisplay,
            new Text(
                Fonts.HeaderFont,
                Some<Player>() ? Fonts.PromptSize : Fonts.BodySize,
                Stores.TextStorage.GetID($"{fullscreen}"),
                MoonWorks.Graphics.Font.HorizontalAlignment.Left,
                MoonWorks.Graphics.Font.VerticalAlignment.Middle));
        Set(fullscreenDisplay, new KeepOpacityWhenPaused());
        Set(fullscreenDisplay, new Depth(0.1f));
        Set(fullscreenDisplay, new FollowsCamera(startY));
        Set(fullscreenDisplay, new DestroyOnStateTransition());
        Set(fullscreenDisplay, new Highlight());
        if (!Some<Player>())
            Set(fullscreenDisplay, new UI());

        Relate(fullscreenLabel, fullscreenDisplay, new SettingDisplay());

        Relate(sfxVolumeLabel, fullscreenLabel, new VerticalConnection());

        startY += Some<Player>() ? 30f : 50f;

        var playingSong = GetSingleton<Song>();

        var songLabel = CreateEntity();
        Set(songLabel,
            new Position(new Vector2(x, startY)));
        Set(songLabel,
         new Text(
            Fonts.HeaderFont,
            Some<Player>() ? Fonts.PromptSize : Fonts.BodySize,
            Stores.TextStorage.GetID("song"),
            MoonWorks.Graphics.Font.HorizontalAlignment.Left,
            MoonWorks.Graphics.Font.VerticalAlignment.Middle));
        Set(songLabel, new KeepOpacityWhenPaused());
        Set(songLabel, new Depth(0.1f));
        Set(songLabel, new FollowsCamera(startY));
        Set(songLabel, new DestroyOnStateTransition());
        Set(songLabel, new Setting());
        Relate(songLabel, GetSingletonEntity<Song>(), new SettingControls());
        if (!Some<Player>())
            Set(songLabel, new UI());

        var songDisplay = CreateEntity();
        Set(songDisplay,
            new Position(new Vector2(x + (Some<Player>() ? 150f : 200f), startY)));
        Set(songDisplay, new Highlight());
        Set(songDisplay,
            new Text(
                Fonts.HeaderFont,
                Some<Player>() ? Fonts.PromptSize : Fonts.BodySize,
                playingSong.NameID,
                MoonWorks.Graphics.Font.HorizontalAlignment.Left,
                MoonWorks.Graphics.Font.VerticalAlignment.Middle));
        Set(songDisplay, new KeepOpacityWhenPaused());
        Set(songDisplay, new Depth(0.1f));
        Set(songDisplay, new FollowsCamera(startY));
        Set(songDisplay, new DestroyOnStateTransition());
        if (!Some<Player>())
            Set(songDisplay, new UI());

        Relate(songLabel, songDisplay, new SettingDisplay());

        Relate(fullscreenLabel, songLabel, new VerticalConnection());

        startY += Some<Player>() ? 30f : 50f;

        var currentPalette = GetSingleton<Palette>();

        var paletteLabel = CreateEntity();
        Set(paletteLabel,
            new Position(new Vector2(x, startY)));
        Set(paletteLabel,
         new Text(
            Fonts.HeaderFont,
            Some<Player>() ? Fonts.PromptSize : Fonts.BodySize,
            Stores.TextStorage.GetID("palette"),
            MoonWorks.Graphics.Font.HorizontalAlignment.Left,
            MoonWorks.Graphics.Font.VerticalAlignment.Middle));
        Set(paletteLabel, new KeepOpacityWhenPaused());
        Set(paletteLabel, new Depth(0.1f));
        Set(paletteLabel, new FollowsCamera(startY));
        Set(paletteLabel, new DestroyOnStateTransition());
        Set(paletteLabel, new Setting());
        Relate(paletteLabel, GetSingletonEntity<Palette>(), new SettingControls());
        if (!Some<Player>())
            Set(paletteLabel, new UI());

        var paletteDisplay = CreateEntity();
        Set(paletteDisplay,
            new Position(new Vector2(x + (Some<Player>() ? 200f : 250f), startY)));
        Set(paletteDisplay, new Highlight());
        Set(paletteDisplay,
            new Text(
                Fonts.HeaderFont,
                Some<Player>() ? Fonts.PromptSize : Fonts.BodySize,
                currentPalette.NameID,
                MoonWorks.Graphics.Font.HorizontalAlignment.Left,
                MoonWorks.Graphics.Font.VerticalAlignment.Middle));
        Set(paletteDisplay, new KeepOpacityWhenPaused());
        Set(paletteDisplay, new Depth(0.1f));
        Set(paletteDisplay, new FollowsCamera(startY));
        Set(paletteDisplay, new DestroyOnStateTransition());
        if (!Some<Player>())
            Set(paletteDisplay, new UI());

        Relate(paletteLabel, paletteDisplay, new SettingDisplay());

        Relate(songLabel, paletteLabel, new VerticalConnection());

        startY += Some<Player>() ? 30f : 50f;

        var rb = CreateEntity();
        Set(rb, new RebindControls(false));

        var rebindLabel = CreateEntity();
        Set(rebindLabel,
            new Position(new Vector2(x, startY)));
        Set(rebindLabel,
         new Text(
            Fonts.HeaderFont,
            Some<Player>() ? Fonts.PromptSize : Fonts.BodySize,
            Stores.TextStorage.GetID("rebind controls"),
            MoonWorks.Graphics.Font.HorizontalAlignment.Left,
            MoonWorks.Graphics.Font.VerticalAlignment.Middle));
        Set(rebindLabel, new KeepOpacityWhenPaused());
        Set(rebindLabel, new Depth(0.1f));
        Set(rebindLabel, new FollowsCamera(startY));
        Set(rebindLabel, new DestroyOnStateTransition());
        Set(rebindLabel, new Setting());
        Relate(rebindLabel, rb, new SettingControls());
        Relate(rebindLabel, rb, new SettingDisplay());
        if (!Some<Player>())
            Set(rebindLabel, new UI());

        Relate(paletteLabel, rebindLabel, new VerticalConnection());

    }

}