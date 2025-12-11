using UnityEngine;

namespace Game.Terrain
{
    public enum ObjectType { Tree, Rock, None }

    public class ObjectCategory : MonoBehaviour
    {
        public ObjectType objectType = ObjectType.None;
    }
}
