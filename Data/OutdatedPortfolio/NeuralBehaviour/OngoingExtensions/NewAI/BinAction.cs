using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BinAction : MonoBehaviour {

    [Tooltip("Excluding input and output")]
    public int[] size;
    public int outputSize;

    public abstract void Execute(List<float> input, Bin.BinCharacter character); //binary output
}
