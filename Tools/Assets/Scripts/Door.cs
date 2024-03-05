using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Door : MonoBehaviour
{

    private void OnDrawGizmos()
    {
        if(Selection.activeGameObject == transform.root.gameObject)
        {           
            // Draw Forward Arrow
            Handles.color = Color.red;
            Handles.ArrowHandleCap(0, transform.position, transform.rotation * Quaternion.LookRotation(Vector3.forward), 1f, EventType.Repaint);

            // Draw Backward Arrow
            Handles.color = Color.green;
            Handles.ArrowHandleCap(0, transform.position, transform.rotation * Quaternion.LookRotation(Vector3.back), 1f, EventType.Repaint);

            // Draw labels
            DrawLabel(transform.position + transform.forward, "Forward");
            DrawLabel(transform.position - transform.forward, "Backward");
        }
        
    }

    private void DrawLabel(Vector3 position, string labelText)
    {
        Handles.Label(position, labelText, EditorStyles.boldLabel);
    }

}
