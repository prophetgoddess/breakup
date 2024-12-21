using Ball;
using MoonTools.ECS;
using MoonWorks.Graphics.Font;

public class MarqueeController : MoonTools.ECS.System
{
    Filter MarqueeFilter;

    public MarqueeController(World world) : base(world)
    {
        MarqueeFilter = FilterBuilder.Include<Position>().Include<Marquee>().Include<Text>().Build();
    }

    public override void Update(TimeSpan delta)
    {
        var dt = (float)delta.TotalSeconds;

        foreach (var entity in MarqueeFilter.Entities)
        {
            var position = Get<Position>(entity).Value;
            var marquee = Get<Marquee>(entity);
            var text = Get<Text>(entity);
            var font = Stores.FontStorage.Get(text.FontID);
            var str = Stores.TextStorage.Get(text.TextID);
            WellspringCS.Wellspring.Rectangle rect;
            font.TextBounds(str, text.Size, text.HorizontalAlignment, text.VerticalAlignment, out rect);
            position.X += marquee.Speed * dt;
            var max = position.X + rect.W * 0.5f;
            var min = position.X - rect.W * 0.5f;
            if (marquee.Speed < 0 && max < 0)
            {
                position.X = (Has<UI>(entity) ? Dimensions.UIWidth : Dimensions.GameWidth) + rect.W * 0.5f;
            }
            else if (marquee.Speed > 0 && min > (Has<UI>(entity) ? Dimensions.UIWidth : Dimensions.GameWidth))
            {
                position.X = rect.W * -0.5f;
            }

            Set(entity, new Position(position));
        }
    }
}