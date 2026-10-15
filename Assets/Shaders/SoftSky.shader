Shader "FruitTree/SoftSky"
{
    Properties
    {
        _Horizon ("Horizon", Color) = (0.88,0.94,0.95,1)
        _Zenith ("Upper Sky", Color) = (0.43,0.68,0.86,1)
        _HazeStrength ("Horizon Atmosphere", Range(0,1)) = 0.35
        _Panorama ("Equirectangular Panorama", 2D) = "white" {}
        _PanoramaBlend ("Panorama Blend", Range(0,1)) = 0
        _PanoramaGroundFade ("Panorama Ground Fade", Range(0,1)) = 1
        _PanoramaHorizonHeight ("Panorama Horizon Height", Range(-0.2,0.5)) = 0.12
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
            float _PanoramaGroundFade;
            float _PanoramaHorizonHeight;
            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos: SV_POSITION; float3 dir : TEXCOORD0; };
            v2f vert(appdata v)
            {
                v2f o;
                // Sample the infinite sky by direction, not by the camera's
                // position inside a finite sphere. Otherwise the panorama
                // slides as the player orbits the tree.
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = normalize(mul((float3x3)unity_ObjectToWorld, v.vertex.xyz));
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
                        0.5 + asin(clamp(dir.y, -1.0, 1.0)) / UNITY_PI);
                    fixed3 panorama = tex2D(_Panorama, uv).rgb;
                    // Perspective-style foreground in concept art appears
                    // huge on a sky sphere. Fade its lower hemisphere into
                    // the biome haze instead of showing a nearby fake floor.
                    // Fade photographic foreground before it intersects the
                    // actual 3D terrain. Keep the sky above the horizon.
                    // Suppress close-up photographed ground and oversized
                    // near-field objects; keep the distant upper sky only.
                    // Shiftable horizon helps align varying panorama sources.
                    float skyOnly = smoothstep(_PanoramaHorizonHeight - .07,
                        _PanoramaHorizonHeight + .20, dir.y);
                    float groundFade = lerp(1.0, skyOnly,
                        saturate(_PanoramaGroundFade));
                    sky.rgb = lerp(sky.rgb, panorama,
                        saturate(_PanoramaBlend * groundFade));
                }
                return sky;
            }
            ENDCG
        }
    }
}
