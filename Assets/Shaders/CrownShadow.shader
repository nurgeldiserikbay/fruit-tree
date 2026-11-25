Shader "FruitTree/CrownShadow"
{
    Properties
    {
        _MainTex ("Shadow Shape (alpha)", 2D) = "white" {}
        _Strength ("Strength", Range(0,1)) = 0.38
        _ShadowColor ("Shadow Colour", Color) = (0.52,0.58,0.48,1)
    }
    SubShader
    {
        // After the ground and grass, so it darkens both; multiply blend never brightens.
        Tags { "Queue"="Geometry+60" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend DstColor Zero
        ZWrite Off
        Cull Off
        Offset -2, -2
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float _Strength;
            fixed4 _ShadowColor;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; UNITY_FOG_COORDS(1) };
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float shade = tex2D(_MainTex, i.uv).a * _Strength;
                // Fade out towards the decal border so no edge ever shows.
                float2 edge = abs(i.uv * 2.0 - 1.0);
                shade *= saturate((1.0 - max(edge.x, edge.y)) * 6.0);
                fixed4 colour = lerp(fixed4(1, 1, 1, 1), _ShadowColor, shade);
                UNITY_APPLY_FOG_COLOR(i.fogCoord, colour, fixed4(1, 1, 1, 1));
                return colour;
            }
            ENDCG
        }
    }
}
