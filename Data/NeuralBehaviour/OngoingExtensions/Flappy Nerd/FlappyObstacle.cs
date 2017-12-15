using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlappyObstacle : MonoBehaviour {

    [SerializeField]
    private float speed = 5f;
	private void Update () {
        transform.Translate(Vector3.left * speed * Time.deltaTime);
	}
}
