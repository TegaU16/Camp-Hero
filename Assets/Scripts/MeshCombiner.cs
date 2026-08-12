#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class MeshCombiner : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool includeInactiveObjects = false;
    [SerializeField] private bool mergeSubMeshes = true;
    [SerializeField] private bool disableOriginalRenderers = false;
    [SerializeField] private bool addMeshCollider = false;
    [SerializeField] private string assetName = "CombinedMesh";

    [ContextMenu("Combine Meshes Into Asset")]
    public void CombineMeshes()
    {
        MeshFilter[] filters = GetComponentsInChildren<MeshFilter>(includeInactiveObjects);

        List<CombineInstance> combineInstances = new();
        List<Material> materials = new();

        foreach (MeshFilter filter in filters)
        {
            if (filter.transform == transform) continue;
            if (filter.sharedMesh == null) continue;
            if (!filter.TryGetComponent(out MeshRenderer renderer)) continue;

            Mesh mesh = filter.sharedMesh;

            for (int subMeshIndex = 0; subMeshIndex < mesh.subMeshCount; subMeshIndex++)
            {
                CombineInstance combine = new()
                {
                    mesh = mesh,
                    subMeshIndex = subMeshIndex,
                    transform = transform.worldToLocalMatrix * filter.transform.localToWorldMatrix
                };

                combineInstances.Add(combine);

                if (subMeshIndex < renderer.sharedMaterials.Length)
                    materials.Add(renderer.sharedMaterials[subMeshIndex]);
            }

            if (disableOriginalRenderers)
                renderer.enabled = false;
        }

        if (combineInstances.Count == 0)
        {
            Debug.LogWarning("No valid meshes found to combine.");
            return;
        }

        Mesh combinedMesh = new()
        {
            name = assetName
        };

        combinedMesh.CombineMeshes(
            combineInstances.ToArray(),
            mergeSubMeshes,
            useMatrices: true
        );

        GameObject combinedObject = new(assetName);

        combinedObject.transform.SetPositionAndRotation(
            transform.position,
            transform.rotation
        );

        combinedObject.transform.localScale = transform.localScale;

        MeshFilter combinedFilter = combinedObject.AddComponent<MeshFilter>();
        MeshRenderer combinedRenderer = combinedObject.AddComponent<MeshRenderer>();

        combinedFilter.sharedMesh = combinedMesh;

        if (mergeSubMeshes)
        {
            if (materials.Count > 0)
                combinedRenderer.sharedMaterial = materials[0];
        }
        else
        {
            combinedRenderer.sharedMaterials = materials.ToArray();
        }

        if (addMeshCollider)
        {
            MeshCollider collider = combinedObject.AddComponent<MeshCollider>();
            collider.sharedMesh = combinedMesh;
        }

        string path = $"Assets/{assetName}.asset";

        AssetDatabase.CreateAsset(combinedMesh, path);
        AssetDatabase.SaveAssets();

        Debug.Log($"Combined mesh saved at: {path}");
    }
}

#endif
