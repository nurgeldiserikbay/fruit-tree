Shader "FruitTree/VertexLit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Wind ("Wind Amount", Range(0,0.1)) = 0
        _Cull ("Cull Back Faces", Float) = 2
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 120
        Cull [_Cull]
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            fixed4 _Color;
            float _Wind;
            sampler2D _MainTex;
            struct appdata
            {
                float4 vertex: POSITION;
                float3 normal: NORMAL;
                float4 color: COLOR;
                float2 uv: TEXCOORD0;
            };
            struct v2f
            {
                float4 pos: SV_POSITION;
                fixed4 color: COLOR;
                float3 normal: TEXCOORD0;
                float2 uv: TEXCOORD1;
            };
            v2f vert(appdata v)
            {
                v2f o;
                float3 local = v.vertex.xyz;
                float gust = sin(_Time.y * 1.1 + local.x * 3.7 + local.z * 2.6) * 0.6 +
                             sin(_Time.y * 1.9 + local.y * 3.3) * 0.4;
                // Hold stems still while leaf tips gently bend in the breeze.
                local.xz += gust * _Wind * saturate(v.uv.y);
                o.pos = UnityObjectToClipPos(float4(local,1));
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.color = v.color * _Color;
                o.uv = v.uv;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float3 n = normalize(i.normal);
                float diffuse = saturate(dot(n, normalize(_WorldSpaceLightPos0.xyz)));
                float light = .48 + .52 * diffuse;
                fixed4 tex = tex2D(_MainTex, i.uv);
                return fixed4(tex.rgb * i.color.rgb * light, 1);
            }
            ENDCG
        }
    }
    Fallback "Diffuse"
}
