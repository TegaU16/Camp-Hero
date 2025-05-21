using UnityEngine;

public interface IInteractable
{
    void Interact();
    string GetInteractText(); // Optional: shows a tooltip like "Press E to Talk"
    Transform GetTransform(); // For UI positioning
}
