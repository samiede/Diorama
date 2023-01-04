using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotate : MonoBehaviour
{

    public float Speed = 90f; 
    
    void Update()
    {
        transform.RotateAround(transform.position, transform.up, -Time.deltaTime * Speed);
    }
}
