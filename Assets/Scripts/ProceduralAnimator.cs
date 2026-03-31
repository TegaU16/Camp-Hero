using Game.Terrain;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class ProceduralAnimator : MonoBehaviour
{
    [Header("Biped Bones")]
    public Transform leftLeg;
    public Transform rightLeg;
    public Transform leftArm;
    public Transform rightArm;
    public Transform torso;
    public Transform waist;
    public Transform head;

    [Header("Walk Parameters")]
    public float walkCycleSpeed = 6f;
    public float legAmplitude = 25f;
    public float armAmplitude = 20f;
    public float bodyBob = 0.05f;
    public float torsoTwist = 10f;
    public float waistCounterTwist = 0.8f;
    public float headCounterTwist = 0.7f; // how strongly the head opposes torso rotation

    [Header("Blend Settings")]
    public float startWalkThreshold = 0.1f;
    public float blendSpeed = 5f;

    [Header("Footstep Settings")]
    public float footstepPitchVariance = 0.1f;
    public float stepTriggerThreshold = 0.95f; // when to fire a step (phase-based)

    [Header("Footstep Clips")]
    public AudioClip[] grassClips;
    public AudioClip[] woodClips;
    public AudioClip[] stoneClips;

    private bool leftStepPlayed;

    private float walkCycle;
    private float movementSpeed;
    private float walkWeight = 0f;

    private Quaternion leftLegRestRot;
    private Quaternion rightLegRestRot;
    private Quaternion leftArmRestRot;
    private Quaternion rightArmRestRot;
    private Quaternion torsoRestRot;
    private Quaternion waistRestRot;
    private Quaternion headRestRot;
    private Vector3 torsoRestPos;

    private Vector3 legAxisLocal;
    private Vector3 armAxisLocal;
    private Vector3 torsoTwistAxisLocal;
    private Vector3 waistTwistAxisLocal;
    private Vector3 headTwistAxisLocal;

    private float proceduralWeight = 1f;
    private bool isAttacking;

    private CharacterController characterController;
    private Animator animator;
    private bool additiveMode;

    private bool overrideLegs;
    private bool overrideArms;
    private bool overrideTorso;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        additiveMode = animator && animator.enabled;

        if (rightLeg)
            rightLegRestRot = rightLeg.localRotation;

        if (rightArm)
            rightArmRestRot = rightArm.localRotation;

        if (leftLeg)
        {
            leftLegRestRot = leftLeg.localRotation;
            legAxisLocal = leftLeg.InverseTransformDirection(transform.right);
        }

        if (leftArm)
        {
            leftArmRestRot = leftArm.localRotation;
            armAxisLocal = leftArm.InverseTransformDirection(transform.right);
        }

        if (torso)
        {
            torsoRestPos = torso.localPosition;
            torsoRestRot = torso.localRotation;
            torsoTwistAxisLocal = torso.InverseTransformDirection(transform.up);
        }

        if (waist)
        {
            waistRestRot = waist.localRotation;
            waistTwistAxisLocal = waist.InverseTransformDirection(transform.up);
        }

        if (head)
        {
            headRestRot = head.localRotation;
            headTwistAxisLocal = head.InverseTransformDirection(transform.up);
        }
    }

    void LateUpdate()
    {
        additiveMode = animator && animator.enabled;
        Animate(Time.deltaTime);
    }

    public void SetMovementSpeed(float speed) => movementSpeed = speed;

    public void SetAttacking(bool attacking) => isAttacking = attacking;

    private void Animate(float deltaTime)
    {
        if (!leftLeg || !rightLeg) return;

        bool legAdditive = !(isAttacking && overrideLegs) && additiveMode;
        bool armAdditive = !(isAttacking && overrideArms) && additiveMode;
        bool torsoAdditive = !(isAttacking && overrideTorso) && additiveMode;

        // Fade procedural motion during attack
        float targetWeight = isAttacking ? 0.3f : 1f;
        proceduralWeight = Mathf.MoveTowards(proceduralWeight, targetWeight, deltaTime * 5f);

        // Blend walking vs idle
        float targetWalkWeight = movementSpeed > startWalkThreshold ? 1f : 0f;
        walkWeight = Mathf.MoveTowards(walkWeight, targetWalkWeight, deltaTime * blendSpeed);

        float finalWeight = Mathf.Clamp01(walkWeight * proceduralWeight);
        if (finalWeight < 0.05f) return;

        // Animate cycle
        walkCycle += deltaTime * walkCycleSpeed * movementSpeed;

        float sinCycle = Mathf.Sin(walkCycle);
        float leftPhase = sinCycle;
        float rightPhase = -sinCycle; // since it's just sin(π + θ) = -sin(θ)
        float torsoPhase = Mathf.Sin(walkCycle * 2f);

        // === FOOTSTEP TRIGGER ===
        if (movementSpeed > startWalkThreshold && proceduralWeight > 0.2f)
        {
            // Left foot down (sin wave crosses downward through -threshold)
            if (leftPhase > stepTriggerThreshold && !leftStepPlayed)
            {
                PlayFootstep();
                leftStepPlayed = true;
            }
            else if (leftPhase < 0f)
            {
                leftStepPlayed = false;
            }
        }

        // === APPLY PROCEDURAL MOTION ===

        // Legs
        if (leftLeg)
        {
            Quaternion leftLegRot = Quaternion.AngleAxis(leftPhase * legAmplitude * finalWeight, legAxisLocal);
            leftLeg.localRotation = legAdditive ? leftLeg.localRotation * leftLegRot : leftLegRestRot * leftLegRot;
        }
        if (rightLeg)
        {
            Quaternion rightLegRot = Quaternion.AngleAxis(rightPhase * legAmplitude * finalWeight, legAxisLocal);
            rightLeg.localRotation = legAdditive ? rightLeg.localRotation * rightLegRot : rightLegRestRot * rightLegRot;
        }

        // Arms
        if (leftArm)
        {
            Quaternion leftArmRot = Quaternion.AngleAxis(rightPhase * armAmplitude * finalWeight, armAxisLocal);
            leftArm.localRotation = armAdditive ? leftArm.localRotation * leftArmRot : leftArmRestRot * leftArmRot;
        }
        if (rightArm)
        {
            Quaternion rightArmRot = Quaternion.AngleAxis(leftPhase * armAmplitude * finalWeight, armAxisLocal);
            rightArm.localRotation = armAdditive ? rightArm.localRotation * rightArmRot : rightArmRestRot * rightArmRot;
        }

        // Torso / Waist / Head
        if (torso)
        {
            // Torso bob
            float offset = torsoPhase * bodyBob * finalWeight;
            offset = Mathf.Max(0f, offset);

            Vector3 torsoPos = torsoRestPos;
            torsoPos.z -= offset;
            torso.localPosition = torsoPos;

            float twist = Mathf.Sin(walkCycle) * torsoTwist * finalWeight;
            torso.localRotation = torsoAdditive ? torso.localRotation * Quaternion.AngleAxis(twist, torsoTwistAxisLocal)
                                                : torsoRestRot * Quaternion.AngleAxis(twist, torsoTwistAxisLocal);

            if (waist)
            {
                float waistTwist = -twist * waistCounterTwist;
                waist.localRotation = torsoAdditive ? waist.localRotation * Quaternion.AngleAxis(waistTwist, waistTwistAxisLocal)
                                                    : waistRestRot * Quaternion.AngleAxis(waistTwist, waistTwistAxisLocal);
            }

            if (head)
            {
                float headTwist = -twist * headCounterTwist;
                head.localRotation = torsoAdditive ? head.localRotation * Quaternion.AngleAxis(headTwist, headTwistAxisLocal)
                                                   : headRestRot * Quaternion.AngleAxis(headTwist, headTwistAxisLocal);
            }
        }
    }

    private void PlayFootstep()
    {
        if (!characterController.isGrounded) return;
        if (!Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out RaycastHit hit, 1.2f)) return;

        SurfaceType surface = hit.collider.GetComponent<SurfaceType>();
        AudioClip[] clips = null;

        if (surface != null)
        {
            switch (surface.surfaceType)
            {
                case SurfaceType.Type.Grass: clips = grassClips; break;
                case SurfaceType.Type.Wood: clips = woodClips; break;
                case SurfaceType.Type.Stone: clips = stoneClips; break;
            }
        }

        if (clips == null || clips.Length == 0) return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        float pitch = 1f + Random.Range(-footstepPitchVariance, footstepPitchVariance);

        AudioManager.Instance.PlaySFX(clip, pitch: pitch, position: transform.position);
    }

    public void SetProceduralOverrides(bool legs, bool arms, bool torso)
    {
        overrideLegs = legs;
        overrideArms = arms;
        overrideTorso = torso;
    }
}
