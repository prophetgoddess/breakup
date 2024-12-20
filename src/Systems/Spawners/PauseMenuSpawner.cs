using System.Numerics;
using MoonTools.ECS;

namespace Ball;

public class PauseMenuSpawner : Manipulator
{
    public PauseMenuSpawner(World world) : base(world)
    {
    }

    public void OpenPauseMenu()
    {
        var pauseEntity = CreateEntity();
        Set(pauseEntity, new Position(new Vector2(Dimensions.GameWidth * 0.5f, Dimensions.GameHeight * 0.5f)));
        Set(pauseEntity,
         new Text(
            Fonts.HeaderFont,
            Fonts.HeaderSize,
            Stores.TextStorage.GetID("PAUSED"),
            MoonWorks.Graphics.Font.HorizontalAlignment.Center,
            MoonWorks.Graphics.Font.VerticalAlignment.Middle));
        Set(pauseEntity, new KeepOpacityWhenPaused());
        Set(pauseEntity, new Pause());
        Set(pauseEntity, new Marquee(100f));
        Set(pauseEntity, new Depth(0.1f));
        Set(pauseEntity, new FollowsCamera(Dimensions.GameHeight * 0.5f));

        var text = Get<Text>(pauseEntity);
        var font = Stores.FontStorage.Get(text.FontID);
        var str = Stores.TextStorage.Get(text.TextID);
        WellspringCS.Wellspring.Rectangle rect;
        font.TextBounds(str, text.Size, text.HorizontalAlignment, text.VerticalAlignment, out rect);

        var pauseDouble = CreateEntity();
        Set(pauseDouble, new Position(new Vector2(Dimensions.GameWidth * 0.5f - rect.W - text.Size, Dimensions.GameHeight * 0.5f)));
        Set(pauseDouble,
         new Text(
            Fonts.HeaderFont,
            Fonts.HeaderSize,
            Stores.TextStorage.GetID("PAUSED"),
            MoonWorks.Graphics.Font.HorizontalAlignment.Center,
            MoonWorks.Graphics.Font.VerticalAlignment.Middle));
        Set(pauseDouble, new KeepOpacityWhenPaused());
        Set(pauseDouble, new Pause());
        Set(pauseDouble, new Marquee(100f));
        Set(pauseDouble, new Depth(0.1f));
        Set(pauseDouble, new FollowsCamera(Dimensions.GameHeight * 0.5f));
    }
}