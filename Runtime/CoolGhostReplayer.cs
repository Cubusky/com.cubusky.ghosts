using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Jobs;
using Unity.Serialization.Json;
using UnityEngine;
using UnityEngine.Jobs;

namespace Cubusky.Ghosts
{
    public class CoolGhostReplayer : MonoBehaviour
    {
        [field: SerializeField] public GameObject ghostPrefab { get; set; }
        [field: SerializeReference, ReferenceDropdown] public IEnumerableLoader loader { get; set; }

        private TransformAccessArray _ghostTransforms;
        private JobHandle transformGhostsJobHandle;

        private JsonSerializationParameters jsonSerializationParameters = new()
        {
            Minified = false,
            Simplified = false,
            DisableValidation = false,
            UserDefinedAdapters = new() { new GhostAdapter() }
        };

        [SerializeField] private float _time;
        public float time
        {
            get => _time;
            set
            {
                transformGhostsJobHandle.Complete();
                transformGhostsJobHandle = new TransformGhostsJob
                {
                    //ghosts
                }.Schedule(_ghostTransforms);

                _time = value;
            }
        }

        [ContextMenu(nameof(Load))]
        public void Load()
        {
            _ghostTransforms = new(16);

            // load
            var jsons = loader.Load<IEnumerable<string>>();
            var ghostsByTime = jsons.Select(json => JsonSerialization.FromJson<Dictionary<float, Ghost>>(json, jsonSerializationParameters));

            foreach (var ghostByTime in ghostsByTime)
            {
                // Create ghost
                var ghostInstance = Instantiate(ghostPrefab);
                ghostInstance.hideFlags = HideFlags.HideInHierarchy;
                _ghostTransforms.Add(ghostInstance.transform);
            }
        }

        private void OnDestroy()
        {
            _ghostTransforms.Dispose();
        }
    }
}
