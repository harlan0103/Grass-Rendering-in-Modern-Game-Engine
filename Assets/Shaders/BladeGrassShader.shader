Shader"Unlit/BladeGrassShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        Cull off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

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

            // Properties
            float _Height;
            float _Offset;

            // Compute shader
            StructuredBuffer<float3> _Positions;

            float rand(float2 co) {
                return frac(sin(dot(co.xy, float2(12.9898, 78.233))) * 43758.5453);
            }

            float4x4 RotateY(float angle) {
                float c = cos(angle);
                float s = sin(angle);
                return float4x4(
                    c, 0, s, 0,
                    0, 1, 0, 0,
                    -s, 0, c, 0,
                    0, 0, 0, 1
                );
            }

            // A quadratic bezier curve function
            float2 quadraticBezierCurve(float3 p0, float3 p1, float3 p2, float t)
            {
                float a = (1 - t) * (1 - t);
                float b = 2 * (1 - t) * t;
                float c = t * t;

                float x = a * p0.x + b * p1.x + c * p2.x;
                float y = a * p0.y + b * p1.y + c * p2.y;

                return float2(x, y);
            }

            v2f vert (appdata v, uint instanceID : SV_InstanceID)
            {
                // Bezier curve
                float3 p0 = float3(0.0, 0.0, 0.0);
                float3 p1 = float3(0.0, _Height, 0.0);
                float3 p2 = float3( _Offset, _Height, 0.0);

                float3 curvePoint = float3(quadraticBezierCurve(p0, p1, p2, v.uv.y), 0.0);
                float3 spawnPos = float3(_Positions[instanceID]);

                float randomAngle = rand(spawnPos.xz) * 360.0;
                float4x4 rotateMatrix = RotateY(randomAngle);
                float3 rotateVertex = mul(rotateMatrix, v.vertex + curvePoint); 
                
				float3 worldVertPos = spawnPos + mul(unity_ObjectToWorld, rotateVertex * 10.0);
				float3 objectVertPos = mul(unity_WorldToObject, float4(worldVertPos.xyz, 1));

				v2f o;
				o.uv = v.uv;
                o.normal = v.normal;
				o.vertex = UnityObjectToClipPos(objectVertPos);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float shading = saturate(dot(_WorldSpaceLightPos0.xyz, i.normal));
				shading = (shading + 1.2) / 1.4;

				//return float4(i.color.xyz * shading, 1);
                return float4(0.3, 0.4, 0.2, 0.0);
            }
            ENDCG
        }
    }
}
