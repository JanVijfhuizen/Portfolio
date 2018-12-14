using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

public class JNNet : IComparable<JNNet>
{
    private int[] structure;
    private double[][] biases;
    private double[][][] weights;

    #region Cache
    private double[][] nextCalculations;
    #endregion

    public double Score { get; set; }

    // As new
    public JNNet(int[] structure, ref System.Random random)
    {
        this.structure = structure;

        // Initialize weights
        weights = new double[structure.Length - 1][][];
        for (int layer = 0; layer < weights.Length; layer++)
        {
            weights[layer] = new double[structure[layer]][];
            for (int node = 0; node < weights[layer].Length; node++)
            {
                weights[layer][node] = new double[structure[layer]];
                for (int weight = 0; weight < weights[layer][node].Length; weight++)
                    weights[layer][node][weight] = random.NextDouble() * random.Next() == 0 ? 1 : 1;
            }
        }

        // Initialize biases
        biases = new double[structure.Length - 1][];
        for (int layer = 0; layer < biases.Length; layer++)
        {
            biases[layer] = new double[structure[layer + 1]];
            for (int node = 0; node < biases[layer].Length; node++)
                biases[layer][node] = random.NextDouble() * random.Next() == 0 ? 1 : 1;
        }

        // Initialize Next cache
        nextCalculations = new double[structure.Length][];
        for (int layer = 0; layer < structure.Length; layer++)
            nextCalculations[layer] = new double[structure[layer]];
    }

    // From parents
    public void Transform(List<JNNet> nnets, ref System.Random random)
    {
        structure = nnets[0].structure;

        int length = nnets.Count;

        // Transform weights
        for (int layer = 0; layer < weights.Length; layer++)
            for (int node = 0; node < weights[layer].Length; node++)
                for (int weight = 0; weight < weights[layer][node].Length; weight++)
                    weights[layer][node][weight] = nnets[random.Next(0, length)].weights[layer][node][weight];

        // Transform biases
        for (int layer = 0; layer < biases.Length; layer++)
            for (int node = 0; node < biases[layer].Length; node++)
                biases[layer][node] = nnets[random.Next(0, length)].biases[layer][node];

        ResetScores();
    }

    public void ReInitialize(ref System.Random random)
    {
        // Transform weights
        for (int layer = 0; layer < weights.Length; layer++)
            for (int node = 0; node < weights[layer].Length; node++)
                for (int weight = 0; weight < weights[layer][node].Length; weight++)
                    weights[layer][node][weight] = random.NextDouble() * random.Next() == 0 ? 1 : 1;

        // Transform biases
        for (int layer = 0; layer < biases.Length; layer++)
            for (int node = 0; node < biases[layer].Length; node++)
                biases[layer][node] = random.NextDouble() * random.Next() == 0 ? 1 : 1;

        ResetScores();
    }

    private void ResetScores()
    {
        Score = 0;
    }

    #region Accesible Functions
    // This always gives the same result
    public double[] Next(double[] input)
    {
        int inputLength = input.Length;
        double val;

        for (int i = 0; i < inputLength; i++)
            nextCalculations[0][i] = input[i];

        int biasesLength = biases.Length, layerLength, weightLength;

        for (int layer = 0; layer < biasesLength; layer++)
        {
            layerLength = biases[layer].Length;
            for (int node = 0; node < layerLength; node++)
            {
                weightLength = weights[layer][node].Length;
                val = biases[layer][node];
                for (int weight = 0; weight < weightLength; weight++)
                    val += nextCalculations[layer][weight] * weights[layer][node][weight];
                
                val = Sigmoid(val);
                nextCalculations[layer + 1][node] = val;
            }
        }

        return nextCalculations[biasesLength];
    }

    public void Mutate(ref System.Random random, double mutateChance)
    {
        for (int layer = 0; layer < weights.Length; layer++)
            for (int node = 0; node < weights[layer].Length; node++)
                for (int weight = 0; weight < weights[layer][node].Length; weight++)
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

        for (int layer = 0; layer < biases.Length; layer++)
            for (int node = 0; node < biases[layer].Length; node++)
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

    #endregion

    private double Sigmoid(double x)
    {
        return 2 / (1 + Math.Exp(-2 * x)) - 1;
    }

    public int CompareTo(JNNet other)
    {
        return Mathf.RoundToInt((float)(other.Score - Score));
    }
}
