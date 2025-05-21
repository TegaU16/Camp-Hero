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
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            // Shader properties
            fixed4 _Color;
            fixed4 _Ambient;
            fixed4 _LightColor;
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
                float3 normal : TEXCOORD0;
                float2 uv : TEXCOORD1; // Pass UVs to the fragment shader
            };

            // Vertex shader: transforms vertex and normal
            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.uv = v.uv; // Pass the UVs
                return o;
            }

            // Fragment shader: apply lighting and texture
            fixed4 frag (v2f i) : SV_Target
            {
                // Calculate directional light strength based on normals
                float lightStrength = max(0, dot(normalize(i.normal), normalize(_LightDirection.xyz)));
                fixed4 lighting = _Ambient + _LightColor * lightStrength;

                // Apply global day/night light multiplier
                lighting *= _LightMultiplier;

                // Sample the texture color at the given UV coordinate
                fixed4 texColor = tex2D(_MainTex, i.uv) * _Color;

                // Multiply the texture color with the calculated lighting
                return texColor * lighting;
            }
            ENDCG
        }
    }
}
