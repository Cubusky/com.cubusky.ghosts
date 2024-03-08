using System.Collections.Generic;
using System.Text;
using Unity.Serialization.Json;
using UnityEngine;

namespace Cubusky.Ghosts
{
    public class GhostRecorder : MonoBehaviour
    {
        [field: SerializeField] public Timer timer { get; set; } = new Timer()
        {
            SynchronizingObject = new UpdateSynchronizer()
        };

        [SerializeField] private int _digits = 6;
        [field: SerializeReference, ReferenceDropdown] public ISaver saver { get; set; }

        public int digits 
        {
            get => ((FloatRounder)jsonSerializationParameters.UserDefinedAdapters[1]).digits;
            set => jsonSerializationParameters.UserDefinedAdapters[1] = new FloatRounder(_digits = value);
        }

        private void OnValidate() => digits = _digits;

        private Ghost ghost;
        private Animator animator;
        private StringBuilder ghostJsonBuilder;

        private JsonSerializationParameters jsonSerializationParameters = new()
        {
            Minified = false,
            Simplified = false,
            DisableValidation = true,
            UserDefinedAdapters = new()
            {
                new GhostAdapter(),
                new FloatRounder(default),
            }
        };

        private void Start()
        {
            ghostJsonBuilder = new StringBuilder("[");
            TryGetComponent(out animator);

            Serialize(0f);
            timer.Elapsed += Serialize;
            timer.Start();
        }

        private void Serialize(double time)
        {
            ghost = animator ? new(animator) : new(transform);

            if (GhostAdapter.WillSerialize(ghost, jsonSerializationParameters))
            {
                var json = JsonSerialization.ToJson(new KeyValuePair<double, Ghost>(time, ghost), jsonSerializationParameters);
                ghostJsonBuilder.Append(json + ",");
            }
        }

        private void OnDestroy()
        {
            timer.Elapsed -= Serialize;
            timer.Stop();
            timer.Dispose();

            if (ghostJsonBuilder.Length > 0)
            {
                ghostJsonBuilder.Remove(ghostJsonBuilder.Length - 1, 1);
                ghostJsonBuilder.Append("]");
                saver.SaveAsync(ghostJsonBuilder.ToString());
            }
        }
    }
}
