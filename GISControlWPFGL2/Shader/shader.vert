#version 410 core

in dvec3 vPosition;

uniform dmat4 uView;
uniform dmat4 uProjection;
uniform dmat4 uViewProjection;

void main(void)
{
    // dvec4 pos = uProjection * uView * dvec4(vPosition, 1.0);
    dvec4 pos = uViewProjection * dvec4(vPosition, 1.0);
    gl_Position = vec4(pos);
}