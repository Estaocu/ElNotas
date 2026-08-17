void GetMainLightData_float(
    float3 WorldPos, 
    out float3 Direction, 
    out float3 Color, 
    out float DistanceAtten, 
    out float ShadowAtten)
{
#if defined(UNIVERSAL_LIGHTING_INCLUDED)
    // 1. Convert World Position to Shadow Coordinates
    #if defined(_MAIN_LIGHT_SHADOWS_CASCADE) || defined(_MAIN_LIGHT_SHADOWS)
        float4 shadowCoord = TransformWorldToShadowCoord(WorldPos);
    #else
        float4 shadowCoord = float4(0, 0, 0, 0);
    #endif

    // 2. Fetch Main Light data with Shadow Attenuation
    Light mainLight = GetMainLight(shadowCoord);
    
    Direction = mainLight.direction;
    Color = mainLight.color;
    DistanceAtten = mainLight.distanceAttenuation;
    ShadowAtten = mainLight.shadowAttenuation;
#else
    // Fallback for Shader Graph Preview
    Direction = float3(0.5, 0.5, 0.0);
    Color = float3(1.0, 1.0, 1.0);
    DistanceAtten = 1.0;
    ShadowAtten = 1.0;
#endif
}