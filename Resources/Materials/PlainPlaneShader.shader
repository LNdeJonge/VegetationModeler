// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Unlit/PlainPlaneShader"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1, 1, 1, 1)
    }
        SubShader
        {
            Cull Off
            //ZTest LEqual
            //ZWrite On

            Tags { "RenderType" = "Opaque" }
            LOD 100

            Pass
            {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma multi_compile_instancing

    #pragma target 5.0

                #include "UnityCG.cginc"

                struct appdata
                {
                    float4 vertex : POSITION;
                    float2 uv : TEXCOORD0;
                    float3 normal : NORMAL;
                };

                struct v2f
                {
                    float2 uv : TEXCOORD0;
                    float4 vertex : SV_POSITION;
                    float3 normal : TEXCOORD1;
                };

                sampler2D _MainTex;
                float4 _MainTex_ST;

                uniform float3 _LightDir;
                uniform int CurrentEffect;
                uniform fixed4 _Color;

                      v2f vert(appdata v)
                      {
                          v2f o;
                          o.vertex = UnityObjectToClipPos(v.vertex);
                          o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                          //  o.normal = v.normal;
                          o.normal = mul(unity_ObjectToWorld, float4(v.normal, 0.0)).xyz;


                            return o;
                        }

                        float3 cel_shading(float3 n, float3 lightDir, float3 lightColor)
                        {
                            float3 color = float3(0.1, 0.1, 0.1);
                            float intensity = dot(n, normalize(lightDir));
                            intensity = ceil(intensity * 4) / 4;
                            intensity = max(intensity, 0.1);
                            color = lightColor * intensity;
                            return color;
                        }

                        fixed4 frag(v2f i) : SV_Target
                        {
                            // sample the texture
                            fixed4 col = _Color;

                        // clip(col.a - 1);

                         float nS = dot(-_LightDir.xyz, normalize(i.normal));

                         float3 shading = float3(nS, nS, nS);

                         if (CurrentEffect == 2)
                         {
                             shading = cel_shading(i.normal, -_LightDir, float3(1, 1, 1));
                         }

                         float4 shade = float4(shading.xyz, 1);

                         return col * shade;
                     }
                     ENDCG
                 }

        }
            Fallback "Diffuse"
}
