using UnityEngine;
using UnityEditor;

public class RagdollAutoBuilder : EditorWindow
{
    private GameObject rootObject;
    private float mass = 1f;
    private bool useCapsule = true;

    [MenuItem("Tools/Auto Ragdoll Builder")]
    static void Init()
    {
        GetWindow<RagdollAutoBuilder>("Auto Ragdoll Builder");
    }

    void OnGUI()
    {
        rootObject = (GameObject)EditorGUILayout.ObjectField("Root Object", rootObject, typeof(GameObject), true);
        useCapsule = EditorGUILayout.Toggle("Use Capsule Collider", useCapsule);
        mass = EditorGUILayout.FloatField("Bone Mass", mass);

        if (GUILayout.Button("Build Ragdoll"))
        {
            if (rootObject == null)
            {
                Debug.LogWarning("Assign a root object first.");
                return;
            }

            CreateRagdoll(rootObject);
        }
    }

    void CreateRagdoll(GameObject root)
    {
        MeshRenderer[] meshRenderers = root.GetComponentsInChildren<MeshRenderer>(true);
        if (meshRenderers.Length == 0)
            meshRenderers = root.GetComponentsInChildren<MeshRenderer>(true);

        int count = 0;

        foreach (MeshRenderer meshRenderer in meshRenderers)
        {
            Transform meshTransform = meshRenderer.transform;
            Transform bone = meshTransform.parent;

            if (bone == null || bone == root.transform) continue;

            if (bone.GetComponent<Rigidbody>() == null)
            {
                Rigidbody rb = bone.gameObject.AddComponent<Rigidbody>();
                rb.mass = mass;
            }

            if (useCapsule)
            {
                if (!bone.TryGetComponent(out CapsuleCollider col))
                    col = bone.gameObject.AddComponent<CapsuleCollider>();

                if (!meshRenderer.TryGetComponent(out MeshFilter meshFilter)) continue;

                Bounds localBounds = meshFilter.sharedMesh.bounds;
                Vector3 worldCenter = meshRenderer.transform.TransformPoint(localBounds.center);
                Vector3 worldSize = Vector3.Scale(localBounds.size, meshRenderer.transform.lossyScale);
                worldSize = new Vector3(Mathf.Abs(worldSize.x), Mathf.Abs(worldSize.y), Mathf.Abs(worldSize.z));

                col.center = bone.InverseTransformPoint(worldCenter);
                col.height = Mathf.Max(worldSize.y, 0.01f);
                col.radius = Mathf.Max(worldSize.x, worldSize.z) * 0.5f;
                col.direction = 1; // Y axis
            }
            else
            {
                if (!bone.TryGetComponent(out BoxCollider col))
                    col = bone.gameObject.AddComponent<BoxCollider>();

                if (!meshRenderer.TryGetComponent(out MeshFilter meshFilter)) continue;

                Bounds localBounds = meshFilter.sharedMesh.bounds;
                Vector3 worldCenter = meshRenderer.transform.TransformPoint(localBounds.center);
                Vector3 worldSize = Vector3.Scale(localBounds.size, meshRenderer.transform.lossyScale);

                col.center = bone.InverseTransformPoint(worldCenter);

                Vector3 localSize = bone.InverseTransformVector(worldSize);
                col.size = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));
            }

            count++;
        }

        // Add CharacterJoints
        foreach (Rigidbody rb in root.GetComponentsInChildren<Rigidbody>())
        {
            if (rb.transform == root.transform) continue;

            rb.isKinematic = true;
            Transform parent = rb.transform.parent;
            Rigidbody parentRb = null;

            // Walk up the parent chain until a Rigidbody is found
            while (parent != null && parentRb == null)
            {
                parentRb = parent.GetComponent<Rigidbody>();
                parent = parent.parent;
            }

            if (parentRb)
            {
                parentRb.isKinematic = true;
                CharacterJoint joint = rb.GetComponent<CharacterJoint>();
                if (!joint)
                {
                    joint = rb.gameObject.AddComponent<CharacterJoint>();
                    joint.connectedBody = parentRb;

                    // Optional: Add some default limits
                    SoftJointLimit lowTwistLimit = new() { limit = -20f };
                    SoftJointLimit highTwistLimit = new() { limit = 20f };
                    SoftJointLimit swingLimit = new() { limit = 30f };

                    joint.lowTwistLimit = lowTwistLimit;
                    joint.highTwistLimit = highTwistLimit;
                    joint.swing1Limit = swingLimit;
                    joint.swing2Limit = swingLimit;
                }
            }
        }

        Debug.Log($"✅ Auto ragdoll created with {count} bones.");
    }
}
