using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class ShadowProjector : MonoBehaviour
{
    [Tooltip("Direction to project shadows (e.g. sun direction)")]
    public Vector3 lightDirection = new(1, -1, 1);

    [Tooltip("Material for the shadow (usually transparent black)")]
    public Material shadowMaterial;

    [Tooltip("Layers that can receive shadows")]
    public LayerMask shadowReceiverLayers = ~0;

    private GameObject shadowObject;
    private Mesh shadowMesh;

    private Vector3 lastLightDirection;
    private Vector3 lastPosition;
    private Quaternion lastRotation;

    void Start()
    {
        CreateShadowObject();
        UpdateShadowMesh();
        lastLightDirection = lightDirection;
        lastPosition = transform.position;
        lastRotation = transform.rotation;
    }

    void Update()
    {
        if (transform.position != lastPosition || transform.rotation != lastRotation || lightDirection != lastLightDirection)
        {
            UpdateShadowMesh();
            lastPosition = transform.position;
            lastRotation = transform.rotation;
            lastLightDirection = lightDirection;
        }
    }

    void CreateShadowObject()
    {
        // Create child object to hold shadow mesh
        shadowObject = new GameObject("ShadowMesh");
        shadowObject.transform.parent = transform;
        shadowObject.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        shadowObject.transform.localScale = Vector3.one;

        var meshFilter = shadowObject.AddComponent<MeshFilter>();
        shadowMesh = new Mesh();
        meshFilter.mesh = shadowMesh;

        var meshRenderer = shadowObject.AddComponent<MeshRenderer>();
        meshRenderer.material = shadowMaterial;

        // Optionally disable shadow casting/receiving for this object
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
    }

    void UpdateShadowMesh()
    {
        var originalMesh = GetComponent<MeshFilter>().sharedMesh;
        if (originalMesh == null || shadowMaterial == null)
        {
            Debug.LogWarning("No original mesh or shadow material assigned.");
            return;
        }

        Vector3[] originalVertices = originalMesh.vertices;
        int[] triangles = originalMesh.triangles;
        Vector3[] projectedVertices = new Vector3[originalVertices.Length];

        Vector3 lightDir = lightDirection.normalized;

        for (int i = 0; i < originalVertices.Length; i++)
        {
            Vector3 worldPos = transform.TransformPoint(originalVertices[i]);

            // Cast a ray along lightDir starting a bit opposite lightDir (backwards)
            Vector3 rayStart = worldPos + (-lightDir) * 10f;
            Vector3 rayDir = lightDir;

            Vector3 projectedPos = Vector3.zero;
            bool foundValidHit = false;

            RaycastHit[] hits = Physics.RaycastAll(rayStart, rayDir, 20f, shadowReceiverLayers);

            float closestDistance = Mathf.Infinity;

            foreach (var hit in hits)
            {
                // Ignore hits on self
                if (hit.collider.gameObject == gameObject) continue;

                if (hit.distance < closestDistance)
                {
                    closestDistance = hit.distance;
                    projectedPos = hit.point;
                    foundValidHit = true;
                }
            }

            if (foundValidHit)
            {
                projectedPos.y += 0.01f; // small offset to avoid z-fighting
                projectedVertices[i] = shadowObject.transform.InverseTransformPoint(projectedPos);
            }
            else
            {
                // Fallback: project onto y=0 plane
                float t = (worldPos.y) / -lightDir.y;
                projectedPos = worldPos + lightDir * t;
                projectedPos.y += 0.01f;
                projectedVertices[i] = shadowObject.transform.InverseTransformPoint(projectedPos);
            }
        }

        shadowMesh.Clear();
        shadowMesh.vertices = projectedVertices;
        shadowMesh.triangles = triangles;
        shadowMesh.RecalculateNormals();
        shadowMesh.RecalculateBounds();
    }
}
