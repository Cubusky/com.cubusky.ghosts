using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Serialization.Json;
using UnityEngine;

namespace Cubusky.Ghosts
{
    public readonly struct TimedGhost
    {
        public readonly float[] times;
        public readonly Ghost[] ghosts;

        public TimedGhost(float[] times, Ghost[] ghosts)
        {
            this.times = times;
            this.ghosts = ghosts;
        }

        public readonly Ghost? this[float time, float warpDistance = float.PositiveInfinity]
        {
            get
            {
                var binaryIndex = Array.BinarySearch(times, time);
                if (binaryIndex >= 0)
                {
                    return ghosts[binaryIndex];
                }
                else if (~binaryIndex == 0 || ~binaryIndex >= ghosts.Length)
                {
                    return null;
                }
                else
                {
                    var ghostA = ghosts[~binaryIndex - 1];
                    var ghostB = ghosts[~binaryIndex];
                    var t = Mathf.InverseLerp(times[~binaryIndex - 1], times[~binaryIndex], time);

                    var deltaPosition = ghostB.position - ghostA.position;
                    return deltaPosition.sqrMagnitude >= warpDistance * warpDistance 
                        ? t < 0.5f ? ghostA : ghostB 
                        : Ghost.Lerp(ghostA, ghostB, t);
                }
            }
        }
    }

    public class TimedGhostAdapter : IJsonAdapter<TimedGhost>
    {
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#else
        [RuntimeInitializeOnLoadMethod]
#endif
        private static void AddGlobalAdapter()
        {
            JsonSerialization.AddGlobalAdapter(new TimedGhostAdapter());
        }

        private const string timeKey = nameof(KeyValuePair<float, Ghost>.Key);
        private const string ghostKey = nameof(KeyValuePair<float, Ghost>.Value);

        void IJsonAdapter<TimedGhost>.Serialize(in JsonSerializationContext<TimedGhost> context, TimedGhost value)
        {
            using var arrayScope = context.Writer.WriteArrayScope();
            for (int i = 0; i < value.times.Length; i++)
            {
                using var objectScope = context.Writer.WriteObjectScope();
                context.SerializeValue(timeKey, value.times[i]);
                context.SerializeValue(ghostKey, value.ghosts[i]);
            }
        }

        TimedGhost IJsonAdapter<TimedGhost>.Deserialize(in JsonDeserializationContext<TimedGhost> context)
        {
            var timesGhostsArray = context.SerializedValue.AsArrayView().ToArray();
            var times = new float[timesGhostsArray.Length];
            var ghosts = new Ghost[timesGhostsArray.Length];
            for (int i = 0; i < timesGhostsArray.Length; i++)
            {
                times[i] = context.DeserializeValue<float>(timesGhostsArray[i].GetValue(timeKey));
                ghosts[i] = context.DeserializeValue<Ghost>(timesGhostsArray[i].GetValue(ghostKey));
            }
            return new(times, ghosts);
        }
    }
}
