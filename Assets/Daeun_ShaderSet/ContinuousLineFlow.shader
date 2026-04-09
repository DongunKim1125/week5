Shader "Custom/ContinuousLineFlow"
{
    Properties
    {
        [HDR]_Color ("Glow Color", Color) = (0, 1, 1, 1) // 빛의 색상
        _Speed ("Flow Speed", Float) = 1.0
        _Length ("Glow Length", Range(0.01, 1.0)) = 0.15 // 빛의 꼬리 길이
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        Blend One One // 바탕색을 덮지 않고 더해줌 (그리드 색상 유지)
        ZWrite Off
        Cull Off

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
                o.uv = v.uv; 
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                // UV.x를 기준으로 시간이 지남에 따라 0~1을 반복하는 흐름 생성
                float flow = frac(i.uv.x - (_Time.y * _Speed));
                float glow = smoothstep(1.0 - _Length, 1.0, flow);
                return _Color * glow;
            }
            ENDCG
        }
    }
}