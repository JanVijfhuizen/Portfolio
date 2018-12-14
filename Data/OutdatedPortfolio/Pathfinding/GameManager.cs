using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour {

    public static GameManager self;
    public Cam cam;
    public TurnManager tm;

    private void Awake()
    {
        self = this;
        tm = GetComponent<TurnManager>();
    }
}
