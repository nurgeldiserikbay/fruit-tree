Shader "FruitTree/Water"
{
    Properties
    {
        _ShallowColor ("Shallow Water", Color) = (0.20,0.27,0.23,0.72)
        _DeepColor ("Deep Water", Color) = (0.09,0.15,0.16,0.88)
        _Flow ("Flow Speed", Float) = 0.35
        _GroundFade ("Fade Into Photo", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent-20" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
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
            fixed4 _ShallowColor;
            fixed4 _DeepColor;
            float _Flow;
            float _GroundFade;
            float3 _WeatherDim;
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 world : TEXCOORD1;
                UNITY_FOG_COORDS(2)
            };
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                // u runs downstream in metres, v goes bank to bank.
                float2 p = float2(i.uv.x - _Time.y * _Flow, i.uv.y * 3.0);
                float t = _Time.y;
                float rippleA = sin(p.x * 6.3 + sin(p.y * 2.1 + t * .7) * 1.3);
                float rippleB = sin(p.x * 13.1 - p.y * 4.7 + t * 1.3);
                float rippleC = sin(p.x * 21.0 + p.y * 9.0 - t * 2.0);
                float3 n = normalize(float3(rippleA * .10 + rippleC * .04, 1.0,
                    rippleB * .08 + cos(p.x * 6.3) * .05));
                float3 view = normalize(_WorldSpaceCameraPos - i.world);
                float fresnel = .08 + .5 * pow(1.0 - saturate(dot(n, view)), 3.0);
                float across = abs(i.uv.y * 2.0 - 1.0);
                float depth = 1.0 - across * across;
                fixed4 body = lerp(_ShallowColor, _DeepColor, depth);
                // The horizon colour stands in for the reflected sky and trees.
                float3 color = lerp(body.rgb, unity_FogColor.rgb * .85, fresnel);
                float3 h = normalize(normalize(_WorldSpaceLightPos0.xyz) + view);
                color += pow(saturate(dot(n, h)), 140.0) * _LightColor0.rgb * .9;
                color += smoothstep(.78, 1.0, across) * .08;
                color *= 1.0 - _WeatherDim;
                float alpha = lerp(body.a, _DeepColor.a, depth) * (1.0 - smoothstep(.86, 1.0, across));
                if (_GroundFade > .5)
                    alpha *= 1.0 - smoothstep(9.0, 22.0, length(i.world.xz));
                fixed4 pixel = fixed4(color, alpha);
                UNITY_APPLY_FOG(i.fogCoord, pixel);
                return pixel;
            }
            ENDCG
        }
    }
}
