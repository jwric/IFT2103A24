using System;
using UnityEngine;

namespace Code.Client.Logic
{
    public class ThrusterView : MonoBehaviour
    {
        [SerializeField]
        private ParticleSystem _smokeEffect;
        [SerializeField]
        private ParticleSystem _fireEffect;

        private ParticleSystem.MinMaxCurve _defaultSmokeRate;
        private ParticleSystem.MinMaxCurve _defaultFireRate;

        private void Awake()
        {
            _defaultSmokeRate = _smokeEffect.emission.rateOverTime;
            _defaultFireRate = _fireEffect.emission.rateOverTime;
        }

        public void SetThrust(float thrustPercent)
        {
            var smokeEmission = _smokeEffect.emission;
            var fireEmission = _fireEffect.emission;

            smokeEmission.rateOverTime = _defaultSmokeRate.constant * thrustPercent;
            fireEmission.rateOverTime = _defaultFireRate.constant * thrustPercent;
        }
    }
}