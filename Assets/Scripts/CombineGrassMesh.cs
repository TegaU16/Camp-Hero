using UnityEngine;

public class CombineGrassMesh : MonoBehaviour
{
    void Start()
    {
        MeshFilter[] filters = GetComponentsInChildren<MeshFilter>();

        CombineInstance[] combine = new CombineInstance[filters.Length];

        for (int i = 0; i < filters.Length; i++)
        {
            combine[i].mesh = filters[i].sharedMesh;
            combine[i].transform = filters[i].transform.localToWorldMatrix;
        }

        Mesh mesh = new();
        mesh.CombineMeshes(combine);

        GameObject combined = new("GrassMesh_Combined");
        combined.transform.position = Vector3.zero;

        MeshFilter mf = combined.AddComponent<MeshFilter>();
        mf.mesh = mesh;

        MeshRenderer mr = combined.AddComponent<MeshRenderer>();
        mr.sharedMaterial = filters[0].GetComponent<MeshRenderer>().sharedMaterial;
    }
}
