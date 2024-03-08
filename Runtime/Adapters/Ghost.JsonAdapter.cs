using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Serialization.Json;
using UnityEngine;

namespace Cubusky.Ghosts
{
    public class GhostAdapter : IJsonAdapter<Ghost>
    {
        public Ghost ghost = new(1f);

        public static readonly string[] vector3Keys = new string[3] { nameof(Vector3.x), nameof(Vector3.y), nameof(Vector3.z) };
        public static readonly string[] quaternionKeys = new string[4] { nameof(Quaternion.x), nameof(Quaternion.y), nameof(Quaternion.z), nameof(Quaternion.w) };

        public const string timeScaleKey = "ts";
        public const string positionKey = "pos";
        public const string rotationKey = "rot";
        public const string localScaleKey = "scl";
        public const string speedKey = "spd";
        public const string layersKey = "lyrs";
        public const string parametersKey = "prms";

        public static readonly LayerIndexComparer layerIndexComparer = new();
        public static readonly ParameterIdComparer parameterIdComparer = new();

        public class LayerIndexComparer : IEqualityComparer<Ghost.Layer>
        {
            public bool Equals(Ghost.Layer layer, Ghost.Layer other) => layer.layerIndex.Equals(other.layerIndex);
            public int GetHashCode(Ghost.Layer layer) => HashCode.Combine(layer.layerIndex);
        }

        public class ParameterIdComparer : IEqualityComparer<Ghost.Parameter>
        {
            public bool Equals(Ghost.Parameter parameter, Ghost.Parameter other) => parameter.id.Equals(other.id) && parameter.type.Equals(other.type);
            public int GetHashCode(Ghost.Parameter parameter) => HashCode.Combine(parameter.id, parameter.type);
        }

        public static bool WillSerialize(Ghost ghost, JsonSerializationParameters parameters) => parameters.UserDefinedAdapters.OfType<GhostAdapter>().All(value => value.WillSerialize(ghost));

        public bool WillSerialize(Ghost value) => ghost.timeScale != value.timeScale
            || ghost.position != value.position
            || ghost.rotation != value.rotation
            || ghost.localScale != value.localScale
            || ghost.speed != value.speed
            || !ghost.layers.SequenceEqual(value.layers, layerIndexComparer)
            || !ghost.parameters.SequenceEqual(value.parameters);

        void IJsonAdapter<Ghost>.Serialize(in JsonSerializationContext<Ghost> context, Ghost value)
        {
            using var ghostScope = context.Writer.WriteObjectScope();
            if (ghost.timeScale != value.timeScale)
            {
                context.SerializeValue(timeScaleKey, value.timeScale);
            }
            if (ghost.position != value.position)
            {
                //context.SerializeValue(positionKey, value.position);

                using var positionScope = context.Writer.WriteObjectScope(positionKey);
                for (int i = 0; i < vector3Keys.Length; i++)
                {
                    if (ghost.position[i] != value.position[i])
                    {
                        context.SerializeValue(vector3Keys[i], value.position[i]);
                    }
                }
            }
            if (ghost.rotation != value.rotation)
            {
                //context.SerializeValue(rotationKey, value.rotation);

                using var rotationScope = context.Writer.WriteObjectScope(rotationKey);
                for (int i = 0; i < quaternionKeys.Length; i++)
                {
                    if (ghost.rotation[i] != value.rotation[i])
                    {
                        context.SerializeValue(quaternionKeys[i], value.rotation[i]);
                    }
                }
            }
            if (ghost.localScale != value.localScale)
            {
                //context.SerializeValue(localScaleKey, value.localScale);

                using var localScaleScope = context.Writer.WriteObjectScope(localScaleKey);
                for (int i = 0; i < vector3Keys.Length; i++)
                {
                    if (ghost.localScale[i] != value.localScale[i])
                    {
                        context.SerializeValue(vector3Keys[i], value.localScale[i]);
                    }
                }
            }
            if (ghost.speed != value.speed)
            {
                context.SerializeValue(speedKey, value.speed);
            }

            if (!ghost.layers.SequenceEqual(value.layers, layerIndexComparer))
            {
                using var layersScope = context.Writer.WriteArrayScope(layersKey);
                foreach (var layer in value.layers.Except(ghost.layers, layerIndexComparer))
                {
                    context.SerializeValue(layer);
                }
            }

            if (!ghost.parameters.SequenceEqual(value.parameters))
            {
                using var parameterScope = context.Writer.WriteArrayScope(parametersKey);
                foreach (var parameter in value.parameters.Except(ghost.parameters))
                {
                    context.SerializeValue(parameter);
                }
            }

            ghost = (Ghost)value.Clone();
        }

        Ghost IJsonAdapter<Ghost>.Deserialize(in JsonDeserializationContext<Ghost> context)
        {
            ghost.timeScale = context.SerializedValue.TryGetValue(timeScaleKey, out var view) ? context.DeserializeValue<float>(view) : ghost.timeScale;

            //ghost.position = context.SerializedValue.TryGetValue(positionKey, out view) ? context.DeserializeValue<Vector3>(view) : ghost.position;
            if (context.SerializedValue.TryGetValue(positionKey, out view))
            {
                for (int i = 0; i < vector3Keys.Length; i++)
                {
                    if (view.TryGetValue(vector3Keys[i], out var propertyView))
                    {
                        ghost.position[i] = context.DeserializeValue<float>(propertyView);
                    }
                }
            }

            //ghost.rotation = context.SerializedValue.TryGetValue(rotationKey, out view) ? context.DeserializeValue<Quaternion>(view) : ghost.rotation;
            if (context.SerializedValue.TryGetValue(rotationKey, out view))
            {
                for (int i = 0; (i < quaternionKeys.Length); i++)
                {
                    if (view.TryGetValue(quaternionKeys[i], out var propertyView))
                    {
                        ghost.rotation[i] = context.DeserializeValue<float>(propertyView);
                    }
                }
            }

            //ghost.localScale = context.SerializedValue.TryGetValue(localScaleKey, out view) ? context.DeserializeValue<Vector3>(view) : ghost.localScale;
            if (context.SerializedValue.TryGetValue(localScaleKey, out view))
            {
                for (int i = 0; i < vector3Keys.Length; i++)
                {
                    if (view.TryGetValue(vector3Keys[i], out var propertyView))
                    {
                        ghost.position[i] = context.DeserializeValue<float>(propertyView);
                    }
                }
            }

            ghost.speed = context.SerializedValue.TryGetValue(speedKey, out view) ? context.DeserializeValue<float>(view) : ghost.speed;
            ghost.layers = context.SerializedValue.TryGetValue(layersKey, out view) ? context.DeserializeValue<Ghost.Layer[]>(view).Union(ghost.layers, layerIndexComparer).ToArray() : ghost.layers;
            ghost.parameters = context.SerializedValue.TryGetValue(parametersKey, out view) ? context.DeserializeValue<Ghost.Parameter[]>(view).Union(ghost.parameters, parameterIdComparer).ToArray() : ghost.parameters;

            return (Ghost)ghost.Clone();
        }
    }
}
