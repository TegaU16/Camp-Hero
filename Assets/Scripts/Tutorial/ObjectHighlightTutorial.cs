using UnityEngine;

namespace Game.Tutorial
{
    public class ObjectHighlightTutorial : MonoBehaviour
    {
        private Transform target;
        private float yOffset;
        private bool followTarget;

        public void Initialize(ObjectHighlightTutorialData data)
        {
            target = data.target.transform;
            yOffset = data.yOffset;
            followTarget = data.followTarget;

            UpdatePosition();
        }

        private void LateUpdate()
        {
            if (target == null || !target.gameObject.activeInHierarchy)
            {
                Destroy(gameObject);
                return;
            }

            if (followTarget)
                UpdatePosition();
        }

        private void UpdatePosition()
        {
            transform.SetPositionAndRotation(target.position + Vector3.up * yOffset, Quaternion.LookRotation(
                transform.position - Camera.main.transform.position
            ));
        }
    }

    [System.Serializable]
    public class ObjectHighlightTutorialData
    {
        public GameObject target;
        public float yOffset = 0.5f;
        public bool followTarget = true;
    }
}
