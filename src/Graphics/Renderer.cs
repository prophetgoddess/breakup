using MoonWorks;
using MoonWorks.Graphics;
using System.Numerics;
using Buffer = MoonWorks.Graphics.Buffer;
using MoonTools.ECS;
using System.Runtime.InteropServices;

public class Renderer : MoonTools.ECS.Renderer
{
    Window Window;
    GraphicsDevice GraphicsDevice;

    ComputePipeline ComputePipeline;
    GraphicsPipeline RenderPipeline;
    Sampler Sampler;
    TransferBuffer TransferBuffer;
    Buffer SpriteComputeBuffer;
    Buffer SpriteVertexBuffer;
    Buffer SpriteIndexBuffer;
    Texture Ravioli;
    System.Random Random = new System.Random();

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


    [StructLayout(LayoutKind.Explicit, Size = 48)]
    struct PositionTextureColorVertex : IVertexType
    {
        [FieldOffset(0)]
        public Vector4 Position;

        [FieldOffset(16)]
        public Vector2 TexCoord;

        [FieldOffset(32)]
        public Vector4 Color;

        public static VertexElementFormat[] Formats { get; } =
        [
            VertexElementFormat.Float4,
            VertexElementFormat.Float2,
            VertexElementFormat.Float4
        ];

        public static uint[] Offsets { get; } =
        [
            0,
            16,
            32
        ];
    }


    public Renderer(World world, Window window, GraphicsDevice graphicsDevice) : base(world)
    {
        GraphicsDevice = graphicsDevice;
        Window = window;

        var resourceUploader = new ResourceUploader(GraphicsDevice);
        Ravioli = resourceUploader.CreateTexture2DFromCompressed(
            Path.Join(System.AppContext.BaseDirectory, "Textures", "ravioli.png"),
            TextureFormat.R8G8B8A8Unorm,
            TextureUsageFlags.Sampler
        );
        resourceUploader.Upload();
        resourceUploader.Dispose();

        Shader vertShader = Shader.Create(
            GraphicsDevice,
            Path.Join(System.AppContext.BaseDirectory, "Shaders", "TexturedQuadColorWithMatrix.vert.msl"),
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
            Path.Join(System.AppContext.BaseDirectory, "Shaders", "TexturedQuadColor.frag.msl"),
            "main0",
            new ShaderCreateInfo
            {
                Stage = ShaderStage.Fragment,
                Format = ShaderFormat.MSL,
                NumSamplers = 1
            }
        );

        GraphicsPipelineCreateInfo renderPipelineCreateInfo = new GraphicsPipelineCreateInfo
        {
            TargetInfo = new GraphicsPipelineTargetInfo
            {
                ColorTargetDescriptions = [
                    new ColorTargetDescription
                    {
                        Format = Window.SwapchainFormat,
                        BlendState = ColorTargetBlendState.Opaque
                    }
                ]
            },
            DepthStencilState = DepthStencilState.Disable,
            MultisampleState = MultisampleState.None,
            PrimitiveType = PrimitiveType.TriangleList,
            RasterizerState = RasterizerState.CCW_CullNone,
            VertexInputState = VertexInputState.Empty,
            VertexShader = vertShader,
            FragmentShader = fragShader
        };
        renderPipelineCreateInfo.VertexInputState = VertexInputState.CreateSingleBinding<PositionTextureColorVertex>();

        RenderPipeline = GraphicsPipeline.Create(GraphicsDevice, renderPipelineCreateInfo);

        ComputePipeline = ComputePipeline.Create(
            GraphicsDevice,
            Path.Join(System.AppContext.BaseDirectory, "Shaders", "SpriteBatch.comp.msl"),
            "main0",
            new ComputePipelineCreateInfo
            {
                Format = ShaderFormat.MSL,
                NumReadonlyStorageBuffers = 1,
                NumReadWriteStorageBuffers = 1,
                ThreadCountX = 64,
                ThreadCountY = 1,
                ThreadCountZ = 1
            }
        );
        Sampler = Sampler.Create(GraphicsDevice, SamplerCreateInfo.PointClamp);

        TransferBuffer = TransferBuffer.Create<ComputeSpriteData>(
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

        Matrix4x4 cameraMatrix =
        Matrix4x4.CreateOrthographicOffCenter(
            0,
            1280,
            720,
            0,
            0,
            -1f
        );

        // Build sprite compute transfer
        var data = TransferBuffer.Map<ComputeSpriteData>(true);
        for (var i = 0; i < MAX_SPRITE_COUNT; i += 1)
        {
            data[i].Position = new Vector3(Random.Next(1280), Random.Next(720), 0);
            data[i].Rotation = (float)(Random.NextDouble() * System.Math.PI * 2);
            data[i].Size = new Vector2(32, 32);
            data[i].Color = new Vector4(1f, 1f, 1f, 1f);
        }
        TransferBuffer.Unmap();

        // Upload compute data to buffer
        var copyPass = cmdbuf.BeginCopyPass();
        copyPass.UploadToBuffer(TransferBuffer, SpriteComputeBuffer, true);
        cmdbuf.EndCopyPass(copyPass);

        // Set up compute pass to build sprite vertex buffer
        var computePass = cmdbuf.BeginComputePass(
            new StorageBufferReadWriteBinding(SpriteVertexBuffer, true)
        );

        computePass.BindComputePipeline(ComputePipeline);
        computePass.BindStorageBuffer(SpriteComputeBuffer);
        computePass.Dispatch(MAX_SPRITE_COUNT / 64, 1, 1);

        cmdbuf.EndComputePass(computePass);

        // Render sprites using vertex buffer
        var renderPass = cmdbuf.BeginRenderPass(
            new ColorTargetInfo(renderTexture, Color.CornflowerBlue)
        );

        cmdbuf.PushVertexUniformData(cameraMatrix);

        renderPass.BindGraphicsPipeline(RenderPipeline);
        renderPass.BindVertexBuffer(SpriteVertexBuffer);
        renderPass.BindIndexBuffer(SpriteIndexBuffer, IndexElementSize.ThirtyTwo);
        renderPass.BindFragmentSampler(new TextureSamplerBinding(Ravioli, Sampler));
        renderPass.DrawIndexedPrimitives(MAX_SPRITE_COUNT * 6, 1, 0, 0, 0);

        cmdbuf.EndRenderPass(renderPass);

    }
}
