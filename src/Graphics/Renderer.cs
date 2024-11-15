using MoonWorks;
using MoonWorks.Graphics;
using System.Numerics;
using Buffer = MoonWorks.Graphics.Buffer;
using MoonTools.ECS;
using System.Runtime.InteropServices;
using System.Net.Mime;
using MoonWorks.Graphics.Font;
using MoonWorks.Input;

namespace Ball;

public class Renderer : MoonTools.ECS.Renderer
{
    Window Window;
    GraphicsDevice GraphicsDevice;
    Inputs Inputs;

    GraphicsPipeline RenderPipeline;

    MoonTools.ECS.Filter ModelFilter;
    MoonTools.ECS.Filter UIFilter;
    MoonTools.ECS.Filter TextFilter;
    MoonTools.ECS.Filter ColliderFilter;

    Queue<TextBatch> TextBatchPool;
    GraphicsPipeline TextPipeline;

    Texture GameTexture;
    Texture UITexture;

    public Buffer RectIndexBuffer;
    public Buffer RectVertexBuffer;

    void CreateRenderTextures()
    {

        GameTexture = Texture.Create(GraphicsDevice, new TextureCreateInfo
        {
            Type = TextureType.TwoDimensional,
            Format = Window.SwapchainFormat,
            Usage = TextureUsageFlags.ColorTarget | TextureUsageFlags.Sampler,
            Height = Window.Height,
            Width = (uint)(Window.Height * Dimensions.GameAspectRatio),
            SampleCount = SampleCount.One,
            LayerCountOrDepth = 1,
            NumLevels = 1
        });

        UITexture = Texture.Create(GraphicsDevice, new TextureCreateInfo
        {
            Type = TextureType.TwoDimensional,
            Format = Window.SwapchainFormat,
            Usage = TextureUsageFlags.ColorTarget | TextureUsageFlags.Sampler,
            Height = Window.Height,
            Width = (uint)(Window.Height * Dimensions.WindowAspectRatio),
            SampleCount = SampleCount.One,
            LayerCountOrDepth = 1,
            NumLevels = 1
        });

    }

    TextBatch GetTextBatch()
    {
        if (TextBatchPool.Count > 0)
            return TextBatchPool.Dequeue();

        return new TextBatch(GraphicsDevice);
    }

    public Renderer(World world, Window window, GraphicsDevice graphicsDevice, Inputs inputs) : base(world)
    {
        GraphicsDevice = graphicsDevice;
        Window = window;
        Inputs = inputs;

        TextBatchPool = new Queue<TextBatch>();

        CreateRenderTextures();

        ModelFilter = FilterBuilder.Include<Model>().Include<Position>().Exclude<UI>().Build();
        UIFilter = FilterBuilder.Include<Model>().Include<Position>().Include<UI>().Build();
        TextFilter = FilterBuilder.Include<Text>().Include<Position>().Include<UI>().Build();
        ColliderFilter = FilterBuilder.Include<Position>().Include<BoundingBox>().Build();

        Shader vertShader = Shader.Create(
            GraphicsDevice,
            Path.Join(System.AppContext.BaseDirectory, "Shaders", "Vertex.vert.msl"),
            "main0",
            new ShaderCreateInfo
            {
                Stage = ShaderStage.Vertex,
                Format = ShaderFormat.MSL,
                NumUniformBuffers = 1
            }
        );

        Shader fragShader = Shader.Create(
            GraphicsDevice,
            Path.Join(System.AppContext.BaseDirectory, "Shaders", "Fragment.frag.msl"),
            "main0",
            new ShaderCreateInfo
            {
                Stage = ShaderStage.Fragment,
                Format = ShaderFormat.MSL,
            }
        );

        var renderPipelineCreateInfo = new GraphicsPipelineCreateInfo
        {
            TargetInfo = new GraphicsPipelineTargetInfo
            {
                ColorTargetDescriptions = [
                    new ColorTargetDescription
                    {
                        Format = Window.SwapchainFormat,
                        BlendState = ColorTargetBlendState.NonPremultipliedAlphaBlend
                    }
                ]
            },
            DepthStencilState = DepthStencilState.Disable,
            MultisampleState = MultisampleState.None,
            PrimitiveType = PrimitiveType.TriangleList,
            RasterizerState = RasterizerState.CCW_CullNone,
            VertexInputState = VertexInputState.CreateSingleBinding<PositionVertex>(),
            VertexShader = vertShader,
            FragmentShader = fragShader
        };

        RenderPipeline = GraphicsPipeline.Create(GraphicsDevice, renderPipelineCreateInfo);

        var textPipelineCreateInfo = new GraphicsPipelineCreateInfo
        {
            VertexShader = GraphicsDevice.TextVertexShader,
            FragmentShader = GraphicsDevice.TextFragmentShader,
            VertexInputState = GraphicsDevice.TextVertexInputState,
            PrimitiveType = PrimitiveType.TriangleList,
            RasterizerState = RasterizerState.CCW_CullNone,
            MultisampleState = MultisampleState.None,
            DepthStencilState = DepthStencilState.Disable,
            TargetInfo = new GraphicsPipelineTargetInfo
            {
                ColorTargetDescriptions = [
                    new ColorTargetDescription
                    {
                        Format = Window.SwapchainFormat,
                        BlendState = ColorTargetBlendState.NonPremultipliedAlphaBlend
                    }
                ]
            }
        };

        TextPipeline = GraphicsPipeline.Create(GraphicsDevice, textPipelineCreateInfo);

        var resourceUploader = new ResourceUploader(GraphicsDevice);
        RectVertexBuffer = resourceUploader.CreateBuffer(
            [
                new PositionVertex(new Vector3(-1, 1, 0) * 0.5f),
                new PositionVertex(new Vector3(1, 1, 0) * 0.5f),
                new PositionVertex(new Vector3(1, -1, 0) * 0.5f),
                new PositionVertex(new Vector3(-1, -1, 0) * 0.5f),
            ],
            BufferUsageFlags.Vertex
        );
        RectIndexBuffer = resourceUploader.CreateBuffer(
            [
                0,
                1,
                2,
                0,
                2,
                3,
            ],
            BufferUsageFlags.Index
        );
        resourceUploader.Upload();
        resourceUploader.Dispose();


    }

    public void Draw(CommandBuffer cmdbuf, Texture renderTexture)
    {
        if (Window.Height != GameTexture.Height || Window.Height != UITexture.Height)
        {
            renderTexture.Dispose();
            CreateRenderTextures();
        }

        if (renderTexture == null)
            return;

        var cameraPos = GetSingleton<CameraPosition>().Y;

        Matrix4x4 cameraMatrix =
        Matrix4x4.CreateOrthographicOffCenter(
            0,
            Dimensions.GameWidth,
            Dimensions.GameHeight - cameraPos,
            -cameraPos,
            0,
            -1f
        );

        var gamePass = cmdbuf.BeginRenderPass(
            new ColorTargetInfo(GameTexture, Color.GhostWhite)
        );

        gamePass.BindGraphicsPipeline(RenderPipeline);

        foreach (var entity in ModelFilter.Entities)
        {
            var position = Get<Position>(entity).Value;
            var rotation = Has<Orientation>(entity) ? Get<Orientation>(entity).Value : 0.0f;
            var mesh = Content.Models.IDToModel[Get<Model>(entity).ID];
            var scale = Has<Scale>(entity) ? Get<Scale>(entity).Value : Vector2.One;

            Matrix4x4 model = Matrix4x4.CreateScale(new Vector3(scale.X, scale.Y, 0)) * Matrix4x4.CreateFromAxisAngle(Vector3.UnitZ, rotation) * Matrix4x4.CreateTranslation(new Vector3(position, 0)) * cameraMatrix;
            var uniforms = new TransformVertexUniform(model, Color.DarkGray);

            gamePass.BindVertexBuffer(mesh.VertexBuffer);
            gamePass.BindIndexBuffer(mesh.IndexBuffer, IndexElementSize.ThirtyTwo);
            cmdbuf.PushVertexUniformData(uniforms);
            gamePass.DrawIndexedPrimitives(mesh.TriangleCount * 3, 1, 0, 0, 0);

        }

        if (Inputs.Keyboard.IsHeld(KeyCode.D1))
        {
            foreach (var entity in ColliderFilter.Entities)
            {
                var position = Get<Position>(entity).Value;
                var box = Get<BoundingBox>(entity);

                Matrix4x4 model = Matrix4x4.CreateScale(new Vector3(box.Width, box.Height, 0)) * Matrix4x4.CreateTranslation(new Vector3(position + new Vector2(box.X, box.Y), 0)) * cameraMatrix;
                var uniforms = new TransformVertexUniform(model, Color.Red * 0.5f);

                gamePass.BindVertexBuffer(RectVertexBuffer);
                gamePass.BindIndexBuffer(RectIndexBuffer, IndexElementSize.ThirtyTwo);
                cmdbuf.PushVertexUniformData(uniforms);
                gamePass.DrawIndexedPrimitives(6, 1, 0, 0, 0);
            }
        }

        cmdbuf.EndRenderPass(gamePass);

        cmdbuf.Blit(new BlitInfo
        {
            Source = new BlitRegion(GameTexture),
            Destination = new BlitRegion
            {
                Texture = UITexture,
                X = (uint)((UITexture.Width - GameTexture.Width) * 0.5f),
                W = GameTexture.Width,
                H = GameTexture.Height
            },
            LoadOp = LoadOp.Clear,
            ClearColor = Color.GhostWhite,
            FlipMode = FlipMode.None,
            Filter = MoonWorks.Graphics.Filter.Linear,
            Cycle = true
        });


        var uiPass = cmdbuf.BeginRenderPass(
            new ColorTargetInfo(UITexture, LoadOp.Load)
        );

        Matrix4x4 uiCameraMatrix =
        Matrix4x4.CreateOrthographicOffCenter(
            0,
            Dimensions.WindowWidth,
            Dimensions.WindowHeight,
            0,
            0,
            -1f
        );

        foreach (var entity in UIFilter.Entities)
        {
            var position = Get<Position>(entity).Value;
            var rotation = Has<Orientation>(entity) ? Get<Orientation>(entity).Value : 0.0f;
            var mesh = Content.Models.IDToModel[Get<Model>(entity).ID];
            var scale = Has<Scale>(entity) ? Get<Scale>(entity).Value : Vector2.One;

            Matrix4x4 model = Matrix4x4.CreateScale(new Vector3(scale.X, scale.Y, 0f)) * Matrix4x4.CreateFromAxisAngle(Vector3.UnitZ, rotation) * Matrix4x4.CreateTranslation(new Vector3(position, 0)) * uiCameraMatrix;
            var uniforms = new TransformVertexUniform(model, Color.DarkGray);

            uiPass.BindGraphicsPipeline(RenderPipeline);
            uiPass.BindVertexBuffer(mesh.VertexBuffer);
            uiPass.BindIndexBuffer(mesh.IndexBuffer, IndexElementSize.ThirtyTwo);
            cmdbuf.PushVertexUniformData(uniforms);
            uiPass.DrawIndexedPrimitives(mesh.TriangleCount * 3, 1, 0, 0, 0);

        }

        cmdbuf.EndRenderPass(uiPass);

        foreach (var textEntity in TextFilter.Entities)
        {
            var textBatch = GetTextBatch();
            var text = Get<Text>(textEntity);
            var position = Get<Position>(textEntity).Value;

            textBatch.Start(Stores.FontStorage.Get(text.FontID));
            textBatch.Add(Stores.TextStorage.Get(text.TextID), text.Size, Color.DarkGray, text.HorizontalAlignment, text.VerticalAlignment);
            textBatch.UploadBufferData(cmdbuf);

            var textModel = Matrix4x4.CreateTranslation(position.X, position.Y, 0f);

            var textPass = cmdbuf.BeginRenderPass(
                new ColorTargetInfo(UITexture, LoadOp.Load)
            );
            uiPass.BindGraphicsPipeline(TextPipeline);
            textBatch.Render(cmdbuf, textPass, textModel * uiCameraMatrix);
            cmdbuf.EndRenderPass(textPass);

            TextBatchPool.Enqueue(textBatch);
        }

        cmdbuf.Blit(new BlitInfo
        {
            Source = new BlitRegion(UITexture),
            Destination = new BlitRegion
            {
                Texture = renderTexture,
                X = (uint)((renderTexture.Width - UITexture.Width) * 0.5f),
                W = UITexture.Width,
                H = UITexture.Height
            },
            LoadOp = LoadOp.Clear,
            ClearColor = Color.Transparent,
            FlipMode = FlipMode.None,
            Filter = MoonWorks.Graphics.Filter.Linear,
            Cycle = true
        });

    }
}
