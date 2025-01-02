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
            // reset transform
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            transform.SetParent(null);
            gameObject.SetActive(false);
        }
    }
}