using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Game.Players
{
    public class PlayerDeathUI : MonoBehaviour
    {
        public static PlayerDeathUI Instance;

        public TextMeshProUGUI deathMessageText;
        public TextMeshProUGUI respawnCountdownText;

        [TextArea] public List<string> deathMessages;
        private int lastDeathMessageIndex = -1;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public IEnumerator Show(float deathDuration)
        {
            gameObject.SetActive(true);

            int deathMessageIndex;
            do
            {
                deathMessageIndex = Random.Range(0, deathMessages.Count);
            }
            while (deathMessageIndex == lastDeathMessageIndex);

            deathMessageText.text = $"\"{deathMessages[deathMessageIndex]}\"";
            lastDeathMessageIndex = deathMessageIndex;

            float timer = deathDuration;
            while (timer > 0)
            {
                timer -= Time.deltaTime;
                respawnCountdownText.text = $"Respawning in:\n{Mathf.CeilToInt(timer)}";

                yield return null;
            }

            gameObject.SetActive(false);
        }
    }
}
