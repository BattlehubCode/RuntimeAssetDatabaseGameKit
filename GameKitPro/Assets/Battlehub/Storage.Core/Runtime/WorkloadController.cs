using System.Threading.Tasks;
using UnityEngine;

namespace Battlehub.Storage
{
    public class NoDelaysWorkloadController : IWorkloadController
    {
        public ValueTask TryPostponeTask()
        {
            return default;
        }
    }

    public class WorkloadController : MonoBehaviour, IWorkloadController
    {
        private float m_currentTime;
        private float m_allotedTimePerFrame;

        private float m_targetFramerate;

        [SerializeField]
        private float m_frameBudget = 0.5f;


        public float FrameBudget
        {
            get { return m_frameBudget; }
            set
            {
                if(m_frameBudget != value)
                {
                    m_frameBudget = Mathf.Clamp01(value);
                    Init();
                }
            }
        }

        private void Awake()
        {
            Init();
        }

        private void Init()
        {
            if(Application.targetFrameRate != -1)
            {
                m_targetFramerate = Application.targetFrameRate;
            }
            else
            {
                m_targetFramerate = 30;
            }

            m_allotedTimePerFrame = 1 / m_targetFramerate;
            m_allotedTimePerFrame *= m_frameBudget;
            ReadCurrentTime();
        }

        private void ReadCurrentTime()
        {
            m_currentTime = Time.realtimeSinceStartup;
        }

        private void Update()
        {
            ReadCurrentTime();
        }

        public async ValueTask TryPostponeTask()
        {
            float elapsedTime = Time.realtimeSinceStartup - m_currentTime;
            if (elapsedTime > m_allotedTimePerFrame)
            {
                await Task.Yield();
            }
        }
    }
}

