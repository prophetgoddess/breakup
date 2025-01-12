using MoonWorks;
using MoonWorks.Graphics;
using System.Numerics;
using Buffer = MoonWorks.Graphics.Buffer;
using MoonTools.ECS;
using MoonWorks.Graphics.Font;
using MoonWorks.Input;
using System.Text;
using System.Runtime.InteropServices;

namespace Ball;

readonly record struct DepthUniforms(float ZNear, float ZFar);

public class Renderer : MoonTools.ECS.Renderer
{

    Window Window;
    GraphicsDevice GraphicsDevice;
    Inputs Inputs;
    ComputePipeline ComputePipeline;
    GraphicsPipeline ModelPipeline;
    GraphicsPipeline SDFPipeline;

    MoonTools.ECS.Filter ModelFilter;
    MoonTools.ECS.Filter SDFFilter;

    MoonTools.ECS.Filter UIFilter;
    MoonTools.ECS.Filter TextFilter;
    MoonTools.ECS.Filter GameTextFilter;
    MoonTools.ECS.Filter ColliderFilter;

    Queue<TextBatch> TextBatchPool;
    Queue<(Vector2 pos, float depth, TextBatch batch)> GameTextBatchesToRender = new Queue<(Vector2 pos, float depth, TextBatch batch)>();
    Queue<(Vector2 pos, float depth, TextBatch batch)> UITextBatchesToRender = new Queue<(Vector2 pos, float depth, TextBatch batch)>();
    GraphicsPipeline TextPipeline;

    Texture GameTexture;
    Texture UITexture;
    Texture DepthTexture;
    Texture UIDepthTexture;
    Sampler DepthSampler;
    Sampler SDFSampler;
    DepthUniforms DepthUniforms;
    StringBuilder StringBuilder = new StringBuilder();

    public Buffer RectIndexBuffer;
    public Buffer RectVertexBuffer;

    Buffer SpriteComputeBuffer;
    Buffer SpriteVertexBuffer;
    Buffer SpriteIndexBuffer;
    const int MAX_SPRITE_COUNT = 8192;

    [StructLayout(LayoutKind.Explicit, Size = 48)]
    struct ComputeSpriteData
    {
        [FieldOffset(0)]
        public Vector3 Position;

        [FieldOffset(12)]
        public float Rotation;

        [FieldOffset(16)]
        public Vector2 Size;

        [FieldOffset(32)]
        public Vector4 Color;
    }


    TransferBuffer SpriteComputeTransferBuffer;

    void CreateRenderTextures()
    {
        var windowRatio = Window.Width / Window.Height;

        if (GameTexture != null)
            GameTexture.Dispose();

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

        if (DepthTexture != null)
            DepthTexture.Dispose();

        DepthTexture = Texture.Create(GraphicsDevice, new TextureCreateInfo
        {
            Type = TextureType.TwoDimensional,
            Format = TextureFormat.D16Unorm,
            Usage = TextureUsageFlags.DepthStencilTarget | TextureUsageFlags.Sampler,
            Height = GameTexture.Height,
            Width = GameTexture.Width,
            SampleCount = SampleCount.One,
            LayerCountOrDepth = 1,
            NumLevels = 1
        });

        if (UITexture != null)
        {
            UITexture.Dispose();
        }


        if (windowRatio >= Dimensions.UIAspectRatio)
        {
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
        }
        else
        {
            UITexture = Texture.Create(GraphicsDevice, new TextureCreateInfo
            {
                Type = TextureType.TwoDimensional,
                Format = Window.SwapchainFormat,
                Usage = TextureUsageFlags.ColorTarget | TextureUsageFlags.Sampler,
                Height = (uint)(Window.Width * Dimensions.UIAspectRatioReciprocal),
                Width = Window.Width,
                SampleCount = SampleCount.One,
                LayerCountOrDepth = 1,
                NumLevels = 1
            });
        }

        if (UIDepthTexture != null)
        {
            UIDepthTexture.Dispose();
        }

        UIDepthTexture = Texture.Create(GraphicsDevice, new TextureCreateInfo
        {
            Type = TextureType.TwoDimensional,
            Format = TextureFormat.D16Unorm,
            Usage = TextureUsageFlags.DepthStencilTarget | TextureUsageFlags.Sampler,
            Height = UITexture.Height,
            Width = UITexture.Width,
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
        SDFFilter = FilterBuilder.Include<SDFGraphic>().Include<Position>().Exclude<UI>().Exclude<Invisible>().Build();
        UIFilter = FilterBuilder.Include<Model>().Include<Position>().Include<UI>().Exclude<Invisible>().Build();
        TextFilter = FilterBuilder.Include<Text>().Include<Position>().Include<UI>().Exclude<Invisible>().Build();
        GameTextFilter = FilterBuilder.Include<Text>().Include<Position>().Exclude<UI>().Exclude<Invisible>().Build();
        ColliderFilter = FilterBuilder.Include<Position>().Include<BoundingBox>().Build();

        Shader modelVertShader = ShaderCross.Create(
            GraphicsDevice,
            Path.Join(System.AppContext.BaseDirectory, "Shaders", "Vertex.vert.spv"),
            "main",
            ShaderCross.ShaderFormat.SPIRV,
            ShaderStage.Vertex
        );

        Shader modelFragShader = ShaderCross.Create(
            GraphicsDevice,
            Path.Join(System.AppContext.BaseDirectory, "Shaders", "Fragment.frag.spv"),
            "main",
            ShaderCross.ShaderFormat.SPIRV,
            ShaderStage.Fragment
        );

        Shader sdfVertShader = ShaderCross.Create(
            GraphicsDevice,
            Path.Join(System.AppContext.BaseDirectory, "Shaders", "TexturedQuadColorWithMatrix.vert.spv"),
            "main",
            ShaderCross.ShaderFormat.SPIRV,
            ShaderStage.Vertex
        );

        Shader sdfFragShader = ShaderCross.Create(
            GraphicsDevice,
            Path.Join(System.AppContext.BaseDirectory, "Shaders", "TexturedQuadColor.frag.spv"),
            "main",
            ShaderCross.ShaderFormat.SPIRV,
            ShaderStage.Fragment
        );

        ComputePipeline = ShaderCross.Create(
            GraphicsDevice,
            Path.Join(System.AppContext.BaseDirectory, "Shaders", "SpriteBatch.comp.spv"),
            "main",
            ShaderCross.ShaderFormat.SPIRV
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
            VertexShader = modelVertShader,
            FragmentShader = modelFragShader
        };

        ModelPipeline = GraphicsPipeline.Create(GraphicsDevice, renderPipelineCreateInfo);

        var sdfPipelineCreateInfo = new GraphicsPipelineCreateInfo
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
            VertexInputState = VertexInputState.CreateSingleBinding<PositionTextureColorVertex>(),
            VertexShader = sdfVertShader,
            FragmentShader = sdfFragShader
        };

        SDFPipeline = GraphicsPipeline.Create(graphicsDevice, sdfPipelineCreateInfo);
        SDFSampler = Sampler.Create(GraphicsDevice, SamplerCreateInfo.LinearClamp);

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

        resourceUploader.Upload();
        resourceUploader.Dispose();

        SpriteComputeTransferBuffer = TransferBuffer.Create<ComputeSpriteData>(
            GraphicsDevice,
            TransferBufferUsage.Upload,
            MAX_SPRITE_COUNT
        );

        SpriteComputeBuffer = Buffer.Create<ComputeSpriteData>(
            GraphicsDevice,
            BufferUsageFlags.ComputeStorageRead,
            MAX_SPRITE_COUNT
        );

        SpriteVertexBuffer = Buffer.Create<PositionTextureColorVertex>(
            GraphicsDevice,
            BufferUsageFlags.ComputeStorageWrite | BufferUsageFlags.Vertex,
            MAX_SPRITE_COUNT * 4
        );

        SpriteIndexBuffer = Buffer.Create<uint>(
            GraphicsDevice,
            BufferUsageFlags.Index,
            MAX_SPRITE_COUNT * 6
        );

        TransferBuffer spriteIndexTransferBuffer = TransferBuffer.Create<uint>(
            GraphicsDevice,
            TransferBufferUsage.Upload,
            MAX_SPRITE_COUNT * 6
        );

        var indexSpan = spriteIndexTransferBuffer.Map<uint>(false);

        for (int i = 0, j = 0; i < MAX_SPRITE_COUNT * 6; i += 6, j += 4)
        {
            indexSpan[i] = (uint)j;
            indexSpan[i + 1] = (uint)j + 1;
            indexSpan[i + 2] = (uint)j + 2;
            indexSpan[i + 3] = (uint)j + 3;
            indexSpan[i + 4] = (uint)j + 2;
            indexSpan[i + 5] = (uint)j + 1;
        }
        spriteIndexTransferBuffer.Unmap();

        var cmdbuf = GraphicsDevice.AcquireCommandBuffer();
        var copyPass = cmdbuf.BeginCopyPass();
        copyPass.UploadToBuffer(spriteIndexTransferBuffer, SpriteIndexBuffer, false);
        cmdbuf.EndCopyPass(copyPass);
        GraphicsDevice.Submit(cmdbuf);
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

        var data = SpriteComputeTransferBuffer.Map<ComputeSpriteData>(true);
        int sdfIndex = 0;
        foreach (var entity in SDFFilter.Entities)
        {
            var position = Get<Position>(entity).Value;
            var rotation = Has<Orientation>(entity) ? Get<Orientation>(entity).Value : 0.0f;
            var uv = Get<SDFGraphic>(entity).UV;
            var scale = Has<Scale>(entity) ? Get<Scale>(entity).Value : Vector2.One;
            var color = Has<Highlight>(entity) ? palette.Highlight : palette.Foreground;
            color.A = Has<Alpha>(entity) ? Get<Alpha>(entity).A : color.A;
            var depth = Has<Depth>(entity) ? Get<Depth>(entity).Value : 0.5f;

            data[sdfIndex].Position = new Vector3(position.X, position.Y, 0);
            data[sdfIndex].Rotation = rotation;
            data[sdfIndex].Size = scale;
            data[sdfIndex].Color = color.ToVector4();
            sdfIndex++;
        }
        SpriteComputeTransferBuffer.Unmap();

        var copyPass = cmdbuf.BeginCopyPass();
        copyPass.UploadToBuffer(SpriteComputeTransferBuffer, SpriteComputeBuffer, true);
        cmdbuf.EndCopyPass(copyPass);

        var computePass = cmdbuf.BeginComputePass(
            new StorageBufferReadWriteBinding(SpriteVertexBuffer, true)
        );

        computePass.BindComputePipeline(ComputePipeline);
        computePass.BindStorageBuffers(SpriteComputeBuffer);
        computePass.Dispatch(MAX_SPRITE_COUNT / 64, 1, 1);

        cmdbuf.EndComputePass(computePass);

        var gamePass = cmdbuf.BeginRenderPass(
            new DepthStencilTargetInfo(DepthTexture, 1f, false),
            new ColorTargetInfo(GameTexture, palette.Background)
        );

        gamePass.BindGraphicsPipeline(ModelPipeline);

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

        cmdbuf.PushVertexUniformData(cameraMatrix);

        gamePass.BindGraphicsPipeline(SDFPipeline);
        gamePass.BindVertexBuffers(SpriteVertexBuffer);
        gamePass.BindIndexBuffer(SpriteIndexBuffer, IndexElementSize.ThirtyTwo);
        gamePass.BindFragmentSamplers(new TextureSamplerBinding(Content.SDF.Atlas, SDFSampler));
        gamePass.DrawIndexedPrimitives((uint)SDFFilter.Count * 6, 1, 0, 0, 0);

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
            new DepthStencilTargetInfo(UIDepthTexture, 1f, false),
            new ColorTargetInfo(UITexture, LoadOp.Load)
        );

        gamePass.BindGraphicsPipeline(ModelPipeline);

        Matrix4x4 uiCameraMatrix =
        Matrix4x4.CreateOrthographicOffCenter(
            0,
            Dimensions.UIWidth,
            Dimensions.UIHeight,
            0,
            0,
            -1f
        );

        uiPass.BindGraphicsPipeline(ModelPipeline);

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
                Y = (uint)((renderTexture.Height - UITexture.Height) * 0.5f),
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
