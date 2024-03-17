using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DummyFire : MonoBehaviour
{
    public GameObject bulletPrefab;
    public Transform spawnPoint;
    public float fireRate = 10f;    // Bullets fired per second
    public float bulletSpeed = 10f; // Speed of the bullets
    private bool isFiring = false;
    private float _time = 0f;

    void Update()
    {
        _time +=  Time.deltaTime;
       
        if (Input.GetKey(KeyCode.T) && _time > 1f/fireRate)
        {
            Fire();
            _time = 0f;
        }
    }

    private void Fire()
    {
        GameObject bullet = Instantiate(bulletPrefab, spawnPoint.position, spawnPoint.rotation);
        Rigidbody bulletRigidbody = bullet.GetComponent<Rigidbody>();
        if (bulletRigidbody != null)
        {
            bulletRigidbody.velocity = spawnPoint.forward * bulletSpeed;
        }
    }
}