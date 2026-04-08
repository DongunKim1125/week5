Shader "Custom/GridEdgeOnlyFlow"
{
    Properties
    {
        [HDR]_Color ("Glow Color", Color) = (0, 1, 1, 1)
        _Thickness ("Edge Thickness", Range(0.001, 0.1)) = 0.02 // 가장자리 두께
        _Speed ("Flow Speed", Float) = 2.0
        _Length ("Glow Length", Range(0.1, 1.0)) = 0.3
    }
    SubShader
    {
        // 투명도 설정을 위해 Transparent 큐 사용
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        Blend One One // 가산 혼합 (빛 효과 극대화)
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
            float _Thickness;
            float _Speed;
            float _Length;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv; 
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                // **핵심: 가장자리 마스크 계산**
                // step 함수를 이용해 UV 좌표가 0~_Thickness 사이거나, 1-_Thickness~1 사이일 때만 1을 반환
                float2 edge = step(0.5 - _Thickness, abs(i.uv - 0.5));
                float mask = saturate(edge.x + edge.y);

                // 마스크가 0인 안쪽 영역은 연산에서 아예 제외 (성능 향상 및 투명 처리)
                if(mask <= 0) discard;

                // 기존 흐르는 빛 연산
                float flow = frac(i.uv.x - (_Time.y * _Speed));
                float glow = smoothstep(1.0 - _Length, 1.0, flow);

                // 마스크를 적용하여 가장자리에만 빛이 나타나도록 함
                return _Color * glow * mask;
            }
            ENDCG
        }
    }
}