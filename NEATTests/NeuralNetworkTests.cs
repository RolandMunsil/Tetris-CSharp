﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tetris.NEAT;
using System.Collections.Generic;
using System.Linq;

namespace NEATTests
{
    [TestClass]
    public class NeuralNetworkTests
    {
        [TestMethod]
        public void TestJustTwoNodes()
        {
            //Genome just leaves its input unchanged (aside from activation function)
            Genome genome = new Genome();
            genome.numInputs = 1;
            genome.numOutputs = 1;
            genome.connectionGenes = new List<ConnectionGene>();
            genome.connectionGenes.Add(new ConnectionGene(0, 1, 1, true, 1));

            NeuralNetwork net = new NeuralNetwork(genome);
            Assert.AreEqual(net.numInputs, 1);
            Assert.AreEqual(net.numOutputs, 1);
            Assert.AreEqual(net.nonInputNodes.Length, 1);
            var outputNode = net.nonInputNodes[0];
            Assert.AreEqual(outputNode.number, 1);
            Assert.AreEqual(outputNode.sourceNodeNums.Count, 1);
            Assert.AreEqual(outputNode.sourceNodeNums[0], 0);
            Assert.AreEqual(outputNode.sourceNodeWeights.Count, 1);
            Assert.AreEqual(outputNode.sourceNodeWeights[0], 1);
            for (double d = -5; d <= 5; d++)
            {
                double[] outputs = net.FeedForward(new[] { d });
                Assert.AreEqual(outputs.Length, 1);
                Assert.AreEqual(outputs[0], net.ActivationFunc(d));
            }
        }

        [TestMethod]
        public void TestInputUnchangeds()
        {
            for (int numInputs = 1; numInputs < 100; numInputs++)
            {
                //Genome just leaves its input unchanged (aside from activation function)
                Genome genome = new Genome();
                genome.numInputs = numInputs;
                genome.numOutputs = numInputs;
                genome.connectionGenes = new List<ConnectionGene>();
                for (int i = 0; i < numInputs; i++)
                {
                    genome.connectionGenes.Add(new ConnectionGene(i, i + numInputs, 1, true, 1));
                }

                NeuralNetwork net = new NeuralNetwork(genome);
                Assert.AreEqual(net.numInputs, numInputs);
                Assert.AreEqual(net.numOutputs, numInputs);
                Assert.AreEqual(net.nonInputNodes.Length, numInputs);
                for (int i = 0; i < numInputs; i++)
                {
                    var outputNode = net.nonInputNodes[i];
                    Assert.AreEqual(outputNode.number, i + numInputs);
                    Assert.AreEqual(outputNode.sourceNodeNums.Count, 1);
                    Assert.AreEqual(outputNode.sourceNodeNums[0], i);
                    Assert.AreEqual(outputNode.sourceNodeWeights.Count, 1);
                    Assert.AreEqual(outputNode.sourceNodeWeights[0], 1);
                }
                double[] inputs = new double[numInputs];
                for (int i = 0; i < numInputs; i++)
                    inputs[i] = i - 2;
                double[] outputs = net.FeedForward(inputs);
                Assert.AreEqual(outputs.Length, numInputs);
                for (int i = 0; i < inputs.Length; i++)
                {
                    Assert.AreEqual(outputs[i], net.ActivationFunc(inputs[i]));
                }
            }
        }

        [TestMethod]
        public void TestSummingAndNodeOrder()
        {
            for (int numInputs = 1; numInputs < 100; numInputs++)
            {
                Genome genome = new Genome();
                genome.numInputs = numInputs;
                genome.numOutputs = 1;
                genome.connectionGenes = new List<ConnectionGene>();
                for (int i = 0; i < numInputs; i++)
                {
                    genome.connectionGenes.Add(new ConnectionGene(i, numInputs, i+1, true, 1));
                }

                NeuralNetwork net = new NeuralNetwork(genome);
                Assert.AreEqual(net.numInputs, numInputs);
                Assert.AreEqual(net.numOutputs, 1);
                Assert.AreEqual(net.nonInputNodes.Length, 1);

                var outputNode = net.nonInputNodes[0];
                Assert.AreEqual(outputNode.number, numInputs);
                Assert.AreEqual(outputNode.sourceNodeNums.Count, numInputs);
                Assert.AreEqual(outputNode.sourceNodeWeights.Count, numInputs);
                for (int i = 0; i < numInputs; i++)
                {
                    Assert.IsTrue(outputNode.sourceNodeNums.Contains(i));
                    int idx = outputNode.sourceNodeNums.IndexOf(i);
                    Assert.AreEqual(outputNode.sourceNodeWeights[idx], i+1);
                }

                double[] inputs = new double[numInputs];
                for (int i = 0; i < numInputs; i++)
                    inputs[i] = i+1;

                //Output should be activationfunc(1*1 + 2*2 + ...)
                double[] outputs = net.FeedForward(inputs);
                Assert.AreEqual(outputs.Length, 1);
                double sumofsquares = (numInputs * (numInputs + 1) * (2 * numInputs + 1)) / 6;
                Assert.AreEqual(outputs[0], net.ActivationFunc(sumofsquares));
            }
        }

        [TestMethod]
        public void TestDisbledGenesNotAdded()
        {
            for (int numInputs = 1; numInputs < 100; numInputs++)
            {
                Genome genome = new Genome();
                genome.numInputs = numInputs;
                genome.numOutputs = numInputs;
                genome.connectionGenes = new List<ConnectionGene>();
                for (int i = 0; i < numInputs; i++)
                {
                    genome.connectionGenes.Add(new ConnectionGene(i, i + numInputs, 1, true, 1));
                }
                //Add disabled genes
                for (int input = 0; input < numInputs; input++)
                {
                    for (int output = 0; output < numInputs; output++)
                    {
                        genome.connectionGenes.Add(new ConnectionGene(input, numInputs+output, 1, false, 1));
                    }
                }

                NeuralNetwork net = new NeuralNetwork(genome);
                Assert.AreEqual(net.numInputs, numInputs);
                Assert.AreEqual(net.numOutputs, numInputs);
                Assert.AreEqual(net.nonInputNodes.Length, numInputs);
                for (int i = 0; i < numInputs; i++)
                {
                    var outputNode = net.nonInputNodes[i];
                    Assert.AreEqual(outputNode.number, i + numInputs);
                    Assert.AreEqual(outputNode.sourceNodeNums.Count, 1);
                    Assert.AreEqual(outputNode.sourceNodeNums[0], i);
                    Assert.AreEqual(outputNode.sourceNodeWeights.Count, 1);
                    Assert.AreEqual(outputNode.sourceNodeWeights[0], 1);
                }
            }
        }

        [TestMethod]
        public void TestHiddenLayers()
        {
            Genome genome = new Genome();
            genome.numInputs = 2; //0, 1
            genome.numOutputs = 3; //2, 3, 4
            genome.numHiddenNodes = 2; //5, 6
            genome.connectionGenes = new List<ConnectionGene>
            {
                new ConnectionGene(0, 2,  1,        true, 1),
                new ConnectionGene(0, 5,  1,        true, 1),
                new ConnectionGene(0, 6,  1,        true, 1),
                new ConnectionGene(1, 5,  1,        true, 1),
                new ConnectionGene(5, 6,  1,        true, 1),
                new ConnectionGene(5, 3,  1,        true, 1),
                new ConnectionGene(6, 3,  1,        true, 1),
                new ConnectionGene(5, 4,  1,        true, 1),
            };

            NeuralNetwork net = new NeuralNetwork(genome);
            Assert.AreEqual(net.numInputs, 2);
            Assert.AreEqual(net.numOutputs, 3);
            Assert.AreEqual(net.nonInputNodes.Length, 5);

            Func<double, double> a = net.ActivationFunc;

            for(double i0 = -10; i0 <= 10; i0 += .1)
            {
                for (double i1 = -10; i1 <= 10; i1 += .1)
                {
                    double[] expected = new double[3];
                    expected[0] = a(i0);
                    double h5 = a(i0 + i1);
                    expected[2] = a(h5);
                    double h6 = a(i0 + h5);
                    expected[1] = a(h5 + h6);

                    double[] input = new[] { i0, i1 };
                    double[] output = net.FeedForward(input);
                    Assert.AreEqual(output.Length, 3);

                    for (int i = 0; i < output.Length; i++)
                    {
                        Assert.AreEqual(expected[i], output[i]);
                    }

                }
            }

        }
    }
}
