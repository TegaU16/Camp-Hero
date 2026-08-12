using UnityEngine;
using Tiny;

public class WeaponSlash : MonoBehaviour
{
    [SerializeField] private Trail trail;

    private void Awake()
    {
        trail.enabled = false;
    }

    public void StartSlash()
    {
        trail.enabled = true;
    }

    public void EndSlash()
    {
        trail.enabled = false;
    }
}
