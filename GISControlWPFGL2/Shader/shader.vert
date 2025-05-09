#version 330 core

in vec3 vPosition;
uniform mat4 uModelViewProjection;

void main(void)
{
    gl_Position = uModelViewProjection * vec4(vPosition, 1.0);
}