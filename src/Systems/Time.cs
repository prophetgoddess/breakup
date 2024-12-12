using System;
using MoonTools.ECS;

namespace Ball;

public class Time : MoonTools.ECS.System
{
    private Filter TimerFilter;
    private Filter PausedTimerFilter;

    public Time(World world) : base(world)
    {
        TimerFilter = FilterBuilder
            .Include<Timer>()
            .Exclude<WhilePaused>()
            .Build();

        PausedTimerFilter = FilterBuilder
            .Include<Timer>()
            .Include<WhilePaused>()
            .Build();
    }

    public override void Update(TimeSpan delta)
    {
        foreach (var entity in PausedTimerFilter.Entities)
        {
            var timer = Get<Timer>(entity);
            var time = timer.Time - (float)delta.TotalSeconds;

            if (time <= 0)
            {
                Destroy(entity);
                continue;
            }

            Set(entity, timer with { Time = time });
        }

        if (Some<Pause>())
            return;

        foreach (var entity in TimerFilter.Entities)
        {
            var timer = Get<Timer>(entity);
            var time = timer.Time - (float)delta.TotalSeconds;

            if (time <= 0)
            {
                Destroy(entity);
                continue;
            }

            Set(entity, timer with { Time = time });
        }
    }
}