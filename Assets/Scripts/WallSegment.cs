using UnityEngine;

public class WallSegment : MonoBehaviour
{
    [SerializeField] private GameObject leftPole;
    [SerializeField] private GameObject rightPole;
    [SerializeField] private GameObject connectorToRight;

    public void SetPoleVisibility(bool left, bool right)
    {
        leftPole.SetActive(left);
        rightPole.SetActive(right);
    }

    public void SetConnectorToRight(bool active) => connectorToRight.SetActive(active);
}
