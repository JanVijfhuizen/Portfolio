using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cam : MonoBehaviour {

    [SerializeField]
    private float offset;
    [HideInInspector]
    public Camera c;
    [SerializeField]
    private float minSize, maxSize;
    [SerializeField]
    private float zoomSpeed, camSpeed;

    private void Awake()
    {
        c = GetComponent<Camera>();
    }

    public void SetPosition(LevelCreator.Node node)
    {
        SetPosition(LevelCreator.self.CalcPos(node));
    }

	public void SetPosition(Vector3 pos)
    {
        pos.z -= offset;
        transform.position = pos;
    }

    public void MoveToPos(Vector3 pos)
    {
        if (moveToPos != null)
            StopCoroutine(moveToPos);
        moveToPos = StartCoroutine(_MoveToPos(pos));
    }

    public void StopMoving()
    {
        if(moveToPos != null)
            StopCoroutine(moveToPos);
    }

    Coroutine moveToPos;
    private IEnumerator _MoveToPos(Vector3 pos)
    {
        while (!Mathf.Approximately(Vector2.Distance(transform.position, pos), 0))
        {
            transform.position = Vector3.MoveTowards(transform.position, pos, camSpeed * Time.deltaTime);
            yield return null;
        }
    }

    public void ZoomCam(float size)
    {
        //zoom dynamically
        c.orthographicSize += size * zoomSpeed * Time.deltaTime;
        if (c.orthographicSize > maxSize)
            c.orthographicSize = maxSize;
        if (c.orthographicSize < minSize)
            c.orthographicSize = minSize;
    }
}
