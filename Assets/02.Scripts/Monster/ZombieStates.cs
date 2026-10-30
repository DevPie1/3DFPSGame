using UnityEngine;

// 상태 객체는 좀비마다 한 번 만들고 전환할 때 재사용한다.
internal abstract class ZombieStateBase
{
    protected readonly Zombie Owner;
    protected float Elapsed;

    protected ZombieStateBase(Zombie owner) => Owner = owner;
    public virtual void Enter() => Elapsed = 0f;
    public virtual void Exit() { }
    public abstract void Tick(float deltaTime);
}

internal sealed class ZombieIdleState : ZombieStateBase
{
    public ZombieIdleState(Zombie owner) : base(owner) { }
    public override void Tick(float deltaTime)
    {
        Owner.StandStill();
        Owner.TryDetectPlayer();
    }
}

internal sealed class ZombiePatrolState : ZombieStateBase
{
    private int _pointIndex;
    private bool _waiting;

    public ZombiePatrolState(Zombie owner) : base(owner) { }
    public override void Enter()
    {
        base.Enter();
        _pointIndex = 0;
        _waiting = false;
    }

    public override void Tick(float deltaTime)
    {
        if (Owner.TryDetectPlayer()) return;
        Transform[] points = Owner.PatrolPoints;
        if (points == null || points.Length == 0)
        {
            Owner.ChangeState(ZombieState.Roam);
            return;
        }

        int checkedPoints = 0;
        while (points[_pointIndex] == null && checkedPoints < points.Length)
        {
            _pointIndex = (_pointIndex + 1) % points.Length;
            checkedPoints++;
        }

        if (checkedPoints == points.Length)
        {
            Owner.ChangeState(ZombieState.Roam);
            return;
        }

        Transform point = points[_pointIndex];
        if (!_waiting && Owner.MoveTowards(point.position))
        {
            _waiting = true;
            Elapsed = 0f;
        }

        if (_waiting)
        {
            Owner.StandStill();
            Elapsed += deltaTime;
            if (Elapsed >= Owner.WaypointWaitTime)
            {
                _pointIndex = (_pointIndex + 1) % points.Length;
                _waiting = false;
            }
        }
    }
}

internal sealed class ZombieRoamState : ZombieStateBase
{
    private Vector3 _destination;
    private bool _waiting;

    public ZombieRoamState(Zombie owner) : base(owner) { }
    public override void Enter()
    {
        base.Enter();
        _waiting = false;
        _destination = Owner.ChooseRoamDestination();
    }

    public override void Tick(float deltaTime)
    {
        if (Owner.TryDetectPlayer()) return;
        Elapsed += deltaTime;
        if (_waiting)
        {
            Owner.StandStill();
            if (Elapsed >= Owner.WaypointWaitTime) Enter();
            return;
        }

        // 장애물에 막혀도 같은 목적지를 계속 밀지 않도록 다시 선택한다.
        if (Owner.MoveTowards(_destination) || Elapsed >= Owner.RoamTravelTime)
        {
            _waiting = true;
            Elapsed = 0f;
        }
    }
}

internal sealed class ZombieTraceState : ZombieStateBase
{
    public ZombieTraceState(Zombie owner) : base(owner) { }
    public override void Tick(float deltaTime)
    {
        // 점프 중에는 상태 전환/이동을 하지 않는다.
        if (Owner.IsJumping) return;

        if (!Owner.HasPlayer || Owner.IsOutsideHomeRange)
        {
            Owner.ChangeState(ZombieState.Comeback);
            return;
        }

        if (Owner.IsPlayerInAttackRange)
        {
            Owner.ChangeState(ZombieState.Attack);
            return;
        }

        if (Owner.TryGetPlayerNavMeshPosition(out Vector3 destination))
        {
            Owner.MoveTowards(destination);
        }
    }
}

internal sealed class ZombieComebackState : ZombieStateBase
{
    public ZombieComebackState(Zombie owner) : base(owner) { }
    public override void Tick(float deltaTime)
    {
        if (Owner.MoveTowards(Owner.HomePosition)) Owner.GoToDefaultState();
    }
}

internal sealed class ZombieAttackState : ZombieStateBase
{
    public ZombieAttackState(Zombie owner) : base(owner) { }
    public override void Tick(float deltaTime)
    {
        if (Owner.IsJumping) return;

        if (Owner.IsAttackCommitted)
        {
            Owner.StandStill();
            Elapsed += deltaTime;
            return;
        }

        if (!Owner.IsPlayerInAttackRange)
        {
            Owner.ChangeState(Owner.HasPlayer ? ZombieState.Trace : ZombieState.Comeback);
            return;
        }

        Owner.StandStill();
        Elapsed += deltaTime;
        if (Elapsed >= Owner.AttackInterval)
        {
            Owner.AttackPlayer();
            Elapsed = 0f;
        }
    }
}

internal sealed class ZombieHitState : ZombieStateBase
{
    public ZombieHitState(Zombie owner) : base(owner) { }
    public override void Tick(float deltaTime)
    {
        Owner.StandStill();
        Elapsed += deltaTime;
        if (Elapsed >= Owner.HitDuration)
        {
            Owner.ChangeState(Owner.HasPlayer ? ZombieState.Trace : ZombieState.Comeback);
        }
    }
}

internal sealed class ZombieDeathState : ZombieStateBase
{
    public ZombieDeathState(Zombie owner) : base(owner) { }
    public override void Enter()
    {
        base.Enter();
        Owner.EnterDeath();
    }

    public override void Tick(float deltaTime)
    {
        Elapsed += deltaTime;
        if (Elapsed >= Owner.DeathDelay) Owner.ReturnToPool();
    }
}
