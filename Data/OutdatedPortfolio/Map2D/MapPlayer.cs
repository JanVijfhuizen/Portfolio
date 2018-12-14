using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapPlayer : MonoBehaviour {

    [SerializeField]
    private float speed = 0.1f, distanceToTargetWhenStopping = 2;
    [HideInInspector]
    public bool moving = false;
    public Image pathPoint;

    #region Test Functions

    public void PrintAction(string s)
    {
        Debug.Log(s);
    }

    #endregion

    private void Awake()
    {
        pathPoint.enabled = false;
    }

	public void Move(List<Vector2> list, bool load)
    {
        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
            pathPoint.enabled = false;
        }
        moveRoutine = StartCoroutine(_Move(list, load));
    }

    private Coroutine moveRoutine;
    private IEnumerator _Move(List<Vector2> list, bool load)
    {
        Vector2 goal = list[list.Count - 1];

        pathPoint.enabled = true;
        pathPoint.transform.position = list[0];

        float dis1, dis2;
        //for testing purposes
        while (list.Count > 0)
        {
            //move to position
            transform.Translate((goal - (Vector2)transform.position).normalized);

            dis1 = Vector2.Distance(transform.position, goal);
            dis2 = Vector2.Distance(transform.position, list[0]);

            if (dis1 <= distanceToTargetWhenStopping || dis1 > dis2)
                if (list.Count > 1)
                {
                    goal = list[list.Count - 2];
                    list.RemoveAt(list.Count - 1);
                }
                else break;
            yield return new WaitForSeconds(speed);
        }

        pathPoint.enabled = false;

        if (!load)
            yield break;
        //presentatie check, beetje vies imo
        MapManager m = MapManager.self;
        if (!m.ready)
            yield break;
        if (m.movingTowardsSaloon)
            m.saloonFunction.Invoke();
        else
            m.dungeonFunction.Invoke();
        m.movingTowardsSaloon = false;
    }
}
