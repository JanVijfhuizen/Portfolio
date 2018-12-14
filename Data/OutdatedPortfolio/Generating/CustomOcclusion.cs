using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomOcclusion : MonoBehaviour {
    /*

    //naar vertexes schieten is misschien een idee

    public static CustomOcclusion self;
    [SerializeField]
    private float occludeRefreshSpeed;
    [SerializeField]
    private bool occludePerFrame;
    [SerializeField]
    private float drawDistance;
    [SerializeField]
    private bool enableDrawDistance;
    public bool activated = false;
    [SerializeField]
    private float fieldOfViewThreshold = 20;

    private List<Renderer> rendererOnScreen = new List<Renderer>(), 
        rendererOffScreen = new List<Renderer>();

    private Camera cam;

    private void Awake()
    {
        self = this;
    }

	private void Start()
    {
        cam = GetComponent<Camera>();
        StartCoroutine(OccludeInvisible());
    }

    private IEnumerator OccludeInvisible()
    {
        while (!activated)
            yield return null;

        //get all objects
        GameObject[] all = FindObjectsOfType(typeof(GameObject)) as GameObject[];
        Renderer r;
        foreach (GameObject possibleBakeObj in all)
            if (possibleBakeObj.isStatic)
            {
                r = possibleBakeObj.GetComponent(typeof(Renderer)) as Renderer;
                if (r != null)
                    rendererOnScreen.Add(r);
            }

        RaycastHit hit;
        Transform trans;
        Vector3 pos;
        Renderer renderer;
        float radius = 0;

        while (true)
        {
            if (!activated)
                for (int i = rendererOnScreen.Count - 1; i > -1; i--)
                    EnableRenderer(rendererOnScreen[i]);
            while (!activated)
                yield return null;

            radius = cam.fieldOfView + fieldOfViewThreshold;

            //disable visible
            for (int i = rendererOnScreen.Count - 1; i > - 1; i--)
            {
                renderer = rendererOnScreen[i];
                if (renderer.isVisible)
                    continue;
                DisableRenderer(renderer);
            }

            for(int i = rendererOffScreen.Count - 1; i > - 1; i--)
            {
                renderer = rendererOffScreen[i];
                trans = renderer.transform;
                pos = trans.position;

                //check if too far for the draw distance
                if (enableDrawDistance)
                    if (Vector3.Distance(pos, transform.position) > drawDistance)
                        continue;

                //check players rotation
                if (Vector3.Angle(transform.forward, pos - transform.position) <= radius)
                {
                    //shoot raycast, if hits then activate
                    //Debug.DrawLine(transform.position, pos, Color.green, 0.1f);
                    //dit moet dus 
                    /*
                    if (Physics.Linecast(transform.position, pos, out hit))
                    {   
                        if (hit.transform == trans)
                            EnableRenderer(renderer);
                    }

                    //tijdelijk
                    EnableRenderer(renderer);
                }
            }

            //refresh speed
            if (occludePerFrame)
                yield return null;
            else
                yield return new WaitForSeconds(occludeRefreshSpeed);
        }
    }

    private void EnableRenderer(Renderer renderer)
    {
        renderer.enabled = true;
        rendererOnScreen.Add(renderer);
        rendererOffScreen.Remove(renderer);
    }

    private void DisableRenderer(Renderer renderer)
    {
        renderer.enabled = false;
        rendererOnScreen.Remove(renderer);
        rendererOffScreen.Add(renderer);
    }

    */

    public static CustomOcclusion self;
    private void Awake()
    {
        self = this;
    }

    private Collider _collider;
    public void Occlude()
    {
        _collider = GetComponent(typeof(Collider)) as Collider;
        ScanForObjects();
    }

    void ScanForObjects()
    {
        GameObject[] all = FindObjectsOfType(typeof(GameObject)) as GameObject[];
        foreach (GameObject g in all)
            if (g.isStatic)
                SetRenderer(g, false);

        //turn all close objects on
        Collider[] otherColliders = Physics.OverlapSphere(transform.position, 60);
        foreach (Collider c in otherColliders)
            if (c.gameObject.isStatic)
                SetRenderer(c, true);
    }

    private Renderer _renderer;
    private void OnTriggerEnter(Collider c)
    {
        SetRenderer(c, true);
    }

    private void OnTriggerExit(Collider c)
    {
        SetRenderer(c, false);
    }

    private void SetRenderer(GameObject g, bool on)
    {
        _renderer = g.GetComponent(typeof(Renderer)) as Renderer;
        SetRenderer(_renderer, on);
    }

    private void SetRenderer(Collider c, bool on)
    {
        _renderer = c.transform.GetComponent(typeof(Renderer)) as Renderer;
        SetRenderer(_renderer, on);
    }

    private void SetRenderer(Renderer r, bool on)
    {
        if (!(_renderer != null))
            return;
        _renderer.enabled = on;
    }
}