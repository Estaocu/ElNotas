using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class AnimationEventRelay : MonoBehaviour
{
    // Drag the sibling's function here in the Inspector
    public UnityEvent onAnimationFinishedEvent;

    // Called by an Animation Event (same GameObject as the Animator)
    // or by the StateMachineBehaviour below
    public void onAnimationFinished()
    {
        onAnimationFinishedEvent?.Invoke();
    }
}
