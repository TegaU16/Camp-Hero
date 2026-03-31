using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SlashEffect : MonoBehaviour
{
    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform endPoint;
    [SerializeField] private float lifeTime = 0.15f;

    private Mesh mesh;
    private readonly List<Vector3> vertices = new();
    private readonly List<int> triangles = new();

    private float timer;
    private bool active;

    void Awake()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
    }

    void LateUpdate()
    {
        transform.localRotation = Quaternion.identity;
    }

    public void StartSlash()
    {
        vertices.Clear();
        triangles.Clear();
        mesh.Clear();
        timer = 0f;
        active = true;
    }

    public void StopSlash() => active = false;

    void Update()
    {
        if (!active) return;

        timer += Time.deltaTime;

        vertices.Add(transform.InverseTransformPoint(startPoint.position));
        vertices.Add(transform.InverseTransformPoint(endPoint.position));

        int count = vertices.Count;

        if (count >= 4)
        {
            int i = count - 4;

            triangles.Add(i);
            triangles.Add(i + 1);
            triangles.Add(i + 2);

            triangles.Add(i + 2);
            triangles.Add(i + 1);
            triangles.Add(i + 3);
        }

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);

        if (timer >= lifeTime)
            active = false;
    }
}
