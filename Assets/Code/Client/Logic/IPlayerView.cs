using UnityEngine;

namespace Code.Client.Logic
{
    public interface IPlayerView
    {
        void Spawn(Vector2 position, float rotation);
        void Die();
        void Destroy();
    }
}