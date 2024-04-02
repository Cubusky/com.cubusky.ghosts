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
            public float time;

            public Layer(int layerIndex, Animator animator, float time = default)
            {
                var animatorStateInfo = animator.GetCurrentAnimatorStateInfo(this.layerIndex = layerIndex);
                this.shortNameHash = animatorStateInfo.shortNameHash;
                this.normalizedTime = animatorStateInfo.normalizedTime;
                this.time = time;
            }

            public readonly bool Equals(Layer other) => layerIndex.Equals(other.layerIndex) && shortNameHash.Equals(other.shortNameHash) && normalizedTime.Equals(other.normalizedTime) && time.Equals(other.time);
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
                if (value.time != default)
                {
                    context.SerializeValue(value.time);
                }
            }

            Layer IJsonAdapter<Layer>.Deserialize(in JsonDeserializationContext<Layer> context)
            {
                var views = context.SerializedValue.AsArrayView().ToArray();
                return new Layer()
                {
                    layerIndex = context.DeserializeValue<int>(views[0]),
                    shortNameHash = context.DeserializeValue<int>(views[1]),
                    normalizedTime = context.DeserializeValue<float>(views[2]),
                    time = views.Length > 3 ? context.DeserializeValue<float>(views[3]) : default,
                };
            }
        }
    }

    public static partial class GhostExtensions
    {
        public static void Store(this Ghost.Layer layer, Animator animator, float time = default)
        {
            var animatorStateInfo = animator.GetCurrentAnimatorStateInfo(layer.layerIndex);
            layer.shortNameHash = animatorStateInfo.shortNameHash;
            layer.normalizedTime = animatorStateInfo.normalizedTime;
            layer.time = time;
        }

        public static void Restore(this Ghost.Layer layer, Animator animator, float time = default)
        {
            float normalizedTime = layer.normalizedTime;
            float deltaTime = layer.time - time;
            if (deltaTime != default && animator.isActiveAndEnabled)
            {
                animator.Play(layer.shortNameHash, layer.layerIndex);
                animator.Update(0f);
                var state = animator.GetCurrentAnimatorStateInfo(layer.layerIndex);

                var timeScaleMultiplier = animator.updateMode != AnimatorUpdateMode.UnscaledTime ? Time.timeScale : 1f;
                normalizedTime += deltaTime * (state.speed * state.speedMultiplier * animator.speed * timeScaleMultiplier) / state.length;

                normalizedTime = state.loop
                    ? Math.Min(normalizedTime, 1f)
                    : normalizedTime % 1f;
            }

            animator.Play(layer.shortNameHash, layer.layerIndex, normalizedTime);
        }
    }
}
