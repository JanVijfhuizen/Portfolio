using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlappyObstacle : MonoBehaviour {

    [HideInInspector]
    public float lifeTime = 1;
    [SerializeField]
    private float speed = 5f;
	private void Update () {
        lifeTime -= Time.deltaTime;
        if (!NeuralFlappy.isDead)
        {
            if (lifeTime < 0)
            {
                Destroy(gameObject);
                return;
            }
            transform.Translate(Vector3.left * speed * Time.deltaTime);
        }
	}
}
