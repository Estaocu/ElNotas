Shader "UI/ScreenSpaceTextureToggle"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _OverlayTex ("Overlay Texture (Screen Space)", 2D) = "white" {}
        _OverlayScale ("Overlay Scale", Vector) = (1, 1, 0, 0)
        _OverlayOffset ("Overlay Offset", Vector) = (0, 0, 0, 0)
        
        // Velocidad de scroll en los ejes X e Y
        _ScrollSpeed ("Scroll Speed (X, Y)", Vector) = (0.1, 0.1, 0, 0)
        
        // 0 = Usar Color Normal, 1 = Usar Textura Screen Space
        [Toggle] _UseOverlay ("Use Screen Space Overlay", Float) = 0

        // Required for UI Canvas stencil masking
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [ZTest]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
                float4 screenPos    : TEXCOORD1;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_OverlayTex);
            SAMPLER(sampler_OverlayTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _OverlayScale;
                float4 _OverlayOffset;
                float4 _ScrollSpeed;
                float _UseOverlay;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                
                output.positionCS = vertexInput.positionCS;
                output.uv = input.uv;
                output.color = input.color * _Color;
                
                // Calculo de coordenadas Normalized Screen Position
                output.screenPos = ComputeScreenPos(output.positionCS);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Muestreo de la textura del sprite
                half4 mainColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                // Calculo de coordenadas en espacio de pantalla
                float2 screenUV = input.screenPos.xy / input.screenPos.w;

                // Aplicar escala, offset estatico y scroll continuo continuo con _Time.y
                screenUV = screenUV * _OverlayScale.xy + _OverlayOffset.xy + (_ScrollSpeed.xy * _Time.y);

                // Muestreo de la textura animada
                half4 overlayColor = SAMPLE_TEXTURE2D(_OverlayTex, sampler_OverlayTex, screenUV);

                // Mezcla segun _UseOverlay
                half3 flatRGB = mainColor.rgb * input.color.rgb;
                half3 overlayRGB = overlayColor.rgb * input.color.rgb;
                half3 finalRGB = lerp(flatRGB, overlayRGB, _UseOverlay);

                half finalAlpha = mainColor.a * input.color.a;

                return half4(finalRGB, finalAlpha);
            }
            ENDHLSL
        }
    }
}