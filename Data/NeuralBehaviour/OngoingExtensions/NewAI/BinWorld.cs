using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;

public class BinWorld : Bin {

    public List<Human> generation = new List<Human>();

    [Serializable]
    public class Human : BinCharacter
    {
        public int health = 10;
    }

    private void Awake()
    {
        generation.ForEach(x => x.Init());

        StartCoroutine(CycleOfLife());
    }

    [SerializeField]
    private float cycleDuration;
    private IEnumerator CycleOfLife()
    {
        while(generation.Count > 0)
        {
            generation.ForEach(x => x.Next());
            generation.RemoveAll(x => x.health < 0);
            yield return new WaitForSeconds(cycleDuration);
        }
    }
}
