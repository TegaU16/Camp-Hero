using UnityEngine;
using UnityEngine.UI;

namespace Game.Terrain.Structures.Trials
{
    public class WaveEntry : MonoBehaviour
    {
        public Image image;
        public Sprite selectedImage, unselectedImage;
        public Transform enemyListContainer;

        public void Select()
        {
            image.sprite = selectedImage;
        }

        public void Deselect()
        {
            image.sprite = unselectedImage;
        }
    }
}
