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