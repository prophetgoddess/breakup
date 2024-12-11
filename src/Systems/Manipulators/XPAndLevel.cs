using MoonWorks.Graphics;
using System.Numerics;
using Ball;
using MoonTools.ECS;
using MoonWorks.Math;

public class XPAndLevel : Manipulator
{
    public XPAndLevel(World world) : base(world)
    {
    }

    Entity CreateUpgrade(float x)
    {
        var upgrade = CreateEntity();
        Set(upgrade,
        new Position(new Vector2(x, Dimensions.GameHeight * 0.45f)));
        Set(upgrade,
         new Text(
            Fonts.HeaderFont,
            Fonts.UpgradeSize,
            Stores.TextStorage.GetID("UPGRADE"),
            MoonWorks.Graphics.Font.HorizontalAlignment.Center,
            MoonWorks.Graphics.Font.VerticalAlignment.Middle));
        Set(upgrade, new KeepOpacityWhenPaused());
        Set(upgrade, new Pause());
        Set(upgrade, new Depth(0.1f));
        Set(upgrade, new FollowsCamera(Dimensions.GameHeight * 0.45f));
        Set(upgrade, new DestroyWhenLeavingUpgradeMenu());

        var description = CreateEntity();
        Set(description,
            new Position(new Vector2(x, Dimensions.GameHeight * 0.5f)));
        Set(description,
         new Text(
            Fonts.BodyFont,
            Fonts.InfoSize,
            Stores.TextStorage.GetID("Lorem ipsum"),
            MoonWorks.Graphics.Font.HorizontalAlignment.Center,
            MoonWorks.Graphics.Font.VerticalAlignment.Middle));
        Set(description, new KeepOpacityWhenPaused());
        Set(description, new Pause());
        Set(description, new Depth(0.1f));
        Set(description, new FollowsCamera(Dimensions.GameHeight * 0.5f));
        Set(description, new DestroyWhenLeavingUpgradeMenu());

        Relate(upgrade, description, new Description());

        return upgrade;
    }

    public void LevelUp()
    {
        var levelEntity = GetSingletonEntity<Level>();
        var level = Get<Level>(levelEntity);

        var targetXP = (int)MathF.Ceiling(float.Lerp(10, 9999, Easing.InQuad(level.Value / 99.0f)));
        var currentXP = 0;
        Set(levelEntity, new Level(level.Value + 1));

        Set(GetSingletonEntity<XP>(), new XP(currentXP, targetXP));

        var promptEntity = CreateEntity();
        Set(promptEntity,
        new Position(new Vector2(Dimensions.GameWidth * 0.5f, Dimensions.GameHeight * 0.25f)));
        Set(promptEntity,
         new Text(
            Fonts.HeaderFont,
            Fonts.HeaderSize,
            Stores.TextStorage.GetID("LEVEL UP"),
            MoonWorks.Graphics.Font.HorizontalAlignment.Center,
            MoonWorks.Graphics.Font.VerticalAlignment.Middle));
        Set(promptEntity, new KeepOpacityWhenPaused());
        Set(promptEntity, new Pause());
        Set(promptEntity, new Depth(0.1f));
        Set(promptEntity, new Marquee(100f));
        Set(promptEntity, new FollowsCamera(Dimensions.GameHeight * 0.25f));
        Set(promptEntity, new DestroyWhenLeavingUpgradeMenu());

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
            Stores.TextStorage.GetID("LEVEL UP"),
            MoonWorks.Graphics.Font.HorizontalAlignment.Center,
            MoonWorks.Graphics.Font.VerticalAlignment.Middle));
        Set(promptDouble, new KeepOpacityWhenPaused());
        Set(promptDouble, new Depth(0.1f));
        Set(promptDouble, new Marquee(100f));
        Set(promptDouble, new FollowsCamera(Dimensions.GameHeight * 0.25f));
        Set(promptDouble, new DestroyWhenLeavingUpgradeMenu());


        var upgrade1 = CreateUpgrade(Dimensions.GameWidth * 0.15f);
        var upgrade2 = CreateUpgrade(Dimensions.GameWidth * 0.5f);
        var upgrade3 = CreateUpgrade(Dimensions.GameWidth * 0.85f);

        Relate(upgrade1, upgrade2, new HorizontalConnection());
        Relate(upgrade2, upgrade3, new HorizontalConnection());
        Relate(upgrade3, upgrade1, new HorizontalConnection());

        Set(upgrade2, new Selected());


    }

    public void GiveXP(int amt)
    {
        var xpEntity = GetSingletonEntity<XP>();
        var xp = Get<XP>(xpEntity);
        var currentXP = xp.Current;
        var targetXP = xp.Target;

        currentXP += amt;
        if (currentXP >= targetXP)
        {
            LevelUp();
        }
        else
        {
            Set(xpEntity, new XP(currentXP, targetXP));
        }
    }

}