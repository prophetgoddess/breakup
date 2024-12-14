#version 450

layout (location = 0) in vec2 TexCoord;

layout (location = 0) out vec4 FragColor;

layout(set = 2, binding = 0) uniform sampler2D Sampler;

void main()
{
	FragColor = vec4(0.6745, 0.7411, 0.729, texture(Sampler, TexCoord).a);
}
