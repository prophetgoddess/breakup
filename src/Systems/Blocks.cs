using System.Drawing;
using System.Numerics;
using MoonTools.ECS;
using MoonWorks.Math;

namespace Ball;

public class Blocks : MoonTools.ECS.System
{

    Filter BlockFilter;
    int CellSize = 32;
    int GridWidth { get { return Dimensions.GameWidth / CellSize; } }
    int GridHeight { get { return Dimensions.GameHeight / CellSize; } }

    System.Random Random = new System.Random();
    float LastGridOffset = -1.0f;
    GemSpawner GemSpawner;

    public Blocks(World world) : base(world)
    {
        GemSpawner = new GemSpawner(world);

        BlockFilter = FilterBuilder
        .Include<Block>()
        .Build();
    }

    void SpawnBlock(int x, int y)
    {
        var hp = Rando.IntInclusive(1, 4);

        var block = CreateEntity();
        Set(block, new Scale(Vector2.One * 1.9f));
        Set(block, new Position(new Vector2(CellSize * 0.5f + x * CellSize, CellSize * 0.5f + y * CellSize)));
        Set(block, new BoundingBox(0, 0, 32, 32));
        Set(block, new SolidCollision());
        Set(block, new Block(hp));
        Set(block, new DestroyOnRestartGame());


        if (Rando.Value < 0.75f)
        {
            Set(block, new HitPoints(hp, hp));
            Set(block, new Model(Content.Models.EmptySquare.ID));

            Set(block, new CanTakeDamageFromBall());
            var hpDisplay = CreateEntity();
            //Set(hpDisplay, new Scale(Vector2.One));
            Set(hpDisplay, new Position(new Vector2(CellSize * 0.5f + x * CellSize, CellSize * 0.5f + y * CellSize)));
            Set(hpDisplay, new DestroyOnRestartGame());
            //Set(hpDisplay, new Model(Content.Models.Square.ID));
            Relate(block, hpDisplay, new HPDisplay());
            Set(hpDisplay, new Text(Fonts.BodyFont, Fonts.InfoSize, Stores.TextStorage.GetID($"{hp}"), MoonWorks.Graphics.Font.HorizontalAlignment.Center, MoonWorks.Graphics.Font.VerticalAlignment.Middle));

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
                var hp = Get<HitPoints>(block);
                var hpDisplay = OutRelationSingleton<HPDisplay>(block);

                //Set(hpDisplay, new Scale(Vector2.One * (hp.Value / (float)hp.Max)));
                Set(hpDisplay, new Text(Fonts.BodyFont, Fonts.InfoSize, Stores.TextStorage.GetID($"{hp.Value}"), MoonWorks.Graphics.Font.HorizontalAlignment.Center, MoonWorks.Graphics.Font.VerticalAlignment.Middle));

                if (hp.Value <= 0)
                {
                    var reward = Get<Block>(block).GemReward;
                    GemSpawner.SpawnGems(Rando.IntInclusive(reward, reward * 2), Get<Position>(block).Value);
                    Destroy(hpDisplay);
                    Destroy(block);
                    continue;
                }
            }

            var position = Get<Position>(block).Value;

            if (position.Y > -cam.Y + (Dimensions.GameHeight * 0.7))
            {
                if (HasOutRelation<HPDisplay>(block))
                {
                    Destroy(OutRelationSingleton<HPDisplay>(block));
                }
                Destroy(block);
                continue;
            }
        }


    }
}