Shader "Trailer/Cloud Mist"
{
    Properties
    {
        [PerRendererData] _MainTex("Texture",2D)="white"{}
        _Travel("Upward travel",Float)=0
        _Density("Cloud density",Float)=1.4
        _Opacity("Opacity",Range(0,1))=1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            float _Travel,_Density,_Opacity;
            v2f vert(appdata v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color; return o; }
            float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            float noise(float2 p)
            {
                float2 i=floor(p),f=frac(p); f=f*f*(3-2*f);
                return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);
            }
            float fbm(float2 p)
            {
                float n=0,a=.55;
                for(int j=0;j<4;j++) { n+=a*noise(p); p=p*2.03+float2(7.1,2.8); a*=.5; }
                return n;
            }
            fixed4 frag(v2f i):SV_Target
            {
                float2 uv=floor(i.uv*float2(640,360))/float2(640,360);
                float2 p=uv*float2(6.5,3.7)+float2(.12*sin(_Travel*.25),_Travel);
                float warp=fbm(p*.75+float2(2.4,-1.8));
                float n=fbm(p+float2(warp,warp*.4)*2.4);
                float wisps=fbm(p*float2(1.2,2.3)+4.2);
                float alpha=smoothstep(.31,.76,n+wisps*.18)*_Density*_Opacity;
                float light=smoothstep(.32,.77,n);
                fixed3 tint=lerp(fixed3(.18,.39,.5),fixed3(.87,1,.98),light);
                return fixed4(tint,saturate(alpha)*i.color.a);
            }
            ENDCG
        }
    }
}
