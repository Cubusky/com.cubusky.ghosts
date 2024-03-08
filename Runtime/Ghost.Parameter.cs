using System;
using System.Linq;
using Unity.Serialization.Json;
using UnityEngine;

namespace Cubusky.Ghosts
{
    public partial struct Ghost
    {
        [Serializable]
        public struct Parameter : IEquatable<Parameter>, ICloneable
        {
            public int id;
            public AnimatorControllerParameterType type;
            public float floatValue;
            public int intValue;
            public bool boolValue;

            public Parameter(int id, AnimatorControllerParameterType type, Animator animator)
            {
                floatValue = default;
                intValue = default;
                boolValue = default;

                switch (this.type = type)
                {
                    case AnimatorControllerParameterType.Float:
                        floatValue = animator.GetFloat(this.id = id);
                        break;
                    case AnimatorControllerParameterType.Int:
                        intValue = animator.GetInteger(this.id = id);
                        break;
                    case AnimatorControllerParameterType.Bool:
                    case AnimatorControllerParameterType.Trigger:
                        boolValue = animator.GetBool(this.id = id);
                        break;
                    default:
                        throw new InvalidOperationException($"Unhandled {nameof(type)} value {type}.");
                }
            }

            public readonly bool Equals(Parameter other) => id.Equals(other.id) && type.Equals(other.type) && floatValue.Equals(other.floatValue) && intValue.Equals(other.intValue) && boolValue.Equals(other.boolValue);
            public readonly object Clone() => MemberwiseClone();
        }

        public class ParameterAdapter : IJsonAdapter<Parameter>
        {
#if UNITY_EDITOR
            [UnityEditor.InitializeOnLoadMethod]
#else
            [RuntimeInitializeOnLoadMethod]
#endif
            private static void AddGlobalAdapter()
            {
                JsonSerialization.AddGlobalAdapter(new ParameterAdapter());
            }

            void IJsonAdapter<Parameter>.Serialize(in JsonSerializationContext<Parameter> context, Parameter value)
            {
                using var parameterScope = context.Writer.WriteArrayScope();
                context.SerializeValue(value.id);
                context.SerializeValue(value.type);

                switch (value.type)
                {
                    case AnimatorControllerParameterType.Float:
                        context.SerializeValue(value.floatValue);
                        break;
                    case AnimatorControllerParameterType.Int:
                        context.SerializeValue(value.intValue);
                        break;
                    case AnimatorControllerParameterType.Bool:
                    case AnimatorControllerParameterType.Trigger:
                        context.SerializeValue(value.boolValue);
                        break;
                }
            }

            Parameter IJsonAdapter<Parameter>.Deserialize(in JsonDeserializationContext<Parameter> context)
            {
                var views = context.SerializedValue.AsArrayView().ToArray();

                int id = context.DeserializeValue<int>(views[0]);
                AnimatorControllerParameterType type = context.DeserializeValue<AnimatorControllerParameterType>(views[1]);

                var floatValue = type == AnimatorControllerParameterType.Float ? context.DeserializeValue<float>(views[2]) : default;
                var intValue = type == AnimatorControllerParameterType.Int ? context.DeserializeValue<int>(views[2]) : default;
                var boolValue = AnimatorControllerParameterType.Bool == type || AnimatorControllerParameterType.Trigger == type ? context.DeserializeValue<bool>(views[2]) : default;

                return new Parameter()
                {
                    id = id,
                    type = type,
                    floatValue = floatValue,
                    intValue = intValue,
                    boolValue = boolValue,
                };
            }
        }
    }

    public static partial class GhostExtensions
    {
        public static void Store(this Ghost.Parameter parameter, Animator animator)
        {
            switch (parameter.type)
            {
                case AnimatorControllerParameterType.Float:
                    parameter.floatValue = animator.GetFloat(parameter.id);
                    break;
                case AnimatorControllerParameterType.Int:
                    parameter.intValue = animator.GetInteger(parameter.id);
                    break;
                case AnimatorControllerParameterType.Bool:
                case AnimatorControllerParameterType.Trigger:
                    parameter.boolValue = animator.GetBool(parameter.id);
                    break;
                default:
                    throw new InvalidOperationException($"Unhandled {nameof(parameter.type)} value {parameter.type}.");
            }
        }

        public static void Restore(this Ghost.Parameter parameter, Animator animator)
        {
            switch (parameter.type)
            {
                case AnimatorControllerParameterType.Float:
                    animator.SetFloat(parameter.id, parameter.floatValue);
                    break;
                case AnimatorControllerParameterType.Int:
                    animator.SetInteger(parameter.id, parameter.intValue);
                    break;
                case AnimatorControllerParameterType.Bool:
                    animator.SetBool(parameter.id, parameter.boolValue);
                    break;
                case AnimatorControllerParameterType.Trigger:
                    if (parameter.boolValue)
                    {
                        animator.SetTrigger(parameter.id);
                    }
                    else
                    {
                        animator.ResetTrigger(parameter.id);
                    }
                    break;
            }
        }
    }
}
