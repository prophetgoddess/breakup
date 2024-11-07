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
        Systems =
        [
            new Input(World, Inputs),
            new Time(World),
            new PlayerController(World),
            new Motion(World),
            new FollowCamera(World)
        ];

        Renderer = new Renderer(World, MainWindow, GraphicsDevice);

        for (var x = 0; x < 10; x++)
        {
            for (var y = 0; y < 3; y++)
            {
                var block = World.CreateEntity();
                World.Set(block, new Model(Content.Models.Square.ID));
                World.Set(block, new Scale(2.0f));
                World.Set(block, new Position(new Vector2(
                                                  200 + x * 100,
                                                30 + y * 100
                                               )));
                World.Set(block, new BoundingBox(0, 0, 32, 32));
                World.Set(block, new DestroyOnContactWithBall());
                World.Set(block, new SolidCollision());
            }
        }

        var ball = World.CreateEntity();
        World.Set(ball, new Model(Content.Models.Donut.ID));
        World.Set(ball, new Scale(16.0f));
        World.Set(ball, new Position(new Vector2(
                                          1280 * 0.5f,
                                          720 * 0.5f
                                       )));
        World.Set(ball, new Velocity(Vector2.Zero));
        World.Set(ball, new BoundingBox(0, 0, 32, 32));
        World.Set(ball, new SolidCollision());
        World.Set(ball, new Bounce());
        World.Set(ball, new CanBeHit());
        World.Set(ball, new HasGravity());

        var player = World.CreateEntity();
        World.Set(player, new Model(Content.Models.Triangle.ID));
        World.Set(player, new Position(new Vector2(
                                          1280 * 0.5f,
                                          720 * 0.9f
                                       )));
        World.Set(player, new Orientation(0f));
        World.Set(player, new Velocity(Vector2.Zero));
        World.Set(player, new BoundingBox(0, 0, 32, 32));
        World.Set(player, new SolidCollision());
        World.Set(player, new HitBall());
        World.Set(player, new Scale(3.0f));
        World.Set(player, new Player());
        World.Set(player, new FollowsCamera(720 * 0.9f));

        var leftBound = World.CreateEntity();
        World.Set(leftBound, new Position(new Vector2(-8, 0)));
        World.Set(leftBound, new BoundingBox(0, 0, 16, 2000));
        World.Set(leftBound, new SolidCollision());
        World.Set(leftBound, new FollowsCamera(0));

        var rightBound = World.CreateEntity();
        World.Set(rightBound, new Position(new Vector2(1280 + 8, 0)));
        World.Set(rightBound, new BoundingBox(0, 0, 16, 2000));
        World.Set(rightBound, new SolidCollision());
        World.Set(rightBound, new FollowsCamera(0));


        // var topBound = World.CreateEntity();
        // World.Set(topBound, new Position(new Vector2(0, -8)));
        // World.Set(topBound, new BoundingBox(0, 0, 1280, 16));
        // World.Set(topBound, new SolidCollision());

        var bottomBound = World.CreateEntity();
        World.Set(bottomBound, new Position(new Vector2(0, 720 + 8)));
        World.Set(bottomBound, new BoundingBox(0, 0, 1280, 16));
        World.Set(bottomBound, new SolidCollision());
        World.Set(bottomBound, new ResetBallOnHit());
        World.Set(bottomBound, new FollowsCamera(720 + 8));


        var cameraEntity = World.CreateEntity();
        World.Set(cameraEntity, new CameraPosition(0f));

        //World.Relate(player, ball, new IgnoreSolidCollision());
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
