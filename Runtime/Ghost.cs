using System;
using UnityEngine;

namespace Cubusky.Ghosts
{
    [Serializable]
    public partial struct Ghost : IEquatable<Ghost>, ICloneable
    {
        public float timeScale;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 localScale;
        public float speed;
        public Layer[] layers;
        public Parameter[] parameters;

        public Ghost(float timeScale)
        {
            this.timeScale = timeScale;
            this.position = Vector3.zero;
            this.rotation = Quaternion.identity;
            this.localScale = Vector3.one;
            this.speed = 1f;
            this.layers = Array.Empty<Layer>();
            this.parameters = Array.Empty<Parameter>();
        }

        public Ghost(Transform transform) : this(1f)
        {
            transform.GetPositionAndRotation(out position, out rotation);
            localScale = transform.localScale;
        }

        public Ghost(Animator animator) : this(animator.transform)
        {
            speed = animator.speed;

            layers = new Layer[animator.layerCount];
            for (int layerIndex = 0; layerIndex < layers.Length; layerIndex++)
            {
                layers[layerIndex] = new(layerIndex, animator);
            }

            // Store all parameters, except those that are controlled by a curve.
            parameters = new Parameter[animator.parameterCount];
            int parameterIndex = 0;
            foreach (var parameter in animator.parameters)
            {
                if (animator.IsParameterControlledByCurve(parameter.nameHash))
                {
                    continue;
                }

                parameters[parameterIndex] = new(parameter.nameHash, parameter.type, animator);
                parameterIndex++;
            }
            Array.Resize(ref parameters, parameterIndex);
        }

        public override readonly bool Equals(object obj) => obj is Ghost ghost && Equals(ghost);
        public override readonly int GetHashCode() => HashCode.Combine(timeScale, position, rotation, localScale, layers, parameters);

        public readonly bool Equals(Ghost other) => timeScale.Equals(other.timeScale)
            && position.Equals(other.position)
            && rotation.Equals(other.rotation)
            && localScale.Equals(other.localScale)
            && speed.Equals(other.speed)
            && layers.Equals(other.layers)
            && parameters.Equals(other.parameters);

        public readonly object Clone() => new Ghost()
        {
            timeScale = timeScale,
            position = position,
            rotation = rotation,
            localScale = localScale,
            speed = speed,
            layers = (Layer[])layers.Clone(),
            parameters = (Parameter[])parameters.Clone(),
        };

        public static bool operator ==(Ghost lhs, Ghost rhs) => lhs.timeScale == rhs.timeScale
            && lhs.position == rhs.position
            && lhs.rotation == rhs.rotation
            && lhs.localScale == rhs.localScale
            && lhs.speed == rhs.speed
            && lhs.layers == rhs.layers
            && lhs.parameters == rhs.parameters;

        public static bool operator !=(Ghost lhs, Ghost rhs) => !(lhs == rhs);
    }

    public static partial class GhostExtensions
    {
        public static void Store(this Ghost ghost)
        {
            ghost.timeScale = Time.timeScale;
        }

        public static void Restore(this Ghost ghost)
        {
            Time.timeScale = ghost.timeScale;
        }

        public static void Store(this Ghost ghost, Transform transform)
        {
            transform.GetPositionAndRotation(out ghost.position, out ghost.rotation);
            ghost.localScale = transform.localScale;

            ghost.Store();
        }

        public static void Restore(this Ghost ghost, Transform transform) 
        {
            transform.SetPositionAndRotation(ghost.position, ghost.rotation);
            transform.localScale = ghost.localScale;

            ghost.Restore();
        }

        public static void Store(this Ghost ghost, Animator animator)
        {
            ghost.speed = animator.speed;
            Array.ForEach(ghost.layers, layer => layer.Store(animator));
            Array.ForEach(ghost.parameters, parameter => parameter.Store(animator));

            ghost.Store(animator.transform);
        }

        public static void Restore(this Ghost ghost, Animator animator) 
        {
            animator.speed = ghost.speed;
            Array.ForEach(ghost.layers, layer => layer.Restore(animator));
            Array.ForEach(ghost.parameters, parameter => parameter.Restore(animator));

            ghost.Restore(animator.transform);
        }
    }
}
