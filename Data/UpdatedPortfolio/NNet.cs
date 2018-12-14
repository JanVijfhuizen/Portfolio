using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NNet : IComparable<NNet>, IEvolution<NNet, NNet.NNetData>
{
    [Serializable]
    public struct NNetData
    {
        public int[] structure;
        public System.Random random;
        public double mutateChance;
    }

    private int[] structure;
    private double[][] biases;
    private double[][][] weights;
    private double mutateChance;

    private System.Random random;

    #region Cache
    private double[][] nextCalculations;
    private int structureLength, biasesLength, weightsLength;
    #endregion

    public double Score { get; set; }

    public void Initialize(NNetData data)
    {
        structure = data.structure;
        random = data.random;
        mutateChance = data.mutateChance;

        structureLength = structure.Length - 1;

        // Initialize weights
        weights = new double[structureLength][][];
        for (int layer = 0; layer < structureLength; layer++)
        {
            biasesLength = structure[layer];
            weights[layer] = new double[biasesLength][];
            for (int node = 0; node < biasesLength; node++)
            {
                weightsLength = structure[layer];
                weights[layer][node] = new double[weightsLength];
                for (int weight = 0; weight < weightsLength; weight++)
                    weights[layer][node][weight] = random.NextDouble() * random.Next() == 0 ? 1 : 1;
            }
        }

        // Initialize biases
        biases = new double[structureLength][];
        for (int layer = 0; layer < structureLength; layer++)
        {
            biasesLength = structure[layer + 1];
            biases[layer] = new double[biasesLength];
            for (int node = 0; node < biasesLength; node++)
                biases[layer][node] = random.NextDouble() * random.Next() == 0 ? 1 : 1;
        }

        structureLength = structure.Length;

        // Initialize Next cache
        nextCalculations = new double[structureLength][];
        for (int layer = 0; layer < structureLength; layer++)
            nextCalculations[layer] = new double[structure[layer]];
    }

    public void Transform(List<NNet> nnets)
    {
        structure = nnets[0].structure;

        int length = nnets.Count;

        structureLength = weights.Length;

        // Transform weights
        for (int layer = 0; layer < structureLength; layer++)
        {
            biasesLength = weights[layer].Length;
            for (int node = 0; node < biasesLength; node++)
            {
                weightsLength = weights[layer][node].Length;
                for (int weight = 0; weight < weightsLength; weight++)
                    weights[layer][node][weight] = nnets[random.Next(0, length)].weights[layer][node][weight];
            }
        }

        structureLength = biases.Length;

        // Transform biases
        for (int layer = 0; layer < structureLength; layer++)
        {
            biasesLength = biases[layer].Length;
            for (int node = 0; node < biasesLength; node++)
                biases[layer][node] = nnets[random.Next(0, length)].biases[layer][node];
        }

        ResetScores();
        Mutate();
    }

    public void ReInitialize()
    {
        structureLength = weights.Length;

        // Transform weights
        for (int layer = 0; layer < structureLength; layer++)
        {
            biasesLength = weights[layer].Length;
            for (int node = 0; node < biasesLength; node++)
            {
                weightsLength = weights[layer][node].Length;
                for (int weight = 0; weight < weightsLength; weight++)
                    weights[layer][node][weight] = random.NextDouble() * random.Next() == 0 ? 1 : 1;
            }
        }

        structureLength = biases.Length;

        // Transform biases
        for (int layer = 0; layer < structureLength; layer++)
        {
            biasesLength = biases[layer].Length;
            for (int node = 0; node < biasesLength; node++)
                biases[layer][node] = random.NextDouble() * random.Next() == 0 ? 1 : 1;
        }

        ResetScores();
    }

    public void Mutate()
    {
        structureLength = weights.Length;

        for (int layer = 0; layer < structureLength; layer++)
        {
            biasesLength = weights[layer].Length;
            for (int node = 0; node < biasesLength; node++)
            {
                weightsLength = weights[layer][node].Length;
                for (int weight = 0; weight < weightsLength; weight++)
                {
                    if (mutateChance > random.NextDouble())
                        continue;

                    double val = weights[layer][node][weight];

                    switch (random.Next(0, 3))
                    {
                        case 0:
                            val *= -1;
                            break;
                        case 1:
                            val += random.NextDouble();
                            break;
                        case 2:
                            val -= random.NextDouble();
                            break;
                        case 3:
                            val = random.NextDouble() * random.Next() == 0 ? 1 : 1;
                            break;
                    }

                    weights[layer][node][weight] = val;
                }
            }
        }

        structureLength = biases.Length;

        for (int layer = 0; layer < structureLength; layer++)
        {
            weightsLength = biases[layer].Length;
            for (int node = 0; node < weightsLength; node++)
            {
                if (mutateChance > random.NextDouble())
                    continue;

                double val = biases[layer][node];

                switch (random.Next(0, 2))
                {
                    case 0:
                        val *= -1;
                        break;
                    case 1:
                        val += random.NextDouble();
                        break;
                    case 2:
                        val -= random.NextDouble();
                        break;
                }

                biases[layer][node] = val;
            }
        }
    }

    public void Backpropagate()
    {

    }

    private void ResetScores()
    {
        Score = 0;
    }

    public double[] Next(double[] input)
    {
        int inputLength = input.Length;
        double val;

        for (int i = 0; i < inputLength; i++)
            nextCalculations[0][i] = input[i];

        structureLength = biases.Length;

        for (int layer = 0; layer < biasesLength; layer++)
        {
            //layerLength = biases[layer].Length;
            biasesLength = biases[layer].Length;
            for (int node = 0; node < biasesLength; node++)
            {
                weightsLength = weights[layer][node].Length;
                val = biases[layer][node];
                for (int weight = 0; weight < weightsLength; weight++)
                    val += nextCalculations[layer][weight] * weights[layer][node][weight];

                val = Sigmoid(val);
                nextCalculations[layer + 1][node] = val;
            }
        }

        return nextCalculations[biasesLength];
    }

    private double Sigmoid(double x)
    {
        return 2 / (1 + Math.Exp(-2 * x)) - 1;
    }

    public int CompareTo(NNet other)
    {
        return Mathf.RoundToInt((float)(other.Score - Score));
    }
}
