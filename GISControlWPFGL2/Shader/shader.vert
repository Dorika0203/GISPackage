#version 330 core

in vec3 vPosition;

uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProjection;

void main(void)
{
    gl_Position = uProjection * uView * uModel * vec4(vPosition, 1.0);
}