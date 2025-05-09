#version 410 core

in dvec3 vPosition;

uniform dmat4 uModel;
uniform dmat4 uView;
uniform dmat4 uProjection;

void main(void)
{
    dvec4 pos = uProjection * uView * uModel * dvec4(vPosition, 1.0);
    gl_Position = vec4(pos);
}