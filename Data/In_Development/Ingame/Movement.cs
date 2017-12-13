using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Movement : MonoBehaviour {

    [SerializeField]
    private float speed = 0.5f, minH, maxH, maxW, maxL;
    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private Vector2[] touch1 = new Vector2[2];
    private float dis, mag, magDel;
    private Vector2 dir;
    private Vector3 pos;
    private Touch[] touches;
    private Vector2 t1dir, t2dir;
    [SerializeField]
    private EventSystem eventSystem;
    private void Update()
    {
        #region PC TOUCH

        //if (eventSystem.IsPointerOverGameObject())
        if(IsPointerOverUIObject())
            return;
        
        if (Input.GetMouseButton(0))
        {
            if (!Input.GetMouseButtonDown(0))
            {
                touch1[0] = cam.ScreenToWorldPoint(Input.mousePosition);
                dis = Vector2.Distance(touch1[0], touch1[1]);
                dir = touch1[1] - touch1[0];
                pos = transform.position + (Vector3)dir;

                //clamp in area
                pos.x = Mathf.Clamp(pos.x, -maxW, maxW);
                pos.y = Mathf.Clamp(pos.y, -maxL, maxL);

                transform.position = pos;
            }

            //if just touch and hits object with right tag place plez

            touch1[1] = cam.ScreenToWorldPoint(Input.mousePosition);
        }
        
        #endregion
        
        touches = Input.touches;

        if (touches.Length == 1)
        {
            if (touches[0].phase == TouchPhase.Moved)
            {
                touch1[0] = cam.ScreenToWorldPoint(touches[0].position);
                dis = Vector2.Distance(touch1[0], touch1[1]);

                dir = touch1[1] - touch1[0];
                pos = transform.position + (Vector3)dir;

                //clamp in area
                pos.x = Mathf.Clamp(pos.x, -maxW, maxW);
                pos.y = Mathf.Clamp(pos.y, -maxL, maxL);

                transform.position = pos;
            }

            touch1[1] = cam.ScreenToWorldPoint(Input.mousePosition);
        }
        
        else if(touches.Length > 1)
        {
            t1dir = touches[0].position - touches[0].deltaPosition;
            t2dir = touches[1].position - touches[1].deltaPosition;

            magDel = (t1dir - t2dir).magnitude;
            mag = (touches[0].position - touches[1].position).magnitude;

            dis = magDel - mag;

            cam.orthographicSize += dis * speed;
            cam.orthographicSize = Mathf.Clamp(cam.fieldOfView, minH, maxH);
        }       
    }

    #region Tools

    private PointerEventData eventDataCurrentPosition;
    private List<RaycastResult> results;
    private bool IsPointerOverUIObject()
    {
        eventDataCurrentPosition = new PointerEventData(EventSystem.current);
        eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        results = new List<RaycastResult>();
        eventSystem.RaycastAll(eventDataCurrentPosition, results);
        return results.Count > 0;
    }

    #endregion
}
