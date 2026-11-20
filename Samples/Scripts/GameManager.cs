using Framework.Sequencing;
using UnityEngine;

namespace FrameworkTest
{
    public class GameManager : MonoBehaviour
    {
        private static GameManager _instance;
        public static GameManager Instance=> _instance;

        [SerializeField]
        private SequentialTask _currentSequentialTask;
        public SequentialTask CurrentSequentialTask => _currentSequentialTask;
        public GuidanceMode CurrentMode => _currentSequentialTask.Mode;

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
            try
            {
                StartSequence();
            }
            catch (System.Exception e) { 
            Debug.LogException(e);
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
            _currentSequentialTask.SetMode(mode);
        }

        public void StartSequence()
        {
            if (_currentSequentialTask != null)
            {
                _currentSequentialTask.Run();
            }
            else
            {
                Debug.LogError("No SequentialTask assigned to GameManager.");
            }
        }

        public void HintButtonPressed()
        {
            _currentSequentialTask?.RequestHint();
        }
    }
}
