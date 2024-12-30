using System.Numerics;
using MoonTools.ECS;
using MoonWorks.Graphics.Font;

namespace Ball;

public class MarqueeSpawner : Manipulator
{
    WellspringCS.Wellspring.Rectangle rect;

    public MarqueeSpawner(World world) : base(world)
    {
    }

    public Entity SpawnMarquee(string str, int fontID, int size, float copies, float speed, float y)
    {
        var font = Stores.FontStorage.Get(fontID);
        var strID = Stores.TextStorage.GetID(str);
        font.TextBounds(str, size, HorizontalAlignment.Center, VerticalAlignment.Middle, out rect);

        var width = Some<Player>() ? Dimensions.GameWidth : Dimensions.UIWidth;

        var totalWidth = width + rect.W;
        totalWidth -= rect.W * copies;
        totalWidth /= copies;
        Entity entity = default;

        for (int i = 0; i < copies; i++)
        {
            entity = CreateEntity();
            Set(entity,
            new Position(new Vector2(i * (rect.W + totalWidth), y)));
            Set(entity,
             new Text(
                fontID,
                size,
                strID,
                HorizontalAlignment.Center,
                VerticalAlignment.Middle));
            Set(entity, new KeepOpacityWhenPaused());
            Set(entity, new Pause());
            Set(entity, new Depth(0.1f));
            Set(entity, new Marquee(speed));
            if (Some<Player>())
                Set(entity, new FollowsCamera(y));
            else
                Set(entity, new UI());

            Set(entity, new DestroyOnStateTransition());

        }
        return entity;
    }
}