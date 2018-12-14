using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Card : MonoBehaviour {

    public string name;
    public Sprite image;

    public int cost;
    public int attack;
    public int health;
    public int morale;
    public int importance;
    public int speed;

    public Transform Self
    {
        get
        {
            return transform;
        }
    }

    public virtual bool StartOfTurn()
    {
        return false;
    }

    public virtual bool Play()
    {
        //play
        GameManager.Player p = GameManager.self.curPlayer;
        if (p.resources >= cost)
        {
            p.resources -= cost;
            return true;
        }
        return false;
    }

    public virtual bool Effect()
    {
        return false;
    }
}
