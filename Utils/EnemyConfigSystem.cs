using UnityEngine;
using System;
using System.Collections.Generic;
using BepInEx.Configuration;

namespace PaintedUtils
{
    public abstract class BaseEnemyConfig
    {
        // Base configuration options that all enemies should have
        public ConfigEntry<float> Health { get; protected set; }
        public ConfigEntry<float> SpeedMultiplier { get; protected set; }
        public ConfigEntry<int> PlayerDamage { get; protected set; }
        public ConfigEntry<float> PlayerDamageCooldown { get; protected set; }
        public ConfigEntry<float> PlayerTumbleForce { get; protected set; }
        public ConfigEntry<float> PhysHitForce { get; protected set; }
        public ConfigEntry<float> PhysHitTorque { get; protected set; }
        public ConfigEntry<bool> PhysDestroy { get; protected set; }

        protected abstract void InitializeConfig(ConfigFile config);
    }

    public class EnemyConfigController : MonoBehaviour
    {
        // References to required components
        protected ItemDropper itemDropper;
        protected EnemyHealth enemyHealth;
        protected UnityEngine.AI.NavMeshAgent navMeshAgent;
        protected List<HurtCollider> hurtColliders = new List<HurtCollider>();
        protected EnemyRigidbody enemyRigidbody;

        protected BaseEnemyConfig config;

        protected virtual void Awake()
        {
            // Get references to all required components, including inactive ones
            itemDropper = GetComponentInChildren<ItemDropper>(true);
            enemyHealth = GetComponentInChildren<EnemyHealth>(true);
            enemyRigidbody = GetComponentInChildren<EnemyRigidbody>(true);
            navMeshAgent = GetComponentInChildren<UnityEngine.AI.NavMeshAgent>(true);

            // Find all HurtColliders in the hierarchy
            hurtColliders.AddRange(GetComponentsInChildren<HurtCollider>(true));

            ValidateComponents();

            // Subscribe to events
            if (enemyHealth != null)
            {
                enemyHealth.onDeath.AddListener(HandleDeath);
            }

            // Apply initial configurations
            ApplyConfigurations();
        }

        protected virtual void ValidateComponents()
        {
            if (itemDropper == null)
            {
                Debug.LogError($"ItemDropper component not found on {gameObject.name} or its children!");
                return;
            }

            if (enemyHealth == null)
            {
                Debug.LogError($"EnemyHealth component not found on {gameObject.name} or its children!");
                return;
            }

            if (hurtColliders.Count == 0)
            {
                Debug.LogError($"No HurtCollider components found on {gameObject.name} or its children!");
                return;
            }

            if (enemyRigidbody == null)
            {
                Debug.LogError($"EnemyRigidbody component not found on {gameObject.name} or its children!");
                return;
            }
        }

        protected virtual void OnDestroy()
        {
            // Unsubscribe from events to prevent memory leaks
            if (enemyHealth != null)
            {
                enemyHealth.onDeath.RemoveListener(HandleDeath);
            }
        }

        protected virtual void HandleDeath()
        {
            // Handle item drops - removed since we're using drop chance directly in ItemDropper
        }

        protected virtual void ApplyConfigurations()
        {
            if (config == null) return;

            // Apply HurtCollider configurations to all hurt colliders
            foreach (var hurtCollider in hurtColliders)
            {
                hurtCollider.playerDamage = config.PlayerDamage.Value;
                hurtCollider.playerDamageCooldown = config.PlayerDamageCooldown.Value;
                hurtCollider.playerTumbleForce = config.PlayerTumbleForce.Value;
                hurtCollider.physHitForce = config.PhysHitForce.Value;
                hurtCollider.physHitTorque = config.PhysHitTorque.Value;
                hurtCollider.physDestroy = config.PhysDestroy.Value;
            }

            // Apply health configuration
            enemyHealth.health = (int)config.Health.Value;

            // Apply movement speed configurations
            enemyRigidbody.positionSpeedIdle = enemyRigidbody.positionSpeedIdle * config.SpeedMultiplier.Value;
            enemyRigidbody.positionSpeedChase = enemyRigidbody.positionSpeedChase * config.SpeedMultiplier.Value;

            // Apply NavMeshAgent configurations
            navMeshAgent.speed = navMeshAgent.speed * config.SpeedMultiplier.Value;
        }

        // This method can be called to reapply configurations if they change at runtime
        public virtual void RefreshConfigurations()
        {
            ApplyConfigurations();
        }
    }
} 