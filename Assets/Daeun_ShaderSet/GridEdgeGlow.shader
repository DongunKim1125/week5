Shader "Custom/GridEdgeGlow"
{
    Properties
    {
        _MainTex ("Grid Texture", 2D) = "white" {}
        _EdgeWidth ("Edge Width", Range(0.005, 0.05)) = 0.02
        _FlowSpeed ("Flow Speed", Float) = 1.0
        _FlowIntensity ("Flow Intensity", Float) = 2.0
        _GlowColor ("Glow Color", Color) = (0, 0.5, 1, 1)
        [HDR] _GlowBloom ("Glow Bloom", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _EdgeWidth;
            float _FlowSpeed;
            float _FlowIntensity;
            float4 _GlowColor;
            float _GlowBloom;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // **1. Base Grid Texture Sampling**
                fixed4 gridBaseColor = tex2D(_MainTex, i.uv);
                float4 finalColor = gridBaseColor;

                // **2. Edge Detection Logic**
                // Calculate the distance from each edge (0 to 1)
                float edgeMask = saturate(
                    step(i.uv.x, _EdgeWidth) + 
                    step(1.0 - _EdgeWidth, i.uv.x) + 
                    step(i.uv.y, _EdgeWidth) + 
                    step(1.0 - _EdgeWidth, i.uv.y)
                );

                // **3. Edge Only Mask (excluding inner grid)**
                // We use the edge mask, and then only apply to the final texture's background area (assuming background is dark/darker)
                // If you want it only *on* the outer cell's outer boundaries, it's simpler
                // The most robust way is to make sure your grid texture has a clear distinction, which it does.
                // However, the simplest way is to just define the external boundary.
                
                // Let's create a combined flow effect only on the mask
                if (edgeMask > 0.5)
                {
                    // **4. Flow Calculation**
                    // Define a flow position along the edges (linear distance mapping is complex, let's use a simpler one)
                    // Flow position can be approximated by: frac(uv.x + uv.y + time)
                    float flowPos = frac(i.uv.x + i.uv.y - (_Time.y * _FlowSpeed));

                    // Make it a sharp pulse
                    float pulse = step(0.9, flowPos); 
                    
                    // The glowing effect (HDR emission)
                    float4 glow = _GlowColor * pulse * _FlowIntensity * _GlowBloom;
                    
                    // Combine with the base grid color but only on the mask
                    finalColor += glow;
                }

                return finalColor;
            }
            ENDCG
        }
    }
}