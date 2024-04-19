using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Unity.Serialization.Json;
using UnityEngine;

namespace Cubusky.Ghosts
{
    public class GhostManager : MonoBehaviour, ISerializationCallbackReceiver
    {
        [field: HelpBox(nameof(Ghost) + " instances will be stored and managed under this " + nameof(GameObject) + ". Make sure to not put any children under this " + nameof(GameObject) + " to avoid any confusion.", UnityEngine.UIElements.HelpBoxMessageType.Info)]
        [field: SerializeField] public GameObject ghostPrefab { get; set; }
        [field: SerializeReference, ReferenceDropdown(nullable = true)] public ICompressor compressor { get; set; }
        [field: SerializeReference, ReferenceDropdown] public IEnumerableLoader loader { get; set; }
        [field: SerializeField, Tooltip("Auto loads the ghosts " + nameof(ISerializationCallbackReceiver.OnAfterDeserialize) + ". This may be desirable if you're doing a lot of script reloading.")] public bool autoLoad { get; set; }

        [field: Header("Control")]
        [field: Tooltip(
@"The playback time of the ghost manager.

To follow a ghost:
- Lock the Inspector window
- Select a Ghost
- Press Shift + F
- Increase / Decrease time"
        )]
        [field: SerializeField] public float time { get; set; }
        [field: SerializeField] public bool playback { get; set; }
        [field: SerializeField] public float playbackScale { get; set; } = 1f;
        [field: SerializeField, Tooltip("If a ghosts moves more than the warp distance in a given frame, they will be warped to their new location instead."), Min(0f)] public float warpDistance { get; set; } = float.PositiveInfinity;
        [field: SerializeField, Min(1), Tooltip("The amount of ghosts that will be updated in unison per frame. Lowering this setting may increase editor responsiveness when profiling ghosts, but may take longer for all ghosts to update correctly.")] public int updateBatchSize { get; set; } = 1024;
        
        public bool hasAnimators => ghostPrefab != null && ghostPrefab.TryGetComponent<Animator>(out _);

        private float _lastTime;
        private int currentGhostIndex;
        private int targetGhostIndex;

        private JsonSerializationParameters jsonSerializationParameters = new()
        {
            Minified = true,
            UserDefinedAdapters = new() { new GhostAdapter() }
        };

        private TimedGhost[] timedGhosts = Array.Empty<TimedGhost>();
        private Transform[] transforms = Array.Empty<Transform>();
        private Animator[] animators = Array.Empty<Animator>();

        public ReadOnlyCollection<TimedGhost> ReadTimedGhosts() => Array.AsReadOnly(timedGhosts);
        public ReadOnlyCollection<Transform> ReadTransforms() => Array.AsReadOnly(transforms);
        public ReadOnlyCollection<Animator> ReadAnimators() => Array.AsReadOnly(animators);

        private CancellationTokenSource destroyCancellationTokenSource;
        private CancellationToken destroyCancellationToken;

        private void OnDestroy()
        {
            destroyCancellationTokenSource.Cancel();
            destroyCancellationTokenSource.Dispose();
        }

        private void OnValidate()
        {
            if (_lastTime != time)
            {
                _lastTime = time;
                UpdateGhosts();
            }
        }

        private void Update()
        {
            if (playback)
            {
                time += Time.deltaTime * playbackScale;
                _lastTime = time;
                UpdateGhosts();
            }
        }

        [ContextMenu("Load Ghosts")]
        public async void LoadGhosts()
        {
            await LoadGhostDataAsync(destroyCancellationToken);
            BindGhosts();
            UpdateGhosts();
        }

        public async Task LoadGhostDataAsync(CancellationToken cancellationToken = default)
        {
            if (timedGhosts.Length < updateBatchSize)
            {
                Array.Resize(ref timedGhosts, updateBatchSize);
            }

            int i = 0;
            await foreach (var data in loader.LoadAsyncEnumerable<byte[]>(cancellationToken).ConfigureAwait(false))
            {
                var json = compressor != null
                    ? loader.encoding.GetString(await compressor.DecompressAsync(data, destroyCancellationToken))
                    : loader.encoding.GetString(data);

                var timedGhost = JsonSerialization.FromJson<TimedGhost>(json, jsonSerializationParameters);
                timedGhosts[i++] = timedGhost;

                if (timedGhosts.Length == i)
                {
                    Array.Resize(ref timedGhosts, i + updateBatchSize);
                }
            }
            Array.Resize(ref timedGhosts, i);
        }

        public void BindGhosts()
        {
            var targetLength = timedGhosts.Length;
            while (transform.childCount > targetLength)
            {
                DestroyLastChild();
            }

            while (transform.childCount < targetLength)
            {
                Instantiate(ghostPrefab, transform);
            }

            if (hasAnimators)
            {
                Array.Resize(ref animators, targetLength);
                for (int i = 0; i < targetLength; i++)
                {
                    animators[i] = transform.GetChild(i).GetComponent<Animator>();
                    animators[i].enabled = false;
                }
            }
            else
            {
                Array.Resize(ref transforms, targetLength);
                for (int i = 0; i < targetLength; i++)
                {
                    transforms[i] = transform.GetChild(i);
                }
            }
        }

        [ContextMenu("Destroy Ghosts")]
        public void DestroyGhosts()
        {
            while (transform.childCount > 0)
            {
                DestroyLastChild();
            }

            currentGhostIndex = targetGhostIndex = 0;
        }

        private void DestroyLastChild()
        {
            var child = transform.GetChild(transform.childCount - 1);
            DestroyImmediate(child.gameObject);
        }

        public void UpdateGhosts()
        {
            if (currentGhostIndex == targetGhostIndex)
            {
                UpdateGhostsRecursively();
            }
            else
            {
                targetGhostIndex = currentGhostIndex;
            }

            async void UpdateGhostsRecursively()
            {
                if (this.timedGhosts.Length == 0)
                {
                    return;
                }

                var end = Math.Min(currentGhostIndex + updateBatchSize, currentGhostIndex < targetGhostIndex ? targetGhostIndex : this.timedGhosts.Length);

                var timedGhosts = this.timedGhosts[currentGhostIndex..end];
                if (hasAnimators)
                {
                    var animators = this.animators[currentGhostIndex..end];

                    for (int i = 0; i < timedGhosts.Length; i++)
                    {
                        var ghost = timedGhosts[i][time];
                        if (ghost.HasValue)
                        {
                            animators[i].gameObject.SetActive(true);
                            animators[i].enabled = true;
                            ghost.Value.Restore(animators[i], time);
                            animators[i].enabled = false;
                        }
                        else
                        {
                            animators[i].gameObject.SetActive(false);
                        }
                    }
                }
                else
                {
                    var transforms = this.transforms[currentGhostIndex..end];

                    for (int i = 0; i < timedGhosts.Length; i++)
                    {
                        var ghost = timedGhosts[i][time, warpDistance];
                        if (ghost.HasValue)
                        {
                            this.transforms[i].gameObject.SetActive(true);
                            ghost.Value.Restore(this.transforms[i]);
                        }
                        else
                        {
                            this.transforms[i].gameObject.SetActive(false);
                        }
                    }
                }

                currentGhostIndex = end % this.timedGhosts.Length;

                await Task.Yield();

                if (currentGhostIndex != targetGhostIndex)
                {
                    UpdateGhostsRecursively();
                }
            }
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize() { }
        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            if (destroyCancellationTokenSource == null)
            {
                destroyCancellationTokenSource = new();
                destroyCancellationToken = destroyCancellationTokenSource.Token;
            }

#if UNITY_EDITOR
            if (timedGhosts.Length == 0)
            {
                if (autoLoad)
                {
                    UnityEditor.EditorApplication.delayCall += LoadGhosts;
                }
                else
                {
                    UnityEditor.EditorApplication.delayCall += () =>
                    {
                        if (this)
                        {
                            DestroyGhosts();
                        }
                    };
                }
            }

            UnityEditor.EditorApplication.delayCall += () =>
            {
                UnityEditor.EditorApplication.update -= Update;
                if (!Application.IsPlaying(this))
                {
                    UnityEditor.EditorApplication.update += Update;
                }
            };
#endif
        }
    }
}
