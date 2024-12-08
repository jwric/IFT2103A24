using System;
using UnityEngine;

namespace Code.Shared
{
    public class SpawnableObject : MonoBehaviour
    {
        private Action<SpawnableObject> _onDeathCallback;

        public void Init(Action<SpawnableObject> onDeathCallback)
        {
            _onDeathCallback = onDeathCallback;
            gameObject.SetActive(false);
        }
        
        protected void ReturnToPool()
        {
            _onDeathCallback(this);
            gameObject.SetActive(false);
        }
    }
}