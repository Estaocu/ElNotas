using System.Collections;
using System.Collections.Generic;
using UnityEditor.UI;
using UnityEngine;

public class SingleNoteSoundwave : MonoBehaviour
{
    public notesEnum myNote;
    [SerializeField] private float lifeTime = 1.5f;

    void Awake()
    {
        myNote = 0;
    }

    public void Expand(notesEnum note)
    {
        myNote = note;
        StartCoroutine(LifeTimeRoutine());
    }

    private IEnumerator LifeTimeRoutine()
    {
        yield return new WaitForSeconds(lifeTime);
        Destroy(gameObject);
    }
}
