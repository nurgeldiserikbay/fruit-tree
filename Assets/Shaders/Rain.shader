Shader "FruitTree/Rain"
{
    Properties
    {
        _Color ("Streak Color", Color) = (0.82,0.87,0.95,0.5)
        _Intensity ("Intensity", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        ZWrite Off
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color;
            float _Intensity;
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float fade : TEXCOORD1;
            };
            v2f vert(appdata v)
            {
                const float box = 11.0, height = 9.0;
                const float3 wind = float3(.13, -1.0, .06);
                v2f o;
                float speed = lerp(11.0, 17.0, v.color.r);
                float3 cam = _WorldSpaceCameraPos;
                // Drops stay anchored in the world and wrap around the camera,
                // so orbiting moves through the rain instead of dragging it.
                float3 rel = float3(v.vertex.x * box, v.vertex.y * height, v.vertex.z * box) +
                    wind * _Time.y * speed - cam;
                rel.x = (frac(rel.x / box + .5) - .5) * box;
                rel.y = (frac(rel.y / height + .5) - .5) * height;
                rel.z = (frac(rel.z / box + .5) - .5) * box;
                float3 center = cam + rel;
                float3 direction = normalize(wind);
                float3 side = normalize(cross(direction, normalize(cam - center)));
                float streak = lerp(.34, .62, v.color.r);
                float3 world = center + direction * streak * (v.uv.y - .5) + side * v.uv.x * .008;
                o.pos = UnityWorldToClipPos(world);
                // Drops above the current intensity collapse and draw nothing.
                o.pos *= step(v.color.g, _Intensity);
                o.uv = v.uv;
                o.fade = saturate((length(rel) - .7) / 1.5);
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float edge = 1.0 - abs(i.uv.x);
                float along = sin(i.uv.y * UNITY_PI);
                return fixed4(_Color.rgb, _Color.a * edge * along * i.fade);
            }
            ENDCG
        }
    }
}
