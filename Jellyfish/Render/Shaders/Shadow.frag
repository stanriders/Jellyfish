#version 460
precision highp float;

#define BIAS 0.005

void main()
{
    gl_FragDepth = gl_FragCoord.z;
    gl_FragDepth += gl_FrontFacing ? BIAS : 0.0;
}