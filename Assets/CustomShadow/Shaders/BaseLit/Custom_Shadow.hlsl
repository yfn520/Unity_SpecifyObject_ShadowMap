#ifndef UNIVERSAL_CUSTOM_SHADOW_INCLUDED
#define UNIVERSAL_CUSTOM_SHADOW_INCLUDED
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

TEXTURE2D_SHADOW(CustomShadowMap);
SAMPLER_CMP(sampler_CustomShadowMap);

half4       _Custom_MainLightShadowOffset0;
half4       _Custom_MainLightShadowOffset1;
half4       _Custom_MainLightShadowOffset2;
half4       _Custom_MainLightShadowOffset3;
half4       _Custom_MainLightShadowParams;  // (x: shadowStrength, y: 1.0 if soft shadows, 0.0 otherwise)
float4      _Custom_MainLightShadowmapSize; // (xy: 1/width and 1/height, zw: width and height)


ShadowSamplingData Custom_GetMainLightShadowSamplingData()
{
    ShadowSamplingData shadowSamplingData;
    shadowSamplingData.shadowOffset0 = _Custom_MainLightShadowOffset0;
    shadowSamplingData.shadowOffset1 = _Custom_MainLightShadowOffset1;
    shadowSamplingData.shadowOffset2 = _Custom_MainLightShadowOffset2;
    shadowSamplingData.shadowOffset3 = _Custom_MainLightShadowOffset3;
    shadowSamplingData.shadowmapSize = _Custom_MainLightShadowmapSize;
    return shadowSamplingData;
}

// ShadowParams
// x: ShadowStrength
// y: 1.0 if shadow is soft, 0.0 otherwise
half4 Custom_GetMainLightShadowParams()
{
    return _Custom_MainLightShadowParams;
}

half Custom_MainLightRealtimeShadow(float4 shadowCoord)
{
//    #if !defined(MAIN_LIGHT_CALCULATE_SHADOWS)
//    return 1.0h;
//    #endif

    ShadowSamplingData samplingData = Custom_GetMainLightShadowSamplingData();
    half4 shadowParams = Custom_GetMainLightShadowParams();
    
    real attenuation;
    real shadowStrength = shadowParams.x;

    // TODO: We could branch on if this light has soft shadows (shadowParams.y) to save perf on some platforms.
    #ifdef _USE_SOFT_SHADOW
    attenuation = SampleShadowmapFiltered(TEXTURE2D_SHADOW_ARGS(CustomShadowMap, sampler_CustomShadowMap), shadowCoord, samplingData);
    #else
    // 1-tap hardware comparison
    attenuation = SAMPLE_TEXTURE2D_SHADOW(CustomShadowMap, sampler_CustomShadowMap, shadowCoord.xyz);
    #endif

    attenuation = LerpWhiteTo(attenuation, shadowStrength);

    // Shadow coords that fall out of the light frustum volume must always return attenuation 1.0
    // TODO: We could use branch here to save some perf on some platforms.
    return BEYOND_SHADOW_FAR(shadowCoord) ? 1.0 : attenuation;
}


#endif