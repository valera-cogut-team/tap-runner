using Pool.Application;
using Pool.Domain;
using TapRunner.Presentation;

namespace TapRunner.Application
{
    /// <summary>Pool entry wrapping an obstacle view instance (Chapter 8 / 14 — pooled objects, no per-frame allocs).</summary>
    public sealed class PooledObstacleHandle : IPoolable
    {
        public TapRunnerObstacleView View { get; }

        public PooledObstacleHandle(TapRunnerObstacleView view)
        {
            View = view;
        }

        public void Reset()
        {
            if (View != null)
                View.gameObject.SetActive(false);
        }
    }
}
