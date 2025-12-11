using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AIMovement : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private NavMeshAgent navAgent;
    [SerializeField] private Animator animator;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 1.0f;
    [SerializeField] private float stoppingDistance = 0.5f;
    [SerializeField] private float acceleration = 8f;
    [SerializeField] private float deceleration = 8f;

    [Header("Target Areas")]
    [SerializeField] private Transform[] targetAreas;
    [SerializeField] private bool moveInSequence = true;
    [SerializeField] private bool loopTargets = true;

    [Header("Idle Settings")]
    [SerializeField] private float minIdleDuration = 2f;
    [SerializeField] private float maxIdleDuration = 5f;

    [Header("Animation")]
    [SerializeField] private string animationParameter = "AIMovement";

    private int currentTargetIndex = 0;
    private float currentBlend = 0f;
    private float blendVelocity = 0f;

    void Start()
    {
        if (navAgent == null)
            navAgent = GetComponent<NavMeshAgent>();

        if (animator == null)
            animator = GetComponent<Animator>();

        navAgent.speed = moveSpeed;
        navAgent.stoppingDistance = stoppingDistance;
        navAgent.acceleration = acceleration;
        navAgent.angularSpeed = 120f;
        navAgent.autoBraking = true;

        if (targetAreas.Length > 0)
        {
            StartCoroutine(MovementRoutine());
        }
        else
        {
            Debug.LogWarning("No target areas assigned to " + gameObject.name);
        }
    }

    void Update()
    {
        UpdateAnimation();
    }

    private IEnumerator MovementRoutine()
    {
        while (true)
        {
            if (targetAreas.Length == 0)
                yield break;

            Transform nextTarget = GetNextTarget();

            if (nextTarget != null)
            {
                yield return StartCoroutine(MoveToTarget(nextTarget));
                yield return StartCoroutine(IdleAtTarget());
            }
            else
            {
                break;
            }
        }
    }

    private IEnumerator MoveToTarget(Transform target)
    {
        navAgent.SetDestination(target.position);

        while (navAgent.pathPending || navAgent.remainingDistance > navAgent.stoppingDistance)
        {
            yield return null;
        }

        navAgent.isStopped = true;
        navAgent.velocity = Vector3.zero;
        animator.SetFloat(animationParameter, 0f); // Force immediate idle
        yield return null;
        navAgent.isStopped = false;

        yield return new WaitForSeconds(0.1f);
    }

    private IEnumerator IdleAtTarget()
    {
        float idleDuration = Random.Range(minIdleDuration, maxIdleDuration);
        yield return new WaitForSeconds(idleDuration);
    }

    private Transform GetNextTarget()
    {
        if (targetAreas.Length == 0)
            return null;

        Transform target = null;

        if (moveInSequence)
        {
            target = targetAreas[currentTargetIndex];
            currentTargetIndex++;

            if (currentTargetIndex >= targetAreas.Length)
            {
                if (loopTargets)
                    currentTargetIndex = 0;
                else
                    return null;
            }
        }
        else
        {
            int randomIndex = Random.Range(0, targetAreas.Length);
            target = targetAreas[randomIndex];
        }

        return target;
    }

    private void UpdateAnimation()
    {
        float targetBlend = Mathf.Clamp01(navAgent.velocity.magnitude / moveSpeed);
        currentBlend = Mathf.SmoothDamp(currentBlend, targetBlend, ref blendVelocity, 0.05f);
        animator.SetFloat(animationParameter, currentBlend);
    }

    // Public methods for external control
    public void SetMoveSpeed(float speed)
    {
        moveSpeed = speed;
        navAgent.speed = speed;
    }

    public void SetIdleDuration(float min, float max)
    {
        minIdleDuration = min;
        maxIdleDuration = max;
    }

    public void AddTargetArea(Transform target)
    {
        List<Transform> targets = new List<Transform>(targetAreas);
        targets.Add(target);
        targetAreas = targets.ToArray();
    }

    public void ClearTargets()
    {
        targetAreas = new Transform[0];
        StopAllCoroutines();
    }

    public void RestartMovement()
    {
        StopAllCoroutines();
        currentTargetIndex = 0;
        StartCoroutine(MovementRoutine());
    }

    public void PauseMovement()
    {
        navAgent.isStopped = true;
        StopAllCoroutines();
    }

    public void ResumeMovement()
    {
        navAgent.isStopped = false;
        StartCoroutine(MovementRoutine());
    }
}