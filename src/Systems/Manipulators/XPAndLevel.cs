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
        new Position(new Vector2(Dimensions.WindowWidth * 0.5f, Dimensions.WindowHeight * 0.25f)));
        Set(promptEntity,
         new Text(
            Fonts.HeaderFont,
            Fonts.HeaderSize,
            Stores.TextStorage.GetID("LEVEL UP"),
            MoonWorks.Graphics.Font.HorizontalAlignment.Center,
            MoonWorks.Graphics.Font.VerticalAlignment.Middle));
        Set(promptEntity, new UI());
        Set(promptEntity, new Pause());
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