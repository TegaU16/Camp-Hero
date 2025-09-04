using System.Collections;
using UnityEngine;

public class OneWayLayerPass : MonoBehaviour
{
    public string passthroughLayerName = "IgnoreTrialBarrier";
    public float revertDelay = 0.3f;
    private int originalLayer;
    private Coroutine revertCo;

    public void AllowPassTemporarily()
    {
        if (revertCo != null) StopCoroutine(revertCo);
        originalLayer = gameObject.layer;
        int ptLayer = LayerMask.NameToLayer(passthroughLayerName);
        if (ptLayer >= 0) gameObject.layer = ptLayer;
        revertCo = StartCoroutine(Revert());
    }

    private IEnumerator Revert()
    {
        yield return new WaitForSeconds(revertDelay);
        gameObject.layer = originalLayer;
        revertCo = null;
    }
}
