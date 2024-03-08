using Unity.Collections;
using UnityEngine.Jobs;

namespace Cubusky.Ghosts
{
    public struct TransformGhostsJob : IJobParallelForTransform
    {
        [ReadOnly] public NativeArray<Ghost> ghosts;

        public void Execute(int index, TransformAccess transform)
        {
            var ghost = ghosts[index];
            transform.SetPositionAndRotation(ghost.position, ghost.rotation);
            transform.localScale = ghost.localScale;
        }
    }
}
