using System.Numerics;
using MoonTools.ECS;

namespace Ball;

public class Animations : MoonTools.ECS.System
{
    Filter AnimationFilter;
    Filter PulsateFilter;
    Filter ExpandingEchoesFilter;
    Filter GrowFilter;
    Filter FadeFilter;

    public Animations(World world) : base(world)
    {
        AnimationFilter = FilterBuilder.Include<AnimationTimer>().Build();

        PulsateFilter = FilterBuilder.Include<Pulsate>().Include<Model>().Build();
        ExpandingEchoesFilter = FilterBuilder.Include<ExpandingEchoes>().Include<Model>().Build();
        GrowFilter = FilterBuilder.Include<GrowOverTime>().Include<Model>().Build();
        FadeFilter = FilterBuilder.Include<FadeOut>().Include<Model>().Include<Timer>().Build();

    }

    public override void Update(TimeSpan delta)
    {
        var dt = (float)delta.TotalSeconds;

        foreach (var entity in AnimationFilter.Entities)
        {
            var t = Get<AnimationTimer>(entity);
            Set(entity, new AnimationTimer(t.Time + dt));
        }

        foreach (var entity in PulsateFilter.Entities)
        {
            var pulsate = Get<Pulsate>(entity);
            if (!Has<AnimationTimer>(entity))
            {
                Set(entity, new AnimationTimer(0f));
            }

            var t = Get<AnimationTimer>(entity).Time * pulsate.Rate;

            Set(entity, new Scale(new Vector2(
                pulsate.Scale.X + MathF.Abs(MathF.Sin(t)) * pulsate.Amount,
                pulsate.Scale.Y + MathF.Abs(MathF.Sin(t)) * pulsate.Amount
            )));
        }

        foreach (var entity in ExpandingEchoesFilter.Entities)
        {
            var echoes = Get<ExpandingEchoes>(entity);
            if (!Has<AnimationTimer>(entity))
            {
                Set(entity, new AnimationTimer(0f));
            }

            var t = Get<AnimationTimer>(entity).Time;

            if (t >= echoes.Rate)
            {
                Set(entity, new AnimationTimer(0f));

                var echo = CreateEntity();
                Set(echo, new Model(Get<Model>(entity).ID));
                Set(echo, new Position(Get<Position>(entity).Value));
                Set(echo, new Scale(Has<Scale>(entity) ? Get<Scale>(entity).Value : Vector2.One));
                Set(echo, new GrowOverTime(echoes.GrowthRate));
                Set(echo, new Timer(echoes.Lifetime));

                if (Has<Highlight>(entity))
                {
                    Set(echo, new Highlight());
                    Set(echo, new DestroyOnStartGame());
                }
            }

        }

        foreach (var entity in GrowFilter.Entities)
        {
            var growth = Get<GrowOverTime>(entity);

            var scale = Has<Scale>(entity) ? Get<Scale>(entity).Value : Vector2.One;

            Set(entity, new Scale(new Vector2(
                scale.X + growth.Rate * dt,
                scale.Y + growth.Rate * dt
            )));
        }

        foreach (var entity in FadeFilter.Entities)
        {
            var timer = Get<Timer>(entity);
            Set(entity, new Alpha((byte)(timer.Remaining * 255)));
        }
    }
}