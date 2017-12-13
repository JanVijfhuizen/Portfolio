using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomInterior : MonoBehaviour {

    [Tooltip("DISABLE ALL OBJECTS IN THE ROOM! IT'S CHEAPER THIS WAY (MUCH CHEAPER).")]
    public List<InteriorItem> objectsInRoom = new List<InteriorItem>();
    public List<Transform> spawnPositions = new List<Transform>();

    [Serializable]
	public class InteriorItem
    {
        public GameObject obj;
        public int cost;
    }
}
