using Code.Shared;
using UnityEngine;

namespace Code.Client.Logic
{
    public interface IHardpointView
    {
        void Initialize(Transform parent, Vector2Int position);
        
        void AimAt(Vector2 target);

        void CurrentRotation(float rotation);
        
        float GetRotation();
        
        void OnHardpointAction(byte action);
        
        Vector2 GetFirePosition();
        
        void Destroy();
    }
}