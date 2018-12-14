using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NEAT
{
    // This used Icomparable because the Species class will sort all NEAT based on performance
    public class Neat : IComparable<Neat>
    {
        // This is the data that the NEAT will use for size / mutation and intializing
        [Serializable]
        public struct Data
        {
            // Maxnodes and maxconnections are being used to initialize the lists, to prevent garbage being made
            public int inpSize, outpSize, 
                maxNodes, maxConnections;
            // where 1 is 100%
            [Range(0, 1)]
            public double mutateChanceWeights,
                mutateChanceNodes, mutateChanceConnection;
        }

        public int NodeCount
        {
            get
            {
                return values.Count;
            }
        }

        public int ConnectionCount
        {
            get
            {
                return connections.Count;
            }
        }

        // This is a connection between nodes, it uses Icomparable to sort based on the starting index
        // That way I can iterate through the connections in one go, starting with starting index 0 and finishing with the NEAT's output
        public struct Connection : IComparable<Connection>
        {
            // The ID is the global number for this specific mutation, used to compare connections
            public int id, startIndex, endIndex;
            // When you have a connection between node a and b, and you generate a new node (c) between a and b, the original connection will
            // be turned off and two new connections will be created: a-c and c-b.
            public bool active;
            // The multiplier for this particular connection, where ouput = input * weight
            public double weight;

            public Connection(int id, int startIndex, int endIndex, System.Random random)
            {
                int inverseInt = 1;

                this.id = id;
                this.startIndex = startIndex;
                this.endIndex = endIndex;

                active = true;

                // Randomly choose whether or not the node has a positive or negative effect on the next layer
                if (random.Next() == 1)
                    inverseInt *= -1;

                // Randomize the weight value
                weight = random.NextDouble() * inverseInt;
            }

            public void SetActive(bool active)
            {
                this.active = active;
            }

            // When a NEAT is transformed into a new child and takes over both parent's values, for innovation to happen
            // the child node's values need to be sligtly adjusted.
            public void MutateWeight(ref System.Random random)
            {
                int r = random.Next(0, 4);

                switch (r)
                {
                    case 0:
                        weight += random.NextDouble();
                        break;
                    case 1:
                        weight -= random.NextDouble();
                        break;
                    case 2:
                        weight = random.NextDouble();
                        break;
                    case 3:
                        weight *= -1;
                        break;
                }
            }

            public int CompareTo(Connection other)
            {
                return startIndex - other.startIndex;
            }
        }

        // This is the score of the NEAT, this will be set externally
        public double fitness;

        // A reference to the random used in NeatEvolver
        private System.Random random;
        // A copy of the data used in this Neat
        private Data data;

        // All the connections between the nodes
        // Some connections will loop back to lower layers, but I decided to keep it that way
        // because it is expensive to check for the correct layer, and ultimately doesn't harm the NEAT in any way
        public List<Connection> connections;
        // This is used to iterate through the network and to pass along values to the next layer
        private List<double> values;

        public Neat(Data data, System.Random random)
        {
            int inpSize = data.inpSize,
                outpSize = data.outpSize;

            this.random = random;
            this.data = data;

            // Specify the maximum size of the lists, this will prevent a LOT of garbage being generated
            values = new List<double>(data.maxNodes);
            connections = new List<Connection>(data.maxConnections);

            // In truth, there are no nodes. there are connections which refer to nodes and the value list will get a new entry,
            // but when programming I found no use for nodes at all in this particular type of neural network.
            for (int i = 0; i < data.inpSize; i++)
                AddNode(i, true);
            for (int i = 0; i < data.outpSize; i++)
                AddNode(i, true);

            // Add connections between all inputs and all outputs
            for (int i = 0; i < inpSize; i++)
                for (int j = 0; j < outpSize; j++)
                    AddConnection(i, i, j + inpSize);

            // Sort based on the starting index of the imaginary node
            connections.Sort();
        }

        // To prevent garbage I decided to make as few NEAT as possible during runtime so I take some 
        // disfunctional networks and transform them into children of successful ones
        public void Transform(ref int id, Neat a, Neat b)
        {
            Neat largestNeat = a.values.Count > b.values.Count ? a : b,
                smallestNeat = largestNeat == a ? b : a;
            int largestNeatNodeCount = largestNeat.values.Count,
                smallestNeatNodeCount = smallestNeat.values.Count, 
                largestNeatConnectionCount = largestNeat.connections.Count,
                smallestNeatConnectionCount = smallestNeat.connections.Count,
                connectionCount;
            bool fit;

            // Adding and removing data will not generate any garbage since the maximum size of the lists
            // are known in advance
            values.Clear();
            connections.Clear();

            for (int i = 0; i < largestNeatNodeCount; i++)
                AddNode();

            for (int i = 0; i < largestNeatConnectionCount; i++)
                AddConnection(largestNeat.connections[i]);

            // This is where the genetics of the two NEAT will combine into one
            // Basically, the system will check for all the connections and
            // add them, and if both parents have the same connection with the ID, pick one at random
            for (int i = 0; i < smallestNeatConnectionCount; i++)
            {
                // If connections already contains a connection with this ID
                fit = true;
                for (int j = 0; j < largestNeatConnectionCount; j++)
                    if (smallestNeat.connections[i].id == connections[j].id)
                    {
                        // Randomly pick between the two NEAT
                        if (random.NextDouble() > .5f)
                            connections[j] = smallestNeat.connections[i];
                        fit = false;
                        break;
                    }

                // If there is only one instance of this connection
                if (fit)
                    AddConnection(smallestNeat.connections[i]);
            }

            // Mutate values
            connectionCount = connections.Count;         
            for (int i = 0; i < connectionCount; i++)
                if (data.mutateChanceWeights < random.NextDouble())
                    connections[i].MutateWeight(ref random);

            // Try adding a new node
            if (connectionCount + 2 < data.maxConnections && values.Count < data.maxNodes)
            {
                if (data.mutateChanceNodes > random.NextDouble())
                    AddNode(ref id);
            }

            // Try adding a new connection
            if (connectionCount < data.maxConnections)
            {
                if (data.mutateChanceConnection > random.NextDouble())
                    AddConnection(ref id);
            }

            connections.Sort();
        }

        private double Sig(double x)
        {
            return 2 / (1 + Math.Exp(-2 * x)) - 1;
        }

        public void Next(double[] input, ref double[] output)
        {
            int inputLength = input.Length,
                valuesCount = values.Count,
                connectionsCount = connections.Count,
                outputLength = output.Length,
                lastNodeIndex = inputLength - 1,
                outputStartIndex = valuesCount - outputLength;
            Connection connection;
            /*
            // This is the activasion function I used
            Func<double, double> sigmoid = delegate (double x)
            {
                return 2 / (1 + Math.Exp(-2 * x)) - 1;
            };
            */
            // Set input
            for (int i = 0; i < inputLength; i++)
                values[i] = input[i];
            
            // Reset all except input
            for (int i = inputLength; i < valuesCount; i++)
                values[i] = 0;

            // Set values
            for (int i = 0; i < connectionsCount; i++)
            {
                connection = connections[i];

                if (!connection.active)
                    continue;

                // Check if this is the first time it visited this imaginary node
                if (lastNodeIndex < connection.startIndex)
                {
                    lastNodeIndex = connection.startIndex;
                    values[lastNodeIndex] = Sig(values[lastNodeIndex]);
                }

                // Iterate through the values, adding them to the next layer
                values[connection.endIndex] += values[connection.startIndex] * connection.weight;
            }

            // Return output values
            for (int i = 0; i < outputLength; i++)
                output[i] = Sig(values[i + outputStartIndex]);
        }

        // Where the reference is used to increase the total mutation count back in NeatEvolver
        public void AddNode(ref int id)
        {
            AddNode(id, false);
            id += 2;
        }

        private void AddNode()
        {
            AddNode(-1, true);
        }

        private void AddNode(int id, bool defaultNode)
        {
            int newIndex = values.Count;

            // This is being used to loop through in the function Next
            values.Add(0);

            // If only used for intialization
            if (defaultNode)
                return;

            int index = random.Next(0, connections.Count - 1),
                start = connections[index].startIndex,
                end = connections[index].endIndex;

            // Disable connection since it is broken up by a new node
            connections[index].SetActive(false);

            // Add a connection from start to new, and from new to end
            AddConnection(id, start, newIndex);
            AddConnection(id + 1, newIndex, end);
        }

        private void AddConnection(Connection connection)
        {
            connections.Add(connection);
        }

        // Add a RANDOM connection
        private void AddConnection(ref int id)
        {
            Func<int> getRandomizedIndex = delegate ()
            {
                return random.Next(0, values.Count - 1);
            };

            // Randomize start and end position
            int start = getRandomizedIndex();
            int end = getRandomizedIndex();

            while (start == end)
                end = getRandomizedIndex();

            AddConnection(id, start, end);
            id++;
        }

        // Add a set connection
        private void AddConnection(int id, int start, int end)
        {
            connections.Add(new Connection(id, start, end, random));
        }

        public int CompareTo(Neat other)
        {
            if (fitness < other.fitness)
                return 1;
            if (fitness == other.fitness)
                return 0;
            return -1;
        }
    }
}