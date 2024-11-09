using System.Numerics;
using MoonTools.ECS;

namespace Ball;

public class Blocks : MoonTools.ECS.System
{

    Filter BlockFilter;
    bool Initialized = false;
    int MinBlockCount = 20;
    int CellSize = 32;
    int GridWidth { get { return Dimensions.GameWidth / CellSize; } }
    int GridHeight { get { return Dimensions.GameHeight / CellSize; } }

    System.Random Random = new System.Random();

    public Blocks(World world) : base(world)
    {
        BlockFilter = FilterBuilder
        .Include<CanDamagePaddle>()
        .Build();
    }

    void SpawnBlock(int x, int y)
    {
        var block = CreateEntity();
        Set(block, new Scale(1.9f));
        Set(block, new Position(new Vector2(CellSize * 0.5f + x * CellSize, CellSize * 0.5f + y * CellSize)));
        Set(block, new BoundingBox(0, 0, 32, 32));
        Set(block, new SolidCollision());
        Set(block, new DestroyOnRestartGame());
        Set(block, new CanDamagePaddle());

        if (Random.NextDouble() < 0.66f)
        {
            Set(block, new Model(Content.Models.EmptySquare.ID));
            Set(block, new CanTakeDamageFromBall());
            Set(block, new HitPoints(1));
        }
        else
        {
            Set(block, new Model(Content.Models.Square.ID));
        }

    }

    void Initialize()
    {
        Initialized = true;
        for (int i = 0; i < MinBlockCount; i++)
        {
            SpawnBlock(
                Random.Next(GridWidth),
                Random.Next(GridHeight - (GridHeight / 2))
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
            if (
                Get<Position>(block).Value.Y > (-cam.Y + Dimensions.GameHeight) ||
                (Has<HitPoints>(block) && Get<HitPoints>(block).Value <= 0)
                )
            {
                Destroy(block);
            }
        }

        if (BlockFilter.Count < MinBlockCount)
        {
            SpawnBlock(
                Random.Next(GridWidth),
                (int)(-cam.Y / CellSize) - Random.Next(GridHeight)
            );
        }

    }
}