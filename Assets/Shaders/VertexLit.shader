Shader "FruitTree/VertexLit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Wind ("Wind Amount", Range(0,0.1)) = 0
        _Cull ("Cull Back Faces", Float) = 2
        _AmbientLight ("Minimum Light", Range(0,1)) = 0.55
        _LeafTranslucency ("Leaf Backlight", Range(0,0.5)) = 0
        _AlphaCutoff ("Leaf Alpha Cutoff", Range(0,1)) = 0
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
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            fixed4 _Color;
            float _Wind;
            float _AmbientLight;
            float _LeafTranslucency;
            float _AlphaCutoff;
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
                UNITY_FOG_COORDS(2)
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
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float3 n = normalize(i.normal);
                float3 sun = normalize(_WorldSpaceLightPos0.xyz);
                float facing = dot(n, sun);
                float diffuse = saturate(facing);
                // Leaf cards are rendered on both sides, but their mesh
                // normals point in only one direction. Use a gentle diffuse
                // response on the reverse side so half the crown does not
                // become a uniformly dark silhouette. Non-leaf materials
                // have _LeafTranslucency = 0 and retain their original light.
                float reverseDiffuse = saturate(-facing);
                float leafLight = max(diffuse,
                    reverseDiffuse * _LeafTranslucency);
                float light = _AmbientLight + (1.0 - _AmbientLight) *
                    saturate(leafLight);
                fixed4 tex = tex2D(_MainTex, i.uv);
                // Optional cutout foliage texture; zero leaves all other
                // opaque wood, grass and scenery materials unchanged.
                clip(tex.a - _AlphaCutoff);
                fixed4 pixel = fixed4(tex.rgb * i.color.rgb * light, 1);
                UNITY_APPLY_FOG(i.fogCoord, pixel);
                return pixel;
            }
            ENDCG
        }
    }
    Fallback "Diffuse"
}
