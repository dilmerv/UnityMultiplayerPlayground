using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnCollisionEnter(Collision collision)
    {
        Logger.Instance.LogInfo($"Enemy OnCollisionEnter with name: {collision.gameObject.name}");
    }

    private void OnTriggerEnter(Collider other)
    {
        Logger.Instance.LogInfo($"Enemy OnTriggerEnter with name: {other.gameObject.name}");
    }
}