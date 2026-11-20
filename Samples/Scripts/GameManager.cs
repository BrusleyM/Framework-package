using Framework.Sequencing;
using Framework.Tasks;
using UnityEngine;
using UnityEngine.Serialization;

namespace FrameworkTest
{
    public class GameManager : MonoBehaviour
    {
        private static GameManager _instance;
        public static GameManager Instance=> _instance;
        public enum GameMode { Guided, LetMeTry }
        [SerializeField]
        private GameMode _currentMode = GameMode.Guided;
        public GameMode CurrentMode => _currentMode;

        [SerializeField, FormerlySerializedAs("_currentSequentialTask")]
        private SequentialTask _guidedSequence;
        [SerializeField]
        private SequentialTask _unguidedSequence;
        [SerializeField]
        private MoveTo _instructor;
        [SerializeField]
        private AudioSource _instructorVoice;
        [SerializeField]
        private Transform _guidedZoneCentre;
        [SerializeField]
        private Transform _unguidedZoneCentre;

        private readonly bool[] _playerIsInside = new bool[2];
        private GameMode _activeMode;

        public SequentialTask CurrentSequentialTask => SequenceFor(_currentMode);

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this);
            }
            else
            {
                _instance = this;
            }

            _activeMode = _currentMode;
        }

        private void Start()
        {
            if (_instructor != null)
            {
                _instructor.SetFollowing(false);
            }
        }

        private void OnValidate()
        {
            if (Application.isPlaying && _instance == this && _currentMode != _activeMode)
            {
                ApplyModeChange();
            }
        }

#if ENABLE_LEGACY_INPUT_MANAGER
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.H))
            {
                HintButtonPressed();
            }
        }
#endif

        public void SetGameMode(GameMode mode)
        {
            _currentMode = mode;
            if (_currentMode != _activeMode)
            {
                ApplyModeChange();
            }
        }

        public void StartSequence()
        {
            StartOrResume(SequenceFor(_currentMode));
        }

        public void PlayerEnteredSection(GameMode section)
        {
            _playerIsInside[(int)section] = true;
            if (section == _activeMode)
            {
                Activate(section);
            }
        }

        public void PlayerLeftSection(GameMode section)
        {
            _playerIsInside[(int)section] = false;
            if (section == _activeMode)
            {
                Deactivate(section);
            }
        }

        public void HintButtonPressed()
        {
            var sequence = SequenceFor(_activeMode);
            if (sequence != null && sequence.CurrentTask is FreeGroupTask freeGroup)
            {
                freeGroup.RequestHint();
            }
        }

        private void ApplyModeChange()
        {
            Deactivate(_activeMode);
            if (_instructorVoice != null)
            {
                _instructorVoice.Stop();
                _instructorVoice.clip = null;
            }

            _activeMode = _currentMode;
            if (_playerIsInside[(int)_activeMode])
            {
                Activate(_activeMode);
            }
        }

        private void Activate(GameMode mode)
        {
            if (_instructor != null)
            {
                _instructor.MoveZoneTo(ZoneCentreFor(mode));
                _instructor.SetFollowing(true);
            }

            StartOrResume(SequenceFor(mode));
        }

        private void Deactivate(GameMode mode)
        {
            if (_instructor != null)
            {
                _instructor.SetFollowing(false);
            }

            SequenceFor(mode)?.Pause();
        }

        private static void StartOrResume(SequentialTask sequence)
        {
            if (sequence == null)
            {
                Debug.LogError("No SequentialTask assigned to GameManager for this mode.");
            }
            else if (!sequence.HasRun)
            {
                sequence.Run();
            }
            else
            {
                sequence.Resume();
            }
        }

        private SequentialTask SequenceFor(GameMode mode)
        {
            return mode == GameMode.Guided ? _guidedSequence : _unguidedSequence;
        }

        private Transform ZoneCentreFor(GameMode mode)
        {
            return mode == GameMode.Guided ? _guidedZoneCentre : _unguidedZoneCentre;
        }
    }
}
