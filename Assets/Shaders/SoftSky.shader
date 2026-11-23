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
        _PanoramaScale ("Panorama Object Shrink", Range(1,4)) = 1
        _PanoramaSoftness ("Panorama Distance Softness", Range(0,1)) = 0
        _PanoramaMist ("Panorama Ground Mist", Range(0,1)) = 0
        _WeatherTint ("Weather Light", Color) = (1,1,1,1)
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
            #pragma target 3.0
            #include "UnityCG.cginc"
            fixed4 _Horizon;
            fixed4 _Zenith;
            float _HazeStrength;
            sampler2D _Panorama;
            float _PanoramaBlend;
            float _PanoramaGroundFade;
            float _PanoramaHorizonHeight;
            float _PanoramaScale;
            float _PanoramaSoftness;
            float _PanoramaMist;
            float4 _WeatherTint;
            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos: SV_POSITION; float3 dir : TEXCOORD0; };
            v2f vert(appdata v)
            {
                v2f o;
                // Sample the infinite sky by direction, not by the camera's
                // position inside a finite sphere. Otherwise the panorama
                // slides as the player orbits the tree.
                // Render the sky sphere centered on the viewer, while the
                // texture direction stays fixed in world space. This removes
                // camera-translation parallax and prevents approaching a sky
                // sphere wall while orbiting or zooming.
                float3 worldDirection = mul((float3x3)unity_ObjectToWorld, v.vertex.xyz);
                float3 cameraCentered = _WorldSpaceCameraPos + worldDirection;
                o.pos = mul(UNITY_MATRIX_VP, float4(cameraCentered, 1.0));
                // Unity platforms differ in depth-buffer direction.
                // Sky must stay at the far plane in both conventional and
                // reversed-Z rendering, behind all gameplay geometry.
                #if defined(UNITY_REVERSED_Z)
                    o.pos.z = 0.001 * o.pos.w;
                #else
                    o.pos.z = 0.999 * o.pos.w;
                #endif
                o.dir = normalize(worldDirection);
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
                sky.rgb *= _WeatherTint.rgb;
                hazeColor *= _WeatherTint.rgb;
                // Latitude/longitude lookup is seamless in yaw and uses one
                // optional texture fetch only when a panorama is enabled.
                if (_PanoramaBlend > .001)
                {
                    float3 dir = normalize(i.dir);
                    // Shrink photographed objects: whole-number yaw repeats stay
                    // seamless, and the curve is steepest at the horizon while
                    // still reaching the photo's zenith and nadir exactly.
                    float shrink = max(_PanoramaScale, 1.0);
                    float elevation = asin(clamp(dir.y, -1.0, 1.0)) / (0.5 * UNITY_PI);
                    float compressed = sign(elevation) *
                        (1.0 - pow(1.0 - abs(elevation), shrink));
                    float2 uv = float2(
                        (atan2(dir.z, dir.x) * (0.5 / UNITY_PI) + .5) * round(shrink),
                        0.5 + compressed * 0.5);
                    // Aerial perspective: a slightly blurred, paler and less
                    // saturated distance keeps the sharp 3D tree in front of it.
                    fixed3 panorama = tex2Dbias(_Panorama,
                        float4(uv, 0.0, _PanoramaSoftness * 1.6)).rgb;
                    float luma = dot(panorama, float3(.3, .59, .11));
                    panorama = lerp(panorama, luma.xxx, .22 * _PanoramaSoftness);
                    panorama *= _WeatherTint.rgb;
                    panorama = lerp(panorama, hazeColor,
                        .34 * _PanoramaSoftness * exp(-abs(dir.y) * 4.5));
                    // Ground mist: at and below the horizon the photo becomes the
                    // scene fog colour, so the fogged 3D ground has no visible edge.
                    panorama = lerp(panorama, unity_FogColor.rgb,
                        _PanoramaMist * (1.0 - smoothstep(0.0, 0.17, dir.y)));
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
