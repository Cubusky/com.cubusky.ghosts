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
        public AnimatorUpdateMode updateMode;
        public Layer[] layers;
        public Parameter[] parameters;

        public Ghost(float timeScale)
        {
            this.timeScale = timeScale;
            this.position = Vector3.zero;
            this.rotation = Quaternion.identity;
            this.localScale = Vector3.one;
            this.speed = 1f;
            this.updateMode = AnimatorUpdateMode.Normal;
            this.layers = Array.Empty<Layer>();
            this.parameters = Array.Empty<Parameter>();
        }

        public Ghost(Transform transform) : this(1f)
        {
            position = transform.position;
            rotation = transform.rotation;
            localScale = transform.localScale;
        }

        public Ghost(Animator animator, float time = default) : this(animator.transform)
        {
            speed = animator.speed;
            updateMode = animator.updateMode;

            layers = new Layer[animator.layerCount];
            for (int layerIndex = 0; layerIndex < layers.Length; layerIndex++)
            {
                layers[layerIndex] = new(layerIndex, animator, time);
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
            && updateMode.Equals(other.updateMode)
            && layers.Equals(other.layers)
            && parameters.Equals(other.parameters);

        public readonly object Clone() => new Ghost()
        {
            timeScale = timeScale,
            position = position,
            rotation = rotation,
            localScale = localScale,
            speed = speed,
            updateMode = updateMode,
            layers = (Layer[])layers.Clone(),
            parameters = (Parameter[])parameters.Clone(),
        };

        public static bool operator ==(Ghost lhs, Ghost rhs) => lhs.timeScale == rhs.timeScale
            && lhs.position == rhs.position
            && lhs.rotation == rhs.rotation
            && lhs.localScale == rhs.localScale
            && lhs.speed == rhs.speed
            && lhs.updateMode == rhs.updateMode
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
            ghost.Store();
         
            ghost.position = transform.position;
            ghost.rotation = transform.rotation;
            ghost.localScale = transform.localScale;
        }

        public static void Restore(this Ghost ghost, Transform transform) 
        {
            ghost.Restore();

            transform.SetPositionAndRotation(ghost.position, ghost.rotation);
            transform.localScale = ghost.localScale;
        }

        public static void Store(this Ghost ghost, Animator animator, float time = default)
        {
            ghost.Store(animator.transform);

            ghost.speed = animator.speed;
            ghost.updateMode = animator.updateMode;
            Array.ForEach(ghost.layers, layer => layer.Store(animator, time));
            Array.ForEach(ghost.parameters, parameter => parameter.Store(animator));
        }

        public static void Restore(this Ghost ghost, Animator animator, float time = default)
        {
            ghost.Restore(animator.transform);

            animator.speed = ghost.speed;
            animator.updateMode = ghost.updateMode;
            Array.ForEach(ghost.layers, layer => layer.Restore(animator, time));
            Array.ForEach(ghost.parameters, parameter => parameter.Restore(animator));

            if (animator.isActiveAndEnabled)
            {
                animator.Update(0f);
            }
        }
    }
}
