using UnityEngine;
using UnityEngine.UI;

public class UIImageAnimator : MonoBehaviour
{
    public Image targetImage;
    public Sprite[] frames;
    public float frameRate = 12f;

    private int currentFrame;
    private float timer;

    public bool startOnAwake;

    private Sprite originalSprite;

    private void Awake()
    {
        originalSprite = targetImage.sprite;

        if (startOnAwake)
            Play();
        else
            Stop();
    }

    void Update()
    {
        if (frames == null || frames.Length == 0) return;
        if (targetImage == null) return;

        timer += Time.deltaTime;
        if (timer < 1f / frameRate) return;

        currentFrame = (currentFrame + 1) % frames.Length;
        targetImage.sprite = frames[currentFrame];
        timer = 0f;
    }

    public void Play()
    {
        enabled = true;
        currentFrame = 0;
        timer = 0f;
        if (targetImage != null && frames.Length > 0)
            targetImage.sprite = frames[0];
    }

    public void Stop()
    {
        targetImage.sprite = originalSprite;
        enabled = false;
    }
}
