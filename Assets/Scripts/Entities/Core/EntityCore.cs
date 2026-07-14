using UnityEngine;

/// <summary>
/// Provides the shared movement, perception, and navigation behavior of an entity.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public abstract class EntityCore : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private EntityMovementController _movement;

    [Header("Perception")]
    [SerializeField] private EntityPerceptionController _perception;

    [Header("Navigation")]
    [SerializeField] private EntityNavigationController _navigation;

    public EntityData Data { get; protected set; }
    public virtual EntityRuntimeData RuntimeData => null;
    protected EntityMovementController Movement => _movement;
    protected EntityPerceptionController Perception => _perception;
    protected EntityNavigationController Navigation => _navigation;

    public virtual void Initialize(EntityData data)
    {
    }

    public virtual bool RequestState(StateId stateId, bool forceReset = false)
    {
        return false;
    }

    public void SetTarget(Transform newTarget)
    {
        Perception.SetTarget(newTarget);
    }

    public bool HasTarget => Perception.HasTarget;
    public Transform Target => Perception.Target;

    public bool CanSeeTarget(out Vector2 dirToTarget)
    {
        return Perception.CanSeeTarget(out dirToTarget);
    }

    public bool ArrivedAt(Vector2 worldTarget)
    {
        return Movement.ArrivedAt(worldTarget);
    }

    public virtual void StopMovement()
    {
        Navigation.Stop();
        HandleMovementDirection(Vector2.zero);
    }

    protected bool CanUsePathPosition(Vector2 worldPosition, Collider2D movementCollider)
    {
        return Navigation.CanUsePathPosition(worldPosition, movementCollider);
    }

    /// <summary>
    /// Requests a path to the target and passes the resulting direction to the entity movement behavior.
    /// </summary>
    protected void MoveToUsingPath(
        Vector2 worldTarget,
        Collider2D movementCollider,
        float repathIntervalSeconds,
        float pathNodeStep,
        int maxPathIterations,
        bool hasDetourTarget = false,
        Vector2 detourTarget = default)
    {
        var movementDirection = Navigation.MoveTo(
            worldTarget: worldTarget,
            movementCollider: movementCollider,
            repathIntervalSeconds: repathIntervalSeconds,
            pathNodeStep: pathNodeStep,
            maxPathIterations: maxPathIterations,
            hasDetourTarget: hasDetourTarget,
            detourTarget: detourTarget
        );

        HandleMovementDirection(movementDirection);
    }

    protected virtual void HandleMovementDirection(Vector2 desiredDirection)
    {
    }
}
