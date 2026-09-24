Shader "Custom/TreeCanopyFade"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        [PerRendererData] _Color ("Sprite Tint", Color) = (1,1,1,1)
        _TrunkCutoffUV ("Trunk Cutoff UV.y", Range(0, 1)) = 0.35
        _BlendSoftness  ("Blend Softness", Range(0, 0.2)) = 0.05
        _CanopyAlpha ("Canopy Alpha Occluded", Range(0, 1)) = 0.22
        _FadeBlend   ("Fade Blend 0=visible 1=occluded", Range(0, 1)) = 0.0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [HideInInspector] [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [HideInInspector] [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "TreeCanopyFade"
            Tags { "LightMode" = "Universal2D" }
            HLSLPROGRAM
            #pragma vertex   SpriteVertex
            #pragma fragment SpriteFragment
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
                UNITY_DEFINE_INSTANCED_PROP(float4, _MainTex_ST)
                UNITY_DEFINE_INSTANCED_PROP(half4,  _Color)
                UNITY_DEFINE_INSTANCED_PROP(half4,  _RendererColor)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Flip)
                UNITY_DEFINE_INSTANCED_PROP(float,  _TrunkCutoffUV)
                UNITY_DEFINE_INSTANCED_PROP(float,  _BlendSoftness)
                UNITY_DEFINE_INSTANCED_PROP(half,   _CanopyAlpha)
                UNITY_DEFINE_INSTANCED_PROP(half,   _FadeBlend)
                UNITY_DEFINE_INSTANCED_PROP(float,  _EnableExternalAlpha)
            UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 color       : COLOR;
                float2 uv          : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings SpriteVertex(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                float4 mainTex_ST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _MainTex_ST);
                OUT.uv = IN.uv * mainTex_ST.xy + mainTex_ST.zw;
                half4 tint = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Color);
                half4 rendererColor = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _RendererColor);
                OUT.color = IN.color * tint * rendererColor;
                return OUT;
            }

            half4 SpriteFragment(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * IN.color;

                float cutoff = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _TrunkCutoffUV);
                float softness = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BlendSoftness);
                half canopyAlpha = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _CanopyAlpha);
                half fadeBlend = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _FadeBlend);

                // smoothstep: 0 below cutoff (trunk), 1 above cutoff (canopy)
                float canopyMask = smoothstep(cutoff - softness, cutoff + softness, IN.uv.y);

                // When fadeBlend=1: trunk stays full alpha col.a, foliage fades to col.a * canopyAlpha
                half targetAlpha = lerp(col.a, col.a * canopyAlpha, canopyMask * fadeBlend);
                col.a = targetAlpha;

                return col;
            }
            ENDHLSL
        }
    }
    FallBack "Sprites/Default"
}