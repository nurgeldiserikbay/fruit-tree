Shader "FruitTree/SoftSky"
{
    Properties
    {
        _Horizon ("Horizon", Color) = (0.88,0.94,0.95,1)
        _Zenith ("Upper Sky", Color) = (0.43,0.68,0.86,1)
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
                return lerp(_Horizon, _Zenith, h);
            }
            ENDCG
        }
    }
}
