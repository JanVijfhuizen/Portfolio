using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;
using UnityEngine;

/// <summary>
/// Functions: GetInput, Rate, Call, Train, Load, Save. See the script "ChaseBox" for an example.
/// </summary>
public abstract class NeuralBehaviour : MonoBehaviour {

    [SerializeField]
    protected int outputSize;
    private int inputCount;

    protected NeuralNetwork Network
    {
        get
        {
            return generation[0];
        }
    }
    private List<NeuralNetwork> generation = new List<NeuralNetwork>();
    [SerializeField]
    private int generationSize = 24, mutateCount = 4;
    [SerializeField]
    private int[] hiddenLayers;
    [SerializeField, Range(0, 100)]
    private float mutateChance;

    private bool initialized;
    private void Initialize()
    {
        initialized = true;
        inputCount = GetInput(false).Count;
        for (int net = 0; net < generationSize; net++)
            generation.Add(new NeuralNetwork(inputCount, outputSize, hiddenLayers, mutateChance));
    }

    /// <summary>
    /// This will be used to determine the quality of the neural network. Note: set the score of the network if you want anything to work at all.
    /// </summary>
    protected abstract IEnumerator Rate(NeuralNetwork net);

    /// <summary>
    /// This is being used to get the realtime input. This is the data that the neuralnetwork wil be working with.
    /// </summary>
    /// <returns>Your input for the neural network to work with.</returns>
    protected abstract List<float> GetInput(bool isTraining);

    /// <summary>
    /// The network will give it's opinion on the matter. It's opinion will improve over time.
    /// </summary>
    /// <returns>The output of the neural network</returns>
    protected NeuralOutput Call()
    {
        if (!initialized)
            Initialize();
        nOutput.Convert(Network.GetNext(GetInput(false)), generationNum, Network);
        nOutput.progression = progression;
        return nOutput;
    }

    private NeuralOutput nOutput;
    protected struct NeuralOutput
    {
        public float progression;
        public List<float> output;
        public int generation;
        public NeuralNetwork network;

        public void Convert(List<float> output, int generation, NeuralNetwork network)
        {
            this.output = output;
            this.generation = generation;
            this.network = network;
        }
    }

    /// <summary>
    /// This is being used to train the neural network. Without this the neural network will not improve at all.
    /// </summary>
    public void Train()
    {
        if (!initialized)
            Initialize();
        if (training != null)
            return;
        training = StartCoroutine(Shift());
    }

    protected int generationNum = 1;
    private Coroutine training;
    private IEnumerator Shift()
    {
        int half = generation.Count / 2, succeeded = half - mutateCount;

        for (int net = 0; net < half; net++)
            generation.RemoveAt(generation.Count - 1);

        for (int net = 0; net < succeeded; net++)
            generation.Add(ConvertNeuralNetwork(generation[net]));

        for (int mutant = 0; mutant < mutateCount; mutant++)
            generation.Add(new NeuralNetwork(inputCount, outputSize, hiddenLayers, mutateChance));

        progression = 0;
        foreach (NeuralNetwork net in generation)
        {
            progression += (float)100 / generationSize;
            yield return StartCoroutine(Rate(net));
        }

        generation.Sort();
        generationNum++;
        training = null;    
    }

    private static float progression;

    #region Neural Network Data

    /// <summary>
    /// This is my own inheritable neural network.
    /// </summary>
    [System.Serializable]
    public class NeuralNetwork : System.IComparable<NeuralNetwork>
    {
        public float score;

        public float mutateChance;
        public float[][] neurons;
        public float[][][] weights;

        private int InputLength
        {
            get
            {
                if (neurons[0].Length == 0)
                {
                    Debug.LogError("input is zero!");
                    return 0;
                }
                return neurons[0].Length;
            }
        }

        private int OutputLength
        {
            get
            {
                if (neurons[neurons.Length - 1].Length == 0)
                {
                    Debug.LogError("output is zero!");
                    return 0;
                }
                return neurons[neurons.Length - 1].Length;
            }
        }

        /// <summary>
        /// This default constructor is nessecery to serialize it for XML. 
        /// If you want to dig deeper and make your own NeuralNetwork instance use this constructor: 
        /// NeuralNetwork(int inputSize, int outputSize, int[] hiddenLayers, int mutateChance).
        /// </summary>
        public NeuralNetwork()
        {

        }

        /// <summary>
        /// Initializes a neural network before use.
        /// </summary>
        /// <param name="inputSize">Amount of inputs for the neural network.</param>
        /// /// <param name="outputSize">Amount of output for the neural network.</param>
        /// /// <param name="hiddenLayers">Amount of hidden layers for the neural network.</param>
        public NeuralNetwork(int inputSize, int outputSize, int[] hiddenLayers, float mutateChance)
        {
            //debugging
            if (inputSize <= 0 || outputSize <= 0)
            {
                Debug.LogError("In or output size is zero!");
                return;
            }

            this.mutateChance = mutateChance;

            int nLength, wLength;
            #region Neurons
            neurons = new float[hiddenLayers.Length + 2][];

            //init hidden layer neurons
            for (int layer = 0; layer < hiddenLayers.Length; layer++)
                neurons[layer + 1] = new float[hiddenLayers[layer]];

            //init input neurons
            neurons[0] = new float[inputSize];

            /*init output neurons
            reusing old variable for different purpose*/
            nLength = neurons.Length - 1;
            neurons[nLength] = new float[outputSize];
            #endregion

            #region Weights
            //init weights (and set node + weight values)
            weights = new float[nLength][][];
            for (int layer = 0; layer < nLength; layer++)
                weights[layer] = new float[neurons[layer].Length][];

            //get each connection
            for (int layer = 0; layer < nLength; layer++)
                for (int neuron = 0; neuron < neurons[layer].Length; neuron++)
                {
                    //create weights
                    weights[layer][neuron] = new float[neurons[layer + 1].Length];

                    //randomize base values for weights
                    wLength = weights[layer][neuron].Length;
                    for (int weigth = 0; weigth < wLength; weigth++)
                        weights[layer][neuron][weigth] = Ran();
                }
            #endregion
        }

        #region Shortcuts

        private float Ran()
        {
            return Random.Range(-0.5f, 0.5f);
        }

        #endregion

        /// <summary>
        /// Returns a list with floats ranging between 0 and 1
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public List<float> GetNext(List<float> input)
        {
            //nX = neurons list length, nX = neurons y length, nZ = next neurons list length
            int nX = neurons.GetLength(0) - 1, nY, nZ;
            float res;
            List<float> results = input, newInput = new List<float>();
            for (int neuronList = 0; neuronList < nX; neuronList++)
            {
                nY = neurons[neuronList].Length;
                nZ = neurons[neuronList + 1].Length;
                for (int i = 0; i < nZ; i++)
                    newInput.Add(0);

                for (int neuron = 0; neuron < nY; neuron++)
                {
                    res = results[neuron];
                    for (int weightConnection = 0; weightConnection < nZ; weightConnection++)
                        newInput[weightConnection] += res * weights[neuronList][neuron][weightConnection];
                }

                results = ConvertList(newInput);
                for (int f = 0; f < results.Count; f++)
                    results[f] = Activation(results[f]);
                    
                newInput.Clear();
            }

            //change it from -1 and 1 to 0 and 1
            for (int returnable = 0; returnable < results.Count; returnable++)
                results[returnable] = (results[returnable] + 1) / 2;

            return results;
        }

        public void Shift()
        {
            ShiftWeights();
        }

        private void ShiftWeights()
        {
            for (int x = 0; x < weights.Length; x++)
                for (int y = 0; y < weights[x].Length; y++)
                    for (int z = 0; z < weights[x][y].Length; z++)
                        weights[x][y][z] = Ran(weights[x][y][z]);
        }

        private int r;
        private float Ran(float f)
        {
            if (Random.Range(0, 100) > mutateChance)
                return f;
            r = Random.Range(0, 4);
            switch (r)
            {
                case 0:
                    f *= -1;
                    break;
                case 1:
                    //increase 0-100
                    f *= Random.Range(0, 1f) + 1;
                    break;
                case 2:
                    //decrease 0-100
                    f *= Random.Range(0, 1f);
                    break;
                case 3:
                    //set new random
                    f = Random.Range(-0.5f, 0.5f);
                    break;
            }
            return f;
        }

        public int CompareTo(NeuralNetwork other)
        {
            if (other == null)
                return -1;
            if (score > other.score)
                return -1;
            return 1;
        }
    }

    #endregion

    /// <summary>
    /// Uses the Tahn Function.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    private static float Activation(float value)
    {
        return (float)System.Math.Tanh(value);
    }

    private static List<T> ConvertList<T>(List<T> list)
    {
        List<T> ret = new List<T>();
        foreach (T t in list)
            ret.Add(t);
        return ret;
    }

    private static T[] ConvertList<T>(T[] list)
    {
        T[] ret = new T[list.Length];
        for (int i = 0; i < list.Length; i++)
            ret[i] = list[i];
        return ret;
    }

    private NeuralNetwork ConvertNeuralNetwork(NeuralNetwork other)
    {
        NeuralNetwork ret = Clone(other);
        ret.Shift();
        return ret;
    }

    private static int RanInt(int i)
    {
        int r = Random.Range(0,1);
        if (r == 0)
            r = i - 1;
        else
            r = i + 1;
        return r;
    }

    #region Serializer for objects

    //source: https://stackoverflow.com/questions/78536/deep-cloning-objects
    /// <summary>
    /// Perform a deep Copy of the object.
    /// </summary>
    /// <typeparam name="T">The type of object being copied.</typeparam>
    /// <param name="source">The object instance to copy.</param>
    /// <returns>The copied object.</returns>
    public static T Clone<T>(T source)
    {
        if (!typeof(T).IsSerializable)
        {
            throw new System.ArgumentException("The type must be serializable.", "source");
        }

        // Don't serialize a null object, simply return the default for that object
        if (ReferenceEquals(source, null))
        {
            return default(T);
        }

        IFormatter formatter = new BinaryFormatter();
        Stream stream = new MemoryStream();
        using (stream)
        {
            formatter.Serialize(stream, source);
            stream.Seek(0, SeekOrigin.Begin);
            return (T)formatter.Deserialize(stream);
        }
    }

    #endregion

    #region Loading / Saving

    [SerializeField]
    protected string folderNameSaveData;
    [SerializeField, Tooltip("Only write the name of the file, not the extension (.xml)")]
    protected string fileNameSaveData;
    private string _filePath;

    /// <summary>
    /// XML requires a default constructor. For instance: (public NeuralSaveData(){})
    /// </summary>
    public class NeuralSaveData
    {
        public float progression;
        public int inputSize, outputSize;
        /// <summary>
        /// from best [0] to worst
        /// </summary>
        public List<NeuralNetwork> neuralNetworks;    

        public NeuralSaveData()
        {

        }
    }

    protected NeuralSaveData Load()
    {
        return Load<NeuralSaveData>();
    }

    protected T Load<T>() where T : NeuralSaveData
    {
        MakeFolderPath();
        if (!File.Exists(_filePath))
        {
            Debug.LogError("No Save Data has been found!");
            return null;
        }
        XmlSerializer serializer = new XmlSerializer(typeof(T));
        FileStream stream = new FileStream(_filePath, FileMode.Open);
        T ret = (T)serializer.Deserialize(stream) as T;
        stream.Close();
        generation = ret.neuralNetworks;

        initialized = true;
        inputCount = ret.inputSize;
        outputSize = ret.outputSize;
        return ret;
    }

    /// <summary>
    /// Expect a big .xml file and a freeze for about half a second. These things are massive.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="saveData"></param>
    protected void Save<T>(T saveData) where T : NeuralSaveData
    {
        saveData.neuralNetworks = generation;
        saveData.inputSize = inputCount;
        saveData.outputSize = outputSize;

        MakeFolderPath();
        XmlSerializer serializer = new XmlSerializer(typeof(T));
        FileStream stream = new FileStream(_filePath, FileMode.Create);
        serializer.Serialize(stream, saveData);
        stream.Close();
    }

    /// <summary>
    /// I don't own a Mac and I don't have a linux operating system so I can't test whether or not this works on those platforms.
    /// Please leave feedback if it doesn't.
    /// </summary>
    private void MakeFolderPath()
    {
        char s = Path.DirectorySeparatorChar;

#if UNITY_STANDALONE

        _filePath = Application.dataPath + s + folderNameSaveData + s + fileNameSaveData + ".xml";

#endif

#if UNITY_ANDROID || UNITY_IOS

        _filePath =  Application.persistentDataPath + s + folderNameSaveData + s + fileNameSaveData + ".xml";
#endif
    }

    #endregion
}
