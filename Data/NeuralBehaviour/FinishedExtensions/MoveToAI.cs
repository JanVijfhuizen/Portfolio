using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveToAI : NeuralBehaviour {

    [SerializeField]
    private Transform testAI, target;
    private Vector3 startPos;

    [SerializeField]
    private bool loadSaveData;
    public bool train;
    private NeuralSaveData saveData = new NeuralSaveData();

    [SerializeField]
    private float moveSpeed;
    private int maxScore;

    private void Awake()
    {
        maxScore = Mathf.CeilToInt(Mathf.Sqrt(Mathf.Pow(maxDisTarget, 2) + Mathf.Pow(maxDisTarget, 2)));
        startPos = transform.position;
        outputSize = 2;
        if (loadSaveData)
            saveData = Load();
        if (!train)
            testAI.gameObject.SetActive(false);
    }

    private int oldGeneration;
    private void Update()
    {
        if(train)
            Train();
        NeuralOutput nO = Call();

        if(train)
            if(oldGeneration < nO.generation)
            {
                print("Generation: " + nO.generation + ", score: " + nO.network.score + "/" + maxScore);
                oldGeneration++;
                Reset(transform);
                Save(saveData);
                return;
            }
        Move(nO.output, transform);
    }

    private void Reset(Transform t)
    {
        t.position = startPos;
        t.eulerAngles = Vector3.zero;
    }

    private void RanTargetPoint()
    {
        Vector3 pos = new Vector3(Ran(), Ran(), 0);
        target.position = pos;
    }

    [SerializeField]
    private float minDisTarget = 4, maxDisTarget = 8;
    private float Ran()
    {
        float ret = UnityEngine.Random.Range(-maxDisTarget, maxDisTarget);
        if (ret < minDisTarget && ret > 0)
            ret = minDisTarget;
        else if (ret > -minDisTarget && ret < 0)
            ret = -minDisTarget;
        return ret;
    }

    [SerializeField]
    private float animSpeed = 5;
    private void Move(List<float> output, Transform t)
    {
        Animator a = t.GetComponent<Animator>();
        a.speed = (Mathf.Abs(GetOutputDir(output[0])) + Mathf.Abs(GetOutputDir(output[1]))) / 2 * animSpeed;
        float calc = moveSpeed * Time.deltaTime;
        t.Translate(t.up * GetOutputDir(output[0]) * calc);
        t.Translate(t.right * GetOutputDir(output[1]) * calc);
    }
    
    private float GetOutputDir(float output)
    {
        return (output - 0.5f) * 2;
    }

    protected override List<float> GetInput(bool isTraining)
    {
        Transform t = isTraining ? testAI : transform;
        List<float> ret = new List<float>(){
            target.position.x - t.position.x, target.position.y - t.position.y };
        return ret;
    }

    [SerializeField]
    private float testTime, succeedTreshold;
    protected override IEnumerator Rate(NeuralNetwork net)
    {
        Reset(testAI);
        RanTargetPoint();
        List<float> output;
        float remaining = testTime;
        float score = Vector2.Distance(testAI.position, target.position);

        while (remaining > 0)
        {
            remaining -= Time.deltaTime;
            output = net.GetNext(GetInput(true));

            Move(output, testAI); 
            yield return null;
        }

        float range = Vector2.Distance(testAI.position, target.position);
        if (range < succeedTreshold)
            score = maxScore;
        else
            score -= range;
        net.score = score;
    }
}
