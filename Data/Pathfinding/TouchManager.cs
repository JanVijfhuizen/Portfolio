using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TouchManager : MonoBehaviour {

    private Cam c;
    private void Start()
    {
        c = GameManager.self.cam;
    }

    private bool hold;
    private void Update()
    {
#if UNITY_ANDROID

#endif

#if UNITY_EDITOR || UNITY_STANDALONE

        float axis = Input.GetAxis("Mouse ScrollWheel");
        if (!Mathf.Approximately(axis, 0))
        {
            c.ZoomCam(axis);
        }

        if (Input.GetButton("Fire1"))
        {
            Vector3 newTouchPos = c.c.ScreenToWorldPoint(Input.mousePosition);
            newTouchPos.z = transform.position.z;
            RaycastHit[] hits = Physics.RaycastAll(newTouchPos, Vector3.forward);
            TileNode tN;
            Debug.DrawRay(newTouchPos, Vector3.forward, Color.green, 10);
            foreach (RaycastHit hit in hits)
            {
                tN = hit.transform.GetComponent<TileNode>();
                if (tN != null)
                {
                    tN.OnTouch(hold);
                    hold = true;
                    break;
                }
            }
        }
        else
            hold = false;

        if (Input.GetButton("Fire2"))
        {
            Vector3 newTouchPos = c.c.ScreenToWorldPoint(Input.mousePosition);
            newTouchPos.z = transform.position.z;
            c.MoveToPos(newTouchPos);
        }
        else
            c.StopMoving();
#endif
    }
}
