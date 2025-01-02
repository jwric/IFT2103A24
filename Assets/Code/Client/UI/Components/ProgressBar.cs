using System;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Client.UI.Components
{
    [RequireComponent(typeof(RectTransform))]
    public class ProgressBar : MonoBehaviour
    {
        private RectTransform _containerRectTransform;
        [SerializeField]
        private Image _progressBar;
        private float _progress;
        
        private float _targetProgress;
        
        private void Awake()
        {
            _containerRectTransform = GetComponent<RectTransform>();
        }

        public void SetProgress(float progress)
        {
            _targetProgress = Mathf.Clamp01(progress);
        }

        public void Update()
        {
            if (Math.Abs(_progress - _targetProgress) > 0.01f)
            {
                _progress = Mathf.Lerp(_progress, _targetProgress, Time.deltaTime * 5);
                _progressBar.fillAmount = _progress;
            }
            
            _progressBar.fillAmount = _progress;
        }
    }
}