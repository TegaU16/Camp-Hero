using UnityEngine;

public interface IInteractable
{
    Sprite ObjectIcon { get; }

    string InteractKeyText => "<color=#27ef60>\"E\"</color> to Interact";

    void Interact();
    string GetInteractText(); // Optional: shows a tooltip like "Press E to Talk"
    Transform GetTransform(); // For UI positioning
}
