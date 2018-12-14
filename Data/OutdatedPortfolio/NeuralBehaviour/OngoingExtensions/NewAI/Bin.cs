using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Jext;
using System;

public abstract class Bin : MonoBehaviour
{
    [Serializable]
    public class Action
    {
        [HideInInspector]
        public NeuralNetwork network;
        public BinAction action;
        public Execute Function
        {
            get
            {
                return action.Execute;
            }
        }

        public void Execute(BinCharacter character)
        {
            Function(network.GetNext(), character);
        }
    }

    [Serializable]
    public class BinCharacter : IComparable<BinCharacter>
    {
        [HideInInspector]
        public NeuralNetwork binaryPicker;
        public int[] binarySize;
        public int binaryRange, memorySlotSize;
        public float mutateChance;

        private int oldActionCount;
        public int NewAction
        {
            get
            {
                if (oldActionCount < actions.Count)
                {
                    oldActionCount = actions.Count;
                    return 1;
                }
                else
                    return 0;
            }
        }
        public List<Action> actions = new List<Action>();
        public List<Memory> memories = new List<Memory>();

        /// <summary>
        /// Also add all memory data
        /// </summary>
        /// <returns></returns>
        public virtual List<float> GetInput()
        {
            List<float> ret = new List<float>() { actions.Count, NewAction, memorySlotSize };
            ret.Add(ret.Count); //memory start index
            return ret;
        }

        public virtual int CompareTo(BinCharacter other)
        {
            if (other == null)
                return 1;
            return Mathf.RoundToInt(other.binaryPicker.score - binaryPicker.score);
        }

        public virtual void SaveToMemory(Memory memory)
        {

        }

        public virtual void Next()
        {
            string nextBinAsString = "";
            List<float> nextBinAsArr = binaryPicker.GetNext();
            nextBinAsArr.ForEach(x => nextBinAsString += Mathf.RoundToInt(x));
            int next = Convert.ToInt32(nextBinAsString, 2);

            if (next >= actions.Count)
                return;
            actions[next].Execute(this);
        }

        public virtual void Init()
        {
            binaryPicker = new NeuralNetwork(
                GetInput().Count, binaryRange, binarySize, mutateChance, memorySlotSize, GetInput);
        }

        public virtual bool IsBreedable(BinCharacter other)
        {
            if (other.GetInput().Count < GetInput().Count ||
                other.binaryRange != binaryRange ||
                other.memorySlotSize != memorySlotSize ||
                other.binarySize.Length != binarySize.Length)
                return false;

            for (int i = 0; i < binarySize.Length; i++)
                if (binarySize[i] != other.binarySize[i])
                    return false;

            return true;
        }

        #region Evolve

        public void Mutate(bool mutateActions)
        {
            binaryPicker.Shift();
            if(mutateActions)
                actions.ForEach(x => x.network.Shift());
        }

        public BinCharacter Breed(BinCharacter other)
        {
            BinCharacter child = Methods.Clone(this);
            child.binaryPicker.Combine(
                new List<NeuralNetwork> { binaryPicker, other.binaryPicker });
            for (int action = 0; action < actions.Count; action++)
                child.actions[action].network.Combine(new List<NeuralNetwork> {
                    actions[action].network, other.actions[action].network });
            return child;
        }

        #endregion
    }

    public abstract class Memory { };

    public delegate void Execute(List<float> input, BinCharacter bC);
    public delegate List<float> BinInput();

    [Serializable]
    public class NeuralNetwork : IComparable<NeuralNetwork>
    {
        public BinInput inputFunc;
        public List<float> Input
        {
            get
            {
                return inputFunc();
            }
        }
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
        public NeuralNetwork(int inputSize, int outputSize, int[] hiddenLayers, float mutateChance, int memorySlotSize, BinInput input)
        {
            inputFunc = input;

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

            memoryPrefab = new float[memorySlotSize][];
            for (int memorySlot = 0; memorySlot < memoryPrefab.Length; memorySlot++)
                memoryPrefab[memorySlot] = new float[hiddenLayers[0]];
            RanMemory();
        }

        #region Shortcuts

        private void RanMemory()
        {
            for (int i = 0; i < memoryPrefab.Length; i++)
                for (int weight = 0; weight < memoryPrefab[i].Length; weight++)
                    memoryPrefab[i][weight] = Ran();
        }

        private float Ran()
        {
            return UnityEngine.Random.Range(-0.5f, 0.5f);
        }

        #endregion

        public float[][] memoryPrefab;

        /// <summary>
        /// Returns a list with floats ranging between 0 and 1
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public List<float> GetNext()
        {
            List<float> input = Input;
            //update it to hold new input
            if (input.Count != weights[0].Length)
            {
                float[][] newInputLayer = new float[input.Count][];
                float[] weightConnections;
                for (int neuron = 0; neuron < weights[0].Length; neuron++)
                {
                    weightConnections = weights[0][neuron];
                    for (int weight = 0; weight < input.Count; weight++)
                    {
                        newInputLayer[neuron][weight] =
                            weight < weightConnections.Length ?
                            weightConnections[weight] : memoryPrefab[neuron][weight - input.Count];
                    }
                }

                weights[0] = newInputLayer;
            }

            //nX = neurons list length, nX = neurons y length, nZ = next neurons list length
            int nX = neurons.Length - 1, nY, nZ;
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

                results = Methods.CloneList(newInput);
                for (int f = 0; f < results.Count; f++)
                    results[f] = Methods.Activation(results[f]);

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
            ShiftMemory();
        }

        private void ShiftWeights()
        {
            for (int x = 0; x < weights.Length; x++)
                for (int y = 0; y < weights[x].Length; y++)
                    for (int z = 0; z < weights[x][y].Length; z++)
                        weights[x][y][z] = Ran(weights[x][y][z]);
        }

        private void ShiftMemory()
        {
            for (int x = 0; x < memoryPrefab.Length; x++)
                for (int y = 0; y < memoryPrefab[x].Length; y++)
                    memoryPrefab[x][y] = Ran(memoryPrefab[x][y]);
        }

        public void Combine(List<NeuralNetwork> parents)
        {
            int ran;
            for (int x = 0; x < weights.Length; x++)
                for (int y = 0; y < weights[x].Length; y++)
                    for (int z = 0; z < weights[x][y].Length; z++)
                    {
                        ran = UnityEngine.Random.Range(0, parents.Count);
                        weights[x][y][z] = parents[ran].weights[x][y][z];
                    }

            for (int x = 0; x < memoryPrefab.Length; x++)
                for (int y = 0; y < memoryPrefab[x].Length; y++)
                {
                    ran = UnityEngine.Random.Range(0, parents.Count);
                    memoryPrefab[x][y] = Ran(parents[ran].memoryPrefab[x][y]);
                }
        }

        private int r;
        private float Ran(float f)
        {
            if (UnityEngine.Random.Range(0, 100) > mutateChance)
                return f;
            r = UnityEngine.Random.Range(0, 4);
            switch (r)
            {
                case 0:
                    f *= -1;
                    break;
                case 1:
                    //increase 0-100
                    f *= UnityEngine.Random.Range(0, 1f) + 1;
                    break;
                case 2:
                    //decrease 0-100
                    f *= UnityEngine.Random.Range(0, 1f);
                    break;
                case 3:
                    //set new random
                    f = UnityEngine.Random.Range(-0.5f, 0.5f);
                    break;
            }
            return f;
        }

        public int CompareTo(NeuralNetwork other)
        {
            if (other == null)
                return -1;
            return (int)(other.score - score);
        }
    }
}