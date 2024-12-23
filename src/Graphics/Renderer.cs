using MoonWorks;
using MoonWorks.Graphics;
using System.Numerics;
using Buffer = MoonWorks.Graphics.Buffer;
using MoonTools.ECS;
using MoonWorks.Graphics.Font;
using MoonWorks.Input;
using System.Text;

namespace Ball;

readonly record struct DepthUniforms(float ZNear, float ZFar);

public class Renderer : MoonTools.ECS.Renderer
{

    Window Window;
    GraphicsDevice GraphicsDevice;
    Inputs Inputs;

    GraphicsPipeline RenderPipeline;
    GraphicsPipeline TexturePipeline;

    MoonTools.ECS.Filter ModelFilter;
    MoonTools.ECS.Filter UIFilter;
    MoonTools.ECS.Filter TextFilter;
    MoonTools.ECS.Filter GameTextFilter;
    MoonTools.ECS.Filter ColliderFilter;

    Queue<TextBatch> TextBatchPool;
    Queue<(Vector2 pos, float depth, TextBatch batch)> GameTextBatchesToRender = new Queue<(Vector2 pos, float depth, TextBatch batch)>();
    Queue<(Vector2 pos, float depth, TextBatch batchs)> UITextBatchesToRender = new Queue<(Vector2 pos, float depth, TextBatch batch)>();
    GraphicsPipeline TextPipeline;

    Texture GameTexture;
    Texture UITexture;
    Texture DepthTexture;
    Texture UIDepthTexture;
    Sampler DepthSampler;
    Sampler TextureSampler;
    DepthUniforms DepthUniforms;
    StringBuilder StringBuilder = new StringBuilder();

    Texture QRCode;

    public Buffer RectIndexBuffer;
    public Buffer RectVertexBuffer;

    private Buffer QuadVertexBuffer;
    private Buffer QuadIndexBuffer;

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

        DepthTexture = Texture.Create(GraphicsDevice, new TextureCreateInfo
        {
            Type = TextureType.TwoDimensional,
            Format = TextureFormat.D16Unorm,
            Usage = TextureUsageFlags.DepthStencilTarget | TextureUsageFlags.Sampler,
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
            Width = (uint)(Window.Height * Dimensions.UIAspectRatio),
            SampleCount = SampleCount.One,
            LayerCountOrDepth = 1,
            NumLevels = 1
        });

        UIDepthTexture = Texture.Create(GraphicsDevice, new TextureCreateInfo
        {
            Type = TextureType.TwoDimensional,
            Format = TextureFormat.D16Unorm,
            Usage = TextureUsageFlags.DepthStencilTarget | TextureUsageFlags.Sampler,
            Height = Window.Height,
            Width = (uint)(Window.Height * Dimensions.UIAspectRatio),
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

        DepthSampler = Sampler.Create(GraphicsDevice, new SamplerCreateInfo());
        DepthUniforms = new DepthUniforms(0.01f, 100f);

        ModelFilter = FilterBuilder.Include<Model>().Include<Position>().Exclude<UI>().Exclude<Invisible>().Build();
        UIFilter = FilterBuilder.Include<Model>().Include<Position>().Include<UI>().Exclude<Invisible>().Build();
        TextFilter = FilterBuilder.Include<Text>().Include<Position>().Include<UI>().Exclude<Invisible>().Build();
        GameTextFilter = FilterBuilder.Include<Text>().Include<Position>().Exclude<UI>().Exclude<Invisible>().Build();
        ColliderFilter = FilterBuilder.Include<Position>().Include<BoundingBox>().Build();

        Shader vertShader = ShaderCross.Create(
            GraphicsDevice,
            Path.Join(System.AppContext.BaseDirectory, "Shaders", "Vertex.vert.spv"),
            "main",
            ShaderCross.ShaderFormat.SPIRV,
            ShaderStage.Vertex
        );

        Shader fragShader = ShaderCross.Create(
            GraphicsDevice,
            Path.Join(System.AppContext.BaseDirectory, "Shaders", "Fragment.frag.spv"),
            "main",
            ShaderCross.ShaderFormat.SPIRV,
            ShaderStage.Fragment
        );

        Shader texturedVertShader = ShaderCross.Create(
            GraphicsDevice,
            Path.Join(System.AppContext.BaseDirectory, "Shaders", "TexturedQuad.vert.spv"),
            "main",
            ShaderCross.ShaderFormat.SPIRV,
            ShaderStage.Vertex
        );

        Shader texturedFragShader = ShaderCross.Create(
            GraphicsDevice,
            Path.Join(System.AppContext.BaseDirectory, "Shaders", "TexturedQuad.frag.spv"),
            "main",
            ShaderCross.ShaderFormat.SPIRV,
            ShaderStage.Fragment
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
                    ],
                HasDepthStencilTarget = true,
                DepthStencilFormat = TextureFormat.D16Unorm
            },
            DepthStencilState = new DepthStencilState
            {
                EnableDepthTest = true,
                EnableDepthWrite = true,
                CompareOp = CompareOp.LessOrEqual
            },
            MultisampleState = MultisampleState.None,
            PrimitiveType = PrimitiveType.TriangleList,
            RasterizerState = RasterizerState.CCW_CullNone,
            VertexInputState = VertexInputState.CreateSingleBinding<PositionVertex>(),
            VertexShader = vertShader,
            FragmentShader = fragShader
        };

        RenderPipeline = GraphicsPipeline.Create(GraphicsDevice, renderPipelineCreateInfo);

        var texturePipelineCreateInfo = new GraphicsPipelineCreateInfo
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
            DepthStencilState = new DepthStencilState
            {
                EnableDepthTest = true,
                EnableDepthWrite = true,
                CompareOp = CompareOp.LessOrEqual
            },
            MultisampleState = MultisampleState.None,
            PrimitiveType = PrimitiveType.TriangleList,
            RasterizerState = RasterizerState.CCW_CullNone,
            VertexInputState = VertexInputState.CreateSingleBinding<PositionTextureVertex>(),
            VertexShader = texturedVertShader,
            FragmentShader = texturedFragShader
        };

        TexturePipeline = GraphicsPipeline.Create(graphicsDevice, texturePipelineCreateInfo);
        TextureSampler = Sampler.Create(GraphicsDevice, SamplerCreateInfo.PointClamp);

        var textPipelineCreateInfo = new GraphicsPipelineCreateInfo
        {
            VertexShader = GraphicsDevice.TextVertexShader,
            FragmentShader = GraphicsDevice.TextFragmentShader,
            VertexInputState = GraphicsDevice.TextVertexInputState,
            PrimitiveType = PrimitiveType.TriangleList,
            RasterizerState = RasterizerState.CCW_CullNone,
            MultisampleState = MultisampleState.None,
            TargetInfo = new GraphicsPipelineTargetInfo
            {
                ColorTargetDescriptions = [
                        new ColorTargetDescription
                        {
                            Format = Window.SwapchainFormat,
                            BlendState = ColorTargetBlendState.PremultipliedAlphaBlend
                        }
                    ],
                HasDepthStencilTarget = true,
                DepthStencilFormat = TextureFormat.D16Unorm
            },
            DepthStencilState = new DepthStencilState
            {
                EnableDepthTest = true,
                EnableDepthWrite = true,
                CompareOp = CompareOp.LessOrEqual
            },
        };

        TextPipeline = GraphicsPipeline.Create(GraphicsDevice, textPipelineCreateInfo);

        Span<PositionTextureVertex> vertexData = [
            new PositionTextureVertex(new Vector3(-1,  1, 0), new Vector2(0, 0)),
            new PositionTextureVertex(new Vector3( 1,  1, 0), new Vector2(4, 0)),
            new PositionTextureVertex(new Vector3( 1, -1, 0), new Vector2(4, 4)),
            new PositionTextureVertex(new Vector3(-1, -1, 0), new Vector2(0, 4)),
        ];

        Span<ushort> indexData = [
            0, 1, 2,
            0, 2, 3,
        ];


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

        QuadVertexBuffer = resourceUploader.CreateBuffer(vertexData, BufferUsageFlags.Vertex);
        QuadIndexBuffer = resourceUploader.CreateBuffer(indexData, BufferUsageFlags.Index);

        QRCode = resourceUploader.CreateTexture2DFromCompressed(
            Path.Join(
                System.AppContext.BaseDirectory,
                "qrcode.png"
            ),
            Window.SwapchainFormat,
            TextureUsageFlags.Sampler
        );

        resourceUploader.Upload();
        resourceUploader.Dispose();
    }

    public void Draw(CommandBuffer cmdbuf, Texture renderTexture)
    {

        if (renderTexture == null)
            return;

        if (Window.Height != UITexture.Height)
        {
            CreateRenderTextures();
        }

        var palette = GetSingleton<Palette>();

        var cameraPos = 0f;

        if (Some<CameraPosition>())
            cameraPos = GetSingleton<CameraPosition>().Y;

        Matrix4x4 cameraMatrix =
        Matrix4x4.CreateOrthographicOffCenter(
            0,
            Dimensions.GameWidth,
            Dimensions.GameHeight - cameraPos,
            -cameraPos,
            0,
            -1f
        );

        foreach (var textEntity in GameTextFilter.Entities)
        {
            if (Has<Invisible>(textEntity))
                continue;

            var text = Get<Text>(textEntity);
            var color = Has<Highlight>(textEntity) ? palette.Highlight : palette.Foreground;
            var position = Get<Position>(textEntity).Value;
            var depth = Has<Depth>(textEntity) ? Get<Depth>(textEntity).Value : 0.5f;
            color.A = Has<Alpha>(textEntity) ? Get<Alpha>(textEntity).A : (byte)255;

            if (Some<Pause>() && !Has<KeepOpacityWhenPaused>(textEntity))
                color.A = 200;

            if (!Has<WordWrap>(textEntity))
            {
                var textBatch = GetTextBatch();
                textBatch.Start(Stores.FontStorage.Get(text.FontID));
                textBatch.Add(Stores.TextStorage.Get(text.TextID), text.Size, color, text.HorizontalAlignment, text.VerticalAlignment);
                textBatch.UploadBufferData(cmdbuf);

                GameTextBatchesToRender.Enqueue((position, depth, textBatch));
            }
            else
            {
                TextBatch textBatch;
                var max = Get<WordWrap>(textEntity).Max;
                var font = Stores.FontStorage.Get(text.FontID);
                var str = Stores.TextStorage.Get(text.TextID);
                var words = str.Split(' ');
                StringBuilder.Clear();
                var y = position.Y;
                var current = "";
                WellspringCS.Wellspring.Rectangle rect;

                foreach (var word in words)
                {
                    StringBuilder.Append(word);
                    StringBuilder.Append(" ");
                    font.TextBounds(StringBuilder.ToString(), text.Size, text.HorizontalAlignment, text.VerticalAlignment, out rect);
                    if (rect.W >= max)
                    {
                        textBatch = GetTextBatch();
                        textBatch.Start(font);
                        textBatch.Add(current, text.Size, color, text.HorizontalAlignment, text.VerticalAlignment);
                        textBatch.UploadBufferData(cmdbuf);
                        GameTextBatchesToRender.Enqueue((new Vector2(position.X, y), depth, textBatch));
                        y += rect.H + 2;
                        StringBuilder.Clear();
                        StringBuilder.Append(word);
                        StringBuilder.Append(" ");
                    }
                    current = StringBuilder.ToString();
                }

                textBatch = GetTextBatch();
                textBatch.Start(Stores.FontStorage.Get(text.FontID));
                textBatch.Add(StringBuilder.ToString(), text.Size, color, text.HorizontalAlignment, text.VerticalAlignment);
                textBatch.UploadBufferData(cmdbuf);
                GameTextBatchesToRender.Enqueue((new Vector2(position.X, y), depth, textBatch));
            }
        }

        foreach (var textEntity in TextFilter.Entities)
        {
            if (Has<Invisible>(textEntity))
                continue;

            var textBatch = GetTextBatch();
            var text = Get<Text>(textEntity);
            var color = Has<Highlight>(textEntity) ? palette.Highlight : palette.Foreground;
            var position = Get<Position>(textEntity).Value;
            var depth = Has<Depth>(textEntity) ? Get<Depth>(textEntity).Value : 0.5f;

            textBatch.Start(Stores.FontStorage.Get(text.FontID));
            textBatch.Add(Stores.TextStorage.Get(text.TextID), text.Size, color, text.HorizontalAlignment, text.VerticalAlignment);
            textBatch.UploadBufferData(cmdbuf);

            UITextBatchesToRender.Enqueue((position, depth, textBatch));
        }

        var gamePass = cmdbuf.BeginRenderPass(
            new DepthStencilTargetInfo(DepthTexture, 1f, true),
            new ColorTargetInfo(GameTexture, palette.Background)
        );

        gamePass.BindGraphicsPipeline(RenderPipeline);

        foreach (var entity in ModelFilter.Entities)
        {
            var position = Get<Position>(entity).Value;
            var rotation = Has<Orientation>(entity) ? Get<Orientation>(entity).Value : 0.0f;
            var mesh = Content.Models.IDToModel[Get<Model>(entity).ID];
            var scale = Has<Scale>(entity) ? Get<Scale>(entity).Value : Vector2.One;
            var color = Has<Highlight>(entity) ? palette.Highlight : palette.Foreground;
            color.A = Has<Alpha>(entity) ? Get<Alpha>(entity).A : color.A;
            var depth = Has<Depth>(entity) ? Get<Depth>(entity).Value : 0.5f;

            if (Some<Pause>() && !Has<KeepOpacityWhenPaused>(entity))
                color.A = 128;

            Matrix4x4 model =
                Matrix4x4.CreateScale(new Vector3(scale.X, scale.Y, 0f)) *
                Matrix4x4.CreateFromAxisAngle(Vector3.UnitZ, rotation) *
                Matrix4x4.CreateTranslation(new Vector3(position, depth)) * cameraMatrix;

            var uniforms = new TransformVertexUniform(model, color);

            gamePass.BindVertexBuffers(mesh.VertexBuffer);
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

                gamePass.BindVertexBuffers(RectVertexBuffer);
                gamePass.BindIndexBuffer(RectIndexBuffer, IndexElementSize.ThirtyTwo);
                cmdbuf.PushVertexUniformData(uniforms);
                gamePass.DrawIndexedPrimitives(6, 1, 0, 0, 0);
            }
        }

        gamePass.BindGraphicsPipeline(TextPipeline);

        while (GameTextBatchesToRender.Count > 0)
        {
            var (position, depth, textBatch) = GameTextBatchesToRender.Dequeue();

            var textModel = Matrix4x4.CreateTranslation(position.X, position.Y, depth);

            textBatch.Render(gamePass, textModel * cameraMatrix);
            TextBatchPool.Enqueue(textBatch);
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
            ClearColor = palette.Background,
            FlipMode = FlipMode.None,
            Filter = MoonWorks.Graphics.Filter.Linear,
            Cycle = false
        });


        var uiPass = cmdbuf.BeginRenderPass(
            new DepthStencilTargetInfo(UIDepthTexture, 1f, true),
            new ColorTargetInfo(UITexture, LoadOp.Load)
        );

        gamePass.BindGraphicsPipeline(RenderPipeline);

        Matrix4x4 uiCameraMatrix =
        Matrix4x4.CreateOrthographicOffCenter(
            0,
            Dimensions.UIWidth,
            Dimensions.UIHeight,
            0,
            0,
            -1f
        );

        uiPass.BindGraphicsPipeline(RenderPipeline);

        foreach (var entity in UIFilter.Entities)
        {
            var position = Get<Position>(entity).Value;
            var rotation = Has<Orientation>(entity) ? Get<Orientation>(entity).Value : 0.0f;
            var mesh = Content.Models.IDToModel[Get<Model>(entity).ID];
            var scale = Has<Scale>(entity) ? Get<Scale>(entity).Value : Vector2.One;
            var color = Has<Highlight>(entity) ? palette.Highlight : palette.Foreground;

            Matrix4x4 model = Matrix4x4.CreateScale(new Vector3(scale.X, scale.Y, 0f)) * Matrix4x4.CreateFromAxisAngle(Vector3.UnitZ, rotation) * Matrix4x4.CreateTranslation(new Vector3(position, 0)) * uiCameraMatrix;
            var uniforms = new TransformVertexUniform(model, color);

            uiPass.BindVertexBuffers(mesh.VertexBuffer);
            uiPass.BindIndexBuffer(mesh.IndexBuffer, IndexElementSize.ThirtyTwo);
            cmdbuf.PushVertexUniformData(uniforms);
            uiPass.DrawIndexedPrimitives(mesh.TriangleCount * 3, 1, 0, 0, 0);
        }

        uiPass.BindGraphicsPipeline(TextPipeline);

        while (UITextBatchesToRender.Count > 0)
        {
            var (position, depth, textBatch) = UITextBatchesToRender.Dequeue();

            var textModel = Matrix4x4.CreateTranslation(position.X, position.Y, depth);

            textBatch.Render(uiPass, textModel * uiCameraMatrix);
            TextBatchPool.Enqueue(textBatch);
        }

        cmdbuf.EndRenderPass(uiPass);

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
            Cycle = false
        });

    }
}
