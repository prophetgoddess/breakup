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
    GraphicsPipeline SDFPipeline;

    MoonTools.ECS.Filter SDFFilter;
    MoonTools.ECS.Filter UISDFFilter;

    MoonTools.ECS.Filter TextFilter;
    MoonTools.ECS.Filter GameTextFilter;
    MoonTools.ECS.Filter ColliderFilter;
    List<Entity> SDFSort = new();

    TextBatch GameTextBatch;
    TextBatch UITextBatch;
    GraphicsPipeline TextPipeline;

    PriorityQueue<Entity, float> DrawPriority = new();

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

    [StructLayout(LayoutKind.Explicit, Size = 64)]
    struct ComputeSpriteData
    {
        [FieldOffset(0)]
        public Vector3 Position;

        [FieldOffset(12)]
        public float Rotation;

        [FieldOffset(16)]
        public Vector2 Size;

        [FieldOffset(24)]
        public Vector2 Origin;

        [FieldOffset(32)]
        public Vector4 Color;

        [FieldOffset(48)]
        public Vector4 TextureRect;
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

    public Renderer(World world, Window window, GraphicsDevice graphicsDevice, Inputs inputs) : base(world)
    {
        GraphicsDevice = graphicsDevice;
        Window = window;
        Inputs = inputs;

        GameTextBatch = new TextBatch(GraphicsDevice);
        UITextBatch = new TextBatch(GraphicsDevice);

        CreateRenderTextures();

        DepthSampler = Sampler.Create(GraphicsDevice, new SamplerCreateInfo());
        DepthUniforms = new DepthUniforms(0.01f, 100f);

        SDFFilter = FilterBuilder.Include<SDFGraphic>().Include<Position>().Exclude<UI>().Exclude<Invisible>().Build();
        UISDFFilter = FilterBuilder.Include<SDFGraphic>().Include<Position>().Include<UI>().Exclude<Invisible>().Build();
        TextFilter = FilterBuilder.Include<Text>().Include<Position>().Include<UI>().Exclude<Invisible>().Build();
        GameTextFilter = FilterBuilder.Include<Text>().Include<Position>().Exclude<UI>().Exclude<Invisible>().Build();
        ColliderFilter = FilterBuilder.Include<Position>().Include<BoundingBox>().Build();

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

        var sdfPipelineCreateInfo = new GraphicsPipelineCreateInfo
        {
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

    public void SDFLoad(MoonTools.ECS.Filter filter, CommandBuffer cmdbuf)
    {
        var data = SpriteComputeTransferBuffer.Map<ComputeSpriteData>(true);
        int sdfIndex = 0;

        var palette = GetSingleton<Palette>();

        DrawPriority.Clear();

        foreach (var entity in filter.Entities)
        {
            DrawPriority.Enqueue(entity, 1.0f - (Has<Depth>(entity) ? Get<Depth>(entity).Value : 0.5f));
        }

        while (DrawPriority.Count > 0)
        {
            var entity = DrawPriority.Dequeue();
            var position = Get<Position>(entity).Value;
            var rotation = Has<Orientation>(entity) ? Get<Orientation>(entity).Value : 0.0f;
            var uv = Get<SDFGraphic>(entity).UV;
            var scale = Has<Scale>(entity) ? Get<Scale>(entity).Value : Vector2.One;
            var color = Has<Highlight>(entity) ? palette.Highlight : palette.Foreground;
            color.A = Has<Alpha>(entity) ? Get<Alpha>(entity).A : color.A;
            var depth = Has<Depth>(entity) ? Get<Depth>(entity).Value : 0.5f;

            if (Some<Pause>() && !Has<KeepOpacityWhenPaused>(entity))
                color.A = 200;

            data[sdfIndex].Position = new Vector3(position.X, position.Y, depth);
            data[sdfIndex].Rotation = rotation;
            data[sdfIndex].Size = scale;
            data[sdfIndex].Color = color.ToVector4();
            data[sdfIndex].TextureRect = uv;
            data[sdfIndex].Origin = Has<Origin>(entity) ? Get<Origin>(entity).Value : Vector2.One * 0.5f;
            sdfIndex++;

            if (sdfIndex >= data.Length)
                break;
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

        GameTextBatch.Start();
        DrawPriority.Clear();

        foreach (var textEntity in GameTextFilter.Entities)
        {
            DrawPriority.Enqueue(textEntity, 1.0f - (Has<Depth>(textEntity) ? Get<Depth>(textEntity).Value : 0.5f));
        }

        while (DrawPriority.Count > 0)
        {
            var textEntity = DrawPriority.Dequeue();
            if (Has<Invisible>(textEntity))
                continue;

            if (Some<Pause>() && !Has<KeepOpacityWhenPaused>(textEntity))
                continue;

            var text = Get<Text>(textEntity);
            var color = Has<Highlight>(textEntity) ? palette.Highlight : palette.Foreground;
            var position = Get<Position>(textEntity).Value;
            var depth = Has<Depth>(textEntity) ? Get<Depth>(textEntity).Value : 0.5f;
            color.A = Has<Alpha>(textEntity) ? Get<Alpha>(textEntity).A : (byte)255;


            if (!Has<WordWrap>(textEntity))
            {
                GameTextBatch.Add(
                    Stores.FontStorage.Get(text.FontID),
                    Stores.TextStorage.Get(text.TextID),
                    text.Size,
                    Matrix4x4.CreateTranslation(new Vector3(position.X, position.Y, depth)),
                    color,
                    text.HorizontalAlignment,
                    text.VerticalAlignment
                );
            }
            else
            {
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
                        GameTextBatch.Add(font, current, text.Size, Matrix4x4.CreateTranslation(new Vector3(position.X, y, depth)), color, text.HorizontalAlignment, text.VerticalAlignment);
                        y += rect.H + 2;
                        StringBuilder.Clear();
                        StringBuilder.Append(word);
                        StringBuilder.Append(" ");
                    }
                    current = StringBuilder.ToString();
                }

                GameTextBatch.Add(font, current, text.Size, Matrix4x4.CreateTranslation(new Vector3(position.X, y, depth)), color, text.HorizontalAlignment, text.VerticalAlignment);

            }
        }

        GameTextBatch.UploadBufferData(cmdbuf);

        int count = 0;
        UITextBatch.Start();
        foreach (var textEntity in TextFilter.Entities)
        {
            if (Has<Invisible>(textEntity))
                continue;

            var text = Get<Text>(textEntity);
            var color = Has<Highlight>(textEntity) ? palette.Highlight : palette.Foreground;
            var position = Get<Position>(textEntity).Value;
            var depth = Has<Depth>(textEntity) ? Get<Depth>(textEntity).Value : 0.5f;

            UITextBatch.Add(
                Stores.FontStorage.Get(text.FontID),
                Stores.TextStorage.Get(text.TextID),
                text.Size,
                Matrix4x4.CreateTranslation(new Vector3(position.X, position.Y, depth)),
                color,
                text.HorizontalAlignment,
                text.VerticalAlignment
            );

            count++;
        }
        UITextBatch.UploadBufferData(cmdbuf);

        SDFLoad(SDFFilter, cmdbuf);

        var gamePass = cmdbuf.BeginRenderPass(
            new DepthStencilTargetInfo(DepthTexture, 1f, false),
            new ColorTargetInfo(GameTexture, palette.Background)
        );

        if (SDFFilter.Count > 0)
        {
            cmdbuf.PushVertexUniformData(cameraMatrix);

            gamePass.BindGraphicsPipeline(SDFPipeline);
            gamePass.BindVertexBuffers(SpriteVertexBuffer);
            gamePass.BindIndexBuffer(SpriteIndexBuffer, IndexElementSize.ThirtyTwo);
            gamePass.BindFragmentSamplers(new TextureSamplerBinding(Content.SDF.Atlas, SDFSampler));
            gamePass.DrawIndexedPrimitives((uint)SDFFilter.Count * 6, 1, 0, 0, 0);
        }

        gamePass.BindGraphicsPipeline(TextPipeline);

        if (GameTextBatch.VertexCount > 0)
            GameTextBatch.Render(gamePass, cameraMatrix);

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

        SDFLoad(UISDFFilter, cmdbuf);

        var uiPass = cmdbuf.BeginRenderPass(
            new DepthStencilTargetInfo(UIDepthTexture, 1f, false),
            new ColorTargetInfo(UITexture, LoadOp.Load)
        );

        Matrix4x4 uiCameraMatrix =
        Matrix4x4.CreateOrthographicOffCenter(
            0,
            Dimensions.UIWidth,
            Dimensions.UIHeight,
            0,
            0,
            -1f
        );

        if (UISDFFilter.Count > 0)
        {
            cmdbuf.PushVertexUniformData(uiCameraMatrix);

            uiPass.BindGraphicsPipeline(SDFPipeline);
            uiPass.BindVertexBuffers(SpriteVertexBuffer);
            uiPass.BindIndexBuffer(SpriteIndexBuffer, IndexElementSize.ThirtyTwo);
            uiPass.BindFragmentSamplers(new TextureSamplerBinding(Content.SDF.Atlas, SDFSampler));
            uiPass.DrawIndexedPrimitives((uint)UISDFFilter.Count * 6, 1, 0, 0, 0);
        }

        uiPass.BindGraphicsPipeline(TextPipeline);

        if (UITextBatch.VertexCount > 0)
            UITextBatch.Render(uiPass, uiCameraMatrix);

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
