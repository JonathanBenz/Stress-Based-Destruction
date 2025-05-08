using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Editor tool to pre-bake all Node and NodeLink data. 
/// </summary>
public class NodeEditorUtility : EditorWindow
{
    float linkDistanceThreshold = 0.5f;  // Value to determine how far another node needs to be in order to calculate a link between them
    float maxLinkStrength = 200f;        // Value to clamp the maximum strength a link could have
    float strengthMultiplier = 10f;      // Value to adjust/ scale strength values of each node, depending on the node's size
    float layerHeight = 0.2f;            // Value to differentiate which layer a node is in based on its y-distance away from the ground

    [MenuItem("Tools/Node Graph Builder")]
    public static void ShowWindow()
    {
        GetWindow<NodeEditorUtility>("Node Graph Builder");
    }

    private void OnGUI()
    {
        GUILayout.Label("Node Graph Setup", EditorStyles.boldLabel);

        linkDistanceThreshold = EditorGUILayout.FloatField("Link Distance Threshold", linkDistanceThreshold);
        maxLinkStrength = EditorGUILayout.FloatField("Max Link Strength", maxLinkStrength);
        strengthMultiplier = EditorGUILayout.FloatField("Strength Multiplier", strengthMultiplier);
        layerHeight = EditorGUILayout.FloatField("Layer Height", layerHeight);

        if (GUILayout.Button("Build Graph from Scene")) BuildGraph();
    }

    /// <summary>
    /// Go through each Node, assign its links, layer, and strength values.
    /// </summary>
    public void BuildGraph()
    {
        Node[] nodes = FindObjectsOfType<Node>();

        // Loop through each node and create links to nearby nodes
        foreach (Node node in nodes)
        {
            // Clear existing links before rebuilding
            node.Links.Clear();

            foreach (Node otherNode in nodes)
            {
                if (node == otherNode) continue; // Prevent link to self

                // Calculate distance between the nodes
                float distance = Vector3.Distance(node.transform.position, otherNode.transform.position);

                if (distance <= linkDistanceThreshold)
                {
                    // Calculate the strength of the link based on the node strengths
                    float linkStrength = Mathf.Clamp01(1f / distance) * strengthMultiplier * ((CalculateNodeStrength(node) + CalculateNodeStrength(otherNode)) / 2f);
                    linkStrength = Mathf.Min(linkStrength, maxLinkStrength); // Cap the link strength

                    // Create and assign the link
                    NodeLink newLink = new NodeLink
                    {
                        TargetNode = otherNode,
                        MaxStressTransfer = linkStrength
                    };
                    node.Links.Add(newLink);

                    // Make edges doubly linked, assign the reverseLink
                    bool alreadyLinked = otherNode.Links.Exists(l => l.TargetNode == node);
                    if (!alreadyLinked)
                    {
                        NodeLink reverseLink = new NodeLink
                        {
                            TargetNode = node,
                            MaxStressTransfer = linkStrength
                        };
                        otherNode.Links.Add(reverseLink);
                    }
                }
            }
        }
        Debug.Log("Node Graph Built!");
    }

    /// <summary>
    /// Calculate the strength value of the given Node.
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    private float CalculateNodeStrength(Node node)
    {
        // Calculate based on the MeshFilter and apply scaling with the strengthMultiplier
        MeshFilter meshFilter = node.GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            // Volume calculation (approximation)
            float volume = ApproximateMeshVolume(meshFilter.sharedMesh, node.transform.localScale);

            // Apply a scaling factor to the volume to make it more impactful
            float scaledVolume = volume * 100f; 

            // Apply the strength multiplier
            float nodeStrength = scaledVolume * strengthMultiplier;

            return Mathf.Max(nodeStrength, 1f); // Ensure strength is at least 1
        }

        // Default strength if no mesh is found
        return 1f;
    }

    /// <summary>
    /// Helper function to help calculate Node strength. Strength is based off of the mesh volume. 
    /// Bigger volume = bigger strength. 
    /// </summary>
    /// <param name="mesh"></param>
    /// <param name="scale"></param>
    /// <returns></returns>
    private float ApproximateMeshVolume(Mesh mesh, Vector3 scale)
    {
        float volume = 0f;
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;

        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 v0 = Vector3.Scale(vertices[triangles[i + 0]], scale);
            Vector3 v1 = Vector3.Scale(vertices[triangles[i + 1]], scale);
            Vector3 v2 = Vector3.Scale(vertices[triangles[i + 2]], scale);

            volume += SignedVolumeOfTriangle(v0, v1, v2);
        }

        return Mathf.Abs(volume);
    }

    private float SignedVolumeOfTriangle(Vector3 p1, Vector3 p2, Vector3 p3)
    {
        return Vector3.Dot(Vector3.Cross(p1, p2), p3) / 6f;
    }
}