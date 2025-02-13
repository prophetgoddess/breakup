using System.Drawing;
using System.Numerics;
using System.Reflection.Metadata;
using Microsoft.VisualBasic;
using MoonTools.ECS;
using MoonWorks;
using MoonWorks.Graphics.Font;
using MoonWorks.Math;
using Nito.Collections;

namespace Ball;



public class Blocks : MoonTools.ECS.System
{

    Filter BlockFilter;
    Filter IncomingFilter;
    public static int CellSize = 32;
    public static int GridWidth { get { return Dimensions.GameWidth / CellSize; } }
    public static int GridHeight { get { return Dimensions.GameHeight / CellSize; } }
    UpgradeMenuSpawner UpgradeMenuSpawner;
    MarqueeSpawner MarqueeSpawner;
    SaveGame SaveGame;

    Deque<Entity[]> BlockBuffer = new(GridHeight + 3);

    System.Random Random = new System.Random();
    float LastGridOffset = -1.0f;
    GemSpawner GemSpawner;
    BallSpawner BallSpawner;

    float MinBlockDensity = 0.1f;
    float MaxBlockDensity = 0.66f;

    float MaxHP = 99;
    float MinHP = 1;


    public Blocks(World world) : base(world)
    {
        GemSpawner = new GemSpawner(world);


        BlockFilter = FilterBuilder
        .Include<Block>()
        .Build();

        IncomingFilter = FilterBuilder.Include<IncomingIndicator>().Build();

        UpgradeMenuSpawner = new UpgradeMenuSpawner(world);
        BallSpawner = new BallSpawner(world);
        MarqueeSpawner = new MarqueeSpawner(world);

        SaveGame = new SaveGame(world);
    }

    Entity SpawnBlock(int x, int y, bool unbreakable = false)
    {
        var cameraY = GetSingleton<CameraPosition>().Y;

        int min = (int)float.Lerp(MinHP, MaxHP, cameraY / GameplaySettings.MaxCameraY);
        int max = min * 2;

        var hp = Rando.IntInclusive(min, max);

        if (Some<BlocksSpawnWithLessHealth>())
            hp = (int)MathF.Ceiling(hp * 0.7f);

        if (Rando.Value > 0.9f)
        {
            hp = Rando.IntInclusive(min * 2, max * 2);
        }

        var block = CreateEntity();
        Set(block, new Scale(Vector2.One * 32));
        Set(block, new Position(new Vector2(CellSize * 0.5f + x * CellSize, CellSize * 0.5f + y * CellSize)));
        Set(block, new BoundingBox(0, 0, 32, 32));
        Set(block, new SolidCollision());
        Set(block, new Block(hp));
        Set(block, new DestroyOnStateTransition());

        if (Rando.Value < 0.75f && !unbreakable)
        {
            Set(block, new HitPoints(hp, hp));
            Set(block, new SDFGraphic(Content.SDF.RoundedHollowSquare));

            Set(block, new CanTakeDamage());
            var hpDisplay = CreateEntity();
            Set(hpDisplay, new Position(new Vector2(CellSize * 0.5f + x * CellSize, CellSize * 0.5f + y * CellSize)));
            Set(hpDisplay, new DestroyOnStateTransition());
            Set(hpDisplay, new CanTakeDamage());
            Relate(block, hpDisplay, new HPDisplay());

            if (Rando.Value < 0.04f && UpgradeMenuSpawner.UpgradesAvailable() && !Some<GivesUpgrade>())
            {
                Set(block, new GivesUpgrade());
                Set(block, new Highlight());
                Set(hpDisplay, new Highlight());
                Set(block, new Pulsate(Vector2.One * 32, 3.0f, 0.2f));
                hp *= 2;
                Set(block, new HitPoints(hp, hp));
                Set(hpDisplay, new Text(Fonts.BodyFont, Fonts.InfoSize, Stores.TextStorage.GetID($"{GameStateManager.GetFormattedNumber(hp, 2)}"), MoonWorks.Graphics.Font.HorizontalAlignment.Center, MoonWorks.Graphics.Font.VerticalAlignment.Middle));
            }
            else if (Rando.Value < 0.01f && Some<SetHighScoreThisRun>())
            {
                Set(block, new GivesUnlock());
                Set(block, new Highlight());
                Set(block, new Block(0));
                Set(hpDisplay, new Scale(Vector2.One * 20));
                Set(block, new HitPoints(1, 1));
                Set(block, new Pulsate(Vector2.One * 32, 3.0f, 0.2f));
                Set(hpDisplay, new Highlight());
                Set(hpDisplay, new SDFGraphic(Content.SDF.Unlock));
            }
            else
            {
                Set(hpDisplay, new Text(Fonts.BodyFont, Fonts.InfoSize, Stores.TextStorage.GetID($"{GameStateManager.GetFormattedNumber(hp, 2)}"), MoonWorks.Graphics.Font.HorizontalAlignment.Center, MoonWorks.Graphics.Font.VerticalAlignment.Middle));
            }
        }
        else
        {
            Set(block, new SDFGraphic(Content.SDF.RoundedSquare));
        }

        return block;
    }

    void Initialize()
    {
        Destroy(GetSingletonEntity<Initialize>());
        LastGridOffset = 0.0f;

        for (int y = -3; y < GridHeight * 0.4f; y++)
        {
            var rowArray = new Entity[GridWidth];
            for (int x = 0; x < GridWidth; x++)
            {
                if (Random.NextDouble() < float.Lerp(MinBlockDensity, MaxBlockDensity, 0f))
                {
                    rowArray[x] =
                    SpawnBlock(
                        x,
                        y
                    );
                }
            }

            BlockBuffer.AddToBack(rowArray);
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

        var upcomingRow = BlockBuffer[2];

        IncomingFilter.DestroyAllEntities();

        for (int x = 0; x < GridWidth; x++)
        {
            if (Has<Block>(upcomingRow[x]))
            {
                var incomingEntity = CreateEntity();
                Set(incomingEntity, new Position(new Vector2(CellSize * 0.5f + x * CellSize, -cam.Y + CellSize * 0.5f)));
                Set(incomingEntity, new Scale(Vector2.One * 16));
                Set(incomingEntity, new BoundingBox(0, -CellSize * 1.5f, CellSize, CellSize));
                Set(incomingEntity, new CheckForStaticCollisions());
                Set(incomingEntity, new DestroyOnStateTransition());
                Set(incomingEntity, new SDFGraphic(Has<HitPoints>(upcomingRow[x]) ? Content.SDF.EmptyTriangle : Content.SDF.Triangle));
                Set(incomingEntity, new Depth(0.01f));
                Set(incomingEntity, new IncomingIndicator());
            }
        }

        if (cam.Y > LastGridOffset && cam.Y > LastGridOffset + CellSize && cam.Y < GameplaySettings.MaxCameraY)
        {
            LastGridOffset = cam.Y;

            int y = -(int)(MathF.Floor(cam.Y + CellSize) / CellSize) - 3;

            var rowArray = BlockBuffer.RemoveFromBack();
            Array.Clear(rowArray);

            for (int x = 0; x < GridWidth; x++)
            {
                if (Random.NextDouble() < float.Lerp(MinBlockDensity, MaxBlockDensity, cam.Y / GameplaySettings.MaxCameraY))
                {
                    rowArray[x] = SpawnBlock(x, y);
                }
            }

            BlockBuffer.AddToFront(rowArray);
        }

        foreach (var block in BlockFilter.Entities)
        {
            if (Has<HitPoints>(block))
            {
                var hp = Get<HitPoints>(block);
                var hpDisplay = OutRelationSingleton<HPDisplay>(block);

                if (Has<Text>(hpDisplay))
                    Set(hpDisplay, new Text(Fonts.BodyFont, Fonts.InfoSize, Stores.TextStorage.GetID($"{GameStateManager.GetFormattedNumber(hp.Value, 2)}"), MoonWorks.Graphics.Font.HorizontalAlignment.Center, MoonWorks.Graphics.Font.VerticalAlignment.Middle));

                if (hp.Value <= 0)
                {
                    if (Has<GivesUpgrade>(block))
                    {
                        UpgradeMenuSpawner.OpenUpgradeMenu();
                    }
                    else if (Has<GivesUnlock>(block))
                    {
                        var value = Rando.Value;
                        var index = -1;
                        if (value < 0.5f)
                        {
                            index = Music.UnlockSong();

                            if (index >= 0)
                            {

                                var name = Stores.TextStorage.Get(Music.Songs[index].NameID);

                                var entity = CreateEntity();
                                Set(entity, new Timer(10f));
                                Set(entity, new Position(new Vector2(Dimensions.GameWidth, 10f)));
                                Set(entity, new Highlight());
                                Set(entity,
                                 new Text(
                                    Fonts.BodyFont,
                                    Fonts.PromptSize,
                                    Stores.TextStorage.GetID($"new song: {name}"),
                                    HorizontalAlignment.Left,
                                    VerticalAlignment.Top));
                                Set(entity, new Depth(0.1f));
                                Set(entity, new Velocity(new Vector2(-200f, 0f)));
                                Set(entity, new FollowsCamera(5f));
                                Set(entity, new DestroyOnStateTransition());
                            }
                        }
                        if (value >= 0.5f || index == -1)
                        {
                            index = ColorPalettes.Unlock();

                            if (index >= 0)
                            {
                                var name = Stores.TextStorage.Get(ColorPalettes.Palettes[index].NameID);

                                var entity = CreateEntity();
                                Set(entity, new Timer(10f));
                                Set(entity, new Position(new Vector2(Dimensions.GameWidth, 10f)));
                                Set(entity, new Highlight());
                                Set(entity,
                                 new Text(
                                    Fonts.BodyFont,
                                    Fonts.PromptSize,
                                    Stores.TextStorage.GetID($"new palette: {name}"),
                                    HorizontalAlignment.Left,
                                    VerticalAlignment.Top));
                                Set(entity, new Depth(0.1f));
                                Set(entity, new Velocity(new Vector2(-200f, 0f)));
                                Set(entity, new FollowsCamera(5f));
                                Set(entity, new DestroyOnStateTransition());

                            }
                        }
                        SaveGame.Save();
                    }
                    else
                    {
                        var reward = Get<Block>(block).GemReward;
                        GemSpawner.SpawnGems(Rando.IntInclusive(reward, reward * 2), Get<Position>(block).Value);
                    }

                    if (Some<DestroyedBlocksDamageNeighbors>())
                    {
                        var pos = Get<Position>(block).Value;
                        var dmg = GetSingleton<Level>().Value + 1;

                        var up = CreateEntity();
                        Set(up, new Position(new Vector2(pos.X, pos.Y - CellSize)));
                        Set(up, new CanDealDamageToBlock(dmg));
                        Set(up, new BoundingBox(0, 0, 16, 16));
                        Set(up, new DestroyOnStateTransition());
                        Set(up, new Velocity(-Vector2.UnitY));
                        Set(up, new Timer(0.1f));

                        var down = CreateEntity();
                        Set(down, new Position(new Vector2(pos.X, pos.Y + CellSize)));
                        Set(down, new CanDealDamageToBlock());
                        Set(down, new BoundingBox(0, 0, 16, 16));
                        Set(down, new DestroyOnStateTransition());
                        Set(down, new Velocity(Vector2.UnitY));
                        Set(down, new Timer(0.1f));

                        var left = CreateEntity();
                        Set(left, new Position(new Vector2(pos.X - CellSize, pos.Y)));
                        Set(left, new CanDealDamageToBlock(dmg));
                        Set(left, new BoundingBox(0, 10, 16, 16));
                        Set(left, new DestroyOnStateTransition());
                        Set(left, new Velocity(-Vector2.UnitX));
                        Set(left, new Timer(0.1f));

                        var right = CreateEntity();
                        Set(right, new Position(new Vector2(pos.X + CellSize, pos.Y)));
                        Set(right, new CanDealDamageToBlock(dmg));
                        Set(right, new BoundingBox(0, 0, 16, 16));
                        Set(right, new DestroyOnStateTransition());
                        Set(right, new Velocity(Vector2.UnitX));
                        Set(right, new Timer(0.1f));
                    }

                    if (Some<BlocksSpawnBonusBalls>() && Rando.Value < 0.2f)
                    {
                        BallSpawner.SpawnBall(Get<Position>(block).Value);
                    }

                    if (Rando.Value < 0.01f)
                    {
                        var extraLife = CreateEntity();
                        Set(extraLife, new SDFGraphic(Content.SDF.Heart));
                        Set(extraLife, new Scale(Vector2.One * 20.0f));
                        Set(extraLife, new Position(Get<Position>(block).Value));
                        Set(extraLife, new Velocity(new Vector2(Rando.Range(-50f, 50f), Rando.Range(-100f, -10f))));
                        Set(extraLife, new BoundingBox(0, 0, 18, 18));
                        Set(extraLife, new HasGravity(1f));
                        Set(extraLife, new DestroyOnStateTransition());
                        Set(extraLife, new Highlight());
                        Set(extraLife, new GivesExtraLife());
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