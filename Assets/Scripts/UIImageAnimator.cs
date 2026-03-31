using UnityEngine;
using UnityEngine.UI;

public class UIImageAnimator : MonoBehaviour
{
    [SerializeField] private Image targetImage;
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float frameRate = 12f;
    [SerializeField] private bool startOnAwake;

    private int currentFrame;
    private float frameTimer;
    private float frameDuration;
    private Sprite originalSprite;
    private bool isPlaying;

    private void Awake()
    {
        if (targetImage == null)
        {
            Debug.LogWarning($"UIImageAnimator on {gameObject.name}: Target Image missing.");
            enabled = false;
            return;
        }

        frameDuration = 1f / frameRate;
        originalSprite = targetImage.sprite;
    }

    private void Update()
    {
        if (!isPlaying || frames == null || frames.Length == 0) return;

        frameTimer += Time.deltaTime;

        while (frameTimer >= frameDuration)
        {
            frameTimer -= frameDuration;

            currentFrame++;
            if (currentFrame >= frames.Length)
                currentFrame = 0;

            targetImage.sprite = frames[currentFrame];
        }
    }

    public void Play()
    {
        if (frames == null || frames.Length == 0)
        {
            Debug.LogWarning($"No frames assigned on {gameObject.name}");
            return;
        }

        if (targetImage == null)
        {
            Debug.LogWarning($"Target Image missing on {gameObject.name}");
            return;
        }

        if (frames[0] == null)
        {
            Debug.LogWarning($"First frame is null on {gameObject.name}");
            return;
        }

        currentFrame = 0;
        frameTimer = 0f;
        targetImage.sprite = frames[0];
        isPlaying = true;
    }

    public void Stop()
    {
        isPlaying = false;

        if (targetImage != null && originalSprite != null)
            targetImage.sprite = originalSprite;
    }
}
