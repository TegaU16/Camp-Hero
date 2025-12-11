using UnityEngine;

[RequireComponent(typeof(Animator))]
public class AutoDestroyAfterAnimation : MonoBehaviour
{
    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (!animator) return;

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);

        // When animation is finished
        if (state.normalizedTime >= 1f && !animator.IsInTransition(0))
            Destroy(gameObject);
    }
}
