using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClickCheck : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0)) 
        {
            // Get the mouse position in screen space (pixels)
            Vector3 mousePosition = Input.mousePosition;

            // Log the screen space position
            Debug.Log("Mouse Position: " + mousePosition);

            // Convert screen space to world space if needed
            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(mousePosition);
            Debug.Log("World Position: " + worldPosition);
        }
    }
}
