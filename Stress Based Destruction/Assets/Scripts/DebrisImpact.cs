using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Turn broken nodes into debris which could interact with and apply stress to unbroken nodes upon impact. 
/// </summary>
public class DebrisImpact : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        // Ignore low-speed impacts
        if (collision.relativeVelocity.magnitude < 1f) return;

        // Try to apply stress to any node it hits
        Node targetNode = collision.collider.GetComponent<Node>();
        if (targetNode != null && !targetNode.HasBroken)
        {
            float impulse = collision.impulse.magnitude;
            float stress = impulse * 0.5f; 
            targetNode.ApplyStress(stress);
        }
    }
}