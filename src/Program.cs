using MoonTools.ECS;
using MoonWorks;
using MoonWorks.Graphics;
using SDL3;
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
        FramePacingSettings frameLimiterSettings,
        ShaderFormat availableShaderFormats,
        bool debugMode = false
    ) : base(
        windowCreateInfo,
        frameLimiterSettings,
        availableShaderFormats,
        debugMode
    )
    {
        AppDomain.CurrentDomain.UnhandledException += HandleUnhandledException;

        Content.LoadAll(GraphicsDevice, AudioDevice);

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
            new Trail(World),
            new MarqueeController(World),
            new Audio(World, AudioDevice),
            new Upgrade(World)
        ];

        Renderer = new Renderer(World, MainWindow, GraphicsDevice, Inputs);

        World.Set(World.CreateEntity(), Palettes.MillenialApartment);

    }

    private static void HandleUnhandledException(object sender, UnhandledExceptionEventArgs args)
    {
        var e = (Exception)args.ExceptionObject;

        var outFile = Path.Combine(AppContext.BaseDirectory, $"error-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}.txt");
        File.WriteAllText(outFile, e.ToString());

        SDL.SDL_ShowSimpleMessageBox(
            SDL.SDL_MessageBoxFlags.SDL_MESSAGEBOX_ERROR,
            "FLAGRANT SYSTEM ERROR",
            e.ToString(),
            IntPtr.Zero
        );
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
            1280,
            720,
            ScreenMode.Windowed
        );

#else
        var windowCreateInfo = new WindowCreateInfo(
            "Ball",
            1920,
            1080,
            ScreenMode.Windowed
        );
#endif


        var frameLimiterSettings = FramePacingSettings.CreateLatencyOptimized(
            60
        );

        var game = new Program(
            windowCreateInfo,
            frameLimiterSettings,
            ShaderFormat.SPIRV | ShaderFormat.MSL,
            debugMode
        );

        game.Run();
    }
}
