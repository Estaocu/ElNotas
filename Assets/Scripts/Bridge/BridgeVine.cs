using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using System.Runtime.CompilerServices;


public class BridgeVine : MonoBehaviour
{

    public bool isEnd = false;
    [ShowIf("isEnd")]
    [SerializeField] private BridgeSpawn assignedSpawner;

    [SerializeField] private BridgeTile[] petals = new BridgeTile[2];

    public BridgeTile[] Petals => petals;

    private Material iMat;
    private int createPositionID;
    private int destroyPositionID;

    [SerializeField] private float destroyDuration = 0.2f;
    [SerializeField] private float growDuration = 0.2f;

    private bool grew = false;
    private bool died = false;

    private RhythmClock clock;

    void Awake()
    {
        Renderer renderer = GetComponent<Renderer>();
        iMat = renderer.material;
        createPositionID = Shader.PropertyToID("_CreatePosition");
        destroyPositionID = Shader.PropertyToID("_DestroyPosition");

        clock = FindFirstObjectByType<RhythmClock>();
    }

    void Start()
    {
        iMat.SetFloat(createPositionID, 0);
        iMat.SetFloat(destroyPositionID, 0);
        died = false;
        grew = false;
    }

    public void Bloom()
    {
        if (grew) return;

        iMat.SetFloat(createPositionID, 0f);
        iMat.SetFloat(destroyPositionID, 0f);
        died = false;
        StartCoroutine(GrowCoroutine(growDuration));
    }

    public void Die()
    {
        if (died) return;
        iMat.SetFloat(destroyPositionID, 0f);
        StartCoroutine(DeathCoroutine(destroyDuration));
    }


    private IEnumerator GrowCoroutine(float duration)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / duration);

            iMat.SetFloat(createPositionID, progress);

            yield return null; // Wait for the next frame
        }

        // Ensure target value is explicitly set at the end
        iMat.SetFloat(createPositionID, 1f);

        grew = true;

        StartDeathCountdown();

        Debug.Log("Vine Grew");
    }

    private void StartDeathCountdown()
    {
        StartCoroutine(DeathTimerCoroutine());
    }

    private IEnumerator DeathTimerCoroutine()
    {
        yield return clock.WaitForSubBeats(10);

        Die();
        
    }

    private IEnumerator DeathCoroutine(float duration)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / duration);

            iMat.SetFloat(destroyPositionID, progress);

            yield return null; // Wait for the next frame
        }

        // Ensure target value is explicitly set at the end
        iMat.SetFloat(createPositionID, 0f);
        iMat.SetFloat(destroyPositionID, 1f);

        
        died = true;
        grew = false;

        Debug.Log("Vine Died");
    }


    private void OnDisable()
    {
        grew = false;
    }

    
}
