// This shader combines the functionality of CutoutComplex and CutoutSkin.
// _Color is normally the skin color. _ColorTwo is an optional second color.
// The red channel of the mask indicates whether the texture color is multiplied by _Color.
// The green channel of the mask indicates whether the texture color is multiplied by _ColorTwo.
// The blue channel of the mask indicates whether the skin effect is applied.
//
// Mask     | Output
// ---------|---------
// 0x000000 | Texture color unchanged
// 0xFF0000 | Texture multiplied by _Color, non-skin
// 0xFF00FF | Texture multiplied by _Color, skin
// 0x00FF00 | Texture multiplied by _ColorTwo, non-skin
// 0x00FFFF | Texture multiplied by _ColorTwo, skin

#version 450

#include "MaskGate.glsl"

layout(location = 0) in vec4 vColor;
layout(location = 1) in vec2 vUv;

layout(location = 0) out vec4 outColor;

layout(set = 2, binding = 0) uniform MaterialUniforms
{
    vec4 _Color;       // @color @default = vec4(1.0, 1.0, 1.0, 1.0)
    vec4 _MainTex_ST;  // @default = vec4(0.0, 0.0, 1.0, 1.0)
    vec4 _ColorTwo;    // @color @default = vec4(1.0, 1.0, 1.0, 1.0)
    vec4 _ShadowColor; // @color @default = vec4(1.0, 0.0, 0.0, 1.0)
} uMaterial;

layout(set = 2, binding = 1) uniform sampler2D _MainTex;
layout(set = 2, binding = 2) uniform sampler2D _MaskTex; // @default = black

void main()
{
    vec2 uv = vUv * uMaterial._MainTex_ST.zw + uMaterial._MainTex_ST.xy;
    vec4 src = texture(_MainTex, uv);
    vec4 f = src;
    vec4 colPower = texture(_MaskTex, uv);
    colPower.rg *= MaskGate(colPower);

    if (f.a < 0.1)
        discard;

    vec4 colOneMultiplier = colPower.r * uMaterial._Color + (1.0 - colPower.r);
    f *= colOneMultiplier;

    vec4 colTwoMultiplier = colPower.g * uMaterial._ColorTwo + (1.0 - colPower.g);
    f *= colTwoMultiplier;

    float shadow = (1.0 - src.r) * src.r; // darkness of the magic-white source art, peaking at mid-grey
    shadow = min(shadow, 0.11);
    f += colPower.b * uMaterial._ShadowColor * shadow;

    if (f.a < 0.5)
        discard;
    outColor = f;
}
