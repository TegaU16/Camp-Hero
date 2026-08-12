using Game;
using UnityEngine;

public class HealthUIPool : ObjectPool<HealthUI>
{
    [SerializeField] private GameObject healthUIPrefab;

    protected override void Awake()
    {
        base.Awake();
        Instance = this;
    }

    protected override HealthUI CreatePooledObject()
    {
        if (!healthUIPrefab.TryGetComponent(out HealthUI _)) return null;

        GameObject healthUIInstance = Instantiate(healthUIPrefab);
        HealthUI healthUI = healthUIInstance.GetComponent<HealthUI>();
        healthUIInstance.SetActive(false);

        return healthUI;
    }
}
