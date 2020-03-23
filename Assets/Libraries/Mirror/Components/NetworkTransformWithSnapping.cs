using UnityEngine;

namespace Mirror
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Network/NetworkTransform")]
    [HelpURL("https://vis2k.github.io/Mirror/Components/NetworkTransform")]
    public class NetworkTransformWithSnapping : NetworkTransformBase
    {
        [SerializeField]
        private float snappingDistance = 5;
        
        protected override Transform targetComponent => transform;

        protected override bool NeedsTeleport()
        {   
            // ADDITION TO FUNC: if distance between start and goal is too much (> snapping), teleport:
            if (Vector3.Distance(start.localPosition, goal.localPosition) > snappingDistance){
                return true;
            }else{
                // calculate time between the two data points
                float startTime = start != null ? start.timeStamp : Time.time - syncInterval;
                float goalTime = goal != null ? goal.timeStamp : Time.time;
                float difference = goalTime - startTime;
                float timeSinceGoalReceived = Time.time - goalTime;
                return timeSinceGoalReceived > difference * 5;
            }
        }
    }
    
}
