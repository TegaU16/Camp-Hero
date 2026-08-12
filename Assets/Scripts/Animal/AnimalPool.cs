using UnityEngine;

namespace Game.AI.Animals
{
    public class AnimalPool : ObjectPool<Animal>
    {
        [SerializeField] private Animal animalPrefab;

        protected override void Awake()
        {
            base.Awake();
            Instance = this;
        }

        protected override void OnReturnObject(Animal animal)
        {
            animal.CancelInvoke();
            animal.StopAllCoroutines();

            if (animal.TryGetComponent(out Animator animator))
            {
                animator.enabled = true;
                animator.SetFloat("Speed", 0f);
            }

            if (animal.TryGetComponent(out SimpleRagdollController ragdollController))
                ragdollController.DisableRagdoll();

            animal.IsActiveAI = false;
            base.OnReturnObject(animal);
        }

        protected override Animal CreatePooledObject()
        {
            Animal animal = Instantiate(animalPrefab);
            animal.gameObject.SetActive(false);
            animal.transform.SetParent(transform);
            return animal;
        }
    }
}
