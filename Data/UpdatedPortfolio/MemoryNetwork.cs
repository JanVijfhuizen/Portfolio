using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Random = System.Random;
using System.Linq;

namespace MemoryNetwork
{
    public class MemoryNetwork<T> : IEvolution<MemoryNetwork<T>, MemoryNetwork<T>.Data>, IComparable<MemoryNetwork<T>> where T : ISolution
    {
        /// <summary>
        /// This is a speciated part of the memory where similar events will be placed in
        /// </summary>
        public class EventType : IComparable<EventType>
        {
            // The most effective solution of the specimen
            public T Solution { get; set; }
            // This multiplies the word values (instances) during comparison to calculate the offset for an outsider
            public double[] offsetMultipliers;
            // All the current specimen
            public List<double[]> instances;

            private Data data;            

            public int Count
            {
                get
                {
                    return instances.Count;
                }
            }

            public EventType(Data data)
            {
                this.data = data;

                int memorySize = data.memorySize,
                    inputSize = data.inputSize;

                instances = new List<double[]>(memorySize);
                offsetMultipliers = new double[inputSize];

                Reset();
            }

            public void Add(double[] instance)
            {
                instances.Add(instance);
            }

            public double[] PopRandom()
            {
                return Pop(data.random.Next(0, instances.Count));
            }

            public double[] Pop()
            {
                return Pop(0);
            }

            private double[] Pop(int index)
            {
                double[] instance = instances[index];

                instances.RemoveAt(index);
                return instance;
            }

            /// <summary>
            /// Checks whether or not outsider is similar to specimen
            /// </summary>
            /// <param name="instance"></param>
            /// <returns></returns>
            public double GetOffset(double[] instance)
            {
                double[] comparable = instances[data.random.Next(0, instances.Count)];
                double offset = 0;
                int inputSize = data.inputSize;

                for (int i = 0; i < inputSize; i++)
                    offset += Mathf.Abs((float)comparable[i] - (float)instance[i]) * offsetMultipliers[i];

                return offset;
            }

            public void Mutate()
            {
                int inputSize = data.inputSize;
                Random random = data.random;
                double mutateChance = data.mutateChance;

                for (int i = 0; i < inputSize; i++)
                    if (random.NextDouble() < mutateChance)
                        switch (random.Next(0, 4))
                        {
                            case 0:
                                offsetMultipliers[i] += random.NextDouble() * (random.Next(0, 2) == 1 ? 1 : -1);
                                offsetMultipliers[i] = Mathf.Clamp((float)offsetMultipliers[i], 0, 1);
                                break;
                            case 1:
                                offsetMultipliers[i] *= random.NextDouble();
                                offsetMultipliers[i] = Mathf.Clamp((float)offsetMultipliers[i], 0, 1);
                                break;
                            case 2:
                                offsetMultipliers[i] /= random.NextDouble();
                                offsetMultipliers[i] = Mathf.Clamp((float)offsetMultipliers[i], 0, 1);
                                break;
                            case 3:
                                offsetMultipliers[i] = random.NextDouble();
                                break;
                        }
            }

            public void Reset()
            {
                int inputSize = data.inputSize;

                for (int i = 0; i < inputSize; i++)
                    offsetMultipliers[i] = data.random.NextDouble();
            }

            public int CompareTo(EventType other)
            {
                return Mathf.RoundToInt(instances.Count - other.instances.Count);
            }
        }

        public class Data
        {
            public int memorySize, inputSize;
            public AnimationCurve fitnessMultiplier;
            public double mutateChance, maximumOffset;
            public T[] solutions;
            public Random random;
        }

        private Data data;
        private List<EventType> eventTypes;

        #region Cache
        // These classes wil get reused as long as this network lives
        // This way I can run this very often without generating any kind of garbage
        private Queue<double[]> availableInstances;
        private Queue<EventType> availableEventTypes;

        public float Fitness { get; private set; }

        public int EventTypesCount
        {
            get
            {
                return eventTypes.Count;
            }
        }

        public int InstancesCount
        {
            get
            {
                return data.memorySize - availableInstances.Count;
            }
        }

        private bool OutOfInstanceMemory
        {
            get
            {
                return availableInstances.Count == 0;
            }
        }

        private bool OutOfEventTypeMemory
        {
            get
            {
                return availableEventTypes.Count == 0;
            }
        }
        #endregion

        // Due to the way I wrote my genetic algorithm, I have to instead initialize in a seperate function
        public MemoryNetwork()
        {
            
        }

        #region Local Utility
        /// <summary>
        /// Add a new type of memory
        /// </summary>
        /// <returns></returns>
        private EventType AddEventType()
        {
            if(OutOfEventTypeMemory)
            {
                eventTypes.Sort();
                RemoveEventType(eventTypes[eventTypes.Count - 1]);
            }

            EventType eventType = availableEventTypes.Dequeue();
            eventTypes.Add(eventType);

            return eventType;
        }
        #endregion

        #region Global Utility
        /// <summary>
        /// Set fitness and set solution for each event type.
        /// The fitness will be calculated based off of how similar the instances react to each solution
        /// </summary>
        public void Set()
        {
            float fitness = 0;
            int fitBalance, totalFitBalance, eventTypesCount = eventTypes.Count,
                solutionsLength = data.solutions.Length, bestFitness;
            AnimationCurve fitnessMultiplier = data.fitnessMultiplier;
            T[] solutions = data.solutions;
            T bestSolution = solutions[0];
            EventType eventType;

            for (int i = 0; i < eventTypesCount; i++)
            {
                eventType = eventTypes[i];
                totalFitBalance = 0;
                bestFitness = 0;

                foreach (T solution in solutions)
                {
                    // The further fitbalance is from being 0 the more decisive the event is on what it wants
                    fitBalance = 0;
                    foreach (double[] instance in eventType.instances)
                        fitBalance += solution.Fits(instance) ? 1 : -1;

                    totalFitBalance += Mathf.Abs(fitBalance);

                    if(fitBalance > bestFitness)
                    {
                        bestSolution = solution;
                        bestFitness = fitBalance;
                    }
                }

                fitness += (float)totalFitBalance / eventType.instances.Count / eventTypesCount / solutionsLength * fitnessMultiplier.Evaluate((float)i / eventTypesCount);         
                eventType.Solution = bestSolution;
            }

            Fitness = fitness;
        }

        /// <summary>
        /// Get an array to be used to describe the world state
        /// </summary>
        /// <returns></returns>
        public double[] GetAvailableInstance()
        {
            if (OutOfInstanceMemory)
            {
                eventTypes.Sort();

                EventType eventType = eventTypes[eventTypes.Count - 1];
                double[] instance = eventType.PopRandom();
                int length = instance.Length;

                for (int i = 0; i < length; i++)
                    instance[i] = 0;

                availableInstances.Enqueue(instance);

                if (eventType.Count == 0)
                    RemoveEventType(eventType);
            }

            return availableInstances.Dequeue();
        }

        /// <summary>
        /// Get the solution for the world state
        /// </summary>
        /// <param name="instance">World state</param>
        /// <returns></returns>
        public T GetSolution(double[] instance)
        {
            eventTypes.Sort();

            foreach(EventType eventType in eventTypes)
                if(eventType.GetOffset(instance) < data.maximumOffset)
                {
                    eventType.Add(instance);
                    return eventType.Solution;
                }

            EventType newEventType = AddEventType();
            newEventType.Add(instance);
            newEventType.Solution = data.solutions[data.random.Next(0, data.solutions.Length)];
            return newEventType.Solution;
        }

        /// <summary>
        /// Remove a memory type
        /// </summary>
        /// <param name="eventType"></param>
        private void RemoveEventType(EventType eventType)
        {
            int count = eventType.Count, length = data.inputSize;
            double[] instance;

            for (int i = 0; i < count; i++)
            {                
                instance = eventType.Pop();
                for (int j = 0; j < length; j++)
                    instance[j] = 0;

                availableInstances.Enqueue(instance);
            }

            eventTypes.Remove(eventType);
            eventType.Reset();

            availableEventTypes.Enqueue(eventType);
        }
        #endregion

        #region Interfaces
        public void Initialize(Data data)
        {
            this.data = data;

            int memorySize = data.memorySize,
                inputSize = data.inputSize;
            double mutateChance = data.mutateChance;
            T[] solutions = data.solutions;
            Random random = data.random;

            #region Initializing Cache Data
            eventTypes = new List<EventType>(memorySize);
            availableInstances = new Queue<double[]>(memorySize);
            availableEventTypes = new Queue<EventType>(memorySize);

            for (int i = 0; i < memorySize; i++)
                availableInstances.Enqueue(new double[inputSize]);
            for (int i = 0; i < memorySize; i++)
                availableEventTypes.Enqueue(new EventType(data));
            #endregion
        }

        /// <summary>
        /// Transform the current network into a child of multiple other networks
        /// </summary>
        /// <param name="parents"></param>
        public void Transform(List<MemoryNetwork<T>> parents)
        {
            int parentsCount = parents.Count, eventTypesCount = eventTypes.Count,
                inputSize = data.inputSize, similarEventTypesCount;
            MemoryNetwork<T> root = parents[data.random.Next(0, parentsCount)];
            List<EventType> similarEventTypes = new List<EventType>(parentsCount);
            EventType eventType, rootEventType;
            Random random = data.random;
            double[] instance;

            Fitness = 0;

            for (int i = eventTypesCount - 1; i >= 0; i--)
                RemoveEventType(eventTypes[i]);

            eventTypesCount = root.eventTypes.Count;

            for (int i = 0; i < eventTypesCount; i++)
            {
                eventType = AddEventType();
                rootEventType = root.eventTypes[0];
                similarEventTypes.Clear();

                similarEventTypes.Add(rootEventType);

                // Get all similarly structured EventTypes
                foreach (MemoryNetwork<T> parent in parents)
                    if (parent != root)
                        foreach (EventType similarEvent in parent.eventTypes)
                        {
                            if (rootEventType.GetOffset(similarEvent.instances[0]) < data.maximumOffset)
                                similarEventTypes.Add(similarEvent);
                            break;
                        }

                similarEventTypesCount = similarEventTypes.Count;

                // Set offset to random in eventtypes
                for (int j = 0; j < inputSize; j++)
                    eventType.offsetMultipliers[j] = similarEventTypes[data.random.Next(0, similarEventTypesCount)].offsetMultipliers[j];

                // Add a single comparable instance
                instance = GetAvailableInstance();

                for (int j = 0; j < inputSize; j++)
                    instance[j] = rootEventType.instances[0][j];

                eventType.Add(instance);
            }
        }

        /// <summary>
        /// Hard reset for the network
        /// </summary>
        public void ReInitialize()
        {           
            int eventTypesCount = eventTypes.Count - 1;
            Fitness = 0;

            for (int i = eventTypesCount; i >= 0; i--)
                RemoveEventType(eventTypes[i]);
        }

        /// <summary>
        /// Mutate existing network
        /// </summary>
        public void Mutate()
        {
            Fitness = 0;
            foreach (EventType eventType in eventTypes)
                eventType.Mutate();
        }

        public int CompareTo(MemoryNetwork<T> other)
        {
            return Mathf.RoundToInt(other.Fitness - Fitness);
        }
        #endregion
    }
}

public interface ISolution
{
    /// <summary>
    /// Does the solution work on target world state
    /// </summary>
    /// <param name="instance"></param>
    /// <returns></returns>
    bool Fits(double[] instance);
}