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
        
        Pass
        {
            cull off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct bladegrass
            {
                float3 position;
                float windOffset;
            };

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
                float3 normal_world : TEXCOORD2;
                float3 view_dir : TEXCOORD3;
                float3 pos_world : TEXCOORD4;
                float fresnel : TEXCOORD5;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            // Properties
            float _Height;
            float _Offset;
            float _ShadingOffset;
            float _ShadingParameter;

            float _SideOffsetAmount;
            float3 _WindDirection;
            float _WindForce;

            // Compute shader
            StructuredBuffer<bladegrass> _BladeBuffer;

            float rand(float2 co) {
                return frac(sin(dot(co.xy, float2(12.9898, 78.233))) * 43758.5453);
            }

            float4x4 rotateY(float angle) {
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
            float3 quadraticBezierCurve(float3 p0, float3 p1, float3 p2, float t)
            {
                float a = (1 - t) * (1 - t);
                float b = 2 * (1 - t) * t;
                float c = t * t;

                float x = a * p0.x + b * p1.x + c * p2.x;
                float y = a * p0.y + b * p1.y + c * p2.y;
                float z = a * p0.z + b * p1.z + c * p2.z;

                return float3(x, y, z);
            }

            float3 calculateBezierCurveTangent(float3 p0, float3 p1, float3 p2, float t)
            {
                return 2 * (1 - t) * (p1 - p0) + 2 * t * (p2 - p1);
            }

            v2f vert (appdata v, uint instanceID : SV_InstanceID)
            {

                v2f o;
                UNITY_INITIALIZE_OUTPUT(v2f, o);

                bladegrass blade = _BladeBuffer[instanceID];
                v.vertex.xyz *= float3(10.0, 10.0, 10.0);
                float3 spawnPos = blade.position;

                // Calculate wind force
                float windForceOffset = _WindForce * blade.windOffset;

                // Bezier curve to calculate knot position
                float3 up_vector = float3(0.0, 0.0, 1.0);
                float3 p0 = float3(0.0, 0.0, 0.0);                  // One endpoint
                float3 p2 = float3(sqrt(_Offset), sqrt(_Height), 0.0) + windForceOffset * _WindDirection;         // Tip of the grass

                float project_length = length(p2 - p0 - up_vector * dot(p2 - p0, up_vector));
                float3 p1 = p0 + _Height * up_vector * max(1 - project_length / _Height, 0.05 * max(project_length / _Height, 1.0));

                float3 knotPoint = quadraticBezierCurve(p0, p1, p2, v.uv.y);
                float3 tangent = normalize(calculateBezierCurveTangent(p0, p1, p2, v.uv.y));
                
                v.vertex += float4(knotPoint, 1.0);

                // Rotate the vertex first
                // Generate a random rotate angle along y axis
                float randomAngle = rand(spawnPos.xz) * 360.0;
                float4x4 rotateMatrix = rotateY(randomAngle);
                v.vertex = mul(rotateMatrix, v.vertex); 
                // Apply rotation matrix to normal
                v.normal = mul(rotateMatrix, float4(v.normal, 0.0));

                // Apply vertex to world position
                float3 worldVertPos = spawnPos + mul(unity_ObjectToWorld, v.vertex).xyz;
                float3 objectVertPos = mul(unity_WorldToObject, float4(worldVertPos, 1.0)).xyz;

				o.uv = v.uv;
                o.normal = v.normal;
				o.vertex = UnityObjectToClipPos(objectVertPos);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float shading = saturate(dot(_WorldSpaceLightPos0.xyz, i.normal));
				shading = (shading + _ShadingOffset) / _ShadingParameter;
                
                // Diffuse color
                float3 grass_color_bottom = float3(0.05, 0.2, 0.01);
                float3 grass_color_tip = float3(0.50, 0.50, 0.10);
                float3 diffuse_color = lerp(grass_color_bottom, grass_color_tip, i.uv.y);

                return float4(diffuse_color * shading, 1.0);
            }
            ENDCG
        }
    }
}
