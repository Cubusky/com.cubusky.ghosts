using System;
using System.Linq;
using UnityEngine;

namespace Cubusky.Ghosts
{
    public partial struct Ghost
    {
        public static Ghost Lerp(Ghost a, Ghost b, float t) => new()
        {
            timeScale = Mathf.Lerp(a.timeScale, b.timeScale, t),
            position = Vector3.Lerp(a.position, b.position, t),
            rotation = Quaternion.Slerp(a.rotation, b.rotation, t),
            localScale = Vector3.Lerp(a.localScale, b.localScale, t),
            speed = Mathf.Lerp(a.speed, b.speed, t),
            layers = Enumerable.Range(0, a.layers.Length).Select(i => Lerp(a.layers[i], b.layers[i], t)).ToArray(),
            parameters = Enumerable.Range(0, a.parameters.Length).Select(i => Lerp(a.parameters[i], b.parameters[i], t)).ToArray(),
        };

        public static Layer Lerp(Layer a, Layer b, float t)
        {
            throw new System.NotImplementedException();
        }

        public static Parameter Lerp(Parameter a, Parameter b, float t)
        {
            var max = Mathf.RoundToInt(t) > 0 ? b : a;
            return new()
            {
                id = max.id,
                type = max.type,
                floatValue = Mathf.Lerp(a.floatValue, b.floatValue, t),
                intValue = Mathf.RoundToInt(Mathf.Lerp(a.intValue, b.intValue, t)),
                boolValue = max.boolValue,
            };
        }
    }
}
