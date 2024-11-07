using MoonWorks;
using MoonWorks.Graphics;
using System.Numerics;
using Buffer = MoonWorks.Graphics.Buffer;
using MoonTools.ECS;
using System.Runtime.InteropServices;
using System.Net.Mime;

namespace Ball;

public class Renderer : MoonTools.ECS.Renderer
{
    Window Window;
    GraphicsDevice GraphicsDevice;

    GraphicsPipeline RenderPipeline;

    MoonTools.ECS.Filter ModelFilter;

    Texture RenderTexture;

    void CreateRenderTexture()
    {

        RenderTexture = Texture.Create(GraphicsDevice, new TextureCreateInfo
        {
            Type = TextureType.TwoDimensional,
            Format = Window.SwapchainFormat,
            Usage = TextureUsageFlags.ColorTarget | TextureUsageFlags.Sampler,
            Height = Window.Height,
            Width = (uint)(Window.Height * 4.0f / 3.0f),
            SampleCount = SampleCount.One,
            LayerCountOrDepth = 1,
            NumLevels = 1
        });

    }

    public Renderer(World world, Window window, GraphicsDevice graphicsDevice) : base(world)
    {
        GraphicsDevice = graphicsDevice;
        Window = window;

        CreateRenderTexture();

        ModelFilter = FilterBuilder.Include<Model>().Include<Position>().Build();

        Shader vertShader = Shader.Create(
            GraphicsDevice,
            Path.Join(System.AppContext.BaseDirectory, "Shaders", "PositionColorWithMatrix.vert.msl"),
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
            Path.Join(System.AppContext.BaseDirectory, "Shaders", "SolidColor.frag.msl"),
            "main0",
            new ShaderCreateInfo
            {
                Stage = ShaderStage.Fragment,
                Format = ShaderFormat.MSL,
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
                        BlendState = ColorTargetBlendState.NonPremultipliedAlphaBlend
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
        renderPipelineCreateInfo.VertexInputState = VertexInputState.CreateSingleBinding<PositionColorVertex>();

        RenderPipeline = GraphicsPipeline.Create(GraphicsDevice, renderPipelineCreateInfo);

        Content.LoadAll(GraphicsDevice);

    }

    public void Draw(CommandBuffer cmdbuf, Texture renderTexture)
    {
        if (Window.Height != RenderTexture.Height)
        {
            renderTexture.Dispose();
            CreateRenderTexture();
        }

        if (renderTexture == null)
            return;

        var cameraPos = GetSingleton<CameraPosition>().Y;

        Matrix4x4 cameraMatrix =
        Matrix4x4.CreateOrthographicOffCenter(
            0,
            Dimensions.WindowWidth,
            Dimensions.WindowHeight - cameraPos,
            -cameraPos,
            0,
            -1f
        );

        //cmdbuf.PushVertexUniformData(cameraMatrix);

        var renderPass = cmdbuf.BeginRenderPass(
            new ColorTargetInfo(RenderTexture, Color.GhostWhite)
        );

        foreach (var entity in ModelFilter.Entities)
        {
            var position = Get<Position>(entity).value;
            var rotation = Has<Orientation>(entity) ? Get<Orientation>(entity).value : 0.0f;
            var mesh = Content.Models.IDToModel[Get<Model>(entity).ID];
            var scale = Has<Scale>(entity) ? Get<Scale>(entity).value : 1;

            Matrix4x4 model = Matrix4x4.CreateFromAxisAngle(Vector3.UnitZ, rotation) * Matrix4x4.CreateScale(Vector3.One * scale) * Matrix4x4.CreateTranslation(new Vector3(position, 0)) * cameraMatrix;
            var uniforms = new TransformVertexUniform(model);

            renderPass.BindGraphicsPipeline(RenderPipeline);
            renderPass.BindVertexBuffer(mesh.VertexBuffer);
            renderPass.BindIndexBuffer(mesh.IndexBuffer, IndexElementSize.ThirtyTwo);
            cmdbuf.PushVertexUniformData(uniforms);
            renderPass.DrawIndexedPrimitives(mesh.TriangleCount * 3, 1, 0, 0, 0);

        }

        cmdbuf.EndRenderPass(renderPass);

        cmdbuf.Blit(new BlitInfo
        {
            Source = new BlitRegion(RenderTexture),
            Destination = new BlitRegion
            {
                Texture = renderTexture,
                X = (uint)((renderTexture.Width - RenderTexture.Width) * 0.5f),
                W = RenderTexture.Width,
                H = RenderTexture.Height
            },
            LoadOp = LoadOp.Clear,
            ClearColor = Color.Black,
            FlipMode = FlipMode.None,
            Filter = MoonWorks.Graphics.Filter.Nearest,
            Cycle = true
        });

    }
}
