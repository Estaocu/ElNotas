// using Febucci.TextAnimatorCore.Typing;
// using Febucci.TextAnimatorForUnity.Actions;
// using UnityEngine;

// [System.Serializable]
// [CreateAssetMenu(
//     fileName = "LearnEntryAction",
//     menuName = "Text Animator/Actions/Learn Entry Action"
// )]
// public class LearnEntryActionScriptable : TypewriterActionScriptable
// {
//     protected override IActionState CreateCustomState(
//         ActionMarker marker,
//         object typewriter)
//     {
//         string entryId = string.Empty;

//         if (marker.parameters != null &&
//             marker.parameters.Length > 0)
//         {
//             entryId = marker.parameters[0];
//         }

//         return new LearnEntryState(entryId);
//     }

//     private struct LearnEntryState : IActionState
//     {
//         private readonly string entryId;
//         private bool hasExecuted;

//         public LearnEntryState(string entryId)
//         {
//             this.entryId = entryId;
//             hasExecuted = false;
//         }

//         public ActionStatus Progress(
//             float deltaTime,
//             ref TypingInfo typingInfo)
//         {
//             if (!hasExecuted)
//             {
//                 hasExecuted = true;

//                 if (!string.IsNullOrEmpty(entryId) &&
//                     NotebookManager.Instance != null)
//                 {
//                     NotebookManager.Instance.LearnEntryById(entryId);
//                 }
//                 else if (NotebookManager.Instance == null)
//                 {
//                     Debug.LogWarning(
//                         "[LearnEntryAction] No NotebookManager instance found in the scene."
//                     );
//                 }
//             }

//             return ActionStatus.Finished;
//         }

//         public void Cancel()
//         {
//         }
//     }
// }