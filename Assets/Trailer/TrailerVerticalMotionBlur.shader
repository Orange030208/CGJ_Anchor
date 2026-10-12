Shader "Trailer/Vertical Motion Blur"
{
    Properties
    {
        [PerRendererData] _MainTex("Texture",2D)="white"{}
        _BlurPixels("Vertical shutter length",Range(0,100))=0
        _StencilComp("Stencil Comparison",Float)=8
        _Stencil("Stencil ID",Float)=0
        _StencilOp("Stencil Operation",Float)=0
        _StencilWriteMask("Stencil Write Mask",Float)=255
        _StencilReadMask("Stencil Read Mask",Float)=255
        _ColorMask("Color Mask",Float)=15
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; float4 local:TEXCOORD1; };
            sampler2D _MainTex;
            float4 _MainTex_TexelSize,_ClipRect;
            float _BlurPixels;
            v2f vert(appdata v) { v2f o; o.local=v.vertex; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color; return o; }
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 color=0;
                float sum=0;
                [unroll] for(int tap=-4;tap<=4;tap++)
                {
                    float weight=exp(-tap*tap*.16);
                    float2 uv=i.uv+float2(0,tap*.25*_BlurPixels*_MainTex_TexelSize.y);
                    color+=tex2D(_MainTex,saturate(uv))*weight;
                    sum+=weight;
                }
                color=color/sum*i.color;
                #ifdef UNITY_UI_CLIP_RECT
                    color.a*=UnityGet2DClipping(i.local.xy,_ClipRect);
                #endif
                return color;
            }
            ENDCG
        }
    }
}
