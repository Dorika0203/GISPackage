#version 330 core

in vec3 vPosition;

uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProjection;

void main(void)
{
    // gl_Position = vec4(vPosition, 1.0) * uModel * uView * uProjection;
    gl_Position = vec4(vPosition, 1.0);
}