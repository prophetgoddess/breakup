using MoonTools.ECS;
using MoonWorks;
using MoonWorks.Graphics;
using System.Numerics;

namespace Ball;

class Program : Game
{
    World World = new World();
    Renderer Renderer;
    MoonTools.ECS.System[] Systems;

    public Program(
        WindowCreateInfo windowCreateInfo,
        FrameLimiterSettings frameLimiterSettings,
        ShaderFormat availableShaderFormats,
        int targetTimestep = 60,
        bool debugMode = false
    ) : base(
        windowCreateInfo,
        frameLimiterSettings,
        availableShaderFormats,
        targetTimestep,
        debugMode
    )
    {
        Content.LoadAll(GraphicsDevice);

        Systems =
        [
            new Input(World, Inputs),
            new GameState(World),
            new Time(World),
            new PlayerController(World),
            new Motion(World),
            new Collision(World),
            new FollowCamera(World),
            new Parent(World),
            new Blocks(World),
            new Meters(World)
        ];

        Renderer = new Renderer(World, MainWindow, GraphicsDevice, Inputs);

        var scoreLabel = World.CreateEntity();
        World.Set(scoreLabel, new Position(new Vector2(Dimensions.WindowWidth - 190, 40)));
        World.Set(scoreLabel,
         new Text(
            Stores.FontStorage.GetID(Content.Fonts.FX300Angular),
            28,
            Stores.TextStorage.GetID("SCORE")));
        World.Set(scoreLabel, new UI());

        var highScoreLabel = World.CreateEntity();
        World.Set(highScoreLabel, new Position(new Vector2(Dimensions.WindowWidth - 190, 120)));
        World.Set(highScoreLabel,
         new Text(
            Stores.FontStorage.GetID(Content.Fonts.FX300Angular),
            28,
            Stores.TextStorage.GetID("BEST")));
        World.Set(highScoreLabel, new UI());

        var highScoreEntity = World.CreateEntity();
        World.Set(highScoreEntity, new HighScore(0));
        World.Set(highScoreEntity, new Position(new Vector2(Dimensions.WindowWidth - 190, 140)));
        World.Set(highScoreEntity,
         new Text(
            Stores.FontStorage.GetID(Content.Fonts.FX300),
            24,
            Stores.TextStorage.GetID("")));
        World.Set(highScoreEntity, new UI());

    }

    protected override void Update(TimeSpan delta)
    {
        foreach (var system in Systems)
        {
            system.Update(delta);
        }
    }

    protected override void Draw(double alpha)
    {
        var cmdbuf = GraphicsDevice.AcquireCommandBuffer();
        var swapchainTexture = cmdbuf.AcquireSwapchainTexture(MainWindow);

        Renderer.Draw(cmdbuf, swapchainTexture);

        GraphicsDevice.Submit(cmdbuf);
    }

    static void Main(string[] args)
    {
        var debugMode = false;

#if DEBUG
        debugMode = true;
#endif

        var windowCreateInfo = new WindowCreateInfo(
            "Ball",
            Dimensions.WindowWidth,
            Dimensions.WindowHeight,
            ScreenMode.Windowed
        );

        var frameLimiterSettings = new FrameLimiterSettings(
            FrameLimiterMode.Capped,
            144
        );

        var game = new Program(
            windowCreateInfo,
            frameLimiterSettings,
            ShaderFormat.MSL,
            60,
            debugMode
        );

        game.Run();
    }
}
