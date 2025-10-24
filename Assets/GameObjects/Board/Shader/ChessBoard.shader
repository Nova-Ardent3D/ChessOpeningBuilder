Shader "Unlit/Shader"
{
    Properties
    {
        _Black ("Black", Color) = (0,0,0,1)
        _White ("White", Color) = (1,1,1,1)
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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 _Black;
            float4 _White;

            fixed4 frag (v2f i) : SV_Target
            {
                int x = floor(i.uv.x * 8) % 2;
                int y = floor(i.uv.y * 8) % 2;

                int black = x == y;
                return lerp(_White, _Black, black);
            }
            ENDCG
        }
    }
}
