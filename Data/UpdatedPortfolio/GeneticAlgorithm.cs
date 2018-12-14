using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeneticAlgorithm<T, U> where T : IEvolution<T, U>, IComparable<T>, new()
{
    [Serializable]
    public struct GeneticAlgorithmData
    {
        public int size, victors, children, parentCount;
        public System.Random random;
    }

    public int GenerationCount { get; private set; }

    private GeneticAlgorithmData data;
    private List<T> generation, open, closed;

    #region Cache
    private List<T> parents;
    #endregion

    public GeneticAlgorithm(GeneticAlgorithmData data, U entityData)
    {
        int size = data.size;

        GenerationCount = 1;

        this.data = data;

        generation = new List<T>(size);
        open = new List<T>(size);
        closed = new List<T>(size);

        parents = new List<T>(data.parentCount);

        // Populate generation
        T entity;
        for (int i = 0; i < size; i++)
        {
            entity = new T();
            entity.Initialize(entityData);
            generation.Add(entity);
            open.Add(entity);
        }
    }

    public T Get()
    {
        return generation[0];
    }

    public T GetRandom()
    {
        if (open.Count == 0)
            EndGeneration();

        int index = data.random.Next(0, open.Count - 1);
        T entity = open[index];

        open.RemoveAt(index);

        return entity;
    }

    public void ReturnRandom(T entity)
    {
        closed.Add(entity);
    }

    private void EndGeneration()
    {
        generation.Sort();

        int length = data.victors + data.children;


        for (int i = data.victors; i < length; i++)
        {
            for (int j = 0; j < data.parentCount; j++)
                parents.Add(generation[data.random.Next(0, data.victors)]);

            generation[i].Transform(parents);
            parents.Clear();
        }

        for (int i = length; i < data.size; i++)
            generation[i].ReInitialize();

        foreach (T entity in closed)
            open.Add(entity);
        closed.Clear();

        GenerationCount++;
    }
}

public interface IEvolution<T, U>
{
    void Initialize(U data);
    void Transform(List<T> parents);
    void ReInitialize();
    void Mutate();
}
