using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseController : MonoBehaviour {

    [SerializeField]
    private MoveToAI moveToAI;
    private bool movable;
    private void Start()
    {
        movable = !moveToAI.train;
    }

    private void Update()
    {
        if (!movable)
            return;
        Vector3 pos = Input.mousePosition;
        pos = Camera.main.ScreenToWorldPoint(pos);
        pos.z = transform.position.z;
        transform.position = pos;
    }
}
