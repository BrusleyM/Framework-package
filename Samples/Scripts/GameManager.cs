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

        [SerializeField, FormerlySerializedAs("_guidedSequence"), FormerlySerializedAs("_currentSequentialTask")]
        private SequentialTask _sequence;
        [SerializeField]
        private MoveTo _instructor;
        [SerializeField]
        private AudioSource _instructorVoice;
        [SerializeField]
        private Transform _guidedZoneCentre;
        [SerializeField]
        private Transform _unguidedZoneCentre;

        private readonly bool[] _playerIsInside = new bool[2];

        public GuidanceMode CurrentMode => _sequence.Mode;
        public SequentialTask CurrentSequentialTask => _sequence;

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
        }

        private void Start()
        {
            if (_instructor != null)
            {
                _instructor.SetFollowing(false);
            }
        }

        private void OnEnable()
        {
            if (_sequence != null)
            {
                _sequence.ModeChanged += OnModeChanged;
            }
        }

        private void OnDisable()
        {
            if (_sequence != null)
            {
                _sequence.ModeChanged -= OnModeChanged;
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

        public void SetGameMode(GuidanceMode mode)
        {
            _sequence.SetMode(mode);
        }

        public void StartSequence()
        {
            _sequence.RunOrResume();
        }

        public void PlayerEnteredSection(GuidanceMode section)
        {
            _playerIsInside[(int)section] = true;
            if (section == _sequence.Mode)
            {
                Activate(section);
            }
        }

        public void PlayerLeftSection(GuidanceMode section)
        {
            _playerIsInside[(int)section] = false;
            if (section == _sequence.Mode)
            {
                Deactivate();
            }
        }

        public void HintButtonPressed()
        {
            if (_sequence.CurrentTask is FreeGroupTask freeGroup)
            {
                freeGroup.RequestHint();
            }
        }

        private void OnModeChanged(GuidanceMode newMode)
        {
            if (_instructor != null)
            {
                _instructor.SetFollowing(false);
            }

            if (_instructorVoice != null)
            {
                _instructorVoice.Stop();
                _instructorVoice.clip = null;
            }

            if (_playerIsInside[(int)newMode])
            {
                Activate(newMode);
            }
        }

        private void Activate(GuidanceMode mode)
        {
            if (_instructor != null)
            {
                _instructor.MoveZoneTo(mode == GuidanceMode.Guided ? _guidedZoneCentre : _unguidedZoneCentre);
                _instructor.SetFollowing(true);
            }

            _sequence.RunOrResume();
        }

        private void Deactivate()
        {
            if (_instructor != null)
            {
                _instructor.SetFollowing(false);
            }

            _sequence.Pause();
        }
    }
}
