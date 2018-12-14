using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class Visualizer : MonoBehaviour {

    [SerializeField]
    private bool on;

	public void ShowRay(Vector3 start, Vector3 dir, Color c)
    {
        if (!on)
            return;
        Debug.DrawRay(start, dir, c, 1);
    }

    public void OnDrawGizmos()
    {
        if (Application.isEditor)
        {
            
        }
    }
}
