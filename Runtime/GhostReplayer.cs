using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Unity.Serialization.Json;
using UnityEngine;

namespace Cubusky.Ghosts
{
    public class GhostReplayer : MonoBehaviour
    {
        public float time;
        [field: SerializeReference, ReferenceDropdown] public IEnumerableLoader loader { get; set; }

        [SerializeField, HideInInspector] private Animator animator;
        private float[] times;  // Do not serialize this: it is too large and slow down the editor.
        private Ghost[] ghosts; // Do not serialize this: it is too large and slow down the editor.

        private void OnValidate()
        {
            if (!animator || times == null || times.Length == 0)
            {
                return;
            }

            var binaryIndex = Array.BinarySearch(times, time);
            gameObject.SetActive(~binaryIndex != 0 && ~binaryIndex < ghosts.Length);
            if (animator.isActiveAndEnabled)
            {
                var ghost = binaryIndex >= 0
                    ? ghosts[binaryIndex]
                    : Ghost.Lerp(ghosts[~binaryIndex], ghosts[~binaryIndex + 1], Mathf.InverseLerp(times[~binaryIndex], times[~binaryIndex + 1], time));

                ghost.Restore(animator);
                animator.Update(time);
            }
        }

        [ContextMenu("Read")]
        private void Read()
        {
            TryGetComponent(out animator);

            var json = loader.Load<IEnumerable<string>>().First();
            var unsortedTimedValues = JsonSerialization.FromJson<Dictionary<float, Ghost>>(json, jsonSerializationParameters);

            times = unsortedTimedValues.Keys.ToArray();
            ghosts = unsortedTimedValues.Values.ToArray();
        }

        private JsonSerializationParameters jsonSerializationParameters = new()
        {
            Minified = false,
            Simplified = false,
            DisableValidation = false,
            UserDefinedAdapters = new() { new GhostAdapter() }
        };

        private void Start()
        {
        }

        private void Update()
        {
            //if (animator)
            //{
            //    ghost.Restore(animator);
            //}
            //else
            //{
            //    ghost.Restore(transform);
            //}
        }
    }
}
