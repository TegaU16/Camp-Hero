using System.Collections.Generic;
using UnityEngine;

public class DamagePopupPool : MonoBehaviour
{
    public static DamagePopupPool Instance;

    [SerializeField] private GameObject popupPrefab;
    [SerializeField] private int initialPoolSize = 20;
    [SerializeField] private Canvas worldCanvas;

    private readonly Queue<GameObject> popupPool = new();

    private void Awake()
    {
        Instance = this;
        FillPool();
    }

    private void FillPool()
    {
        for (int i = 0; i < initialPoolSize; i++)
        {
            GameObject popup = Instantiate(popupPrefab, worldCanvas.transform);
            popup.SetActive(false);
            popupPool.Enqueue(popup);
        }
    }

    public GameObject GetPopup()
    {
        if (popupPool.Count == 0)
            FillPool();  // Optional: expand if empty

        GameObject popup = popupPool.Dequeue();
        popup.SetActive(true);
        return popup;
    }

    public void ReturnPopup(GameObject popup)
    {
        popup.SetActive(false);
        popupPool.Enqueue(popup);
    }
}
