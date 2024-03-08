using System;
using System.Linq;
using Unity.Serialization.Json;
using UnityEngine;

namespace Cubusky.Ghosts
{
    public partial struct Ghost
    {
        [Serializable]
        public struct Layer : IEquatable<Layer>, ICloneable
        {
            public int layerIndex;
            public int shortNameHash;
            public float normalizedTime;

            public Layer(int layerIndex, Animator animator)
            {
                var animatorStateInfo = animator.GetCurrentAnimatorStateInfo(this.layerIndex = layerIndex);
                this.shortNameHash = animatorStateInfo.shortNameHash;
                this.normalizedTime = animatorStateInfo.normalizedTime;
            }

            public readonly bool Equals(Layer other) => layerIndex.Equals(other.layerIndex) && shortNameHash.Equals(other.shortNameHash) && normalizedTime.Equals(other.normalizedTime);
            public readonly object Clone() => MemberwiseClone();
        }

        public class LayerAdapter : IJsonAdapter<Layer>
        {
#if UNITY_EDITOR
            [UnityEditor.InitializeOnLoadMethod]
#else
            [RuntimeInitializeOnLoadMethod]
#endif
            private static void AddGlobalAdapter()
            {
                JsonSerialization.AddGlobalAdapter(new LayerAdapter());
            }

            void IJsonAdapter<Layer>.Serialize(in JsonSerializationContext<Layer> context, Layer value)
            {
                using var layerScope = context.Writer.WriteArrayScope();
                context.SerializeValue(value.layerIndex);
                context.SerializeValue(value.shortNameHash);
                context.SerializeValue(value.normalizedTime);
            }

            Layer IJsonAdapter<Layer>.Deserialize(in JsonDeserializationContext<Layer> context)
            {
                var views = context.SerializedValue.AsArrayView().ToArray();
                return new Layer()
                {
                    layerIndex = context.DeserializeValue<int>(views[0]),
                    shortNameHash = context.DeserializeValue<int>(views[1]),
                    normalizedTime = context.DeserializeValue<float>(views[2]),
                };
            }
        }
    }

    public static partial class GhostExtensions
    {
        public static void Store(this Ghost.Layer layer, Animator animator)
        {
            var animatorStateInfo = animator.GetCurrentAnimatorStateInfo(layer.layerIndex);
            layer.shortNameHash = animatorStateInfo.shortNameHash;
            layer.normalizedTime = animatorStateInfo.normalizedTime;
        }

        public static void Restore(this Ghost.Layer layer, Animator animator)
        {
            animator.Play(layer.shortNameHash, layer.layerIndex, layer.normalizedTime);
            if (animator.isActiveAndEnabled)
            {
                animator.Update(default);
            }
        }
    }
}
