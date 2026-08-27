using UnityEngine;

namespace SpaceSurvivors.Core
{
    /// <summary>
    /// Added automatically to every pooled instance. Lets an object return itself to its
    /// pool (<c>GetComponent&lt;PoolHandle&gt;().Despawn()</c>) without any component needing
    /// to know which <see cref="Pool"/> or <see cref="PoolManager"/> it came from
    /// (AI_Guidelines §1 — loose coupling).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PoolHandle : MonoBehaviour
    {
        private Pool _owner;

        public bool IsPooled => _owner != null;

        internal void Bind(Pool owner) => _owner = owner;

        /// <summary>Return this GameObject to its pool. No-op if it was not pooled.</summary>
        public void Despawn()
        {
            if (_owner != null) _owner.Release(gameObject);
            else gameObject.SetActive(false);
        }
    }
}
