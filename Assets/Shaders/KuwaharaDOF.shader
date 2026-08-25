Shader "Hidden/KuwaharaDOF"
{
    Properties
    {
        _Radius ("Kernel Radius (px steps)", Range(1, 16)) = 4
        _CellSize ("Cell Size", Range(1, 10)) = 1
        _Sharpness ("Sharpness (Sector Hardness, q)", Range(1, 20)) = 8
        _Eccentricity ("Eccentricity (Anisotropy Strength, alpha)", Range(0.1, 4)) = 1
        _FocusDistance ("Focus Distance", Float) = 10
        _FocusRange ("Focus Range (Sharp Zone Width)", Float) = 4
        _NearTransitionRange ("Near Blur Transition", Float) = 5
        _FarTransitionRange ("Far Blur Transition", Float) = 10
        [Toggle(_DEBUG_COC)] _DebugCoC ("Debug: Show Focus Mask", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        Cull Off ZWrite Off ZTest Always

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        // Blit.hlsl provides Vert(), Attributes, Varyings, _BlitTexture,
        // sampler_PointClamp and sampler_LinearClamp.
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        #define KUWAHARA_SECTORS 8
        #define PI 3.14159265359

        float4 _BlitTexture_TexelSize;

        float _Radius;
        float _CellSize;
        float _Sharpness;
        float _Eccentricity;
        float _FocusDistance;
        float _FocusRange;
        float _NearTransitionRange;
        float _FarTransitionRange;

        TEXTURE2D_X(_StructureTensorTex);

        float3 SampleSource(float2 uv)
        {
            return SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv, 0).rgb;
        }

        float Luma(float3 c)
        {
            return dot(c, float3(0.299, 0.587, 0.114));
        }
        ENDHLSL

        // ------------------------------------------------------------
        // PASS 0 - Structure Tensor (Sobel gradients on luma)
        // Output: R = dx*dx (E), G = dy*dy (G), B = dx*dy (F)
        // ------------------------------------------------------------
        Pass
        {
            Name "StructureTensor"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            float4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float2 texel = _BlitTexture_TexelSize.xy;

                float tl = Luma(SampleSource(uv + texel * float2(-1,  1)));
                float tc = Luma(SampleSource(uv + texel * float2( 0,  1)));
                float tr = Luma(SampleSource(uv + texel * float2( 1,  1)));
                float ml = Luma(SampleSource(uv + texel * float2(-1,  0)));
                float mr = Luma(SampleSource(uv + texel * float2( 1,  0)));
                float bl = Luma(SampleSource(uv + texel * float2(-1, -1)));
                float bc = Luma(SampleSource(uv + texel * float2( 0, -1)));
                float br = Luma(SampleSource(uv + texel * float2( 1, -1)));

                float dx = (tr + 2.0 * mr + br) - (tl + 2.0 * ml + bl);
                float dy = (tl + 2.0 * tc + tr) - (bl + 2.0 * bc + br);

                return float4(dx * dx, dy * dy, dx * dy, 1.0);
            }
            ENDHLSL
        }

        // ------------------------------------------------------------
        // PASS 1 - Tensor smoothing (3x3 weighted blur)
        // Reduces gradient noise before eigen-analysis.
        // ------------------------------------------------------------
        Pass
        {
            Name "TensorBlur"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            float4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float2 texel = _BlitTexture_TexelSize.xy;

                float3 sum = 0.0;
                float wsum = 0.0;
                const float weights[3] = { 1.0, 2.0, 1.0 };

                [unroll]
                for (int y = -1; y <= 1; y++)
                {
                    [unroll]
                    for (int x = -1; x <= 1; x++)
                    {
                        float w = weights[x + 1] * weights[y + 1];
                        float3 s = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv + texel * float2(x, y), 0).rgb;
                        sum += s * w;
                        wsum += w;
                    }
                }

                return float4(sum / wsum, 1.0);
            }
            ENDHLSL
        }

        // ------------------------------------------------------------
        // PASS 2 - Anisotropic Kuwahara filter, masked/scaled by scene depth
        // (Kyprianidis-style generalized Kuwahara, 8 overlapping sectors)
        // ------------------------------------------------------------
        Pass
        {
            Name "KuwaharaDOF"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma shader_feature_local_fragment _DEBUG_COC

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            // Returns a 0..1 circle-of-confusion value.
            // 0 = perfectly in focus, 1 = fully blurred.
            float ComputeCoC(float eyeDepth)
            {
                float dist = eyeDepth - _FocusDistance;
                float halfRange = _FocusRange * 0.5;
                float coc = 0.0;

                if (dist < -halfRange)
                {
                    // In front of the focus plane (foreground).
                    coc = saturate((-dist - halfRange) / max(_NearTransitionRange, 1e-4));
                }
                else if (dist > halfRange)
                {
                    // Behind the focus plane (background).
                    coc = saturate((dist - halfRange) / max(_FarTransitionRange, 1e-4));
                }
                return coc;
            }

            // Overlapping "petal" weight for one of the N sectors.
            // p is already warped into the anisotropic unit disk.
            float PetalWeight(float2 p, float sectorAngle)
            {
                float r2 = dot(p, p);
                float2 dir = float2(cos(sectorAngle), sin(sectorAngle));
                float proj = max(0.0, dot(p, dir));
                return proj * proj * exp(-3.0 * r2);
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float3 centerColor = SampleSource(uv);

                float rawDepth = SampleSceneDepth(uv);
                float eyeDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                float coc = ComputeCoC(eyeDepth);

                #if defined(_DEBUG_COC)
                    return float4(coc.xxx, 1.0);
                #endif

                // Sharp zone: skip the filter completely, no wasted samples.
                if (coc <= 0.0015)
                {
                    return float4(centerColor, 1.0);
                }

                float3 tensorSample = SAMPLE_TEXTURE2D_X_LOD(_StructureTensorTex, sampler_LinearClamp, uv, 0).rgb;
                float E = tensorSample.x;
                float G = tensorSample.y;
                float F = tensorSample.z;

                float diff = E - G;
                float discriminant = sqrt(max(0.0, diff * diff + 4.0 * F * F));
                float lambda1 = 0.5 * ((E + G) + discriminant);
                float lambda2 = 0.5 * ((E + G) - discriminant);

                float phi = 0.5 * atan2(2.0 * F, diff);
                float anisotropy = (lambda1 - lambda2) / max(lambda1 + lambda2, 1e-5);

                float2 gradDir = float2(cos(phi), sin(phi));
                float2 tangentDir = float2(-gradDir.y, gradDir.x);

                // alpha / (alpha + A) and (alpha + A) / alpha -> stretches
                // the sampling ellipse along the edge tangent.
                float scaleX = _Eccentricity / (_Eccentricity + anisotropy);
                float scaleY = (_Eccentricity + anisotropy) / _Eccentricity;

                // Clamp so the ellipse never degenerates into a near
                // 1-pixel-wide sliver (which starves most sectors of any
                // sample at all -> see the starved-sector guard below).
                scaleX = clamp(scaleX, 0.3, 3.0);
                scaleY = clamp(scaleY, 0.3, 3.0);

                float3 sumColor[KUWAHARA_SECTORS];
                float3 sumColorSq[KUWAHARA_SECTORS];
                float sumWeight[KUWAHARA_SECTORS];

                [unroll]
                for (int k = 0; k < KUWAHARA_SECTORS; k++)
                {
                    sumColor[k] = 0.0;
                    sumColorSq[k] = 0.0;
                    sumWeight[k] = 0.0;
                }

                int radiusSteps = (int) _Radius;
                float2 texel = _BlitTexture_TexelSize.xy;
                float invRadiusX = 1.0 / max(scaleX * _Radius, 1e-4);
                float invRadiusY = 1.0 / max(scaleY * _Radius, 1e-4);

                // NOTE: pixelOffset is scaled by cellSize and coc, expanding pixel step spacing.
                [loop]
                for (int y = -radiusSteps; y <= radiusSteps; y++)
                {
                    [loop]
                    for (int x = -radiusSteps; x <= radiusSteps; x++)
                    {
                        float2 pixelOffset = float2(x, y) * _CellSize * coc;

                        float u = dot(pixelOffset, gradDir) * invRadiusX;
                        float v = dot(pixelOffset, tangentDir) * invRadiusY;

                        float r2 = u * u + v * v;
                        if (r2 > 1.0) continue;

                        float2 sampleUV = uv + pixelOffset * texel;
                        float3 c = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, sampleUV, 0).rgb;

                        [unroll]
                        for (int k = 0; k < KUWAHARA_SECTORS; k++)
                        {
                            float sectorAngle = k * (2.0 * PI / KUWAHARA_SECTORS);
                            float w = PetalWeight(float2(u, v), sectorAngle);
                            sumColor[k] += c * w;
                            sumColorSq[k] += c * c * w;
                            sumWeight[k] += w;
                        }
                    }
                }

                float3 finalColor = 0.0;
                float finalWeight = 0.0;

                const float MIN_SECTOR_WEIGHT = 1e-3;

                [unroll]
                for (int k = 0; k < KUWAHARA_SECTORS; k++)
                {
                    if (sumWeight[k] <= MIN_SECTOR_WEIGHT) continue;

                    float3 mean = sumColor[k] / sumWeight[k];
                    float3 variance = max(sumColorSq[k] / sumWeight[k] - mean * mean, 0.0);
                    float sigma = dot(variance, float3(0.299, 0.587, 0.114));

                    // Low-variance sectors (flat brush strokes) dominate the blend.
                    float blendW = 1.0 / (1.0 + pow(max(sigma, 1e-5) * 1000.0, _Sharpness * 0.5));
                    finalColor += mean * blendW;
                    finalWeight += blendW;
                }

                if (finalWeight <= 1e-4)
                {
                    return float4(centerColor, 1.0);
                }
                finalColor /= finalWeight;

                return float4(lerp(centerColor, finalColor, saturate(coc)), 1.0);
            }
            ENDHLSL
        }
    }
    FallBack Off
}