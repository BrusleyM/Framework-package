using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveTo : MonoBehaviour
{
    public Transform[] locations; // Array of locations to move to
    public float speed = 5f; // Speed of movement
    public float tiltAngle = 15f; // Tilt angle in degrees
    public int currentLocationIndex = 0; // Current location index
    private Vector3 targetPosition; // Current target position
    private bool destinationReached = false;

    [SerializeField]
    private GameObject lookAtTarget; // Object to look at
    public float lookAtSpeed = 2f; // Speed of looking at the target

    public float idleAmplitude = 0.5f; // Amplitude of the idle floating
    public float idleFrequency = 1f; // Frequency of the idle floating
    private float idleStartTime; // Start time for idle animation
    private Vector3 initialPosition; // Initial position for idle animation

    [SerializeField]
    private float _minDistToTarget = 5f; // Minimum distance to the lookAtTarget
    [SerializeField]
    private float maxDistToTarget = 10f; // Maximum distance to the lookAtTarget

    public Vector3 positionOffset = new Vector3(-5f, 0f, 5f); // Offset relative to the lookAtTarget

    [SerializeField]
    private Transform _zoneCentre;
    [SerializeField]
    private float _zoneRadius = 8f;
    [SerializeField]
    private float _turnThresholdDegrees = 40f;
    [SerializeField]
    private float _minimumMoveDistance = 0.5f;
    [SerializeField]
    private float _arrivalDistance = 0.15f;

    [SerializeField]
    private bool _followsLeader = true;

    private Vector3 _homePosition;
    private Vector3 _repositionTarget;
    private bool _isRepositioning;

    void Start()
    {
        initialPosition = transform.position;
        _homePosition = transform.position;
        idleStartTime = Time.time;

        if (locations.Length > 0)
        {
            targetPosition = locations[currentLocationIndex].position;
        }
        else
        {
            destinationReached = true;
        }
    }

    void Update()
    {
        if (!destinationReached)
        {
            MoveTowardsTarget();
        }
        else
        {
            MaintainDistance();
            SmoothLookAtTarget();
            IdleFloat();
        }
    }

    public void MoveTowardsTarget()
    {
        // Calculate the direction to the target position
        Vector3 direction = (targetPosition - transform.position).normalized;

        // Move towards the target position
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

        // Update target position if close enough to the current target
        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            if (currentLocationIndex < locations.Length)
                targetPosition = locations[currentLocationIndex].position;
        }

        // Handle rotation
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            Quaternion tiltRotation = Quaternion.Euler(tiltAngle, targetRotation.eulerAngles.y, targetRotation.eulerAngles.z);
            transform.rotation = Quaternion.Slerp(transform.rotation, tiltRotation, speed * Time.deltaTime);
        }

        // Check if destination is reached
        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            destinationReached = true;
            idleStartTime = Time.time; // Reset idle start time
            initialPosition = transform.position; // Reset initial position for idle
        }
        else
        {
            destinationReached = false;
        }
    }

    void SmoothLookAtTarget()
    {
        // Smoothly rotate to look at the lookAtTarget
        Vector3 direction = (lookAtTarget.transform.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, lookAtSpeed * Time.deltaTime);
    }

    void MaintainDistance()
    {
        if (!_followsLeader)
        {
            return;
        }

        if (!_isRepositioning)
        {
            BeginRepositioningWhenNeeded();
        }

        if (_isRepositioning)
        {
            MoveTowardsRepositionTarget();
        }
    }

    public void SetFollowing(bool follows)
    {
        _followsLeader = follows;
        if (!follows)
        {
            _isRepositioning = false;
        }
    }

    public void MoveZoneTo(Transform newCentre)
    {
        _zoneCentre = newCentre;
    }

    public void ReturnZoneHome()
    {
        _zoneCentre = null;
    }

    private Vector3 ZoneCentrePosition => _zoneCentre != null ? _zoneCentre.position : _homePosition;

    private Vector3 DesiredPosition()
    {
        var forwardOffset = lookAtTarget.transform.forward * positionOffset.z;
        var rightOffset = lookAtTarget.transform.right * positionOffset.x;
        var wanted = lookAtTarget.transform.position + forwardOffset + rightOffset;
        var distance = Vector3.Distance(FollowRules.Flat(wanted), FollowRules.Flat(lookAtTarget.transform.position));
        if (distance > Mathf.Epsilon)
        {
            var clamped = Mathf.Clamp(distance, _minDistToTarget, maxDistToTarget);
            var direction = FollowRules.Flat(wanted - lookAtTarget.transform.position).normalized;
            wanted = lookAtTarget.transform.position + direction * clamped;
        }

        var inZone = FollowRules.ClampToZone(wanted, ZoneCentrePosition, _zoneRadius);
        return new Vector3(inZone.x, transform.position.y, inZone.z);
    }

    private void BeginRepositioningWhenNeeded()
    {
        var leader = lookAtTarget.transform.position;
        var desired = DesiredPosition();
        var isTooFarOrTooClose = FollowRules.IsOutsideComfortBand(transform.position, leader, _minDistToTarget, maxDistToTarget);
        var hasLeaderTurnedAway = FollowRules.HasLeaderTurnedAway(transform.position, desired, leader, _turnThresholdDegrees);
        var isOutsideZone = !FollowRules.IsInsideZone(transform.position, ZoneCentrePosition, _zoneRadius);
        var isWorthMoving = Vector3.Distance(FollowRules.Flat(desired), FollowRules.Flat(transform.position)) > _minimumMoveDistance;

        if (isWorthMoving && (isTooFarOrTooClose || hasLeaderTurnedAway || isOutsideZone))
        {
            _repositionTarget = desired;
            _isRepositioning = true;
        }
    }

    private void MoveTowardsRepositionTarget()
    {
        var next = Vector3.Lerp(transform.position, _repositionTarget, speed * Time.deltaTime);
        transform.position = new Vector3(next.x, transform.position.y, next.z);
        if (Vector3.Distance(FollowRules.Flat(transform.position), FollowRules.Flat(_repositionTarget)) < _arrivalDistance)
        {
            _isRepositioning = false;
        }
    }

    void IdleFloat()
    {
        // Idle floating animation
        float elapsedTime = Time.time - idleStartTime;
        float newY = initialPosition.y + Mathf.Sin(elapsedTime * idleFrequency) * idleAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }
}