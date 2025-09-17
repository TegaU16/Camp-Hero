using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;
using System.Collections;

public class Companion : MonoBehaviour, IInteractable, ISimulatable
{
    public List<CompanionTaskSO> tasks;
    public Item[] inventoryItems;

    private NavMeshAgent agent;
    private Coroutine currentTaskCoroutine;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Start()
    {
        // For testing, execute the first task right when the game starts
        ExecuteCurrentTask();
    }

    public void StartHarvesting(BreakableObject.ObjectType targetType, int targetAmount)
    {
        if (currentTaskCoroutine != null)
            StopCoroutine(currentTaskCoroutine);

        currentTaskCoroutine = StartCoroutine(HarvestRoutine(targetType, targetAmount));
    }

    public void ExecuteCurrentTask()
    {
        if (tasks.Count > 0)
        {
            CompanionTaskSO task = tasks[1];
            task.Execute(this);
        }
        else
        {
            Debug.Log("No tasks to execute");
        }
    }

    private IEnumerator HarvestRoutine(BreakableObject.ObjectType targetType, int targetAmount)
    {
        int harvested = 0;

        while (harvested < targetAmount)
        {
            BreakableObject[] targets = FindObjectsByType<BreakableObject>(FindObjectsSortMode.None);
            BreakableObject closest = null;
            float closestDist = float.MaxValue;

            foreach (BreakableObject obj in targets)
            {
                if (obj.objectType != targetType) continue;

                float dist = Vector3.Distance(transform.position, obj.transform.position);
                if (dist < closestDist)
                {
                    closest = obj;
                    closestDist = dist;
                }
            }

            if (closest == null)
            {
                yield break;
            }

            agent.SetDestination(closest.transform.position);

            // Wait until the agent reaches the object, or the object is destroyed
            while (closest != null && Vector3.Distance(transform.position, closest.transform.position) > 2f)
            {
                yield return null;
            }

            // Revalidate before attacking
            if (closest == null)
            {
                yield return null;
                continue; // Pick a new target
            }

            // Harvesting loop
            while (closest != null)
            {
                closest.TakeDamage(5, false, Vector3.zero, Vector3.zero);
                yield return new WaitForSeconds(1f);
            }

            harvested++;
            yield return null;
        }
    }

    public void Interact()
    {
        /*if (InventoryManager.Instance.companionMenuUI != null)
        {
            InventoryManager.Instance.companionMenuUI.SetActive(true);
            InventoryManager.Instance.SetVariableExtension(InventoryManager.Instance.companionMenuUI);
            CompanionUIManager.Instance.LoadCompanion(this);
        }
        else
        {
            Debug.LogWarning("Companion menu UI is not assigned.");
        }*/
    }

    public string GetInteractText()
    {
        return "Talk to Companion";
    }

    public Transform GetTransform()
    {
        return transform;
    }

    public void StopTask()
    {
        if (currentTaskCoroutine != null)
        {
            StopCoroutine(currentTaskCoroutine);
            currentTaskCoroutine = null;
        }

        agent.ResetPath();
    }

    public void OnSimulateStart()
    {
        enabled = true;
    }

    public void OnSimulateStop()
    {
        enabled = false;
    }
}
