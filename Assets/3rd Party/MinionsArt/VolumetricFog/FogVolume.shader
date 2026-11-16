Shader "Custom/FogVolume"
{
    Properties
    {
        [HDR]_Color ("Volume Color", Color) = (1.0, 1.0, 1.0, 1)
        _Density ("Volume Density", Float) = 2.0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "DisableBatching" = "True"
        }

        Blend One OneMinusSrcAlpha
        ZWrite Off
        ZTest Always
        Cull Front

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
            float4 _Color;
            float _Density;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 clipPos : SV_POSITION;
                float3 objectPos : TEXCOORD0;
                float4 screenPos : TEXCOORD1; // for depth

            };

            // Checks if a ray hits a 3D box. Returns true if it hits, along with the entry and exit distances.
            bool rayBoxIntersect(float3 rayOrigin, float3 rayDirection, out float entryDistance, out float exitDistance)
            {
                // precompute the direction so we can multiply instead of divide every time/ optimisation
                // we can write * invDirection instead of / raydirection
                float3 invDirection = 1 / rayDirection;

                // find the distance from the ray start point to the box's closest walls (-0.5 on the grid).
                float3 distanceToFrontWalls = (-0.5 - rayOrigin) * invDirection;

                // Find the distance from the ray start point to the box's furthest walls (0.5 on the grid).
                float3 distanceToBackWalls = (0.5 - rayOrigin) * invDirection;

                // Ssort the walls to get the actual entry distance for each side by finding the lowerst(min() number), it can be inverted beccause of negative directions.
                float3 trueEntryPerAxis = min(distanceToFrontWalls, distanceToBackWalls);

                // sort the walls to get the actual exit distance as well by finding the highest(max() number),
                float3 trueExitPerAxis = max(distanceToFrontWalls, distanceToBackWalls);

                // the absolute latest time,/furthest distance the ray enters the box, is the entry point
                entryDistance = max(max(trueEntryPerAxis.x, trueEntryPerAxis.y), trueEntryPerAxis.z);

                //  the absolute earliest / closest distance the ray leaves the box, is the exit point
                exitDistance = min(min(trueExitPerAxis.x, trueExitPerAxis.y), trueExitPerAxis.z);

                // if the ray exits AFTER it enters, and the exit isn't behind us(so higher than 0.0) , we have hit the box
                return exitDistance >= max(entryDistance, 0.0);
            }



            v2f vert(appdata v)
            {
                v2f o;
                o.clipPos = UnityObjectToClipPos(v.vertex);
                o.objectPos = v.vertex.xyz;
                o.screenPos = ComputeScreenPos(o.clipPos);  // screen pos for depth
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Convert the camera position from world space to the local object space of the mesh
                // this allows us to pretend our box is always perfectly sitting at the center (0,0,0) of the world, at no rotation,and at scale 1,1,1
                float3 cameraLocalPos = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1.0)).xyz;

                // calculate the direction the camera is looking by pointing from the camera to the surface pixels
                float3 rayDirection = normalize(i.objectPos - cameraLocalPos);

                float distanceToBoxEntry, distanceToBoxExit;

                // test if the look direction actually hits our box. If it completely misses, dont render the pixel
                if (!rayBoxIntersect(cameraLocalPos, rayDirection, distanceToBoxEntry, distanceToBoxExit))
                {
                    discard;
                }

                // if the camera is sitting inside the box itself, clamp the entry distance to 0.0.
                // so the distance calculation doesnt start behind behind the camera.
                distanceToBoxEntry = max(distanceToBoxEntry, 0.0);

                // get the depth buffer
                float2 screenUV = i.screenPos.xy / i.screenPos.w;
                float rawDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, screenUV);
                float eyeDepth = LinearEyeDepth(rawDepth);  // eyedepth in world units

                // skip if its just the skybox , or super far depth
                if (eyeDepth < 999)
                {
                    // were converting the depth from worldspace to the rays space here

                    // camera forward direction (matrix is backwards pointing so - is needed)
                    float3 camForwardWS = -UNITY_MATRIX_V[2].xyz;

                    // take the ray direction from object to world space to match the camera space
                    float3 rayDirectionWS = normalize(mul((float3x3)unity_ObjectToWorld, rayDirection));

                    // angle between the camera forward and our ray, the ray goes from the camera to all parts of the mesh, so they are not always equal, 1 if same direction, 0 if sideways
                    float cosAngle = dot(camForwardWS, rayDirectionWS);

                    if (cosAngle > 0.001) // avoid dividing by 0 when the ray point sideways to the cam
                    {
                        // convert the eyedepth to be along our ray direction in worldspace
                        float depthAlongRay = eyeDepth / cosAngle;

                        // world space point where our ray hits the eyedepth result
                        float3 sceneHitWS = _WorldSpaceCameraPos + rayDirectionWS * depthAlongRay;

                        // bring it back into object space, since distanceToBoxExit is measured in object space
                        float3 sceneHitOS = mul(unity_WorldToObject, float4(sceneHitWS, 1.0)).xyz;

                        // Project that point back onto our ray to get a plain distance value
                        float rayDepthDistance = dot(sceneHitOS - cameraLocalPos, rayDirection);

                        // if the depth result (floors/walls) is closer than the box, that wins and we stop rendering
                        distanceToBoxExit = min(distanceToBoxExit, rayDepthDistance);

                    }
                }

                // calculate the total distance that the view ray spends traveling inside the box, which works as an alpha
                float distanceInBox = distanceToBoxExit - distanceToBoxEntry;

                // multiply with density
                float density = _Density * distanceInBox;

                // add color
                float4 coloredDensity = saturate(density) * _Color;

                return coloredDensity;
            }

            ENDCG
        }
    }

    FallBack Off
}
