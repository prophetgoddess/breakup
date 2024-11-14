using MoonTools.ECS;

namespace Ball;

public class Parent : MoonTools.ECS.System
{
    Filter PositionFilter;

    public Parent(World world) : base(world)
    {
        PositionFilter = FilterBuilder.Include<Position>().Build();
    }

    public override void Update(TimeSpan delta)
    {
        foreach (var entity in PositionFilter.Entities)
        {
            if (HasOutRelation<ChildOf>(entity))
            {
                var held = OutRelationSingleton<ChildOf>(entity);
                var data = GetRelationData<ChildOf>(entity, held);
                Set(entity, new Position(Get<Position>(held).Value + data.offset));
                continue;
            }
        }
    }
}