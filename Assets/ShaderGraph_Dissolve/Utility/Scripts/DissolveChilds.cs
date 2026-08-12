using System.Collections.Generic;
using UnityEngine;

namespace DissolveExample
{
    public class DissolveChilds : MonoBehaviour
    {
        // Start is called before the first frame update
        readonly List<Material> materials = new();

        void Start()
        {
            Renderer[] renders = GetComponentsInChildren<Renderer>();
            for (int i = 0; i < renders.Length; i++)
                materials.AddRange(renders[i].materials);
        }

        private void Reset()
        {
            Start();
            SetValue(0);
        }

        // Update is called once per frame
        void Update()
        {
            float value = Mathf.PingPong(Time.time * 0.5f, 1f);
            SetValue(value);
        }

        public void SetValue(float value)
        {
            for (int i = 0; i < materials.Count; i++)
                materials[i].SetFloat("_Dissolve", value);
        }
    }
}