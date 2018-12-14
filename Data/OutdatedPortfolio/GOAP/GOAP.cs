using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GOAP : MonoBehaviour {

    //Mister Mi Mi Mi's Triple M AI = Mind, Memory, Muscle

    //character data
    #region Mind
    [Serializable]
	public class Stats
    {
        public string name;
        [Range(0,100)]
        public int
            pointCritical = 10,
            pointImproving = 50;
    }

    [Serializable]
    public class Intelligence
    {
        [Range(0,100)]
        public int value = 50;
        public int
            calcsPerSecond = 500;
        public List<Action> actions = new List<Action>();
    }

    [SerializeField]
    private Stats stats;
    [SerializeField]
    private Intelligence intelligence;
    #endregion

    //short and long term memory
    #region Memory

    private List<Action.State> currentState = new List<Action.State>(),
        improvementState = new List<Action.State>();

    public enum PlanImportance {Normal, Priority }
    public class Plan : IComparable<Plan>
    {
        public List<Action> parts = new List<Action>();
        public PlanImportance importance;
        public int start;
        public string goal;

        public int Duration //estimated
        {
            get
            {
                int duration = 0;
                Action a = null;
                foreach (Action pI in parts)
                {
                    duration += pI.Duration(a);
                    if (pI.positionBound)
                        a = pI;
                }
                return duration;
            }
        }

        public int End
        {
            get{
                return start + Duration;
            }
        }

        public int CompareTo(Plan other)
        {
            if (other == null)
                return 1;
            return other.start - start;
        }
    }

    [HideInInspector]
    public int tick;
    private List<Plan> planning = new List<Plan>();

    #region Test

    private void Start()
    {
        StartGOAP();
    }

    #endregion

    public void StartGOAP()
    {
        StartInnerClock();
        Idle();
    }

    public void StartInnerClock()
    {
        StartCoroutine(InnerClock());
    }

    private IEnumerator InnerClock()
    {
        while (true)
        {
            tick++;
            yield return new WaitForSeconds(1);
        }
    }

    private List<Plan> CheckIfPlanIntervines(Plan newPlan)
    {
        List<Plan> ret = new List<Plan>();
        Plan plan;
        int end, endOther;
        for (int i = planning.Count; i > 0; i--)
        {
            //check if plans intertwine
            plan = planning[i];
            if (plan.start > newPlan.start && plan.start < newPlan.End)
                ret.Add(plan);
            else
            {
                endOther = plan.End;
                end = newPlan.End;
                if (endOther > newPlan.start && endOther < end)
                    ret.Add(plan);
            }
        }
        return ret;
    }

    private void InsertPlan(Plan newPlan, PlanImportance importance) //work has a set schedule and should already have been calculated
    {
        ResetTick();
        planning.Sort();
        List<Plan> interventions;

        //if priority
        if(importance != PlanImportance.Normal)
        {
            interventions = CheckIfPlanIntervines(newPlan);
            foreach (Plan other in interventions)
                interventions.Remove(other);
        }
        else
        {
            //keep checking in between plans    
            int duration = newPlan.Duration;
            int otherDuration;
            for (int i = 0; i < planning.Count; i++)
            {
                if(i == planning.Count - 1)
                {
                    newPlan.start = planning[i].End;
                    break;
                }

                otherDuration = planning[i + 1].start - planning[i].End;
                if(otherDuration > duration)
                {
                    newPlan.start = planning[i].End;
                    break;
                }
            }
        }

        newPlan.importance = importance;
        planning.Add(newPlan);
    }

    //resets inner clock
    public void ResetTick()
    {
        foreach (Plan p in planning)
            p.start -= tick;
        tick = 0;
    }

    private void StartPlan()
    {
        planning.Sort();
        if (planning.Count == 0) //a man with no plan
        {
            Idle();
            return;
        }
        //prepare for plan[0]
        partIndex = -1;
        ExecuteNext();
    }

    private int partIndex;
    public void ExecuteNext()
    {
        partIndex++;
        if (partIndex >= planning[0].parts.Count) {
            planning.RemoveAt(0);
            Idle();
            return;
        }

        //execute
        Action action = planning[0].parts[partIndex];
        if (!action.WillExecute())
        {
            action.OnExecuted(Action.ExecutedType.Failed);
            return;
        }

        action.Execute();
    }

    #endregion

    //calculations
    #region Muscle
    
    [Serializable]
    public class AState : Action.State
    {
        public bool rootStat;

        public AState(string state, int amount) : base(state, amount)
        {
            this.state = state;
            this.amount = amount;
        }
    }

    //states
    public List<AState> states = new List<AState>();
    public State GetState()
    {
        int ret = GetHappiness();
        if (ret <= stats.pointCritical)
            return State.Stabilizing;
        if (ret <= stats.pointImproving)
            return State.Stabilizing;
        return State.Reward;
    }

    public int GetHappiness()
    {
        int ret = 100;
        foreach (AState aS in states)
            if (aS.rootStat)
                if (aS.amount < ret)
                    ret = aS.amount;
        return ret;
    }

    public enum State {Stabilizing, Improving, Reward }
    [SerializeField]
    private string rewardTag = "Reward", concedeTag = "Concede";

    //check planning > if time left try fill
    //if none create plan

    //list of most wanted to least = stabilizing ? fill hunger / health needs | maintaining ? keep filling but dont prioritize | inproving ? 
    //if not able to execute try next
    //if nothing, plan relax
    #region Goap
    //idle = at the start of lifecycle, at the end of plan
    public void Idle()
    {
        State myState = GetState();
        //most to least important
        List<Action.State> needs = new List<Action.State>();
        PlanImportance importance = PlanImportance.Normal;
        bool planned;

        switch (myState) {
            case State.Stabilizing:
                foreach (AState aS in states)
                    if(aS.rootStat)
                        if (aS.amount <= stats.pointCritical)
                            needs.Add(aS);
            importance = PlanImportance.Priority;
            break;
            //check planning
            //when making a plan you automatically make sure you get enough base needs for the job
            case State.Improving:
                foreach (Action.State s in improvementState)
                    needs.Add(s);
                foreach(Action a in intelligence.actions)
                    foreach(Action.State improvement in a.improvements)
                    {
                        int current = 0;
                        foreach(Action.State aState in currentState)
                            if(aState.state == improvement.state)
                            {
                                current = aState.amount;
                                break;
                            }

                        if (improvement.amount > current)
                        {
                            planned = false;
                            foreach (Plan plan in planning)
                                if (plan.goal == improvement.state)
                                {
                                    planned = true;
                                    break;
                                }
                            if(!planned)
                                needs.Add(improvement);
                        }
                    }
                foreach (AState aS in states)
                    if (aS.rootStat)
                        needs.Add(aS);
                break;
            default:
                break;
        }

        if(needs.Count == 0)
            needs.Add(new Action.State(rewardTag, 100 - GetHappiness()));

        needs.Sort();

        if(goap != null)
            StopCoroutine(goap);
        goap = StartCoroutine(GOAP_Planning(needs, importance));
    }

    private Coroutine goap;
    private IEnumerator GOAP_Planning(List<Action.State> wanted, PlanImportance importance) // only gives one option
    {
        if (wanted.Count == 0)
            wanted.Add(new Action.State(concedeTag, 100));

        List<ActionPath> succeeded = new List<ActionPath>();
        List<ActionPath> aps = new List<ActionPath>();
        ActionPath ap;

        List<ActionPath> newActions = new List<ActionPath>();

        List<ActionsBranchable> actionsByWant;
        ActionsBranchable branchable;

        List<BranchTemporaryEntry> possible = new List<BranchTemporaryEntry>();

        ActionsBranchable actionBranch;
        int calc = 0, old;
        bool fit, symbiont;

        int wantedChosen = -1;
        foreach (Action.State want in wanted)
        {
            wantedChosen++;
            aps.Clear();
            actionsByWant = GetBranchableActions(new List<Action.State> {want });
            //its certain that its only one right now
            actionBranch = actionsByWant[0];
            foreach (Action action in actionBranch.actions)
                aps.Add(new ActionPath(action, null));

            while (aps.Count > 0)
            {
                calc++;
                if(calc > intelligence.calcsPerSecond)
                {
                    calc = 0;
                    yield return null;
                }

                ap = aps[0];
                aps.RemoveAt(0);

                //check if able to execute
                #region Execute Check
                fit = true;
                symbiont = false;
                foreach (Action action in ap.actions)
                {
                    if (action.requirements.Count == 0)
                        symbiont = true;
                    foreach (Action.State requirement in action.requirements)
                    {
                        symbiont = false;
                        foreach (AState aState in states)
                            if (requirement.state == aState.state)
                            {
                                symbiont = true;
                                if (requirement.amount > aState.amount)
                                {
                                    fit = false;
                                    break;
                                }
                            }
                        if (!fit || !symbiont)
                            break;
                    }
                    if (!fit)
                        break;
                }

                if (fit && symbiont)
                {
                    succeeded.Add(ap);
                    continue;
                }
                #endregion

                //foreach action get branches
                newActions.Clear();
                fit = true;
                foreach(Action oldAction in ap.actions)
                {
                    actionsByWant = GetBranchableActions(oldAction.requirements);
                    if (actionsByWant.Count == 0)
                        continue;

                    possible.Clear();
                    foreach (Action a in actionsByWant[0].actions)
                        possible.Add(new BranchTemporaryEntry(null, a));

                    for (int i = 1; i < actionsByWant.Count; i++)
                    {
                        branchable = actionsByWant[i];
                        old = possible.Count - 1;
                        //multiply all excisting with each of this list
                        for (int _old = old; _old > -1; _old--)
                            foreach (Action a in branchable.actions)
                                possible.Add(new BranchTemporaryEntry(possible[_old], a));

                        //remove all the old ones
                        for (int _i = old; _i > 0; _i--)
                            possible.RemoveAt(_i);
                    }

                    for (int p = possible.Count - 1; p > -1; p--)
                        if (possible[p].actions.Count < actionsByWant.Count)
                            possible.RemoveAt(p);

                    if (possible.Count == 0)
                    {
                        fit = false;
                        break;
                    }

                    //add actions to temp list
                    foreach (BranchTemporaryEntry bte in possible)
                    {
                        //remove functions that are executed multiple times
                        for (int i = bte.actions.Count - 1; i > -1; i--)
                            for (int _i = bte.actions.Count - 1; _i > -1; _i--)
                            {
                                if (i == _i)
                                    continue;
                                if (bte.actions[i] == bte.actions[_i])
                                    bte.actions.RemoveAt(i);
                            }

                        newActions.Add(new ActionPath(bte.actions, ap));
                    }
                }

                if (!fit)
                    continue;

                foreach (ActionPath aP in newActions)
                    aps.Add(aP);
            }

            //if it continues the loop, the current one has not been solved
            if (succeeded.Count == 0)
                continue;
            succeeded.Sort();
            int choice = Mathf.RoundToInt(Mathf.Lerp(0, succeeded.Count - 1, (float)intelligence.value / 100));

            #region Debug
            /*
            //temporary debug
            foreach (ActionPath e in succeeded)
            {
                string s = "";
                ActionPath _e = e;
                while (true)
                {
                    foreach (Action a in _e.actions)
                        s += a.name + " ";
                    if (!(_e.parent != null))
                        break;
                    _e = _e.parent;
                }

                print(s);
            }
            */
            #endregion

            //convert action path into a planning
            Plan newPlan = new Plan();
            newPlan.goal = wanted[wantedChosen].state;
            ActionPath current = succeeded[choice];
            while (true)
            {
                foreach(Action action in current.actions)
                    newPlan.parts.Add(action);
                if (!(current.parent != null))
                    break;
                current = current.parent;
            }

            InsertPlan(newPlan, importance);
            StartPlan();
            yield break;
        }

        //a man without a plan
    }

    #endregion

    private class BranchTemporaryEntry
    {
        public List<Action> actions = new List<Action>();
        public BranchTemporaryEntry(BranchTemporaryEntry old, Action action)
        {
            if (old != null)
                foreach (Action a in old.actions)
                    actions.Add(a);
            actions.Add(action);
        }
    }

    private class ActionsBranchable
    {
        public Action.State state;
        public List<Action> actions = new List<Action>();

        public ActionsBranchable(Action.State state)
        {
            this.state = state;
        }
    }

    private int gbaCalc;
    private List<ActionsBranchable> GetBranchableActions(List<Action.State> wanted)
    {
        //check all requirements
        List<ActionsBranchable> ret = new List<ActionsBranchable>();
        foreach (Action.State requirements in wanted)
            ret.Add(new ActionsBranchable(requirements));

        //get all actions based on those requirements
        foreach (Action a in intelligence.actions)
            foreach (Action.State aS in a.rewards)
                foreach (ActionsBranchable aB in ret)
                    if (aB.state.state == aS.state)
                    {
                        //check if requirements match
                        gbaCalc = 0;
                        if (aB.state.type == Action.RewardType.Adds)
                            foreach (AState _aS in states)
                                if (_aS.state == aS.state)
                                {
                                    gbaCalc = _aS.amount;
                                    break;
                                }

                        if (aB.state.amount > gbaCalc + aS.amount)
                            continue;
                        if(a.WillExecute())
                            aB.actions.Add(a);
                    }

        return ret;
    }

    public void ChangeValueState(Action.RewardType type, string name, int amount)
    {
        foreach(AState state in states)
            if(state.state == name)
            {
                if (type == Action.RewardType.Sets)
                    state.amount = amount;
                else
                {
                    state.amount += amount;
                    if (state.amount > 100)
                        state.amount = 100;
                    else if (state.amount < 0)
                        state.amount = 0;
                }
            }
    }

    private class ActionPath : IComparable<ActionPath>
    {
        public List<Action> actions;
        public ActionPath parent;

        public int Duration
        {
            get
            {
                int ret = 0;
                Action old = null;
                foreach (Action a in actions)
                {
                    ret += a.Duration(old);
                    old = a;
                }
                return ret;
            }
        }

        public ActionPath(List<Action> actions, ActionPath parent)
        {
            this.actions = actions;
            this.parent = parent;
        }

        public ActionPath(Action action, ActionPath parent)
        {
            actions = new List<Action> { action };
            this.parent = parent;
        }

        public int CompareTo(ActionPath other)
        {
            if (other == null)
                return 1;
            return Duration - other.Duration;
        }
    }

    #region Tools

    private List<Action> ConvertActionListToNew(List<Action> old)
    {
        List<Action> ret = new List<Action>();
        foreach (Action a in old)
            ret.Add(a);
        return ret;
    }

    private delegate void Skippable();
    private void FrameSkip(Skippable skippable)
    {
        StartCoroutine(_FrameSkip(skippable));
    }

    private IEnumerator _FrameSkip(Skippable skippable)
    {
        yield return null;
        skippable();
    }

    #endregion

    #endregion
}
