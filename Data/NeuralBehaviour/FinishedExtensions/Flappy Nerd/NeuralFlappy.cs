using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Jext;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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
    public enum PlayType {AI, Player, AITraining }
    [SerializeField]
    private PlayType playType;
    private PlayType _PlayType
    {
        get
        {
            return playType;
        }
        set
        {
            playType = value;
            switchTo.text = "Switch to " + (playType == PlayType.Player ? "AI" : "Player");
            switch (playType)
            {
                case PlayType.AI:
                    aiTrainingToggle.color = Color.white;
                    break;
                case PlayType.AITraining:
                    aiTrainingToggle.color = Color.green;
                    break;
                case PlayType.Player:
                    aiTrainingToggle.color = Color.grey;
                    break;
            }
        }
    }

    public class FlappySaveData : NeuralSaveData
    {
        public int highScore = 0;
    }
    private FlappySaveData saveData = new FlappySaveData();
    //save start position for a reset when the AI / player fails
    private Vector3 startPos, startPosGameOverScreen;
    private Rigidbody rb;
    private Animator anim;
    public static bool isDead;

    [SerializeField]
    private Text textScore, generationData, switchTo, gameOverScreenHighScore;
    [SerializeField]
    private Image aiTrainingToggle;
    [SerializeField]
    private RectTransform gameOverScreen;
    [SerializeField]
    private Image fadeToBlackObj;

    protected void Awake()
    {
        saveData = Load<FlappySaveData>();
        if (!(saveData != null)) //prevents unity errors
            saveData = new FlappySaveData();
        //this is also set in the editor but I like this as a failsafe
        outputSize = 1;
        startPos = transform.position;
        startPosGameOverScreen = gameOverScreen.localPosition;
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();

        Restart();
    }

    public void RestartWithFadeout()
    {
        StartCoroutine(_RestartWithFadeout());
    }

    [SerializeField]
    private float fadeSpeed = 10;
    private IEnumerator _RestartWithFadeout()
    {
        gameOverScreen.localPosition = startPosGameOverScreen;
        fadeToBlackObj.gameObject.SetActive(true); //multipurpose, this way it also blocks input

        yield return StartCoroutine(Methods.FadeToBlack(
            fadeToBlackObj, fadeSpeed, Methods.FadeType.FadeOut));

        SpawnedObjects.ForEach(x => Destroy(x));

        yield return StartCoroutine(Methods.FadeToBlack(
            fadeToBlackObj, fadeSpeed, Methods.FadeType.FadeIn));

        fadeToBlackObj.gameObject.SetActive(false);

        Restart();
    }

    private void Restart()
    {
        rb.isKinematic = false;
        gameOverScreen.localPosition = startPosGameOverScreen;
        ResetBird();
        TextScore = 0;
        isDead = false;
        anim.SetBool("FlappyFailed", false);
        StartCoroutine(SpawnObstacles());
        //when the AI shows off
        if (_PlayType == PlayType.AI)
            StartCoroutine(PlayAI());

        generationData.gameObject.SetActive(_PlayType == PlayType.AITraining);
    }

    /*
    since the training is in a couroutine I found that the best way to replicate the 
    results were to put this in one as well
    */
    private IEnumerator PlayAI()
    {
        while (!isDead)
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
        if (isDead)
            return;
        //training

        if (Input.GetButtonDown("Escape"))
        {
            //this is because of my network, I can't easily disable training yet
            SceneManager.LoadScene("FlappyMenu", LoadSceneMode.Single);
            return;
        }

        if (_PlayType == PlayType.AITraining)
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

        //player input
        if(_PlayType == PlayType.Player)
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
        while (!isDead)
        {
            spawnedObstacle = Instantiate(obstacle, obstacleSpawnPos);
            SpawnedObjects.Add(spawnedObstacle);

            //create difference in height
            vec = spawnedObstacle.transform.position;
            vec.y += Random.Range(-obstacleMaxHeightVariance, obstacleMaxHeightVariance);
            spawnedObstacle.transform.position = vec;

            spawnedObstacle.GetComponent<FlappyObstacle>().lifeTime = obstacleLifeTime;
            yield return new WaitForSeconds(timeBetweenObstacles);
        }
    }

    public void SwitchTo()
    {
        _PlayType = playType == PlayType.Player ? PlayType.AI : PlayType.Player;
    }

    public void ToggleChange()
    {
        if (playType == PlayType.Player)
            return;
        _PlayType = playType == PlayType.AI ? PlayType.AITraining : PlayType.AI;
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
        switch (_PlayType) {
            case PlayType.AITraining:
                failed = true;
                break;
            default:
                //game over
                _PlayType = playType;
                isDead = true;
                rb.isKinematic = true;
                anim.SetBool("FlappyFailed", true);
                gameOverScreenHighScore.text = "Highscore: " + saveData.highScore;
                StartCoroutine(MoveGameOverScreen());
                break;
        }
    }

    [SerializeField]
    private float gameOverScreenMoveSpeed;
    private IEnumerator MoveGameOverScreen()
    {
        while(gameOverScreen.localPosition.y < 0)
        {
            gameOverScreen.Translate(Vector3.up * gameOverScreenMoveSpeed * Time.deltaTime);
            yield return null;
        }
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
        SpawnedObjects.SortByClosest(transform.position); //custom Jext function
        if(SpawnedObjects.Count > 0)
        {
            GameObject ret = SpawnedObjects[0];
            foreach (GameObject spawned in SpawnedObjects) {
                if (transform.position.x < spawned.transform.position.x) //if the object is behind the player
                {
                    ret = spawned;
                    break;
                }
                /*this is purely visually interesting since it shows what the AI is interested in
                I normally wouldn't use such an expensive for loop so many times a frame but it's nice for debugging
                 */
            }
            foreach (GameObject spawned in SpawnedObjects)
                foreach (SpriteRenderer sR in spawned.GetComponentsInChildren<SpriteRenderer>())
                    sR.color = spawned == ret ? Color.blue : Color.white;

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
            //set possibly new high score
            if (iTextScore > saveData.highScore)
                saveData.highScore = iTextScore;
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
