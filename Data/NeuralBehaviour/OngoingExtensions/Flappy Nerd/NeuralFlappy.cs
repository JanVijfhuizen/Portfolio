using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Jext;
using UnityEngine.UI;

/* 
For this I used this as a point of reference:
https://www.askforgametask.com/tutorial/machine-learning-algorithm-flappy-bird/

Of course I'm using my own neural network so internally it's completely different.
I mainly used it to decide how big my network would be and how the fitness would be calculated.

Intake opdracht specifiek:

Ik denk dat ik een beetje een gekke kant op ben gegaan met de "twist" maar ik hoop dat het gewaardeerd wordt xD

Op art gebied heb ik er minder moeite in gestoken dan ik normaal zou doen, maar dat was omdat het coderen / testen / trainen
hiervan gewoon te veel tijd kostte om me nog ergens anders mee bezig te houden.

Ik heb toch C# gebruikt in plaats van javascript maar dat was vooral omdat
ik het dan kon aansluiten op mijn neural network en het voor mij nu ook sneller werkte.
*/

public class NeuralFlappy : NeuralBehaviour
{
    [SerializeField]
    private bool aiPlays, train, load;

    private NeuralSaveData saveData = new NeuralSaveData();
    //save start position for a reset when the AI / player fails
    private Vector3 startPos;
    private Rigidbody rb;

    [SerializeField]
    private Text textScore, generationData;

    protected void Awake()
    {
        if (load)
            saveData = Load();

        //this is also set in the editor but I like this as a failsafe
        outputSize = 1;
        startPos = transform.position;
        rb = GetComponent<Rigidbody>();

        StartCoroutine(SpawnObstacles());

        //when the AI shows off
        if (aiPlays && !train)
            StartCoroutine(PlayAI());

        //when the player takes the wheel
        if (!aiPlays || !train)
            generationData.gameObject.SetActive(false);
    }

    /*
    since the training is in a couroutine I found that the best way to replicate the 
    results were to put this in one as well
    */
    private IEnumerator PlayAI()
    {
        while (true)
        {
            //NeuralOutput contains all sort of information but it's mainly ment for getting the output (jump or not)
            NeuralOutput nO = Call();
            TryJump(nO.output[0]);
            yield return null;
        }
    }

    private NeuralOutput nO;
    private void Update()
    {
        //training
        if (aiPlays)
        {
            if (train)
            {
                if (Input.GetButtonDown("Jump")) //saving
                {
                    Save(saveData);
                    Debug.Log("Saved!");
                }

                nO = Call();
                generationData.text = "Generation " + //updating UI information when training
                    nO.generation + " " +
                    (int)nO.progression + "%";
                if (nO.currentlyTrainedNetworkIndex == 0)
                    generationData.text += " (Best of generation)";

                Train(); //enable continuation of the training
            }
            return;
        }

        //player input
        if (Input.GetButtonDown("Jump"))
            TryJump(1);
    }

    [SerializeField]
    private float timeBetweenObstacles = 1.5f, 
        obstacleMaxHeightVariance, obstacleLifeTime;
    [SerializeField]
    private GameObject obstacle;
    [SerializeField]
    private Transform obstacleSpawnPos;
    private List<GameObject> spawnedObjects = new List<GameObject>();
    private List<GameObject> SpawnedObjects
    {
        get{
            spawnedObjects.RemoveAll(x => x == null); //since the objects get destroyed after x seconds without warning
            return spawnedObjects;
        }
        set
        {
            spawnedObjects = value;
        }
    }
    private IEnumerator SpawnObstacles()
    {
        GameObject spawnedObstacle;
        Vector3 vec;
        while (true)
        {
            spawnedObstacle = Instantiate(obstacle, obstacleSpawnPos);
            SpawnedObjects.Add(spawnedObstacle);

            //create difference in height
            vec = spawnedObstacle.transform.position;
            vec.y += Random.Range(-obstacleMaxHeightVariance, obstacleMaxHeightVariance);
            spawnedObstacle.transform.position = vec;

            Destroy(spawnedObstacle, obstacleLifeTime);
            yield return new WaitForSeconds(timeBetweenObstacles);
        }
    }

    [SerializeField]
    private float jumpForce;
    private void TryJump(float input)
    {
        if (input < 0.5f) //the input ranges 0 to 1
            return;
        rb.Reset();
        rb.AddForce(0, jumpForce, 0, ForceMode.Impulse);
    }

    private void ResetBird() 
    {
        rb.Reset(); //custom Jext function, resets all velocity
        transform.position = startPos;

        SpawnedObjects.ForEach(x => Destroy(x)); //reset level
        SpawnedObjects.Clear();

        failed = false;
    }

    private bool failed; //this is the only way I can get inside a working coroutine
    public void FlappyFailed()
    {
        failed = true;
    }

    public int Score
    {
        get
        {
            return DistanceTravelled - DistanceToObstacleY;
        }
    }

    #region Calculate Distance

    public int DistanceTravelled
    {
        get
        {
            return (int)timeFlown;
        }
    }

    private GameObject GetClosestObstacle()
    {
        GameObject ret = null;
        SpawnedObjects.SortByClosest(transform.position); //custom Jext function
        if(SpawnedObjects.Count > 0)
        {
            foreach (GameObject spawned in SpawnedObjects) {
                if (transform.position.x < spawned.transform.position.x) //if the object is behind the player
                    ret = spawned;
                /*this is purely visually interesting since it shows what the AI is interested in
                I normally wouldn't use such an expensive for loop so many times a frame but it's nice for debugging
                 */
                foreach (SpriteRenderer sR in spawned.GetComponentsInChildren<SpriteRenderer>())
                    sR.color = spawned == ret ? Color.blue : Color.white;
            }
            return ret;
        }
        return null;
    }

    public int DistanceToObstacleX //distance to closest obstacle
    {
        get
        {
            Vector3 vec = SpawnedObjects.Count == 0 ? obstacleSpawnPos.position:
                GetClosestObstacle().transform.position;
            return (int)Mathf.Abs(vec.x - transform.position.x);
        }
    }

    public int DistanceToObstacleY
    {
        get
        {
            Vector3 vec = SpawnedObjects.Count == 0 ? obstacleSpawnPos.position :
                GetClosestObstacle().transform.position;
            return (int)(vec.y - transform.position.y);
        }
    }

    #endregion

    private float timeFlown; //this is the way I set a network it's fitness
    private int iTextScore = 0; //I could read the string in textScore but this is cheaper
    private int TextScore
    {
        get
        {
            return iTextScore;
        }
        set
        {
            iTextScore = value;
            textScore.text = "Score: " + iTextScore;
        }
    }
    protected override IEnumerator Rate(NeuralNetwork net)
    {
        //reset stuff for the new network
        ResetBird();
        timeFlown = 0;
        TextScore = 0;

        while(!failed)
        {
            timeFlown += Time.deltaTime;
            if (Input.GetMouseButtonDown(0)) //when a network goes infinite this is the only way to "break" out and be able to save it
                break; //(it is properly sorted when the next generation begins)
            TryJump(net.GetNext(GetInput(true))[0]);
            yield return null;
        }
        net.score = Score;
    }

    protected override List<float> GetInput(bool isTraining) //the data the network uses to solve the problem
    {
        List<float> ret = new List<float>() {transform.position.y};
        ret.Add(DistanceToObstacleX);
        ret.Add(DistanceToObstacleY);
        return ret;
    }

    private void OnTriggerEnter(Collider collision)
    {
        //doesnt need to check what else it is since there are only obstacles
        if (collision.transform.tag == "Point")
            TextScore++;
        else
            FlappyFailed();
    }
}
