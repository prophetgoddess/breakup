using System.Numerics;
using MoonTools.ECS;

namespace Ball;

public class BlockGenerator : MoonTools.ECS.System
{

    Filter BlockFilter;
    bool Initialized = false;
    int MinBlockCount = 20;
    System.Random Random = new System.Random();

    public BlockGenerator(World world) : base(world)
    {
        BlockFilter = FilterBuilder
        .Include<DestroyOnContactWithBall>()
        .Build();
    }

    void SpawnBlock(Vector2 position)
    {
        var block = CreateEntity();
        Set(block, new Model(Content.Models.Square.ID));
        Set(block, new Scale(2.0f));
        Set(block, new Position(position));
        Set(block, new BoundingBox(0, 0, 32, 32));
        Set(block, new DestroyOnContactWithBall());
        Set(block, new SolidCollision());
    }

    void Initialize()
    {
        Initialized = true;
        for (int i = 0; i < MinBlockCount; i++)
        {
            SpawnBlock(
                new Vector2(
                    50 + (float)Random.NextDouble() * (Dimensions.WindowWidth - 50),
                    100 + (float)Random.NextDouble() * (Dimensions.WindowHeight - 300)
                )
            );
        }
    }

    public override void Update(TimeSpan delta)
    {
        var cam = GetSingleton<CameraPosition>();

        if (!Initialized)
            Initialize();

        foreach (var block in BlockFilter.Entities)
        {
            var position = Get<Position>(block).value;
            if (position.Y > (-cam.Y + Dimensions.WindowHeight))
            {
                Destroy(block);
            }
        }

        if (BlockFilter.Count < MinBlockCount)
        {
            SpawnBlock(
                new Vector2(
                    100 + (float)Random.NextDouble() * (Dimensions.WindowWidth - 100),
                    -(float)Random.NextDouble() * Dimensions.WindowHeight - cam.Y
                )
            );
        }

    }
}