using System.Collections;
using System.Collections.Generic;
using Game.Utils;
using UnityEngine;

namespace Game.Audio
{
    [System.Serializable]
    public class AmbientSoundEntry
    {
        public SoundName Sound;
        [Tooltip("Trọng số xuất hiện — số càng cao càng dễ được chọn")]
        public float Weight = 1f;
    }

    public class AmbientSoundController : MonoSingleton<AmbientSoundController>
    {
        [Header("Sound Pool")]
        [SerializeField] private List<AmbientSoundEntry> _sounds;

        [Header("Timing")]
        [SerializeField] private float _minInterval = 8f;
        [SerializeField] private float _maxInterval = 25f;

        [Header("Anti-Repeat")]
        [SerializeField] private int _noRepeatWindow = 2;

        private Coroutine _loopRoutine;
        private Queue<SoundName> _recentHistory;
        private bool _isPaused;

        public override void Awake()
        {
            base.Awake();
            _recentHistory = new Queue<SoundName>();
        }

        private void OnEnable()
        {
            StartLoop();
        }

        private void OnDisable()
        {
            StopLoop();
        }

        public void StartLoop()
        {
            if (_loopRoutine != null) return;
            _loopRoutine = StartCoroutine(AmbientLoop());
        }

        public void StopLoop()
        {
            if (_loopRoutine == null) return;
            StopCoroutine(_loopRoutine);
            _loopRoutine = null;
        }

        public void SetPaused(bool paused)
        {
            _isPaused = paused;
        }

        private IEnumerator AmbientLoop()
        {
            while (true)
            {
                float wait = Random.Range(_minInterval, _maxInterval);
                yield return new WaitForSeconds(wait);

                if (_isPaused) continue;

                PlayRandomAmbient();
            }
        }

        private void PlayRandomAmbient()
        {
            if (_sounds == null || _sounds.Count == 0) return;

            var candidate = PickWeightedExcludingRecent();
            if (candidate == null) return;

            AudioController.Instance.PlaySFX(candidate.Sound);
            TrackHistory(candidate.Sound);
        }

        private AmbientSoundEntry PickWeightedExcludingRecent()
        {
            var pool = new List<AmbientSoundEntry>();
            foreach (var entry in _sounds)
            {
                if (_recentHistory.Contains(entry.Sound)) continue;
                pool.Add(entry);
            }

            if (pool.Count == 0) pool = _sounds;

            float totalWeight = 0f;
            foreach (var entry in pool) totalWeight += entry.Weight;

            float roll = Random.Range(0f, totalWeight);
            float cumulative = 0f;
            foreach (var entry in pool)
            {
                cumulative += entry.Weight;
                if (roll <= cumulative) return entry;
            }

            return pool[pool.Count - 1]; 
        }

        private void TrackHistory(SoundName sound)
        {
            _recentHistory.Enqueue(sound);
            if (_recentHistory.Count > _noRepeatWindow)
                _recentHistory.Dequeue();
        }

#if UNITY_EDITOR
        [ContextMenu("Test Play Random Ambient Now")]
        private void TestPlayNow()
        {
            PlayRandomAmbient();
        }
#endif
    }
}