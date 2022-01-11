Shader "Custom/Cutout"
{
    Properties
    {
    }
    SubShader
    {
        Tags {"LightMode" = "Deferred"}
        Lighting Off
        Cull Back
        ColorMask 0
        ZTest LEqual
        Blend Zero One
        Pass {}
    }
}