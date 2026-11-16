// Checks if a ray hits a 3D box. Returns true if it hits, along with the entry and exit distances.
void RayBoxIntersect_float(in float3 rayOrigin, in float3 rayDirection, out float EntryDistance, out float ExitDistance, out float HitBox)
{
    // set the shader graph node previews
    #ifdef SHADERGRAPH_PREVIEW
        EntryDistance = 1;
        ExitDistance = 2;
        HitBox = 1;
    #else
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
        EntryDistance = max(max(trueEntryPerAxis.x, trueEntryPerAxis.y), trueEntryPerAxis.z);

        //  the absolute earliest / closest distance the ray leaves the box, is the exit point
        ExitDistance = min(min(trueExitPerAxis.x, trueExitPerAxis.y), trueExitPerAxis.z);

        HitBox = (ExitDistance >= max(EntryDistance, 0.0)) ? 1.0 : 0.0;

        // if the camera is sitting inside the box itself, clamp the entry distance to 0.0.
        // so the distance calculation doesnt start behind behind the camera.
        EntryDistance = max(EntryDistance, 0.0);
    #endif
}

// depth testing
void DepthTesting_float(in float EntryDistance, in float ExitDistance, in float3 RayDirectionOS, in float3 CameraLocalPos, in float EyeDepth, out float ClippedExitDistance)
{

    // camera forward direction (matrix is backwards pointing so - is needed)
    float3 camForwardWS = -UNITY_MATRIX_V[2].xyz;

    // take the ray direction from object to world space to match the camera space
    float3 rayDirectionWS = normalize(mul((float3x3)unity_ObjectToWorld, RayDirectionOS));

    // angle between the camera forward and our ray, the ray goes from the camera to all parts of the mesh,
    // so they are not always equal, 1 if same direction, 0 if sideways
    float cosAngle = dot(camForwardWS, rayDirectionWS);

    ClippedExitDistance = ExitDistance;
    // skip if its just the skybox , or super far depth
    // avoid dividing by 0 when the ray point sideways to the cam
    if (EyeDepth < 999 && cosAngle > 0.001)
    {
        // convert the eyedepth to be along our ray direction in worldspace
        float depthAlongRay = EyeDepth / cosAngle;

        // world space point where our ray hits the eyedepth result
        float3 sceneHitWS = _WorldSpaceCameraPos + rayDirectionWS * depthAlongRay;

        // bring it back into object space, since distanceToBoxExit is measured in object space
        float3 sceneHitOS = mul(unity_WorldToObject, float4(sceneHitWS, 1.0)).xyz;

        // Project that point back onto our ray to get a plain distance value
        float rayDepthDistance = dot(sceneHitOS - CameraLocalPos, RayDirectionOS);

        // if the depth result (floors/walls) is closer than the box, that wins and we stop rendering
        ClippedExitDistance = min(ExitDistance, rayDepthDistance);
    }

}
