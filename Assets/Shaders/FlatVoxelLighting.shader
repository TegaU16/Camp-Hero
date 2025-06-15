Shader "Custom/FlatVoxelLighting"
{
    Properties
    {
        _Color ("Color Tint", Color) = (1, 1, 1, 1)
        _MainTex ("Base Texture", 2D) = "white" { }
        _LightDirection ("Light Direction", Vector) = (0, 1, 0, 0)
        _LightColor ("Light Color", Color) = (1, 1, 1, 1)
        _Ambient ("Ambient Light", Color) = (0.4, 0.4, 0.4, 1)
        _LightMultiplier ("Light Multiplier", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma multi_compile_shadowcaster

            #include "UnityCG.cginc"
            #include "AutoLight.cginc"

            // Shader properties
            float4 _Color;
            float4 _Ambient;
            float4 _LightColor;
            float4 _LightDirection;
            sampler2D _MainTex; // The texture sampler

            float _LightMultiplier;

            // Vertex data structure
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0; // UVs for the texture
            };

            // Output data structure
            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 worldNormal : TEXCOORD1;
                SHADOW_COORDS(2)
            };

            // Vertex shader: transforms vertex and normal
            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldNormal = float4(UnityObjectToWorldNormal(v.normal), 0);
                o.uv = v.uv; // Pass the UVs
                TRANSFER_SHADOW(o);
                return o;
            }

            // Fragment shader
            float4 frag(v2f i) : SV_Target
            {
                float3 normal = normalize(i.worldNormal.xyz);
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);

                float ndotl = max(0, dot(normal, lightDir));

                // Sample shadow map
                float shadow = SHADOW_ATTENUATION(i);

                float4 lighting = _Ambient + _LightColor * ndotl * shadow;
                lighting *= _LightMultiplier;

                float4 texColor = tex2D(_MainTex, i.uv) * _Color;
                texColor.rgb *= lighting.rgb;

                return texColor;
            }
            ENDCG
        }
    }
}