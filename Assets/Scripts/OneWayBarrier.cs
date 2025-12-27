using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Allows passage from the "inside" (inwardNormal side) -> "outside" through the barrier,
/// but blocks from outside -> inside. Works best for Rigidbody-based actors with Colliders.
/// For CharacterControllers, see notes below.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class OneWayBarrier : MonoBehaviour
{
    [Tooltip("Normal pointing TOWARD the arena center (inward).")]
    public Vector3 inwardNormal = Vector3.left;

    [Tooltip("If true: inside->outside allowed; outside->inside blocked.")]
    public bool allowInsideToExitOnly = true;

    [HideInInspector] public BoxCollider solidCollider;

    // Thin trigger just in front of the wall on both sides to sense approach
    private BoxCollider senseTriggerInside;
    private BoxCollider senseTriggerOutside;

    // track per-collider temporary ignores
    private readonly HashSet<Collider> temporarilyIgnored = new();

    void Awake()
    {
        if (!solidCollider)
            solidCollider = GetComponent<BoxCollider>();

        // Build two thin trigger strips hugging the wall on both sides
        float t = 0.05f; // trigger thickness
        Vector3 size = solidCollider.size;
        Vector3 center = solidCollider.center;

        // Inside trigger (inside is the side the inwardNormal points from)
        senseTriggerInside = gameObject.AddComponent<BoxCollider>();
        senseTriggerInside.isTrigger = true;
        senseTriggerInside.size = size + new Vector3(0.02f, 0.02f, 0.02f);
        senseTriggerInside.center = center + inwardNormal.normalized * (solidCollider.size.magnitude * 0.5f + t);

        // Outside trigger (opposite side)
        senseTriggerOutside = gameObject.AddComponent<BoxCollider>();
        senseTriggerOutside.isTrigger = true;
        senseTriggerOutside.size = size + new Vector3(0.02f, 0.02f, 0.02f);
        senseTriggerOutside.center = center - inwardNormal.normalized * (solidCollider.size.magnitude * 0.5f + t);
    }

    void OnTriggerEnter(Collider other)
    {
        // Decide which trigger fired
        Vector3 localPoint = transform.InverseTransformPoint(other.bounds.center);
        Vector3 localInsideCenter = senseTriggerInside.center;
        Vector3 localOutsideCenter = senseTriggerOutside.center;

        bool isInsideTrigger = (Vector3.Distance(localPoint, localInsideCenter) < Vector3.Distance(localPoint, localOutsideCenter));

        // If we only allow inside->outside:
        // - When colliding from INSIDE, allow a brief pass-through
        // - When colliding from OUTSIDE, keep solid
        if (allowInsideToExitOnly && isInsideTrigger)
            TryAllowPass(other);
    }

    void OnTriggerExit(Collider other)
    {
        // When they leave triggers, restore collision if we ignored it
        if (temporarilyIgnored.Contains(other))
        {
            Physics.IgnoreCollision(other, solidCollider, false);
            temporarilyIgnored.Remove(other);
        }
    }

    private void TryAllowPass(Collider other)
    {
        CharacterController cc = other.GetComponentInParent<CharacterController>();
        if (cc != null)
        {
            if (!cc.TryGetComponent(out OneWayLayerPass layerPass)) 
                cc.gameObject.AddComponent<OneWayLayerPass>();

            layerPass.AllowPassTemporarily();
            return;
        }

        // Prefer actors with Rigidbody (cleanest)
        Rigidbody rb = other.attachedRigidbody;
        if (rb != null)
        {
            Physics.IgnoreCollision(other, solidCollider, true);
            temporarilyIgnored.Add(other);
            return;
        }

        // Fallback: non-Rigidbody colliders (projectiles or kinematic objects)
        // Still works per-collider pair.
        Physics.IgnoreCollision(other, solidCollider, true);
        temporarilyIgnored.Add(other);
    }
}
