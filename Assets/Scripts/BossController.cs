using UnityEngine;
using System;

public class BossController : MonoBehaviour
{
    public event Action OnBossDefeated;

    public void Die()
    {
        // Boss death logic here...
        OnBossDefeated?.Invoke();
        Destroy(gameObject);
    }
}
