Shader "Custom/FlowLine"
{
    Properties
    {
        [HDR]_Color ("Color", Color) = (1,1,1,1)
        _Speed ("Flow Speed", Float) = 2.0
        _Length ("Glow Length", Range(0, 1)) = 0.2
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        Blend One One
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float4 _Color;
            float _Speed;
            float _Length;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv; // LineRenderer의 시작점이 0, 끝점이 1
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                // 선의 길이를 따라 시간이 흐름에 따라 이동하는 마스크
                float flow = frac(i.uv.x - (_Time.y * _Speed));
                float glow = smoothstep(1.0 - _Length, 1.0, flow);
                return _Color * glow;
            }
            ENDCG
        }
    }
}