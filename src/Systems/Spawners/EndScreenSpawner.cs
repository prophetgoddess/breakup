using System.Numerics;
using MoonTools.ECS;

namespace Ball;

public class EndScreenSpawner : Manipulator
{
    Filter DestroyFilter;


    public EndScreenSpawner(World world) : base(world)
    {
        DestroyFilter = FilterBuilder.Include<DestroyOnStateTransition>().Exclude<DontDestroyOnNextTransition>().Build();
    }

    public void OpenEndScreen()
    {
        foreach (var entity in DestroyFilter.Entities)
        {
            Destroy(entity);
        }
    }
}