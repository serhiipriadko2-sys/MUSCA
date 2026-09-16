using UnityEngine;

namespace MUSCA.Gate3D
{
    public sealed class ReagentStation : MonoBehaviour
    {
        [SerializeField] private Reagent reagent;
        [SerializeField] private Renderer glowRenderer;
        [SerializeField] private Light stationLight;
        [SerializeField] private Color idleEmission = Color.black;
        [SerializeField] private Color activeEmission = Color.white;

        private MaterialPropertyBlock _properties;
        private bool _highlighted;

        public Reagent Reagent => reagent;

        public void Configure(Reagent value, Renderer renderer, Light light, Color idle, Color active)
        {
            EnsureProperties();
            reagent = value;
            glowRenderer = renderer;
            stationLight = light;
            idleEmission = idle;
            activeEmission = active;
            ApplyVisual();
        }

        public void SetHighlighted(bool highlighted)
        {
            if (_highlighted == highlighted)
            {
                return;
            }

            _highlighted = highlighted;
            ApplyVisual();
        }

        private void OnEnable()
        {
            EnsureProperties();
            ApplyVisual();
        }

        private void EnsureProperties()
        {
            if (_properties == null)
            {
                _properties = new MaterialPropertyBlock();
            }
        }

        private void ApplyVisual()
        {
            EnsureProperties();
            if (glowRenderer != null)
            {
                glowRenderer.GetPropertyBlock(_properties);
                Color emission = _highlighted ? activeEmission : idleEmission;
                _properties.SetColor("_EmissionColor", emission);
                glowRenderer.SetPropertyBlock(_properties);
            }

            if (stationLight != null)
            {
                stationLight.intensity = _highlighted ? 6.5f : 3f;
            }
        }
    }
}
