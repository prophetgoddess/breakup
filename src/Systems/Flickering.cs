using MoonTools.ECS;

namespace Ball;

public class Flickering : MoonTools.ECS.System
{
    Filter FlickerFilter;

    public Flickering(World world) : base(world)
    {
        FlickerFilter = FilterBuilder.Include<Flicker>().Build();
    }

    public override void Update(TimeSpan delta)
    {

        foreach (var entity in FlickerFilter.Entities)
        {
            var flicker = Get<Flicker>(entity);
            if (!HasOutRelation<FlickerTimer>(entity))
            {
                if (!Has<Invisible>(entity))
                {
                    Set(entity, new Invisible());
                }
                else
                {
                    Remove<Invisible>(entity);
                }

                var timer = CreateEntity();
                Set(timer, new WhilePaused());
                Set(timer, new Timer(flicker.Rate));
                Relate(entity, timer, new FlickerTimer());
            }

        }
    }
}