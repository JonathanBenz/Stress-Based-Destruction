using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hammer : MonoBehaviour
{
    [SerializeField] float impactForce = 30f;  // Force from the hammer swing

    // This method will be called from the animation event
    public void OnHit()
    {
        // Collision check to detect node hit
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 0.8f);
        foreach (var hit in hitColliders)
        {
            Node hitNode = hit.GetComponent<Node>();
            if (hitNode != null)
            {
                // Apply stress to the node that was hit
                hitNode.ApplyStress(impactForce);
                
                // Add force to the node's rigidbody
                Rigidbody fracturedRb = hitNode.GetComponent<Rigidbody>();
                Vector3 direction = (fracturedRb.transform.position - transform.position).normalized; // Direction from hammer to node
                fracturedRb.AddForce(direction * impactForce, ForceMode.Impulse); // Apply force in that direction
                
                // Rerun simulation check after hitting
                SimulationManager.Instance.RunTopDownSupportCheck();
            }
        }
    }
}