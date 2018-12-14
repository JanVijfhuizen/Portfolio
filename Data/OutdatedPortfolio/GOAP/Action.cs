using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Action : MonoBehaviour {

    public string name;
    [SerializeField]
    private int duration;
    public List<State> rewards, requirements, improvements;
    [Range(0,100)]
    public int successRate;
    [SerializeField, Range(0,100)]
    private int onFailAmount = 20, onSucceedAmount = 10;

    [Tooltip("Whether or not you can socialize / eat, drink / pause while performing this action")]
    public bool intensive;
    public bool positionBound;

    public enum RewardType {Adds, Sets }

    [Serializable]
    public class State : IComparable<State>
    {
        public string state;
        [Range(0,100)]
        public int amount;
        public RewardType type;

        public State(string state, int amount)
        {
            this.state = state;
            this.amount = amount;
        }

        public int CompareTo(State other)
        {
            if (other == null)
                return 1;
            return amount - other.amount;
        }
    }

    protected GOAP myGoap;
    protected virtual void Awake()
    {
        myGoap = GetComponent<GOAP>();
    }

    protected virtual int GetDurationMovement(Vector3 other)
    {
        return 0;
    }

    private int GetDurationMovement()
    {
        return GetDurationMovement(myGoap.transform.position);
    }

    //execute func
    public int Duration()
    {
        //if positionbound return + walk duration
        if (positionBound)
            return duration + GetDurationMovement();
        return duration;
    }

    public int Duration(Action other)
    {
        if (!(other != null))
            return duration;
        if(other.positionBound)
            return duration;

        //return + walk duration
        return duration + GetDurationMovement(other.transform.position);
    }

    //depends on the action ofcourse, but this checks things as intelligence or morale
    public virtual bool WillExecute()
    {
        return true;
    }

    public virtual void Execute()
    {

        //do stuff
        //...

        //test
        Debug.Log(name);
        OnExecuted(ExecutedType.Succeeded);
    }

    public enum ExecutedType {Succeeded, Failed, Removed }
    public virtual void OnExecuted(ExecutedType type)
    {
        if (type != ExecutedType.Failed)
        {
            bool contains;
            foreach (State s in rewards)
            {
                contains = false;
                foreach (GOAP.AState gS in myGoap.states)
                    if (gS.state == s.state)
                    {
                        contains = true;
                        if (s.type == RewardType.Sets)
                            gS.amount = s.amount;
                        else
                        {
                            gS.amount += s.amount;
                            if (gS.amount > 100)
                                gS.amount = 100;
                            else if (gS.amount < 0)
                                gS.amount = 0;
                        }
                    }
                if (!contains)
                    myGoap.states.Add(new GOAP.AState(s.state, s.amount));
            }

            if (type == ExecutedType.Succeeded)
            {
                successRate += onSucceedAmount;
                if (successRate > 100)
                    successRate = 100;
            }
        }
        else
        {
            successRate -= onFailAmount;
            if (successRate < 0)
                successRate = 0;
        }
        myGoap.ExecuteNext();
    }
}
