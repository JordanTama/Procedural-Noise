Shader "Unlit/NoiseGeneratorPreview"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Mask ("Channel Mask", Int) = 15
    }
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            int _Mask;

            float get_mask_multiplier(const int val)
            {
                return (val & _Mask) == val ? 1.0 : 0.0;
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (const v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

                const float4 mask = float4(
                    get_mask_multiplier(1),
                    get_mask_multiplier(2),
                    get_mask_multiplier(4),
                    get_mask_multiplier(8)
                    );

                col *= mask;

                if (mask.a == 1)
                    col.rgb = float3(col.a, col.a, col.a);
                
                return col;
            }
            ENDCG
        }
    }
}
