using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Edge between source node and target node with an associated weight.
/// </summary>
[System.Serializable]
public class NodeLink
{
    public Node TargetNode;
    public float MaxStressTransfer;
}

/// <summary>
/// Nodes are used to calculate stress propagation. They have a strength value and a layer index.
/// If stress is applied to a node, the force of that stress plus the node's strength value is to be compared with a link's MaxStressTransfer value within the same layer index.
/// This calculation is what determines if it breaks off or not. When it breaks off, it counts as debris and the DebrisImpact component is enabled so that it can apply stress upon collision to other nodes. 
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class Node : MonoBehaviour
{
    public int LayerIndex;    // The Layer Index is used to propagate stress amongst the rest of the nodes in the same layer before moving down a layer.
    public bool IsGroundNode; // Flag to check if Node is touching the ground (used to determine when to end Top-To-Bottom stress checking). 
    public float Strength;    // The node's strength intrinsic strength value. 
    private float StressDecayRate = 50f; // Fudge factor to make node's act more dramatically.
    public float CurrentStress { get; private set; } // Obtain the status of the node's stress. 
    public bool HasBroken { get; private set; } // Flag to check if the node has broken off the main structures. 

    public List<NodeLink> Links = new(); // All edges connected to this node. 

    // Cached components
    private Rigidbody rb;
    private DebrisImpact debris;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        debris = GetComponent<DebrisImpact>();
        debris.enabled = false;
        rb.isKinematic = true;
    }

    private void FixedUpdate()
    {
        ApplyDecay();
    }

    /// <summary>
    /// Apply stress decay over time to the node
    /// </summary>
    private void ApplyDecay()
    {
        if (!HasBroken && CurrentStress > 0f)
        {
            CurrentStress = CurrentStress + StressDecayRate * Time.fixedDeltaTime;
        }
    }

    /// <summary>
    /// When the node is collided by an object (such as a hammer swing or from debris), apply stress to the current node. 
    /// </summary>
    /// <param name="amount"> The amount of force applied from collision. </param>
    public void ApplyStress(float amount)
    {
        if (HasBroken) return;

        CurrentStress += amount;

        if (CurrentStress >= Strength) Break();
    }

    /// <summary>
    /// Break the current node off of the main structure, turn it into debris, do Lever Arm torque calculation for realistic breakage. 
    /// Before breaking the links, propagate stress along to the neighbors. 
    /// </summary>
    /// <returns> Return a list of tupled newly stressed nodes and their associated stress values. </returns>
    public List<(Node node, float stress)> Break()
    {
        if (HasBroken) return null;

        HasBroken = true;
        rb.isKinematic = false;
        rb.useGravity = true;
        debris.enabled = false;

        // Calculate torque relative to center of mass of all nodes
        Vector3 centerOfMass = CalculateCenterOfMass();
        Vector3 leverArm = transform.position - centerOfMass;
        Vector3 torqueDirection = Vector3.Cross(leverArm.normalized, Vector3.down).normalized;

        // Adjust torque magnitude for more dramatic result
        float stressFactor = Mathf.Clamp01(CurrentStress / Strength); // Increase torque with stress
        float torqueMagnitude = leverArm.magnitude * rb.velocity.magnitude * 500f * (1f + stressFactor); // Scale torque with velocity and stress
        rb.AddTorque(torqueDirection * torqueMagnitude, ForceMode.Impulse);

        List<(Node, float)> stressedNodes = new();
        foreach (NodeLink link in Links)
        {
            if (link.TargetNode != null && !link.TargetNode.HasBroken)
            {
                float stressToSend = Mathf.Min(Strength / Links.Count, link.MaxStressTransfer);
                stressedNodes.Add((link.TargetNode, stressToSend));
            }
        }
        return stressedNodes;
    }

    /// <summary>
    /// Helper function to calculate CoM of the model in order to help compute Lever Arm.
    /// </summary>
    /// <returns> Returns position of Center of Mass. </returns>
    private Vector3 CalculateCenterOfMass()
    {
        List<Node> allNodes = SimulationManager.Instance.GetAllNodes();
        if (allNodes.Count == 0) return transform.position; // Exit Case

        Vector3 total = Vector3.zero;
        foreach (var node in allNodes)
        {
            total += node.transform.position;
        }
        return total / allNodes.Count;
    }

    /// <summary>
    /// Check if the current Node is supported by a node underneath.
    /// </summary>
    /// <returns></returns>
    public bool IsSupported()
    {
        if (IsGroundNode) return true;

        foreach (NodeLink link in Links)
        {
            if (link.TargetNode == null) continue;
            if (link.TargetNode.LayerIndex < this.LayerIndex && !link.TargetNode.HasBroken) return true; 
        }

        return false;
    }

#if UNITY_EDITOR
    /// <summary>
    /// Visualize the links for each node in the editor.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        foreach (NodeLink link in Links)
        {
            if (link.TargetNode != null)
            {
                Gizmos.DrawLine(transform.position, link.TargetNode.transform.position);
                UnityEditor.Handles.Label((transform.position + link.TargetNode.transform.position) / 2, $"{link.MaxStressTransfer:F0}");
            }
        }
    }
#endif
}
