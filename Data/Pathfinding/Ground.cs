using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ground : MonoBehaviour {

	public enum GroundType {Walkable, Unwalkable, Wall, Ladder, Exit, Connection }
    public GroundType type;
}
