using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manager which runs calculations and checks for stress propagation. 
/// </summary>
public class SimulationManager : MonoBehaviour
{
    public static SimulationManager Instance { get; private set; } // Static Instance to make global calls easy.

    private List<Node> allNodes = new(); // Every Node in the scene.
    private Queue<(Node node, float stress)> stressQueue = new(); // Work Queue to add nodes which have experienced an applied stress. 
    private bool isProcessing = false;

    private float supportCheckInterval = 2f;  // How often to run the top-down support check
    private float nextSupportCheckTime = 0f;

    public List<Node> GetAllNodes() => allNodes;

    private void Start()
    {
        Instance = this;
        allNodes = new List<Node>(FindObjectsOfType<Node>());
    }

    // Continuously check for stress propagation from the top layers to the bottom layers.
    private void FixedUpdate()
    {
        if (Time.time >= nextSupportCheckTime)
        {
            RunTopDownSupportCheck();
            nextSupportCheckTime = Time.time + supportCheckInterval; // Set next check time
        }
    }

    /// <summary>
    /// Add a stressed node to the work queue. 
    /// </summary>
    /// <param name="node"> The specific node. </param>
    /// <param name="amount"> The amount of applied stress. </param>
    public void AddStress(Node node, float amount)
    {
        stressQueue.Enqueue((node, amount));

        if (!isProcessing) StartCoroutine(StressPropagationCoroutine());
    }

    private IEnumerator StressPropagationCoroutine()
    {
        isProcessing = true;
        while (stressQueue.Count > 0)
        {
            var (node, stress) = stressQueue.Dequeue();
            node.ApplyStress(stress);

            yield return null; 
        }
        isProcessing = false;
        RunTopDownSupportCheck(); // After stress propagation, run integrity sweep
    }

    /// <summary>
    /// Check each node, starting from the top layer going down to the ground layer, and find any newly stressed nodes.
    /// If newly stressed nodes are found, add them to the work queue. 
    /// </summary>
    public void RunTopDownSupportCheck()
    {
        Debug.Log("DEBUG: Running top-down support check...");

        Dictionary<int, List<Node>> layers = new();

        // Group nodes by their layer index
        foreach (Node node in allNodes)
        {
            if (!layers.ContainsKey(node.LayerIndex))
                layers[node.LayerIndex] = new List<Node>();

            layers[node.LayerIndex].Add(node);
        }

        List<int> sortedLayers = new(layers.Keys);
        sortedLayers.Sort((a, b) => b.CompareTo(a)); // Descending layer order

        List<(Node, float)> newlyStressedNodes = new();

        // Check for any nodes that should be broken
        foreach (int layer in sortedLayers)
        {
            foreach (Node node in layers[layer])
            {
                if (!node.HasBroken && !node.IsSupported())
                {
                    Debug.Log($"DEBUG: Node '{node.name}' at layer {layer} is unsupported and will break.");
                    List<(Node, float)> stressedFromBreak = node.Break();
                    if (stressedFromBreak != null) newlyStressedNodes.AddRange(stressedFromBreak);
                }
            }
        }

        // If we stressed any new nodes, continue propagation and add them to the work queue
        if (newlyStressedNodes.Count > 0)
        {
            foreach (var (node, stress) in newlyStressedNodes)
            {
                AddStress(node, stress);
            }
        }
    }
}
