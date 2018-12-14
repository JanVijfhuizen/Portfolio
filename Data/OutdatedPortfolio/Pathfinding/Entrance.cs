using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Entrance : MonoBehaviour {

    public static bool initialized;

	void Start () {
        GameManager.self.cam.SetPosition(transform.position);
        initialized = true;
	}
}
