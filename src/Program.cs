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
    System.Random Random = new System.Random();

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
            new Flickering(World),
            new PlayerController(World),
            new PlayerAttractor(World),
            new Rotation(World),
            new Motion(World),
            new Collision(World),
            new FollowCamera(World),
            new Parent(World),
            new Blocks(World),
            new PowerMeter(World),
            new Stars(World),
        ];

        Renderer = new Renderer(World, MainWindow, GraphicsDevice, Inputs);

        World.Set(World.CreateEntity(), Palettes.MillenialApartment);

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
        var windowCreateInfo = new WindowCreateInfo(
            "Ball",
            Dimensions.WindowWidth,
            Dimensions.WindowHeight,
            ScreenMode.Windowed
        );

#else
        var windowCreateInfo = new WindowCreateInfo(
            "Ball",
            Dimensions.WindowWidth,
            Dimensions.WindowHeight,
            ScreenMode.Windowed
        );
#endif


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
