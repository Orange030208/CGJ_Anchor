Shader "Trailer/Page Turn"
{
    Properties { [PerRendererData] _MainTex("Page",2D)="white"{} _Progress("Progress",Range(0,1))=0 }
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
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; };
            sampler2D _MainTex;
            float _Progress;
            v2f vert(appdata v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; return o; }
            fixed4 frag(v2f i):SV_Target
            {
                float p=smoothstep(0,1,_Progress);
                float fold=1.12-p*1.4+(i.uv.y-.5)*sin(p*3.14159)*.18;
                float width=sin(p*3.14159)*.19;
                float x=i.uv.x;
                if(x<fold-width) return tex2D(_MainTex,i.uv);
                if(x<fold && width>.001)
                {
                    float bend=(x-fold+width)/width;
                    float2 uv=float2(fold-width+sin(bend*1.5708)*width,i.uv.y);
                    fixed4 c=tex2D(_MainTex,uv);
                    float shade=.52+.48*cos(bend*3.14159);
                    c.rgb=lerp(c.rgb*shade,float3(.61,.87,.86),smoothstep(.42,1,bend)*.83);
                    c.rgb+=pow(sin(bend*3.14159),18)*.22;
                    c.a=1;
                    return c;
                }
                float shadow=exp(-max(0,x-fold)*55)*.42*sin(p*3.14159);
                return fixed4(.01,.03,.06,shadow);
            }
            ENDCG
        }
    }
}
