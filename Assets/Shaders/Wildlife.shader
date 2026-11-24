Shader "FruitTree/Wildlife"
{
    Properties
    {
        _Brightness ("Brightness", Range(0.5, 1.5)) = 1
    }
    SubShader
    {
        // Wing phase comes from each animal's own transform, so no batching.
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "DisableBatching"="True" }
        Cull Off
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            float _Brightness;
            float3 _WeatherDim;
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 color : COLOR;
                // x: wing beats per second (radians), y: wingtip travel in metres.
                float2 flap : TEXCOORD1;
            };
            struct v2f
            {
                float4 pos : SV_POSITION;
                fixed3 color : COLOR;
                float3 normal : TEXCOORD0;
                UNITY_FOG_COORDS(1)
            };
            v2f vert(appdata v)
            {
                v2f o;
                float3 local = v.vertex.xyz;
                float3 origin = float3(unity_ObjectToWorld._m03, unity_ObjectToWorld._m13,
                    unity_ObjectToWorld._m23);
                float phase = dot(origin, float3(1.7, .3, 2.9));
                // Vertex alpha marks the wing: 0 at the body, 1 at the tip.
                local.y += sin(_Time.y * v.flap.x + phase) * v.flap.y * v.color.a;
                o.pos = UnityObjectToClipPos(float4(local, 1));
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.color = v.color.rgb;
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float facing = abs(dot(normalize(i.normal), normalize(_WorldSpaceLightPos0.xyz)));
                float3 light = .55 + .45 * facing * _LightColor0.rgb;
                fixed4 pixel = fixed4(i.color * light * _Brightness * (1.0 - _WeatherDim), 1);
                UNITY_APPLY_FOG(i.fogCoord, pixel);
                return pixel;
            }
            ENDCG
        }
    }
}
