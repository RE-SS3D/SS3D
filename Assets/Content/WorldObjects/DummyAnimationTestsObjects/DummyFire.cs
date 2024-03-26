using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DummyFire : MonoBehaviour
{
    public GameObject bulletPrefab;
    public Transform spawnPoint;
    public float fireRate = 10f;    // Bullets fired per second
    public float bulletSpeed = 10f; // Speed of the bullets
    private bool _readyToFire = true;

    public void Fire()
    {
        if (!_readyToFire)
            return;

        _readyToFire = false;

        StartCoroutine(ReadyToFire());
        GameObject bullet = Instantiate(bulletPrefab, spawnPoint.position, spawnPoint.rotation);
        Rigidbody bulletRigidbody = bullet.GetComponent<Rigidbody>();
        if (bulletRigidbody != null)
        {
            bulletRigidbody.velocity = spawnPoint.forward * bulletSpeed;
        }
    }

    private IEnumerator ReadyToFire()
    {
        yield return new WaitForSeconds(1f / fireRate);
        _readyToFire = true;
    }
}