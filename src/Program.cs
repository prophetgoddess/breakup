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
    Input Input;
    PlayerController PlayerController;
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

        Input = new Input(World, Inputs);
        PlayerController = new PlayerController(World);
        Motion = new Motion(World);

        // for (int i = 0; i < 50; i++)
        // {
        //     var sprite = World.CreateEntity();
        //     World.Set(sprite, new Sprite());
        //     World.Set(sprite, new Position(new Vector2(
        //                                       Random.Next(1280),
        //                                       Random.Next(720)
        //                                    )));
        //     //World.Set(sprite, new Orientation((float)Random.NextDouble() * System.MathF.PI * 2.0f));
        //     World.Set(sprite, new Velocity(new Vector2(
        //         (float)Random.NextDouble() * 100.0f * (Random.NextDouble() < 0.5f ? -1.0f : 1.0f),
        //         (float)Random.NextDouble() * 100.0f * (Random.NextDouble() < 0.5f ? -1.0f : 1.0f)
        //     )));
        //     World.Set(sprite, new BoundingBox(0, 0, 32, 32));
        //     World.Set(sprite, new SolidCollision());
        // }

        var sprite = World.CreateEntity();
        World.Set(sprite, new Model(Content.Models.Donut.ID));
        World.Set(sprite, new Scale(16.0f));
        World.Set(sprite, new Position(new Vector2(
                                          1280 * 0.5f,
                                          720 * 0.5f
                                       )));
        //World.Set(sprite, new Orientation((float)Random.NextDouble() * System.MathF.PI * 2.0f));
        World.Set(sprite, new Velocity(Vector2.Zero));
        World.Set(sprite, new BoundingBox(0, 0, 32, 32));
        World.Set(sprite, new SolidCollision());

        var player = World.CreateEntity();
        World.Set(player, new Model(Content.Models.Triangle.ID));
        World.Set(player, new Position(new Vector2(
                                          1280 * 0.5f,
                                          720 * 0.75f
                                       )));
        //World.Set(sprite, new Orientation((float)Random.NextDouble() * System.MathF.PI * 2.0f));
        World.Set(player, new Velocity(Vector2.Zero));
        World.Set(player, new BoundingBox(0, 0, 32, 32));
        World.Set(player, new SolidCollision());
        World.Set(player, new Scale(3.0f));
        World.Set(player, new Player());

        var leftBound = World.CreateEntity();
        World.Set(leftBound, new Position(new Vector2(-8, 0)));
        World.Set(leftBound, new BoundingBox(0, 0, 16, 720));
        World.Set(leftBound, new SolidCollision());

        var rightBound = World.CreateEntity();
        World.Set(rightBound, new Position(new Vector2(1280 + 8, 0)));
        World.Set(rightBound, new BoundingBox(0, 0, 16, 720));
        World.Set(rightBound, new SolidCollision());

        var topBound = World.CreateEntity();
        World.Set(topBound, new Position(new Vector2(0, -8)));
        World.Set(topBound, new BoundingBox(0, 0, 1280, 16));
        World.Set(topBound, new SolidCollision());

        var bottomBound = World.CreateEntity();
        World.Set(bottomBound, new Position(new Vector2(0, 720 + 8)));
        World.Set(bottomBound, new BoundingBox(0, 0, 1280, 16));
        World.Set(bottomBound, new SolidCollision());
    }

    protected override void Update(TimeSpan delta)
    {
        Input.Update(delta);
        PlayerController.Update(delta);
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
