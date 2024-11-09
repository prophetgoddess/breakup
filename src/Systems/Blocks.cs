using System.Numerics;
using MoonTools.ECS;

namespace Ball;

public class Blocks : MoonTools.ECS.System
{

    Filter BlockFilter;
    int CellSize = 32;
    int GridWidth { get { return Dimensions.GameWidth / CellSize; } }
    int GridHeight { get { return Dimensions.GameHeight / CellSize; } }

    System.Random Random = new System.Random();
    float LastGridOffset = -1.0f;

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
        Destroy(GetSingletonEntity<Initialize>());
        LastGridOffset = 0.0f;
        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = -3; y < GridHeight * 0.4f; y++)
            {
                if (Random.NextDouble() < 0.1f)
                {
                    SpawnBlock(
                        x,
                        y
                    );
                }
            }
        }
    }

    public override void Update(TimeSpan delta)
    {
        if (Some<Initialize>())
            Initialize();

        var cam = GetSingleton<CameraPosition>();

        if (cam.Y > LastGridOffset && cam.Y > LastGridOffset + CellSize)
        {
            LastGridOffset = cam.Y;

            int y = -(int)(MathF.Floor(cam.Y + CellSize) / CellSize) - 3;

            for (int x = 0; x < GridWidth; x++)
            {
                if (Random.NextDouble() < 0.1f)
                {
                    SpawnBlock(x, y);
                }
            }
        }

        foreach (var block in BlockFilter.Entities)
        {
            if (Has<HitPoints>(block))
            {
                var hp = Get<HitPoints>(block).Value;

                if (hp <= 0)
                {
                    Destroy(block);
                    continue;
                }
            }

            var position = Get<Position>(block).Value;

            if (position.Y > -cam.Y + (Dimensions.GameHeight * 0.7))
            {
                Destroy(block);
                continue;
            }
        }


    }
}