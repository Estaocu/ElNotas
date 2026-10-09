using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndClimbing : StateMachineBehaviour
{
    private AnimationEventRelay relay;
    private bool hasNotified;
    
    //OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (relay == null) relay = animator.GetComponent<AnimationEventRelay>();
        hasNotified = false;
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (hasNotified || animator.IsInTransition(layerIndex)) return;

        if(stateInfo.normalizedTime >= 1f)
        {
           hasNotified = true;
           if(relay !=null) relay.onAnimationFinished();
           Debug.Log("Relay called");
        }
        
    }

    //OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    // override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    // {
        
    //     if (climb == null) Debug.Log("no climb script errorrrrrrrrrr");
        
    // }

    // OnStateMove is called right after Animator.OnAnimatorMove()
    //override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that processes and affects root motion
    //}

    // OnStateIK is called right after Animator.OnAnimatorIK()
    //override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that sets up animation IK (inverse kinematics)
    //}
}
