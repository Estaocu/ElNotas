Shader "Hidden/Universal Render Pipeline/Kuwahara Depth Of Field"
{
    Properties
    {
        _Intensity ("Effect Intensity", Range(0, 1)) = 1.0
        _Radius ("Kuwahara Radius", Range(1, 20)) = 4.0

        [Header(Depth Of Field)]
        _DepthStrength ("Depth Blur Strength", Range(0, 5)) = 1.0
        _FocusDistance ("Focus Distance", Float) = 10.0
        _FocusRange ("Focus Range", Range(0.01, 50)) = 5.0
        _MaxBlurRadius ("Maximum Depth Radius", Range(0, 20)) = 6.0
        _DepthFalloff ("Depth Falloff", Range(0.1, 10)) = 2.0
        _DepthBias ("Depth Bias", Range(0, 1)) = 0.0

        [Header(Brush)]
        _BrushScale ("Brush Scale", Range(0.25, 4)) = 1.0
        _BrushAnisotropy ("Brush Anisotropy", Range(0, 1)) = 0.5
        _BrushRotation ("Brush Rotation", Range(0, 6.283185)) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
        }

        Pass
        {
            Name "Kuwahara"

            ZTest Always
            ZWrite Off
            Cull Off
            Blend One Zero

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_BlitTexture);
            SAMPLER(sampler_BlitTexture);

            TEXTURE2D(_CameraDepthTexture);
            SAMPLER(sampler_CameraDepthTexture);

            float4 _ScreenParams;
            float4 _ZBufferParams;

            float _Intensity;
            float _Radius;

            float _DepthStrength;
            float _FocusDistance;
            float _FocusRange;
            float _MaxBlurRadius;
            float _DepthFalloff;
            float _DepthBias;

            float _BrushScale;
            float _BrushAnisotropy;
            float _BrushRotation;

            Varyings Vert(Attributes input)
        {
            Varyings output;

            float2 uv = float2(
                (input.vertexID << 1) & 2,
                input.vertexID & 2
            );

            output.positionCS =
                float4(
                    uv * 2.0 - 1.0,
                    0.0,
                    1.0
                );

            #if UNITY_UV_STARTS_AT_TOP
                output.uv = float2(
                    uv.x,
                    1.0 - uv.y
                );
            #else
                output.uv = uv;
            #endif

            return output;
        }

            float3 SampleColor(float2 uv)
            {
                return SAMPLE_TEXTURE2D(
                    _BlitTexture,
                    sampler_BlitTexture,
                    uv
                ).rgb;
            }

            float SampleDepth(float2 uv)
            {
                float rawDepth =
                    SAMPLE_TEXTURE2D(
                        _CameraDepthTexture,
                        sampler_CameraDepthTexture,
                        uv
                    ).r;

                return LinearEyeDepth(
                    rawDepth,
                    _ZBufferParams
                );
            }

            float GetDepthBlurFactor(float depth)
            {
                float distanceFromFocus =
                    abs(depth - _FocusDistance);

                float normalizedDistance =
                    distanceFromFocus /
                    max(_FocusRange, 0.0001);

                normalizedDistance =
                    saturate(normalizedDistance);

                normalizedDistance =
                    pow(
                        normalizedDistance,
                        _DepthFalloff
                    );

                normalizedDistance =
                    saturate(
                        normalizedDistance +
                        _DepthBias
                    );

                return normalizedDistance *
                       _DepthStrength;
            }

            float2 RotateVector(
                float2 value,
                float angle
            )
            {
                float sine = sin(angle);
                float cosine = cos(angle);

                return float2(
                    value.x * cosine -
                    value.y * sine,

                    value.x * sine +
                    value.y * cosine
                );
            }

            void AddSample(
                float2 uv,
                float2 offset,
                float2 texelSize,
                inout float3 sum,
                inout float3 squareSum,
                inout float count
            )
            {
                float2 sampleUV =
                    uv +
                    offset *
                    texelSize;

                float3 color =
                    SampleColor(sampleUV);

                sum += color;
                squareSum += color * color;
                count += 1.0;
            }

            void EvaluateRegion(
                float2 uv,
                float2 regionDirection,
                float radius,
                float2 texelSize,
                inout float3 bestMean,
                inout float bestVariance
            )
            {
                float3 sum = 0.0;
                float3 squareSum = 0.0;
                float count = 0.0;

                float2 perpendicular =
                    float2(
                        -regionDirection.y,
                        regionDirection.x
                    );

                float step =
                    radius * 0.5;

                // Center
                AddSample(
                    uv,
                    0.0,
                    texelSize,
                    sum,
                    squareSum,
                    count
                );

                // First row
                AddSample(
                    uv,
                    regionDirection * step * 0.5,
                    texelSize,
                    sum,
                    squareSum,
                    count
                );

                AddSample(
                    uv,
                    regionDirection * step * 0.5 +
                    perpendicular * step * 0.5,
                    texelSize,
                    sum,
                    squareSum,
                    count
                );

                AddSample(
                    uv,
                    regionDirection * step * 0.5 -
                    perpendicular * step * 0.5,
                    texelSize,
                    sum,
                    squareSum,
                    count
                );

                // Second row
                AddSample(
                    uv,
                    regionDirection * step,
                    texelSize,
                    sum,
                    squareSum,
                    count
                );

                AddSample(
                    uv,
                    regionDirection * step +
                    perpendicular * step,
                    texelSize,
                    sum,
                    squareSum,
                    count
                );

                AddSample(
                    uv,
                    regionDirection * step -
                    perpendicular * step,
                    texelSize,
                    sum,
                    squareSum,
                    count
                );

                // Outer corners
                AddSample(
                    uv,
                    regionDirection * step * 0.5 +
                    perpendicular * step,
                    texelSize,
                    sum,
                    squareSum,
                    count
                );

                AddSample(
                    uv,
                    regionDirection * step * 0.5 -
                    perpendicular * step,
                    texelSize,
                    sum,
                    squareSum,
                    count
                );

                float3 mean =
                    sum / max(count, 1.0);

                float3 variance =
                    abs(
                        squareSum / max(count, 1.0) -
                        mean * mean
                    );

                float varianceValue =
                    dot(
                        variance,
                        float3(
                            0.299,
                            0.587,
                            0.114
                        )
                    );

                if (varianceValue < bestVariance)
                {
                    bestVariance =
                        varianceValue;

                    bestMean =
                        mean;
                }
            }

            float3 Kuwahara(
                float2 uv,
                float radius,
                float2 texelSize
            )
            {
                float angle =
                    _BrushRotation;

                float2 direction =
                    float2(
                        cos(angle),
                        sin(angle)
                    );

                float anisotropy =
                    lerp(
                        1.0,
                        0.2,
                        _BrushAnisotropy
                    );

                float2 directionX =
                    direction;

                float2 directionY =
                    float2(
                        -direction.y,
                        direction.x
                    );

                directionX.x *= anisotropy;
                directionY.y *= anisotropy;

                directionX =
                    normalize(directionX);

                directionY =
                    normalize(directionY);

                float3 bestMean = 0.0;
                float bestVariance = 1e20;

                // Region 1
                EvaluateRegion(
                    uv,
                    directionX + directionY,
                    radius,
                    texelSize,
                    bestMean,
                    bestVariance
                );

                // Region 2
                EvaluateRegion(
                    uv,
                    -directionX + directionY,
                    radius,
                    texelSize,
                    bestMean,
                    bestVariance
                );

                // Region 3
                EvaluateRegion(
                    uv,
                    -directionX - directionY,
                    radius,
                    texelSize,
                    bestMean,
                    bestVariance
                );

                // Region 4
                EvaluateRegion(
                    uv,
                    directionX - directionY,
                    radius,
                    texelSize,
                    bestMean,
                    bestVariance
                );

                return bestMean;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv =
                    input.uv;

                float2 texelSize =
                    1.0 /
                    _ScreenParams.xy;

                float3 originalColor =
                    SampleColor(uv);

                float depth =
                    SampleDepth(uv);

                float depthFactor =
                    GetDepthBlurFactor(depth);

                float radius =
                    _Radius +
                    depthFactor *
                    _MaxBlurRadius;

                radius =
                    clamp(
                        radius,
                        1.0,
                        20.0
                    );

                float3 kuwaharaColor =
                    Kuwahara(
                        uv,
                        radius,
                        texelSize
                    );

                float effectAmount =
                    saturate(
                        depthFactor
                    );

                effectAmount *=
                    _Intensity;

                float3 finalColor =
                    lerp(
                        originalColor,
                        kuwaharaColor,
                        effectAmount
                    );

                return half4(
                    finalColor,
                    1.0
                );
            }

            ENDHLSL
        }
    }

    FallBack Off
}