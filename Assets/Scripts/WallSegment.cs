using UnityEngine;

public class WallSegment : MonoBehaviour
{
    public GameObject leftPole;
    public GameObject rightPole;
    public GameObject connectorToRight;

    public void SetPoleVisibility(bool left, bool right)
    {
        leftPole.SetActive(left);
        rightPole.SetActive(right);
    }

    public void SetConnectorToRight(bool active)
    {
        connectorToRight.SetActive(active);
    }
}
