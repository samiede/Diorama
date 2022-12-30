TEXTURE2D(_CameraColorTexture);
SAMPLER(sampler_CameraColorTexture);
float4 _CameraColorTexture_TexelSize;
//
TEXTURE2D(_TransparentDiscontinuityTexture);
SAMPLER(sampler_TransparentDiscontinuityTexture);
//
TEXTURE2D(_CameraDepthTexture);
SAMPLER(sampler_CameraDepthTexture);

TEXTURE2D(_DiscontinuityTexture);
SAMPLER(sampler_DiscontinuityTexture);

TEXTURE2D(_TransparentDepthSourceTexture);
SAMPLER(sampler_TransparentDepthSourceTexture);

#ifndef SHADERGRAPH_PREVIEW
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
#endif
 
float3 DecodeNormal(float4 enc)
{
    float kScale = 1.7777;
    float3 nn = enc.xyz*float3(2*kScale,2*kScale,0) + float3(-kScale,-kScale,1);
    float g = 2.0 / dot(nn.xyz,nn.xyz);
    float3 n;
    n.xy = g*nn.xy;
    n.z = g-1;
    return n;
}

void Outline_float(float2 UV, float OutlineThickness, float discSensitivity, float transparentDiscSensitivity, float DepthSensitivity, float NormalsSensitivity, float ColorSensitivity, float4 OutlineColor, out float4 Out)
{
    float halfScaleFloor = floor(OutlineThickness * 0.5);
    float halfScaleCeil = ceil(OutlineThickness * 0.5);
    float2 Texel = (1.0) / float2(_CameraColorTexture_TexelSize.z, _CameraColorTexture_TexelSize.w);

    float2 uvSamples[4];
    float depthSamples[4];
    float3 normalSamples[4], colorSamples[4];
    float4 discontinuitySamples[4];
    float4 transparentDiscontinuitySamples[4];

    uvSamples[0] = UV - float2(Texel.x, Texel.y) * halfScaleFloor;
    uvSamples[1] = UV + float2(Texel.x, Texel.y) * halfScaleCeil;
    uvSamples[2] = UV + float2(Texel.x * halfScaleCeil, -Texel.y * halfScaleFloor);
    uvSamples[3] = UV + float2(-Texel.x * halfScaleFloor, Texel.y * halfScaleCeil);
    
    for(int i = 0; i < 4 ; i++)
    {
        discontinuitySamples[i] = SAMPLE_TEXTURE2D(_DiscontinuityTexture, sampler_DiscontinuityTexture, uvSamples[i]);
        transparentDiscontinuitySamples[i] = SAMPLE_TEXTURE2D(_TransparentDiscontinuityTexture, sampler_TransparentDiscontinuityTexture, uvSamples[i]);
        depthSamples[i] = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, uvSamples[i]).r;
        normalSamples[i] = SampleSceneNormals(uvSamples[i]);
        colorSamples[i] = SAMPLE_TEXTURE2D(_CameraColorTexture, sampler_CameraColorTexture, uvSamples[i]);
    }

    float4 discontinuitySamplesOrig = SAMPLE_TEXTURE2D(_DiscontinuityTexture, sampler_DiscontinuityTexture, UV);
    float4 transparentDiscontinuitySamplesOrig = SAMPLE_TEXTURE2D(_TransparentDepthSourceTexture, sampler_TransparentDepthSourceTexture, UV);

    // Custom Source
    const float discontinuityDifference0 = discontinuitySamples[1].r - discontinuitySamples[0].r;
    const float discontinuityDifference1 = discontinuitySamples[3].r - discontinuitySamples[2].r;
    float edgeDiscontinuity = sqrt(pow(discontinuityDifference0, 2) + pow(discontinuityDifference1, 2)) * 100;
    const float discontinuityThreshold = (1/discSensitivity) * discontinuitySamples[0];
    edgeDiscontinuity = edgeDiscontinuity > discontinuityThreshold ? 1 : 0;
    edgeDiscontinuity += discontinuitySamplesOrig.g > 0 ? 1 : 0;

    // Transparent Outlines
    const float transparentDiscontinuityDifference0 = transparentDiscontinuitySamples[1] - transparentDiscontinuitySamples[0];
    const float transparentDiscontinuityDifference1 = transparentDiscontinuitySamples[3] - transparentDiscontinuitySamples[2];
    float edgeTransparentDiscontinuity = sqrt(dot(transparentDiscontinuityDifference0, transparentDiscontinuityDifference0) + dot(transparentDiscontinuityDifference1, transparentDiscontinuityDifference1));
    edgeTransparentDiscontinuity = edgeTransparentDiscontinuity > (1/transparentDiscSensitivity) ? 1 : 0;
    

    // Depth
    float depthFiniteDifference0 = depthSamples[1] - depthSamples[0];
    float depthFiniteDifference1 = depthSamples[3] - depthSamples[2];
    float edgeDepth = sqrt(pow(depthFiniteDifference0, 2) + pow(depthFiniteDifference1, 2)) * 100;
    float depthThreshold = (1/DepthSensitivity) * depthSamples[0];
    edgeDepth = edgeDepth > depthThreshold ? 1 : 0;
    
    // // Normals
    float3 normalFiniteDifference0 = normalSamples[1] - normalSamples[0];
    float3 normalFiniteDifference1 = normalSamples[3] - normalSamples[2];
    float edgeNormal = sqrt(dot(normalFiniteDifference0, normalFiniteDifference0) + dot(normalFiniteDifference1, normalFiniteDifference1));
    edgeNormal = edgeNormal > (1/NormalsSensitivity) ? 1 : 0;

    // Color
     float3 colorFiniteDifference0 = colorSamples[1] - colorSamples[0];
     float3 colorFiniteDifference1 = colorSamples[3] - colorSamples[2];
     float edgeColor = sqrt(dot(colorFiniteDifference0, colorFiniteDifference0) + dot(colorFiniteDifference1, colorFiniteDifference1));
     edgeColor = edgeColor > (1/ColorSensitivity) ? 1 : 0;

    float edge = max(max(max(edgeDepth, max(edgeNormal, edgeColor)), edgeDiscontinuity), edgeTransparentDiscontinuity);
    // float edge = max(edgeTransparentDiscontinuity, edgeColor);

    float4 original = SAMPLE_TEXTURE2D(_CameraColorTexture, sampler_CameraColorTexture, uvSamples[0]);
    // float4 original = transparentDiscontinuitySamplesOrig;

    Out = (1 - edge) * original + edge * saturate(lerp(original, OutlineColor,  OutlineColor.a));
    // Out = original;
}