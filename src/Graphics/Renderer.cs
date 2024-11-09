using MoonWorks;
using MoonWorks.Graphics;
using System.Numerics;
using Buffer = MoonWorks.Graphics.Buffer;
using MoonTools.ECS;
using System.Runtime.InteropServices;
using System.Net.Mime;
using MoonWorks.Graphics.Font;

namespace Ball;

public class Renderer : MoonTools.ECS.Renderer
{
    Window Window;
    GraphicsDevice GraphicsDevice;

    GraphicsPipeline RenderPipeline;

    MoonTools.ECS.Filter ModelFilter;
    MoonTools.ECS.Filter UIFilter;

    TextBatch TextBatch;
    GraphicsPipeline TextPipeline;

    Texture GameTexture;
    Texture UITexture;

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

    public Renderer(World world, Window window, GraphicsDevice graphicsDevice) : base(world)
    {
        GraphicsDevice = graphicsDevice;
        Window = window;

        TextBatch = new TextBatch(GraphicsDevice);

        CreateRenderTextures();

        ModelFilter = FilterBuilder.Include<Model>().Include<Position>().Exclude<UI>().Build();
        UIFilter = FilterBuilder.Include<Model>().Include<Position>().Include<UI>().Build();

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
        Content.LoadAll(GraphicsDevice);

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

        //cmdbuf.PushVertexUniformData(cameraMatrix);

        var gamePass = cmdbuf.BeginRenderPass(
            new ColorTargetInfo(GameTexture, Color.GhostWhite)
        );

        foreach (var entity in ModelFilter.Entities)
        {
            var position = Get<Position>(entity).Value;
            var rotation = Has<Orientation>(entity) ? Get<Orientation>(entity).Value : 0.0f;
            var mesh = Content.Models.IDToModel[Get<Model>(entity).ID];
            var scale = Has<Scale>(entity) ? Get<Scale>(entity).Value : 1;

            Matrix4x4 model = Matrix4x4.CreateFromAxisAngle(Vector3.UnitZ, rotation) * Matrix4x4.CreateScale(Vector3.One * scale) * Matrix4x4.CreateTranslation(new Vector3(position, 0)) * cameraMatrix;
            var uniforms = new TransformVertexUniform(model);

            gamePass.BindGraphicsPipeline(RenderPipeline);
            gamePass.BindVertexBuffer(mesh.VertexBuffer);
            gamePass.BindIndexBuffer(mesh.IndexBuffer, IndexElementSize.ThirtyTwo);
            cmdbuf.PushVertexUniformData(uniforms);
            gamePass.DrawIndexedPrimitives(mesh.TriangleCount * 3, 1, 0, 0, 0);

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
            ClearColor = Color.Black,
            FlipMode = FlipMode.None,
            Filter = MoonWorks.Graphics.Filter.Linear,
            Cycle = true
        });

        TextBatch.Start(Content.Fonts.Kosugi);
        TextBatch.Add($"{(int)cameraPos}", 16, Color.White, HorizontalAlignment.Left, VerticalAlignment.Middle);
        TextBatch.UploadBufferData(cmdbuf);

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
            var scale = Has<Scale>(entity) ? Get<Scale>(entity).Value : 1;

            Matrix4x4 model = Matrix4x4.CreateFromAxisAngle(Vector3.UnitZ, rotation) * Matrix4x4.CreateScale(Vector3.One * scale) * Matrix4x4.CreateTranslation(new Vector3(position, 0)) * uiCameraMatrix;
            var uniforms = new TransformVertexUniform(model);

            uiPass.BindGraphicsPipeline(RenderPipeline);
            uiPass.BindVertexBuffer(mesh.VertexBuffer);
            uiPass.BindIndexBuffer(mesh.IndexBuffer, IndexElementSize.ThirtyTwo);
            cmdbuf.PushVertexUniformData(uniforms);
            uiPass.DrawIndexedPrimitives(mesh.TriangleCount * 3, 1, 0, 0, 0);

        }

        var textModel = Matrix4x4.CreateTranslation(Dimensions.GameWidth - 70, -cameraPos + 100, 0f);

        uiPass.BindGraphicsPipeline(TextPipeline);
        TextBatch.Render(cmdbuf, uiPass, textModel * cameraMatrix);

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
            Cycle = true
        });

    }
}
