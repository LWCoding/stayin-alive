Shader "Custom/UI/WhiteMask"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1) // This will be the white color
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Blend SrcAlpha OneMinusSrcAlpha // Standard alpha blending
        Cull Off
        Lighting Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color; // Pass vertex color for UI Tinting
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                fixed alpha = col.a; // Get the alpha from the texture

                // If alpha is greater than a small threshold (to account for anti-aliasing),
                // set the color to white, otherwise keep the original alpha.
                if (alpha > 0.01)
                {
                    col.rgb = i.color * _Color.rgb; // Set to white (or desired color from _Color)
                    col.a = alpha; // Maintain original alpha
                }
                else
                {
                    col.a = 0; // Fully transparent
                }
                return col;
            }
            ENDCG
        }
    }
}