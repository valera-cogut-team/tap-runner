using TapRunner.Domain;
using TapRunner.Facade;
using UnityEngine;

namespace TapRunner.Presentation
{
    public sealed class TapRunnerPlayerHitProxy : MonoBehaviour
    {
        private ITapRunnerFacade _facade;

        public void Initialize(ITapRunnerFacade facade)
        {
            _facade = facade;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_facade == null || _facade.Phase != TapRunnerGamePhase.Running)
                return;
            if (other.GetComponentInParent<TapRunnerObstacleView>() == null)
                return;
            _facade.NotifyPlayerHitObstacle();
        }
    }
}
