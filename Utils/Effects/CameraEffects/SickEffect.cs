using UnityEngine;
using REPOLib.Modules;

namespace PaintedUtils
{
    public class SickEffect : PaintedPostProcessingEffect
    {
        private float rotationSpeed = 0.5f;
        private float rotationAmount = 2f;
        private float currentRotation = 0f;
        private bool rotatingRight = true;
        private float originalCameraNoise;
        private float originalCameraShake;

        public SickEffect(float duration = 10f, float intensity = 1f)
        {
            this.duration = duration;
            this.intensity = intensity;
            this.isActive = false;
        }

        public override void ApplyEffect(PlayerAvatar player)
        {
            if (!player.isLocal) return;
            
            isActive = true;
            Debug.Log("Applying Sick Effect to " + player.name);
            
            // Store original camera settings
            originalCameraNoise = GameplayManager.instance.cameraNoise;
            originalCameraShake = GameplayManager.instance.cameraShake;
            
            // Override camera settings for sick effect
            GameplayManager.instance.OverrideCameraNoise(0.5f * intensity, duration);
            GameplayManager.instance.OverrideCameraShake(0.3f * intensity, duration);
            
            // Apply post-processing through the game's systems
            if (PostProcessing.Instance != null)
            {
                PostProcessing.Instance.colorGrading.tint.value = 20f * intensity; // Green tint
                PostProcessing.Instance.colorGrading.tint.overrideState = true;
                
                PostProcessing.Instance.chromaticAberration.intensity.value = 0.5f * intensity;
                PostProcessing.Instance.chromaticAberration.intensity.overrideState = true;
            }
        }

        public override void RemoveEffect(PlayerAvatar player)
        {
            if (!player.isLocal) return;
            
            isActive = false;
            
            // Restore original camera settings
            GameplayManager.instance.OverrideCameraNoise(originalCameraNoise, 0.5f);
            GameplayManager.instance.OverrideCameraShake(originalCameraShake, 0.5f);
            
            // Reset post-processing
            if (PostProcessing.Instance != null)
            {
                PostProcessing.Instance.colorGrading.tint.overrideState = false;
                PostProcessing.Instance.chromaticAberration.intensity.overrideState = false;
            }
            
            // Reset camera rotation through GameDirector
            if (GameDirector.instance != null && GameDirector.instance.MainCamera != null)
            {
                GameDirector.instance.MainCamera.transform.localRotation = Quaternion.identity;
            }
        }

        public override void UpdateEffect(PlayerAvatar player)
        {
            if (!isActive || !player.isLocal) return;

            // Update camera rotation through GameDirector
            if (GameDirector.instance != null && GameDirector.instance.MainCamera != null)
            {
                // Update rotation
                if (rotatingRight)
                {
                    currentRotation += rotationSpeed * Time.deltaTime;
                    if (currentRotation >= rotationAmount)
                    {
                        rotatingRight = false;
                    }
                }
                else
                {
                    currentRotation -= rotationSpeed * Time.deltaTime;
                    if (currentRotation <= -rotationAmount)
                    {
                        rotatingRight = true;
                    }
                }

                // Apply rotation through GameDirector's camera
                GameDirector.instance.MainCamera.transform.localRotation = 
                    Quaternion.Euler(0f, 0f, currentRotation * intensity);
            }
        }
    }
} 