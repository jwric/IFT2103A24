using Code.Shared;
using UnityEngine;

namespace Code.Client.Logic
{
    public interface IHardpointView
    {
        void Initialize(Transform parent, Vector2Int position, ObjectPoolManager objectPoolManager);
        
        void AimAt(Vector2 target, float dt);

        void CurrentRotation(float rotation);
        
        float GetRotation();
        
        void OnHardpointAction(byte action);
        
        void SpawnFire(Vector2 to);
        
        Vector2 GetFirePosition();
        
        void Destroy();
    }
}