Shader "Trailer/Title Breathing Glow"
{
    Properties { [PerRendererData] _MainTex("Texture",2D)="white"{} _Reveal("Reveal",Range(0,1))=1 }
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
            float _Reveal;
            v2f vert(appdata v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color; return o; }
            fixed4 frag(v2f i):SV_Target
            {
                float2 pos=(i.uv-.5)*float2(880,440);
                float2 q=abs(pos)-float2(378,155)+8;
                float d=length(max(q,0))+min(max(q.x,q.y),0)-8;
                float breath=.5+.5*sin(_Time.y*2.61799);
                float ring=exp(-abs(d)/2.4);
                float aura=exp(-abs(d)/(10+8*breath))*.36;
                float alpha=(ring*.8+aura)*(.3+.7*breath)*_Reveal;
                float glint=pow(saturate(sin(atan2(pos.y,pos.x)*2-_Time.y*1.2)),20)*ring*.3;
                return fixed4(lerp(i.color.rgb,1,ring*.65),saturate(alpha+glint)*i.color.a);
            }
            ENDCG
        }
    }
}
