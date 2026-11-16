using Coimbra;
using FishNet;
using FishNet.Component.Animating;
using SS3D.Core;
using SS3D.Systems.Furniture.Disposal;
using SS3D.Systems.IdAccess;
using SS3D.Systems.Inventory.Items;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Furniture
{
    /// <summary>
    /// Disposal network terminal. The untagged/main outlet (<see cref="TargetDepartment"/> ==
    /// <see cref="Department.None"/>) can eject unclaimed arrivals into space after a grace window
    /// when <see cref="_spaceEjectionPoint"/> or an <see cref="IDisposalSweepable"/> is wired
    /// (design doc §6). Until then, main-outlet arrivals sit for pickup like tagged outlets.
    /// </summary>
    public class DisposalOutlet : MonoBehaviour, IDisposalElement
    {
        private const float ClaimProximityMeters = 2.5f;

        private static readonly int OpenStateHash = Animator.StringToHash("DisposalOutletOpen");

        [SerializeField]
        [Tooltip("Department.None marks this as the main/untagged outlet — the one with space ejection.")]
        private Department _targetDepartment = Department.None;

        [SerializeField]
        [Tooltip("How long an unclaimed item sits at the main outlet before ejection. Balancing value, left unset for now (design doc §12).")]
        private float _graceWindowSeconds = 30f;

        [SerializeField]
        [Tooltip("How far in front of the outlet (along its facing) arrivals are spat out.")]
        private float _spitDistance = 0.85f;

        [SerializeField]
        [Tooltip("Delay after arrival before spitting the item, so the door open animation can play first.")]
        private float _spitDelaySeconds = 1f;

        [SerializeField]
        private Transform _spaceEjectionPoint;

        [SerializeField]
        private NetworkAnimator _networkAnimator;

        private readonly List<PendingArrival> _pendingArrivals = new();
        private readonly List<PendingArrival> _pendingSpits = new();

        public GameObject GameObject => gameObject;

        public Department TargetDepartment => _targetDepartment;

        public bool IsMainOutlet => _targetDepartment == Department.None;

        private void Update()
        {
            if (!InstanceFinder.IsServer)
            {
                return;
            }

            TickPendingSpits();
            TickGraceWindow();
        }

        private void TickPendingSpits()
        {
            if (_pendingSpits.Count == 0)
            {
                return;
            }

            for (int i = _pendingSpits.Count - 1; i >= 0; i--)
            {
                PendingArrival pending = _pendingSpits[i];
                pending.RemainingSeconds -= Time.deltaTime;

                if (pending.RemainingSeconds > 0f)
                {
                    _pendingSpits[i] = pending;
                    continue;
                }

                _pendingSpits.RemoveAt(i);
                FinishArrival(pending.Item);
            }
        }

        private void TickGraceWindow()
        {
            // Grace / eject is server-authoritative; only the main outlet schedules these.
            if (!IsMainOutlet || _pendingArrivals.Count == 0)
            {
                return;
            }

            for (int i = _pendingArrivals.Count - 1; i >= 0; i--)
            {
                PendingArrival arrival = _pendingArrivals[i];
                arrival.RemainingSeconds -= Time.deltaTime;

                if (arrival.RemainingSeconds > 0f)
                {
                    _pendingArrivals[i] = arrival;
                    continue;
                }

                _pendingArrivals.RemoveAt(i);
                ResolveUnclaimed(arrival.Item);
            }
        }

        /// <summary>
        /// Called by the disposal subsystem when a capsule arrives at this outlet.
        /// </summary>
        public void OnItemArrived(Item item)
        {
            if (item == null)
            {
                return;
            }

            // Stay frozen inside the outlet until spit delay elapses so the doors can open first.
            item.transform.position = transform.position;
            PlayOpenAnimation();
            _pendingSpits.Add(new PendingArrival(item, _spitDelaySeconds));
        }

        private void FinishArrival(Item item)
        {
            if (item == null)
            {
                return;
            }

            if (SubSystems.TryGet(out DisposalSubSystem disposalSubSystem))
            {
                disposalSubSystem.RevealItem(item);
            }
            else
            {
                item.SetVisibility(true);
            }

            item.Unfreeze();
            SpitItemOut(item);

            // Main outlet: schedule space-eject / Cargo sweep after the grace window.
            // Tagged department outlets: item sits for pickup, no timer (§6).
            // Skip the timer when nothing can claim or eject yet — otherwise grace expiry
            // despawned in place and items looked like they vanished after a few seconds.
            if (IsMainOutlet && CanResolveUnclaimed())
            {
                _pendingArrivals.Add(new PendingArrival(item, _graceWindowSeconds));
            }
        }

        private bool CanResolveUnclaimed()
        {
            if (_spaceEjectionPoint != null)
            {
                return true;
            }

            foreach (IDisposalSweepable sweepable in GetComponents<IDisposalSweepable>())
            {
                if (sweepable != null)
                {
                    return true;
                }
            }

            return false;
        }

        private void PlayOpenAnimation()
        {
            if (_networkAnimator == null)
            {
                _networkAnimator = GetComponent<NetworkAnimator>();
            }

            if (_networkAnimator != null)
            {
                _networkAnimator.Play(OpenStateHash, 0, 0f);
                return;
            }

            if (TryGetComponent(out Animator animator))
            {
                animator.Play(OpenStateHash, 0, 0f);
            }
        }

        /// <summary>
        /// Places the item just in front of the outlet along its facing direction (tile rotation).
        /// </summary>
        private void SpitItemOut(Item item)
        {
            Vector3 spitPosition = transform.position + GetFacingDirection() * _spitDistance;
            Quaternion spitRotation = ItemPlacement.WorldRotation(item, ItemPlacement.YawOf(GetFacingDirection()));
            item.transform.SetPositionAndRotation(spitPosition, spitRotation);
        }

        private Vector3 GetFacingDirection()
        {
            // Tile objects are yaw-rotated to their Direction; flatten in case of slight pitch.
            Vector3 facing = transform.forward;
            facing.y = 0f;
            if (facing.sqrMagnitude < 0.0001f)
            {
                return Vector3.forward;
            }

            return facing.normalized;
        }

        private void ResolveUnclaimed(Item item)
        {
            if (!StillUnclaimedAtOutlet(item))
            {
                return;
            }

            foreach (IDisposalSweepable sweepable in GetComponents<IDisposalSweepable>())
            {
                if (sweepable.TrySweep(this, item))
                {
                    return;
                }
            }

            EjectIntoSpace(item);
        }

        /// <summary>
        /// True while the arrival is still a world item near this outlet. Picked-up / moved items
        /// must not be teleported or despawned when the grace window expires.
        /// </summary>
        private bool StillUnclaimedAtOutlet(Item item)
        {
            if (item == null)
            {
                return false;
            }

            if (item.IsInContainer())
            {
                return false;
            }

            float distance = Vector3.Distance(item.transform.position, transform.position);
            return distance <= ClaimProximityMeters;
        }

        private void EjectIntoSpace(Item item)
        {
            // Design §6 wants an honest discard into space. Until a vacuum ejection point
            // (or Cargo sweep) is wired, do not despawn at the outlet — that reads as the
            // item disappearing for no reason. Leave it for pickup instead.
            if (_spaceEjectionPoint == null)
            {
                return;
            }

            item.transform.SetPositionAndRotation(_spaceEjectionPoint.position, item.transform.rotation);

            // Off-station: gone from play. Item.Delete assumes a container; despawn/dispose directly.
            if (InstanceFinder.ServerManager != null)
            {
                InstanceFinder.ServerManager.Despawn(item.GameObject);
            }
            else
            {
                item.GameObject.Dispose(true);
            }
        }

        private struct PendingArrival
        {
            public readonly Item Item;
            public float RemainingSeconds;

            public PendingArrival(Item item, float remainingSeconds)
            {
                Item = item;
                RemainingSeconds = remainingSeconds;
            }
        }
    }
}
