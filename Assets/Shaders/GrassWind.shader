Shader "Custom/GrassWind"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _WindStrength ("Wind Strength", Float) = 0.08
        _WindSpeed ("Wind Speed", Float) = 1.0
        _WindFrequency ("Wind Frequency", Float) = 2.0
    }

    SubShader
    {
        Tags { "RenderType"="TransparentCutout" }
        LOD 100

        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _WindStrength;
            float _WindSpeed;
            float _WindFrequency;

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
            UNITY_INSTANCING_BUFFER_END(Props)

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float hash(float n)
            {
                return frac(sin(n) * 43758.5453);
            }

            v2f vert(appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);

                float heightFactor = saturate(v.vertex.y); // assumes pivot at bottom (0 → 1)
                float heightScale = 0.6; // tweak this (0.5–0.8 good range)

                v.vertex.y *= lerp(1.0, heightScale, heightFactor);

                float instanceSeed = unity_ObjectToWorld._m03 + unity_ObjectToWorld._m23;

                float noise = hash(instanceSeed);

                float waveSpeed = _Time.y * _WindSpeed + instanceSeed * _WindFrequency;
                float wave = sin(waveSpeed) * 0.5 + sin(waveSpeed * 0.5) * 0.5;

                // voxel-style stepping
                wave = round(wave * 3.0) / 3.0;

                float wind = wave * _WindStrength * heightFactor;

                v.vertex.x += wind;
                v.vertex.z += wind * 0.5;

                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

                clip(col.a - 0.7);

                float4 color = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
                return col * color;
            }
            ENDCG
        }
    }
}
