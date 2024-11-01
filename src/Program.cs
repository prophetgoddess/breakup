using MoonTools.ECS;
using MoonWorks;
using MoonWorks.Graphics;
using System.Numerics;

namespace Ball;

class Program : Game
{
    World World = new World();
    Renderer Renderer;
    Motion Motion;
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
        Renderer = new Renderer(World, MainWindow, GraphicsDevice);

        Motion = new Motion(World);

        for (int i = 0; i < 100; i++)
        {
            var sprite = World.CreateEntity();
            World.Set(sprite, new Sprite());
            World.Set(sprite, new Position(new Vector2(
                                              (float)Random.Next(1280),
                                              (float)Random.Next(720)
                                           )));
            //World.Set(sprite, new Orientation((float)Random.NextDouble() * System.MathF.PI * 2.0f));
            World.Set(sprite, new Velocity(new Vector2(
                (float)Random.NextDouble() * 100.0f * (Random.NextDouble() < 0.5f ? -1.0f : 1.0f),
                (float)Random.NextDouble() * 100.0f * (Random.NextDouble() < 0.5f ? -1.0f : 1.0f)
            )));
            World.Set(sprite, new BoundingBox(0, 0, 16, 16));
            World.Set(sprite, new SolidCollision());
        }
    }

    protected override void Update(TimeSpan delta)
    {
        Motion.Update(delta);
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
            1280,
            720,
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
