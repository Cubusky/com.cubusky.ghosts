using System;
using System.Collections.Generic;
using System.Text;
using Unity.Serialization.Json;
using UnityEngine;

namespace Cubusky.Ghosts
{
    public class GhostRecorder : MonoBehaviour, ISerializationCallbackReceiver
    {
        [SerializeField, TimeSpan] private long _minimumRecordingTime = TimeSpan.TicksPerSecond * 10;
        [field: SerializeField] public Timer timer { get; set; } = new Timer()
        {
            SynchronizingObject = new ManualSynchronizer()
        };

        [SerializeField] private int _digits = 6;
        [field: SerializeField] public bool saveInEditor { get; set; }
        [field: SerializeReference, ReferenceDropdown(nullable = true)] public ICompressor compressor { get; set; }
        [field: SerializeReference, ReferenceDropdown] public ISaver saver { get; set; }

        public int digits 
        {
            get => ((FloatRounder)jsonSerializationParameters.UserDefinedAdapters[1]).digits;
            set => jsonSerializationParameters.UserDefinedAdapters[1] = new FloatRounder(_digits = value);
        }

        public TimeSpan minimumRecordingTime { get; set; }
        public float recordingTime { get; private set; }

        void ISerializationCallbackReceiver.OnBeforeSerialize() => _minimumRecordingTime = minimumRecordingTime.Ticks;
        void ISerializationCallbackReceiver.OnAfterDeserialize() => minimumRecordingTime = new(_minimumRecordingTime);

        private Ghost ghost;
        private Animator animator;
        private StringBuilder ghostJsonBuilder;

        private JsonSerializationParameters jsonSerializationParameters = new()
        {
            Minified = true,
            Simplified = true,
            DisableValidation = true,
            UserDefinedAdapters = new()
            {
                new GhostAdapter(),
                new FloatRounder(default),
            }
        };

        private void OnValidate() => digits = _digits;

        private void Start()
        {
            ghostJsonBuilder = new StringBuilder("[");
            TryGetComponent(out animator);

            Serialize(0f);
            timer.Elapsed += Serialize;
            timer.Start();
        }

        private void Update()
        {
            (timer.SynchronizingObject as ManualSynchronizer).ProcessQueue();
            recordingTime += Time.deltaTime;
        }

        private void Serialize(double time)
        {
            var timeAsFloat = (float)(time - time % timer.Interval);
            ghost = animator ? new(animator, timeAsFloat) : new(transform);

            if (GhostAdapter.WillSerialize(ghost, jsonSerializationParameters))
            {
                var json = JsonSerialization.ToJson(new KeyValuePair<float, Ghost>(timeAsFloat, ghost), jsonSerializationParameters);
                ghostJsonBuilder.Append(json + ",");
            }
        }

        private async void OnDestroy()
        {
            timer.Elapsed -= Serialize;
            timer.Stop();
            timer.Dispose();

#if UNITY_EDITOR
            if (saveInEditor)
#endif
            {
                if (recordingTime > minimumRecordingTime.TotalSeconds)
                {
                    var json = JsonSerialization.ToJson(new KeyValuePair<float, Ghost>(recordingTime - recordingTime % (float)timer.Interval, ghost), jsonSerializationParameters);
                    ghostJsonBuilder.Append(json);
                    ghostJsonBuilder.Append("]");

                    var bytes = saver.encoding.GetBytes(ghostJsonBuilder.ToString());
                    bytes = compressor != null
                        ? await compressor.CompressAsync(bytes)
                        : bytes;
                    await saver.SaveAsync(bytes);
                }
            }
        }
    }
}
