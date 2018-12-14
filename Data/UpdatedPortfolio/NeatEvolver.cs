using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NEAT
{
    public class NeatEvolver
    {
        // Data used for this genetic algorithm
        [Serializable]
        public struct Data
        {
            // The total amount of specimen in all species combined
            // Also used to intialize the species list length and a few others
            public int generationSize;
            // The amoun of NEAT in a species that will survive the evolution cycle
            [Range(0, 1)]
            public double fitPercentage;
            // This is used to check whether or not a NEAT is comparable to a species of other NEAT
            public double maxOffsetSpecies;
            // I like to use seeds, not only because threading doesn't allow for UnityEngine.Random, but also for error tracking
            // When using random values it can be very hard to pinpoint a problem if said problem only occurs .1% of the time
            // With seeds the "random" values are always the same and because of that it's much easier to solve problems that only
            // happen occasionaly. And of course, if you want to test it with truly "random" values you just randomize the seed.
            public string seed;
            // The data used for the NEAT
            public Neat.Data neatData;
        }

        // Where normal genetic algorithms are generally just a list of T, to be able to use NEAT effectively you need to speciate the
        // specimen (make different species to be able to make them evolve in very unique ways compared to other species)
        // This allows for the generation of very different type of solutions for a single problem inside just one genetic algorithm
        private class Species : IComparable<Species>
        {
            // The combined fitness of all the specimen inside this species divided by the average of all species
            public double fitness;
            // The amount of specimen fit to survive the next evolution cycle, and how many children will be created for the next evolution cycle
            public int fitAmount, allowedChildrenCount;

            // All the NEAT in this species, every neat will be somewhat comparible to others inside this species
            // which is crucial if you want to evolve the children somewhat effectively
            public List<Neat> specimen = new List<Neat>();

            public int CompareTo(Species other)
            {
                if (fitness < other.fitness)
                    return 1;
                if (fitness == other.fitness)
                    return 0;
                return -1;
            }
        }

        public enum State {Working, Evolveable, Evolving }

        // Public variables used to show how far the NEAT currently are
        public State WorkingState { get; private set; }
        public int GenerationNumber { get; private set; }
        public double Score { get; private set; }
        public Neat BestNeat { get; private set; }

        private Data data;
        private System.Random random;

        // When a new mutation occurs, it receives totalMutationCount as a unique ID and then totalMutationCount
        // will increment. This allows for multiple NEAT to see how much they have in common and is used in the 
        // evolution where the child randomly picks between the connections with the same ID of it's parents
        private int totalMutationCount;
        // List of species currently active
        // Multiple species allows for the generation of multiple solutions to a single problem, which is required
        // if you base your network's data structure off on randomly evolving and adding components
        private List<Species> species;
        // NEAT that still need to be checked before a new evolution cycle can occur
        private List<Neat> open;

        // Cache
        private Neat shortest, longest;
        private List<Neat> newGeneration, 
            transformable;

        public NeatEvolver(Data data)
        {
            Neat neat;

            this.data = data;

            // Initialize on a set size, because this will save a lot of garbage being generated
            random = new System.Random(data.seed.GetHashCode());
            species = new List<Species>(data.generationSize);
            open = new List<Neat>(data.generationSize);
            newGeneration = new List<Neat>(data.generationSize);
            transformable = new List<Neat>(data.generationSize);
            Score = double.NegativeInfinity;

            species.Add(new Species());
            for (int i = 0; i < data.generationSize; i++)
            {
                neat = new Neat(data.neatData, random);
                species[0].specimen.Add(neat);
                open.Add(neat);
            }

            // Initialize temporary best neat
            BestNeat = species[0].specimen[0];
        }

        // Get a random specimen in the list "open"
        public Neat GetTrainable()
        {
            int index = random.Next(0, open.Count);
            Neat returnable = open[index];
            open.RemoveAt(index);

            if (open.Count == 0)
                WorkingState = State.Evolveable;

            return returnable;
        }

        // This is where the genetic algorithm checks which species and specimen work best
        // This basically works as follows: it takes the best of the species/specimen and lets them have offspring,
        // while the weaker performing units will be discarded
        // In my case I reuse their data to instead transform a less performing specimen into a child of well performing
        // specimen, to save a lot of memory each cycle
        // Currently this logic generates zero garbage (down from almost .2 mb) just by reusing those networks that would
        // be discarded anyway
        public void Evolve()
        {
            WorkingState = State.Evolving;
            // Sort both species and specimen based on fitness
            int speciesCount = species.Count,
                allowedChildrenCount,
                fitAmount,
                totalFitAmount = 0,
                specimenCount,
                offsetPopulation,
                randomIndex;
            double averageFitness = Mathf.Epsilon, score;
            Neat parentA, parentB, child;
            Species currentSpecies;
            bool fit;

            // Calculate average fitness
            for (int i = 0; i < speciesCount; i++)
            {
                species[i].specimen.Sort();

                species[i].fitness = 0;
                foreach (Neat specimen in species[i].specimen)
                    species[i].fitness += specimen.fitness;
                averageFitness += species[i].fitness;
            }

            // Set the fitness for each species
            // The minus one is used because this calculation considers "1" to be average, and the fitness will be used to calculate
            // how many children can be produced by this species (species count + fitness)
            averageFitness /= speciesCount;
            for (int i = 0; i < speciesCount; i++)
                species[i].fitness = species[i].fitness / averageFitness - 1;

            species.Sort();
            newGeneration.Clear();

            // Set best score
            Score = double.NegativeInfinity;
            for (int i = 0; i < speciesCount; i++)
            {
                score = species[i].specimen[0].fitness;
                if (score > Score)
                {
                    BestNeat = species[i].specimen[0];
                    Score = score;
                }
            }

            transformable.Clear();

            // Remove all less performing specimen and add them to the transformable list,
            // where they can be transformed into children of better performing specimen
            for (int i = 0; i < speciesCount; i++)
            {
                currentSpecies = species[i];
                specimenCount = currentSpecies.specimen.Count;
                fitAmount = Mathf.Max((int)(specimenCount * data.fitPercentage), 1);
                currentSpecies.fitAmount = fitAmount;
                totalFitAmount += fitAmount;

                allowedChildrenCount = specimenCount + Mathf.RoundToInt((float)currentSpecies.fitness) - fitAmount;
                currentSpecies.allowedChildrenCount = allowedChildrenCount;
                currentSpecies.specimen.Sort();

                for (int j = fitAmount; j < specimenCount; j++)
                {
                    transformable.Add(currentSpecies.specimen[fitAmount]);
                    currentSpecies.specimen.RemoveAt(fitAmount);
                }
            }

            // Convert disfunctional specimen into children of functional networks
            for (int i = 0; i < speciesCount; i++)
            {
                currentSpecies = species[i];
                specimenCount = currentSpecies.specimen.Count;
                allowedChildrenCount = currentSpecies.allowedChildrenCount;

                // Select random fit parents and make children
                for (int j = 0; j < allowedChildrenCount; j++)
                {
                    if (transformable.Count == 0)
                        break;

                    parentA = currentSpecies.specimen[random.Next(0, specimenCount - 1)];
                    parentB = currentSpecies.specimen[random.Next(0, specimenCount - 1)];

                    randomIndex = random.Next(0, transformable.Count - 1);
                    child = transformable[randomIndex];
                    MakeChild(parentA, parentB, child);
                    newGeneration.Add(child);
                    transformable.RemoveAt(randomIndex);
                }
            }

            // Check for over or under population
            offsetPopulation = data.generationSize - totalFitAmount - newGeneration.Count;

            // Add new from random
            for (int i = 0; i < offsetPopulation; i++)
            {
                parentA = newGeneration[random.Next(0, newGeneration.Count - 1)];
                parentB = newGeneration[random.Next(0, newGeneration.Count - 1)];

                child = new Neat(data.neatData, random);
                child.Transform(ref totalMutationCount, parentA, parentB);
                newGeneration.Add(child);
            }          

            // Remove random
            for (int i = 0; i < -offsetPopulation; i++)
                newGeneration.RemoveAt(random.Next(0, newGeneration.Count - 1));

            // Add children to generation
            foreach (Neat neat in newGeneration)
            {
                fit = false;
                foreach (Species sp in species)
                    if (GetCompability(neat, sp.specimen[random.Next(0, sp.specimen.Count - 1)]) < data.maxOffsetSpecies)
                    {
                        sp.specimen.Add(neat);
                        fit = true;
                        break;
                    }

                if (!fit)
                {
                    species.Add(new Species());
                    species[species.Count - 1].specimen.Add(neat);
                }
            }

            // Remove extinct species
            speciesCount = species.Count;
            for (int i = speciesCount - 1; i >= 0; i--)
                if (species[i].specimen.Count == 0)
                    species.RemoveAt(i);

            open.Clear();
            foreach (Species sp in species)
                foreach (Neat neat in sp.specimen)
                    open.Add(neat);
            
            GenerationNumber++;
            WorkingState = State.Working;
        }

        // Convert old ones into new instead
        private void MakeChild(Neat a, Neat b, Neat transformable)
        {
            transformable.Transform(ref totalMutationCount, a, b);
        }

        // By far the most expensive function in this whole script, this checks whether or not
        // the specimen fits inside a species, where a is said specimen and b is a randomly chosen
        // representive of the species
        private double GetCompability(Neat a, Neat b)
        {
            int aConnectionsCount = a.connections.Count,
                bConnectionsCount = b.connections.Count,
                shortestConnectionsCount = Mathf.Min(aConnectionsCount, bConnectionsCount),
                longestConnectionsCount = Mathf.Max(aConnectionsCount, bConnectionsCount),
                excessAmount = Mathf.Abs(aConnectionsCount - bConnectionsCount),
                disjointAmount = shortestConnectionsCount;

            shortest = aConnectionsCount < bConnectionsCount ? a : b;
            longest = aConnectionsCount < bConnectionsCount ? b : a;

            // Calculates average weight difference and the disjoint amount
            for (int i = 0; i < longestConnectionsCount; i++)
                for (int j = 0; j < shortestConnectionsCount; j++)
                    if (longest.connections[i].id == shortest.connections[j].id)
                    {
                        disjointAmount--;
                        break;
                    }

            return excessAmount / longestConnectionsCount + disjointAmount / longestConnectionsCount;
        }
    }
}