using MoonTools.ECS;
using MoonWorks;
using MoonWorks.Graphics;
using SDL3;
using Steamworks;

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
        Content.LoadAll(MainWindow.SwapchainFormat, GraphicsDevice, AudioDevice);

        var saveData = new SaveGame(World).Load();
        MainWindow.SetScreenMode(saveData.Fullscreen ? ScreenMode.Fullscreen : ScreenMode.Windowed);
        World.Set(World.CreateEntity(), new Fullscreen(saveData.Fullscreen));


        Systems =
        [
            new Input(World, Inputs),
            new GameStateManager(World, this),
            new Time(World),
            new Flickering(World),
            new PlayerController(World),
            new PlayerAttractor(World),
            new Rotation(World),
            new Motion(World),
            new Collision(World),
            new FollowCamera(World, Inputs),
            new Parent(World),
            new Blocks(World),
            new PowerMeter(World),
            new Trail(World),
            new MarqueeController(World),
            new Audio(World, AudioDevice),
            new Upgrade(World),
            new Animations(World),
            new Settings(World, MainWindow)
        ];

        Renderer = new Renderer(World, MainWindow, GraphicsDevice, Inputs);

        World.Set(World.CreateEntity(), ColorPalettes.Palettes[0]);

        new MainMenuSpawner(World).OpenMainMenu();

        SDL.SDL_SetWindowResizable(MainWindow.Handle, true);

    }

    private static void HandleUnhandledException(object sender, UnhandledExceptionEventArgs args)
    {
        var e = (Exception)args.ExceptionObject;

        var outFile = Path.Combine(AppContext.BaseDirectory, $"error-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}.txt");
        File.WriteAllText(outFile, e.ToString());

        SDL.SDL_ShowSimpleMessageBox(
            SDL.SDL_MessageBoxFlags.SDL_MESSAGEBOX_ERROR,
            "FLAGRANT SYSTEM ERROR",
            $"Please find the error log in the game directory and send it to prophet_goddess@protonmail.com: {e}",
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
        AppDomain.CurrentDomain.UnhandledException += HandleUnhandledException;

        if (SteamAPI.RestartAppIfNecessary((AppId_t)3397340))
        {
            var outFile = Path.Combine(AppContext.BaseDirectory, $"restart-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}.txt");
            File.WriteAllText(outFile, "Restarting");
            Console.WriteLine("Restarting...");
            return;
        }

        if (!SteamAPI.Init())
        {
            var outFile = Path.Combine(AppContext.BaseDirectory, $"error-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}.txt");
            File.WriteAllText(outFile, "Failed to initialize Steam API!");
            Console.WriteLine("Failed to initialize Steam API!");
            return;
        }

        var debugMode = false;

#if DEBUG
        debugMode = true;
        var windowCreateInfo = new WindowCreateInfo(
            "break.up",
            1280,
            720,
            ScreenMode.Windowed
        );

#else
        var windowCreateInfo = new WindowCreateInfo(
            "break.up",
            1280,
            720,
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
