using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(AudioSource))]
public class ExplosionDecal : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private Sprite[] animationFrames;
    [SerializeField] private float framesPerSecond = 60f;
    [SerializeField] private bool loopAnimation = false;

    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;
    private int currentFrame;
    private float timer;
    private bool animationFinished = false;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        // Play the sound assigned to the AudioSource
        if (audioSource.clip != null)
        {
            audioSource.Play();
            // Destroy the object when the sound finishes
            Destroy(gameObject, audioSource.clip.length);
        }
        else
        {
            Debug.LogWarning($"[ExplosionDecal] No AudioSource clip found on {gameObject.name}. It won't be destroyed automatically based on sound.");
        }

        // Set initial frame
        if (animationFrames != null && animationFrames.Length > 0)
        {
            spriteRenderer.sprite = animationFrames[0];
        }
    }

    void Update()
    {
        // Billboarding: Always face the camera
        if (Camera.main != null)
        {
            transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
        }

        // Handle Sprite Animation
        HandleAnimation();
    }

    private void HandleAnimation()
    {
        if (animationFinished || animationFrames == null || animationFrames.Length == 0) return;

        timer += Time.deltaTime;
        if (timer >= 1f / framesPerSecond)
        {
            timer = 0;
            currentFrame++;

            if (currentFrame < animationFrames.Length)
            {
                spriteRenderer.sprite = animationFrames[currentFrame];
            }
            else
            {
                if (loopAnimation)
                {
                    currentFrame = 0;
                    spriteRenderer.sprite = animationFrames[currentFrame];
                }
                else
                {
                    animationFinished = true;
                    spriteRenderer.enabled = false; // Hide after finishing animation
                }
            }
        }
    }
}
