// PhotoPreview(RawImage)専用のUIブラーシェーダ。
// RectMask2D/ZTest/StencilはUI/Defaultと同形を保ち、他のUI要素の描画には影響しない。
// 撮影はPhotoCamera.targetTextureを直接ReadPixelsするため、このシェーダは撮影画像に一切影響しない。
Shader "Stage/PhotoPreviewBlur"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _BlurStrength ("Blur Strength", Range(0,1)) = 0
        _MaxBlurRadiusPixels ("Max Blur Radius (source texels)", Float) = 16
        _SourceTexelSize ("Source Texel Size (1/w,1/h,w,h)", Vector) = (0.0006944444, 0.0009259259, 1440, 1080)
        _BlurLodScale ("Blur Mip Scale", Float) = 0.5

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
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
            #pragma target 3.0

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
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 texcoord      : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;

            float _BlurStrength;
            float _MaxBlurRadiusPixels;
            float4 _SourceTexelSize;
            float _BlurLodScale;

            // 60度刻みの内周6点(重み2)と、30度オフセットの外周6点(重み1)。
            // 中心(重み3)と合わせた13タップ・合計重み21で円形ディスクぼかしを近似する。
            static const float2 InnerRing[6] = {
                float2( 1.0,       0.0),
                float2( 0.5,       0.8660254),
                float2(-0.5,       0.8660254),
                float2(-1.0,       0.0),
                float2(-0.5,      -0.8660254),
                float2( 0.5,      -0.8660254),
            };
            static const float2 OuterRing[6] = {
                float2( 0.8660254,  0.5),
                float2( 0.0,        1.0),
                float2(-0.8660254,  0.5),
                float2(-0.8660254, -0.5),
                float2( 0.0,       -1.0),
                float2( 0.8660254, -0.5),
            };

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

            // RawImageのテクスチャはCanvasRendererからマテリアル外で差し込まれるため、
            // _MainTex_TexelSizeは信頼できない。C#側から渡された_SourceTexelSizeで
            // 端のクランプとオフセット計算を行う。
            float4 SampleSource(float2 uv, float lod)
            {
                float2 halfTexel = _SourceTexelSize.xy * 0.5;
                uv = clamp(uv, halfTexel, 1.0 - halfTexel);
                return tex2Dlod(_MainTex, float4(uv, 0, lod));
            }

            float4 SampleBlurred(float2 uv)
            {
                float radiusPixels = _BlurStrength * _MaxBlurRadiusPixels;
                if (radiusPixels < 0.5)
                {
                    // ぼかしなしに等しい強度では、通常のUI描画と同じ1タップに落とす。
                    return tex2D(_MainTex, uv);
                }

                // ミップの無いRenderTextureでも安全: 存在しないLODはミップ0にクランプされる。
                float lod = max(0.0, log2(max(1.0, radiusPixels * _BlurLodScale)));
                float2 innerOffset = radiusPixels * 0.5 * _SourceTexelSize.xy;
                float2 outerOffset = radiusPixels * _SourceTexelSize.xy;

                float4 sum = SampleSource(uv, lod) * 3.0;
                [unroll]
                for (int i = 0; i < 6; i++)
                {
                    sum += SampleSource(uv + InnerRing[i] * innerOffset, lod) * 2.0;
                    sum += SampleSource(uv + OuterRing[i] * outerOffset, lod) * 1.0;
                }
                return sum * (1.0 / 21.0);
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 color = (SampleBlurred(IN.texcoord) + _TextureSampleAdd) * IN.color;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif

                return color;
            }
        ENDCG
        }
    }
}
