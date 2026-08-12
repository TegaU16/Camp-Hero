using System.Collections;
using UnityEngine;

public class HitStopController : MonoBehaviour, IHitStoppable
{
    private Coroutine freezeRoutine;
    private Animator animator;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
    }

    public void ApplyHitStop(float duration, float animSpeed = 0f)
    {
        if (freezeRoutine != null)
            StopCoroutine(freezeRoutine);

        freezeRoutine = StartCoroutine(FreezeRoutine(duration, animSpeed));
    }

    private IEnumerator FreezeRoutine(float duration, float animSpeed)
    {
        float originalSpeed = animator != null ? animator.speed : 1f;

        if (animator != null)
            animator.speed = animSpeed;

        // Optional: pause movement scripts
        IHitStopListener[] pauseables = GetComponents<IHitStopListener>();
        foreach (IHitStopListener pauseable in pauseables)
            pauseable.OnHitStopStart();

        yield return new WaitForSecondsRealtime(duration);

        if (animator != null)
            animator.speed = originalSpeed;

        foreach (IHitStopListener pauseable in pauseables)
            pauseable.OnHitStopEnd();

        freezeRoutine = null;
    }
}
