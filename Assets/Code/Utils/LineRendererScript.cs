using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineRendererScript : MonoBehaviour
{
    public GameObject pivot1; // First pivot point
    public GameObject pivot2; // Second pivot point

    private LineRenderer lineRenderer; // Reference to the LineRenderer component

    // Start is called before the first frame update
    void Start()
    {
        // Get the LineRenderer component attached to this GameObject
        lineRenderer = GetComponent<LineRenderer>();

        // Initialize the line with 2 points
        lineRenderer.positionCount = 2;
    }

    // Update is called once per frame
    void Update()
    {
        // Check if both pivot points are assigned
        if (pivot1 != null && pivot2 != null)
        {
            // Update the positions of the LineRenderer to follow pivot points
            lineRenderer.SetPosition(0, pivot1.transform.position);
            lineRenderer.SetPosition(1, pivot2.transform.position);
        }
        else
        {
            // If either pivot1 or pivot2 is missing, destroy the LineRenderer GameObject
            Destroy(gameObject);
        }
    }

    // Method to set the pivot points from another script
    public void SetPivotPoints(GameObject p1, GameObject p2)
    {
        pivot1 = p1;
        pivot2 = p2;
    }
}
