Shader "FruitTree/SoftSky"
{
    Properties
    {
        _Horizon ("Horizon", Color) = (0.88,0.94,0.95,1)
        _Zenith ("Upper Sky", Color) = (0.43,0.68,0.86,1)
        _HazeStrength ("Horizon Atmosphere", Range(0,1)) = 0.35
        _Panorama ("Equirectangular Panorama", 2D) = "white" {}
        _PanoramaBlend ("Panorama Blend", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Opaque" }
        Cull Front ZWrite Off ZTest LEqual
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Horizon;
            fixed4 _Zenith;
            float _HazeStrength;
            sampler2D _Panorama;
            float _PanoramaBlend;
            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos: SV_POSITION; float3 dir : TEXCOORD0; };
            v2f vert(appdata v)
            {
                v2f o;
                float3 world = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = normalize(world - _WorldSpaceCameraPos);
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float h = saturate(i.dir.y * .65 + .45);
                h = h * h * (3 - 2 * h);
                fixed4 sky = lerp(_Horizon, _Zenith, h);
                // Soft atmospheric scattering close to the horizon. The
                // panoramic direction keeps the effect seamless at 360 degrees.
                float horizonBand = exp2(-abs(i.dir.y) * 17.0);
                fixed3 hazeColor = lerp(_Horizon.rgb, fixed3(1, 1, 1), .16);
                sky.rgb = lerp(sky.rgb, hazeColor,
                    saturate(horizonBand * _HazeStrength));
                // Latitude/longitude lookup is seamless in yaw and uses one
                // optional texture fetch only when a panorama is enabled.
                if (_PanoramaBlend > .001)
                {
                    float3 dir = normalize(i.dir);
                    float2 uv = float2(atan2(dir.z, dir.x) * (0.5 / UNITY_PI) + .5,
                        0.5 - asin(clamp(dir.y, -1.0, 1.0)) / UNITY_PI);
                    fixed3 panorama = tex2D(_Panorama, uv).rgb;
                    sky.rgb = lerp(sky.rgb, panorama, saturate(_PanoramaBlend));
                }
                return sky;
            }
            ENDCG
        }
    }
}
