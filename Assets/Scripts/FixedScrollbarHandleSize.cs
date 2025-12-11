using UnityEngine;
using UnityEngine.UI;

public class FixedScrollbarHandleSize : MonoBehaviour
{
    public Scrollbar scrollbar;
    public float fixedSize = 0.15f;

    void LateUpdate()
    {
        scrollbar.size = fixedSize;
    }
}
