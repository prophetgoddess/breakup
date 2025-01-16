import argparse, os, os.path, subprocess, shutil, json, hashlib, pickle
from pathlib import Path


def get_digest(path):
    with open(path, "rb") as f:
        return hashlib.file_digest(f, "sha256").digest()


#
# PARSER
#

parser = argparse.ArgumentParser(
    prog="Asset Cooker", description="Cooks assets for MoonWorks."
)

parser.add_argument("-f", "--force", action="store_true", dest="force")
parser.add_argument("-i" "--in", dest="input")
parser.add_argument("-o" "--out", dest="output")
parser.add_argument("-p" "--project", dest="project")

args = parser.parse_args()

#
# SETUP
#

input = os.path.abspath(args.input)
output = os.path.abspath(args.output)
project = os.path.abspath(args.project)

generated = os.path.join(os.path.dirname(project), "Generated")

hashes = {}

if os.path.isfile(os.path.join(input, "hashes")):
    with open(os.path.join(input, "hashes"), "rb") as f:
        hashes = pickle.load(f)

if not os.path.isdir(generated):
    os.mkdir(generated)

content = os.path.join(generated, "Content.cs")

shader_in = os.path.join(input, "Shaders")
svg_in = os.path.join(input, "SVG")
texture_in = os.path.join(input, "Textures")
levels_in = os.path.join(input, "Levels")
models_in = os.path.join(input, "Models")
sfx_in = os.path.join(input, "SFX")
music_in = os.path.join(input, "Music")
fonts_in = os.path.join(input, "Fonts")
text_in = os.path.join(input, "Text")

shader_out = os.path.join(output, "Shaders")
texture_out = os.path.join(output, "Textures")
svg_out = os.path.join(texture_in, "SDF")
sfx_out = os.path.join(output, "SFX")
music_out = os.path.join(output, "Music")
fonts_out = os.path.join(output, "Fonts")
text_out = os.path.join(output, "Text")
levels_out = os.path.join(output, "Levels")


if not os.path.exists(project):
    print(f"ERROR: could not find {project}.")

if not os.path.exists(input):
    print(f"ERROR: could not find {input}.")

if not os.path.exists(output):
    os.mkdir(output)

#
# SHADER COMPILE
#

if os.path.exists(shader_in):
    if not os.path.isdir(shader_out):
        os.mkdir(shader_out)

    for shader in Path(shader_in).glob("**/*.*"):
        subprocess.run(
            [
                "glslc",
                shader,
                "-o",
                f"{os.path.join(shader_out, os.path.basename(shader))}.spv",
            ]
        )

#
# SDF RENDER
#

if os.path.exists(svg_in):
    if not os.path.isdir(texture_out):
        os.mkdir(texture_out)

    if not os.path.isdir(svg_out):
        os.mkdir(svg_out)

    for svg in Path(svg_in).glob("**/*.svg"):
        subprocess.run(
            [
                "msdfgen",
                "-svg",
                os.path.join(svg_in, svg),
                "-o",
                os.path.join(svg_out, f"{Path(svg).stem}.png"),
                "-dimensions",
                "32",
                "32",
                "-autoframe",
            ]
        )

#
# TEXTURE PACKING
#


if os.path.exists(texture_in):
    if not os.path.isdir(texture_out):
        os.mkdir(texture_out)

    for dir in os.listdir(texture_in):
        subprocess.run(
            [
                "cramcli",
                os.path.join(texture_in, dir),
                texture_out,
                dir,
                "--padding",
                "4",
                "--premultiply",
            ]
        )

#
# SFX
#

if os.path.exists(sfx_in):
    if not os.path.isdir(sfx_out):
        os.mkdir(sfx_out)

    for sfx in Path(sfx_in).glob("**/*.wav"):
        subprocess.run(
            [
                "ffmpeg",
                "-y",
                "-i",
                sfx,
                os.path.join(sfx_out, f"{Path(sfx).stem}.wav"),
            ]
        )

#
# MUSIC
#

if os.path.exists(music_in):
    if not os.path.isdir(music_out):
        os.mkdir(music_out)

    for song in Path(music_in).glob("**/*.wav"):
        subprocess.run(
            [
                "ffmpeg",
                "-y",
                "-i",
                song,
                os.path.join(music_out, f"{Path(song).stem}.ogg"),
            ]
        )

#
# MSDF FONT ATLAS GENERATION
#

fontsDirty = False

if os.path.exists(fonts_in):
    if not os.path.isdir(fonts_out):
        os.mkdir(fonts_out)

    for font in Path(fonts_in).glob("**/*.ttf"):
        digest = get_digest(font)

        if not font in hashes:
            fontsDirty

        if not font in hashes or hashes[font] != digest or args.force:
            subprocess.run(
                [
                    "msdf-atlas-gen",
                    "-yorigin",
                    "top",
                    "-font",
                    font,
                    "-imageout",
                    f"{os.path.join(fonts_out, Path(font).stem)}.png",
                    "-json",
                    f"{os.path.join(fonts_out, Path(font).stem)}.json",
                    "-fontname",
                    os.path.basename(font),
                ]
            )
            shutil.copy(font, f"{os.path.join(fonts_out, Path(font).stem)}.font")
            hashes[font] = digest

        for font in Path(fonts_in).glob("**/*.otf"):
            digest = get_digest(font)

            if not font in hashes:
                fontsDirty

            if not font in hashes or hashes[font] != digest or args.force:
                subprocess.run(
                    [
                        "msdf-atlas-gen",
                        "-yorigin",
                        "top",
                        "-font",
                        font,
                        "-imageout",
                        f"{os.path.join(fonts_out, Path(font).stem)}.png",
                        "-json",
                        f"{os.path.join(fonts_out, Path(font).stem)}.json",
                        "-fontname",
                        os.path.basename(font),
                    ]
                )
                shutil.copy(font, f"{os.path.join(fonts_out, Path(font).stem)}.font")
                hashes[font] = digest


#
# CODEGEN
#

projectName = Path(project).stem

with open(content, "w+") as f:
    f.seek(0)
    f.write(
        f"""using MoonWorks.Graphics;
using System;
using System.Text;
using System.Numerics;
using MoonWorks.Graphics.Font;
using MoonWorks.Audio;
using System.Reflection;
using Path = System.IO.Path;
using Buffer = MoonWorks.Graphics.Buffer;

namespace {projectName};

public static class Content
{{        
"""
    )

    spritesheets = []

    for spritesheet in Path(texture_out).glob("*.json"):
        f.write(f"public static class {Path(spritesheet).stem}\n{{\n")
        f.write(f"public static Texture? Atlas {{get; private set;}}")

        with open(spritesheet, "r+") as s:
            data = json.loads(s.read())

        texture_count = len(data["Images"])
        f.write(f"public static Vector4[] Textures = new Vector4[{texture_count}];\n")

        f.write(f"public static string[] TextureNames = new string[{texture_count}];\n")

        width = float(data["Width"])
        height = float(data["Height"])

        for image in data["Images"]:
            name = image["Name"]
            x = float(image["X"]) / width
            y = float(image["Y"]) / height
            w = float(image["W"]) / width
            h = float(image["H"]) / height
            f.write(
                f"public static readonly Vector4 {Path(name).stem} = new Vector4({x}f, {y}f, {w}f, {h}f);\n"
            )

        f.write(
            """public static string GetTextureName(Vector4 texture)
    {
        return TextureNames[Array.IndexOf(Textures, texture)];
    }
    public static Vector4 GetTexture(string name)
    {
        return Textures[Array.IndexOf(TextureNames, name)];
    }
"""
        )

        f.write(
            f"""public static void LoadTextures(TextureFormat format, ResourceUploader resourceUploader, string path)
            {{
            Atlas = resourceUploader.CreateTexture2DFromCompressed(Path.Join(path, "{Path(spritesheet).stem}.png"), format, TextureUsageFlags.Sampler);
            """
        )

        index = 0
        for image in data["Images"]:
            name = image["Name"]
            f.write(f"Textures[{index}] = {Path(name).stem};\n")
            f.write(f'TextureNames[{index}] = "{Path(name).stem}";\n')
            index += 1

        f.write(
            """
            }
        }
            """
        )

    f.write(
        """public static class Fonts
        {
"""
    )

    for font in Path(fonts_out).glob("*.json"):
        f.write(f"public static Font {Path(font).stem};\n")

    f.write(
        """public static void LoadFonts(GraphicsDevice graphicsDevice, string path)
        {
"""
    )

    for font in Path(fonts_out).glob("*.json"):
        f.write(
            f'{Path(font).stem} = Font.Load(graphicsDevice, Path.Join(path, "{Path(font.stem)}.font"));\n'
        )

    f.write("\n}\n}\n\n")

    f.write(
        """public static class SFX
        {
"""
    )

    for sfx in Path(sfx_out).glob("*.wav"):
        f.write(f"public static AudioBuffer {Path(sfx).stem};\n")

    f.write(
        """public static void LoadSFX(AudioDevice audioDevice, string path)
        {
"""
    )

    for sfx in Path(sfx_out).glob("*.wav"):
        f.write(
            f'{Path(sfx).stem} = AudioDataWav.CreateBuffer(audioDevice, Path.Join(path, "{Path(sfx).stem}.wav"));\n'
        )

    f.write("\n}\n}\n\n")

    f.write(
        """public static class Music
        {
"""
    )

    for song in Path(music_out).glob("*.ogg"):
        f.write(f"public static string {Path(song).stem};\n")

    f.write(
        """public static void LoadMusic(AudioDevice audioDevice, string path)
        {
"""
    )

    for song in Path(music_out).glob("*.ogg"):
        f.write(f'{Path(song).stem} = Path.Join(path, "{Path(song).stem}.ogg");\n')

    f.write("\n}\n}\n\n")

    f.write(
        """public static class Models
        {
        public abstract class Model
        {
            public int ID;
            public uint VertexCount { get; internal set; }
            public uint TriangleCount { get; internal set; }
            public Buffer VertexBuffer { get; internal set; }
            public Buffer IndexBuffer { get; internal set; }
        }

        public static Model[] IDToModel;
"""
    )

    modelCount = 0

    for model in Path(models_in).glob("*.obj"):
        f.write(f"public static Model {Path(model).stem};\n")
        modelCount += 1

    for model in Path(models_in).glob("*.obj"):
        with open(model, "r") as m:
            f.write(
                f"""public class {Path(model).stem}Model : Model\n{{
                    public {Path(model).stem}Model(int id, ResourceUploader resourceUploader)
                    {{
                    ID = id;
                    VertexBuffer = resourceUploader.CreateBuffer(
                    [\n"""
            )
            vertex_count = 0
            for line in m:
                if line.startswith("v "):
                    vertex_count += 1
                    vertex = line.split(" ")
                    f.write(
                        f"new PositionVertex(new Vector3({vertex[1].strip()}f, {vertex[2].strip()}f, {vertex[3].strip()}f)),"
                    )
            f.write("], BufferUsageFlags.Vertex);\n")
            f.write("IndexBuffer = resourceUploader.CreateBuffer(\n[\n")
            m.seek(0)
            tri_count = 0
            for line in m:
                if line.startswith("f "):
                    tri_count += 1
                    index = line.split(" ")
                    f.write(
                        f"{int(index[1].strip()) - 1}, {int(index[2].strip()) - 1}, {int(index[3].strip()) - 1},\n"
                    )
            f.write("], BufferUsageFlags.Index);\n")
            f.write(f"TriangleCount = {tri_count};\n")
            f.write(f"VertexCount = {vertex_count};\n")
            f.write("}\n}")

    f.write(
        f"""public static void LoadModels(ResourceUploader resourceUploader)
    {{
        IDToModel = new Model[{modelCount}];
    """
    )

    index = 0
    for model in Path(models_in).glob("*.obj"):
        f.write(
            f"IDToModel[{index}] = {Path(model).stem} = new {Path(model).stem}Model({index}, resourceUploader);\n"
        )
        index += 1

    f.write("}\n")

    f.write("\n}\n\n")

    f.write(
        """public static void LoadAll(TextureFormat textureFormat, GraphicsDevice graphicsDevice, AudioDevice audioDevice)
        {
            var cmdbuf = graphicsDevice.AcquireCommandBuffer();

            Fonts.LoadFonts(graphicsDevice, Path.Join(System.AppContext.BaseDirectory, "Fonts"));
            SFX.LoadSFX(audioDevice, Path.Join(System.AppContext.BaseDirectory, "SFX"));
            Music.LoadMusic(audioDevice, Path.Join(System.AppContext.BaseDirectory, "Music"));

            graphicsDevice.Submit(cmdbuf);
    
            var resourceUploader = new ResourceUploader(graphicsDevice);
            Models.LoadModels(resourceUploader);

            var texturesPath = Path.Join(System.AppContext.BaseDirectory, "Textures");
"""
    )

    for spritesheet in Path(texture_out).glob("*.json"):
        f.write(
            f"{Path(spritesheet).stem}.LoadTextures(textureFormat, resourceUploader, texturesPath);"
        )

    f.write(
        """resourceUploader.Upload();
resourceUploader.Dispose();
}
}
"""
    )

subprocess.run(["dotnet", "restore", project])
subprocess.run(["dotnet", "format", project])
