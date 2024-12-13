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
    UpgradeMenuSpawner UpgradeMenuSpawner;


    System.Random Random = new System.Random();
    float LastGridOffset = -1.0f;
    GemSpawner GemSpawner;

    float MinBlockDensity = 0.1f;
    float MaxBlockDensity = 0.66f;
    float MaxCameraY = 10000;

    float MaxHP = 99;
    float MinHP = 1;

    string GetFormattedHP(int amount, int length = 2)
    {
        return amount >= 0
            ? amount.ToString($"D{length}")
            : amount.ToString($"D{length - 1}");
    }

    public Blocks(World world) : base(world)
    {
        GemSpawner = new GemSpawner(world);

        BlockFilter = FilterBuilder
        .Include<Block>()
        .Build();

        UpgradeMenuSpawner = new UpgradeMenuSpawner(world);
    }

    void SpawnBlock(int x, int y, bool barrier = false)
    {
        var cameraY = GetSingleton<CameraPosition>().Y;

        int min = (int)float.Lerp(MinHP, MaxHP, cameraY / MaxCameraY);
        int max = min * 2;

        var hp = Rando.IntInclusive(min, max);

        if (Rando.Value > 0.9f || barrier)
        {
            hp = Rando.IntInclusive(min * 2, max * 2);
        }

        var block = CreateEntity();
        Set(block, new Scale(Vector2.One * 1.9f));
        Set(block, new Position(new Vector2(CellSize * 0.5f + x * CellSize, CellSize * 0.5f + y * CellSize)));
        Set(block, new BoundingBox(0, 0, 32, 32));
        Set(block, new SolidCollision());
        Set(block, new Block(hp));
        Set(block, new DestroyOnStartGame());

        if (Rando.Value < 0.75f || barrier)
        {
            Set(block, new HitPoints(hp, hp));
            Set(block, new Model(Content.Models.RoundEmptySquare.ID));


            Set(block, new CanTakeDamageFromBall());
            var hpDisplay = CreateEntity();
            //Set(hpDisplay, new Scale(Vector2.One));
            Set(hpDisplay, new Position(new Vector2(CellSize * 0.5f + x * CellSize, CellSize * 0.5f + y * CellSize)));
            Set(hpDisplay, new DestroyOnStartGame());
            //Set(hpDisplay, new Model(Content.Models.Square.ID));
            Relate(block, hpDisplay, new HPDisplay());
            Set(hpDisplay, new Text(Fonts.BodyFont, Fonts.InfoSize, Stores.TextStorage.GetID($"{GetFormattedHP(hp)}"), MoonWorks.Graphics.Font.HorizontalAlignment.Center, MoonWorks.Graphics.Font.VerticalAlignment.Middle));

            if (Rando.Value < 0.01)
            {
                Set(block, new GivesUpgrade());
                Set(block, new Highlight());
                Set(hpDisplay, new Highlight());
                hp *= 2;
                Set(block, new HitPoints(hp, hp));
                Set(hpDisplay, new Text(Fonts.BodyFont, Fonts.InfoSize, Stores.TextStorage.GetID($"{GetFormattedHP(hp)}"), MoonWorks.Graphics.Font.HorizontalAlignment.Center, MoonWorks.Graphics.Font.VerticalAlignment.Middle));

            }

        }
        else
        {
            Set(block, new Model(Content.Models.RoundSquare.ID));
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
                if (Random.NextDouble() < float.Lerp(MinBlockDensity, MaxBlockDensity, 0f))
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
        if (Some<Pause>())
            return;

        if (!Some<CameraPosition>())
            return;

        if (Some<Initialize>())
            Initialize();

        var cam = GetSingleton<CameraPosition>();

        if (cam.Y > LastGridOffset && cam.Y > LastGridOffset + CellSize)
        {
            LastGridOffset = cam.Y;

            int y = -(int)(MathF.Floor(cam.Y + CellSize) / CellSize) - 3;

            for (int x = 0; x < GridWidth; x++)
            {
                if (Random.NextDouble() < float.Lerp(MinBlockDensity, MaxBlockDensity, cam.Y / MaxCameraY))
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

                Set(hpDisplay, new Text(Fonts.BodyFont, Fonts.InfoSize, Stores.TextStorage.GetID($"{GetFormattedHP(hp.Value)}"), MoonWorks.Graphics.Font.HorizontalAlignment.Center, MoonWorks.Graphics.Font.VerticalAlignment.Middle));

                if (hp.Value <= 0)
                {

                    if (Has<GivesUpgrade>(block))
                    {
                        UpgradeMenuSpawner.OpenUpgradeMenu();

                    }
                    else
                    {
                        var reward = Get<Block>(block).GemReward;
                        GemSpawner.SpawnGems(Rando.IntInclusive(reward, reward * 2), Get<Position>(block).Value);
                    }

                    Set(CreateEntity(), new PlayOnce(Stores.SFXStorage.GetID(Content.SFX.pop), true));
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