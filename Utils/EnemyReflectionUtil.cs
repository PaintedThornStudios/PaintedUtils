using UnityEngine;
using System;
using System.Reflection;

namespace PaintedThornStudios.PaintedUtils;

/// <summary>
/// Utility class for accessing internal Enemy class members through reflection.
/// Provides a clean interface to access private fields without using BepInEx Publicizer.
/// </summary>
public class EnemyReflectionUtil
{
    #region Enemy Component Access
    /// <summary>
    /// Gets the NavMeshAgent component from an Enemy instance
    /// </summary>
    public static EnemyNavMeshAgent GetEnemyNavMeshAgent(Enemy enemy)
    {
        Type enemyType = enemy.GetType();
        FieldInfo agentField = enemyType.GetField("NavMeshAgent", BindingFlags.NonPublic | BindingFlags.Instance);

        if (agentField != null)
        {
            return (EnemyNavMeshAgent) agentField.GetValue(enemy);
        }
        else
        {
            Debug.LogError("NavMeshAgent field not found!");
            return null;
        }
    }

    /// <summary>
    /// Gets the Rigidbody component from an Enemy instance
    /// </summary>
    public static EnemyRigidbody GetEnemyRigidbody(Enemy enemy)
    {
        Type enemyType = enemy.GetType();
        FieldInfo rigidbodyField = enemyType.GetField("Rigidbody", BindingFlags.NonPublic | BindingFlags.Instance);

        if (rigidbodyField != null)
        {
            return (EnemyRigidbody) rigidbodyField.GetValue(enemy);
        }
        else
        {
            Debug.LogError("Rigidbody field not found!");
            return null;
        }
    }

    /// <summary>
    /// Gets the EnemyParent component from an Enemy instance
    /// </summary>
    public static EnemyParent GetEnemyParent(Enemy enemy)
    {
        Type enemyType = enemy.GetType();
        FieldInfo parentField = enemyType.GetField("EnemyParent", BindingFlags.NonPublic | BindingFlags.Instance);

        if (parentField != null)
        {
            return (EnemyParent) parentField.GetValue(enemy);
        }
        else
        {
            Debug.LogError("EnemyParent field not found!");
            return null;
        }
    }

    /// <summary>
    /// Gets the Vision component from an Enemy instance
    /// </summary>
    public static EnemyVision GetEnemyVision(Enemy enemy)
    {
        Type enemyType = enemy.GetType();
        FieldInfo visionField = enemyType.GetField("Vision", BindingFlags.NonPublic | BindingFlags.Instance);

        if (visionField != null)
        {
            return (EnemyVision) visionField.GetValue(enemy);
        }
        else
        {
            Debug.LogError("Vision field not found!");
            return null;
        }
    }

    /// <summary>
    /// Gets the StateInvestigate component from an Enemy instance
    /// </summary>
    public static EnemyStateInvestigate GetEnemyStateInvestigate(Enemy enemy)
    {
        Type enemyType = enemy.GetType();
        FieldInfo investigateField = enemyType.GetField("StateInvestigate", BindingFlags.NonPublic | BindingFlags.Instance);

        if (investigateField != null)
        {
            return (EnemyStateInvestigate) investigateField.GetValue(enemy);
        }
        else
        {
            Debug.LogError("StateInvestigate field not found!");
            return null;
        }
    }

    #endregion

    #region Enemy State Checks
    /// <summary>
    /// Checks if an enemy is currently jumping
    /// </summary>
    public static bool IsEnemyJumping(Enemy enemy)
    {
        Type enemyType = enemy.GetType();
        FieldInfo jumpField = enemyType.GetField("Jump", BindingFlags.NonPublic | BindingFlags.Instance);

        if (jumpField != null)
        {
            Type jumpType = jumpField.FieldType;
            FieldInfo jumpingField = jumpType.GetField("jumping", BindingFlags.NonPublic | BindingFlags.Instance);

            if (jumpingField != null)
            {
                return (bool) jumpingField.GetValue(jumpField.GetValue(enemy));
            }
            else
            {
                Debug.LogError("Jumping field not found!");
                return false;
            }
        }
        else
        {
            Debug.LogError("Jump field not found!");
            return false;
        }
    }

    /// <summary>
    /// Checks if a player is currently disabled
    /// </summary>
    public static bool IsPlayerDisabled(PlayerAvatar playerTarget)
    {
        Type playerType = playerTarget.GetType();
        FieldInfo disabledField = playerType.GetField("isDisabled", BindingFlags.NonPublic | BindingFlags.Instance);

        if (disabledField != null)
        {
            return (bool) disabledField.GetValue(playerTarget);
        }
        else
        {
            Debug.LogError("isDisabled field not found!");
            return false;
        }
    }
    #endregion

    #region Component Property Access
    /// <summary>
    /// Gets the velocity of a NavMeshAgent
    /// </summary>
    public static Vector3 GetAgentVelocity(EnemyNavMeshAgent agent)
    {
        Type agentType = agent.GetType();
        FieldInfo velocityField = agentType.GetField("AgentVelocity", BindingFlags.NonPublic | BindingFlags.Instance);

        if (velocityField != null)
        {
            return (Vector3) velocityField.GetValue(agent);
        }
        else
        {
            Debug.LogError("AgentVelocity field not found!");
            return Vector3.zero;
        }
    }

    /// <summary>
    /// Gets the position where an enemy was triggered to investigate
    /// </summary>
    public static Vector3 GetOnInvestigateTriggeredPosition(EnemyStateInvestigate investigate)
    {
        Type visionType = investigate.GetType();
        FieldInfo triggeredPositionField = visionType.GetField("onInvestigateTriggeredPosition", BindingFlags.NonPublic | BindingFlags.Instance);

        if (triggeredPositionField != null)
        {
            return (Vector3) triggeredPositionField.GetValue(investigate);
        }
        else
        {
            Debug.LogError("onInvestigateTriggeredPosition field not found!");
            return Vector3.zero;
        }
    }

    /// <summary>
    /// Gets the player that triggered an enemy's vision
    /// </summary>
    public static PlayerAvatar GetVisionTriggeredPlayer(EnemyVision vision)
    {
        Type visionType = vision.GetType();
        FieldInfo triggeredPlayerField = visionType.GetField("onVisionTriggeredPlayer", BindingFlags.NonPublic | BindingFlags.Instance);

        if (triggeredPlayerField != null)
        {
            return (PlayerAvatar) triggeredPlayerField.GetValue(vision);
        }
        else
        {
            Debug.LogError("onVisionTriggeredPlayer field not found!");
            return null;
        }
    }

    /// <summary>
    /// Gets the not moving timer from an enemy's rigidbody
    /// </summary>
    public static float GetNotMovingTimer(EnemyRigidbody rb)
    {
        Type rbType = rb.GetType();
        FieldInfo timerField = rbType.GetField("notMovingTimer", BindingFlags.NonPublic | BindingFlags.Instance);

        if (timerField != null)
        {
            return (float) timerField.GetValue(rb);
        }
        else
        {
            Debug.LogError("NotMovingTimer field not found!");
            return 0f;
        }
    }

    /// <summary>
    /// Sets the not moving timer on an enemy's rigidbody
    /// </summary>
    public static void SetNotMovingTimer(EnemyRigidbody rb, float value)
    {
        Type rbType = rb.GetType();
        FieldInfo timerField = rbType.GetField("notMovingTimer", BindingFlags.NonPublic | BindingFlags.Instance);

        if (timerField != null)
        {
            timerField.SetValue(rb, value);
        }
        else
        {
            Debug.LogError("NotMovingTimer field not found!");
        }
    }
    #endregion
} 