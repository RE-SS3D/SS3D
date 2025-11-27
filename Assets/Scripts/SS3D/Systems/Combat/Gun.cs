using FishNet.Object;
using SS3D.Core;
using SS3D.Data.Generated;
using SS3D.Systems.Audio;
using SS3D.Systems.Inputs;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using AudioType = UnityEngine.AudioType;

namespace SS3D.Systems.Combat.Interactions
{
    public class Gun : NetworkBehaviour
    {
        [SerializeField]
        private Transform _rifleButt;

        [SerializeField]
        private GameObject _bulletPrefab;

        [SerializeField]
        private Transform _spawnPoint;

        [SerializeField]
        private float _fireRate = 5f; // Bullets fired per second

        [SerializeField]
        private float _bulletSpeed = 10f; // Speed of the bullets

        [SerializeField]
        private bool _readyToFire = true;

        private bool _isHolding;

        public Transform RifleButt => _rifleButt;

        public override void OnStartClient()
        {
            base.OnStartClient();
            Subsystems.Get<InputSubSystem>().Inputs.GunFire.Fire.started += ctx => _isHolding = true;
            Subsystems.Get<InputSubSystem>().Inputs.GunFire.Fire.canceled += ctx => _isHolding = false;
        }

        protected void Update()
        {
            if (_isHolding)
            {
                Fire();
            }
        }

        // Shit code, just to get the guns going a bit, to change
        private void Fire()
        {
            Debug.Log("Fire");
            if (!_readyToFire)
            {
                return;
            }

            Subsystems.Get<AudioSubSystem>().PlayAudioSource(Audio.AudioType.Sfx, Sounds.MachineGun, GetComponent<NetworkObject>());

            _readyToFire = false;

            StartCoroutine(ReadyToFire());

            IRecoilGiver recoilGiver = GetComponentInParent<IRecoilGiver>();

            if (recoilGiver != null)
            {
                recoilGiver.AddRecoil(38f);
            }

            GameObject bullet = Instantiate(_bulletPrefab, _spawnPoint.position, _spawnPoint.rotation);

            if (bullet.TryGetComponent(out Rigidbody bulletRigidbody))
            {
                bulletRigidbody.velocity = _spawnPoint.forward * _bulletSpeed;
            }
        }

        private IEnumerator ReadyToFire()
        {
            yield return new WaitForSeconds(1f / _fireRate);
            _readyToFire = true;
        }
    }
}
