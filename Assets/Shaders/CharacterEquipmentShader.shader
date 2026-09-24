Shader "Custom/CharacterEquipmentShader"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        // UI Canvas Masking & Stencil Properties
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0

        [Header(Equipment Colors)]
        _ArmorColor ("Armor / Tunic Color", Color) = (0.77, 0.14, 0.19, 1)
        _ArmorSecColor ("Armor Accent Color", Color) = (0.54, 0.12, 0.17, 1)
        _CloakColor ("Cloak / Scarf Color", Color) = (0.85, 0.20, 0.20, 1)
        _PantsColor ("Pants / Greaves Color", Color) = (0.24, 0.24, 0.24, 1)
        _GlovesColor ("Gloves / Bracers Color", Color) = (0.85, 0.85, 0.85, 1)
        _BootsColor ("Boots / Shoes Color", Color) = (0.36, 0.17, 0.15, 1)
        _HelmetColor ("Helmet / Hair Color", Color) = (0.54, 0.28, 0.21, 1)

        [Header(Tier Enchantment Aura)]
        _TierGlowColor ("Tier Glow Color", Color) = (0, 0, 0, 0)
        _TierGlowIntensity ("Tier Glow Intensity", Range(0, 2)) = 0
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
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;

            fixed4 _ArmorColor;
            fixed4 _ArmorSecColor;
            fixed4 _CloakColor;
            fixed4 _PantsColor;
            fixed4 _GlovesColor;
            fixed4 _BootsColor;
            fixed4 _HelmetColor;
            fixed4 _TierGlowColor;
            float _TierGlowIntensity;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd);
                if (color.a < 0.05) return fixed4(0, 0, 0, 0);

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif

                float r = color.r;
                float g = color.g;
                float b = color.b;
                float uvY = IN.texcoord.y;

                // 1. Skin detection (Face / Neck) - Always preserve natural skin tone
                bool isSkin = (r > 0.60 && g > 0.35 && b > 0.20 && r > g && g > b && (r - b) > 0.15);

                // 2. White / Silver Handwraps / Gloves (R:255, G:255, B:255 / R:180, G:180, B:180)
                bool isWhiteGloves = (r > 0.65 && g > 0.65 && b > 0.65 && abs(r - g) < 0.10 && abs(g - b) < 0.10);

                // 3. Red Fabric (Outer Tunic / Scarf / Armor)
                bool isRedFabric = (r > 0.35 && g < 0.28 && b < 0.32 && r > g * 1.5) || 
                                   (r > 0.20 && g < 0.15 && b < 0.18 && r > g * 1.3);

                // 4. Dark Fabric (Pants / Boots / Shadow)
                bool isDarkFabric = (r < 0.45 && g < 0.45 && b < 0.45 && abs(r - g) < 0.15 && abs(g - b) < 0.15 && r > 0.04);

                // 5. Brown Tone (Hair / Helmet / Boots)
                bool isBrownTone = (r > 0.25 && g > 0.10 && g < 0.45 && b < 0.35 && r > g * 1.15 && !isSkin);

                fixed3 finalRGB = color.rgb;

                if (!isSkin)
                {
                    float lum = (r + g + b) / 3.0;

                    // HELMET / HAT / HAIR (Upper head region uvY >= 0.58)
                    if (uvY >= 0.58 && (isBrownTone || isRedFabric))
                    {
                        finalRGB = _HelmetColor.rgb * (lum > 0.30 ? 1.15 : 0.85);
                    }
                    // GLOVES (Handwraps area)
                    else if (isWhiteGloves)
                    {
                        finalRGB = _GlovesColor.rgb * (lum > 0.70 ? 1.10 : 0.90);
                    }
                    // BOOTS / SHOES / SOCKS (Lower foot region uvY < 0.34)
                    else if (uvY < 0.34 && (isDarkFabric || isBrownTone || isRedFabric))
                    {
                        finalRGB = _BootsColor.rgb * (lum > 0.20 ? 1.15 : 0.80);
                    }
                    // PANTS (Lower-mid body region 0.30 <= uvY < 0.45)
                    else if (uvY >= 0.30 && uvY < 0.45 && isDarkFabric)
                    {
                        finalRGB = _PantsColor.rgb * (lum > 0.15 ? 1.10 : 0.75);
                    }
                    // SCARF / CLOAK (Upper chest / neck 0.50 <= uvY < 0.60)
                    else if (uvY >= 0.50 && uvY < 0.60 && isRedFabric)
                    {
                        finalRGB = _CloakColor.rgb * (lum > 0.30 ? 1.15 : 0.85);
                    }
                    // ARMOR / SHIRT (Torso region)
                    else if (isRedFabric)
                    {
                        if (lum > 0.32)
                        {
                            finalRGB = lerp(_ArmorSecColor.rgb, _ArmorColor.rgb, saturate((lum - 0.32) / 0.35)) * 1.15;
                        }
                        else
                        {
                            finalRGB = _ArmorSecColor.rgb * (lum / 0.32) * 1.10;
                        }
                    }
                }

                if (_TierGlowIntensity > 0.01)
                {
                    finalRGB += _TierGlowColor.rgb * (_TierGlowIntensity * 0.35);
                }

                finalRGB *= IN.color.rgb;
                return fixed4(finalRGB, color.a * IN.color.a);
            }
            ENDCG
        }
    }
}
