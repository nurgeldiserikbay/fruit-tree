Shader "FruitTree/VertexLit"
{
    Properties { _Color ("Tint", Color) = (1,1,1,1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 120
        Cull Off
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            fixed4 _Color;
            struct appdata { float4 vertex: POSITION; float3 normal: NORMAL; fixed4 color: COLOR; };
            struct v2f { float4 pos: SV_POSITION; fixed4 color: COLOR; float3 normal: TEXCOORD0; };
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.color = v.color * _Color;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float3 n = normalize(i.normal);
                float diffuse = saturate(dot(n, normalize(_WorldSpaceLightPos0.xyz)));
                float light = .48 + .52 * diffuse;
                return fixed4(i.color.rgb * light, 1);
            }
            ENDCG
        }
    }
    Fallback "Diffuse"
}
