using System.Diagnostics;

namespace Task02;

internal static class Program
{
    private static void Main()
    {
        var stopwatch = new Stopwatch();
        const string inputFileName = "input2.csv";
        const int numberOfPoints = 5;
        const int iterations = 1;
        const bool printMatrices = true;
        try
        {
            stopwatch.Start();
            
            // Read whole file, skip header and take N lines, for each line skip first value (label) and parse the rest
            var allValues = File.ReadLines(inputFileName).Skip(1).Take(numberOfPoints).Select(line => line.Split(',').Skip(1).Select(int.Parse).ToArray()).ToArray();
            
            var similarityMatrix = new double[numberOfPoints, numberOfPoints];
            
            // Parallel options
            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount
            };
            
            // Calculate similarity matrix
            var minValue = double.MaxValue;
            Parallel.For(0, allValues.Length, options, i =>
            {
                for (var j = i + 1; j < allValues.Length; j++)
                {
                    var valueSum = 0;
                    for (var k = 0; k < allValues[i].Length; k++)
                    {
                        valueSum += (int)Math.Pow(allValues[i][k] - allValues[j][k], 2);
                    }
                    similarityMatrix[i, j] = similarityMatrix[j, i] = -valueSum;
                    minValue = Math.Min(minValue, -valueSum);
                }
            });

            // Set diagonal to minimum value, change from task
            Parallel.For(0, numberOfPoints, options, i =>
            {
                similarityMatrix[i, i] = minValue;
            });

            // Initialize responsibility, availability and criterion matrices
            var responsibilityMatrix = new double[numberOfPoints, numberOfPoints];
            var availabilityMatrix = new double[numberOfPoints, numberOfPoints];
            var criterionMatrix = new double[numberOfPoints, numberOfPoints];
            
            // Main loop
            for (var x = 0; x < iterations; x++)
            {
                // Calculate responsibility matrix R(i,j) = S(i,j) - max{a(i,k) + S(i,k)} for k != j
                Parallel.For(0, numberOfPoints, options, i  =>
                {
                    for (var j = 0; j < numberOfPoints; j++)
                    {
                        var max = double.MinValue;
                        for (var k = 0; k < numberOfPoints; k++)
                        {
                            if (k != j)
                            {
                                max = Math.Max(max, availabilityMatrix[i, k] + similarityMatrix[i, k]);
                            }
                        }
                        responsibilityMatrix[i, j] = similarityMatrix[i, j] - max;
                    }
                });
                
                // Calculate availability matrix
                Parallel.For(0, numberOfPoints, options, i  =>
                {
                    for (var j = 0; j < numberOfPoints; j++)
                    {
                        var sum = 0.0;
                        if (i == j) // A(j, j) = sum(max{0, R(k, j}) for k != j
                        {
                            for (var k = 0; k < numberOfPoints; k++)
                            {
                                if (k != j)
                                {
                                    sum += Math.Max(0.0, responsibilityMatrix[k, j]);
                                }
                            }

                            availabilityMatrix[i, j] = sum;
                        }
                        else // A(i,j) = min{0, R(j,j) + sum(max{0, R(k,j)}) for k != i
                        {
                            for (var k = 0; k < numberOfPoints; k++)
                            {
                                if (k != i)
                                {
                                    sum += Math.Max(0.0, responsibilityMatrix[k, j]);
                                }
                            }
                            availabilityMatrix[i, j] = Math.Min(0, responsibilityMatrix[j, j] + sum);
                        }
                    }
                });
                
                // Calculate criterion matrix C(i, j) = R(i, j) + A(i, j)
                Parallel.For(0, numberOfPoints, options, i  =>
                {
                    for (var j = 0; j < numberOfPoints; j++)
                    {
                        criterionMatrix[i, j] = responsibilityMatrix[i, j] + availabilityMatrix[i, j];
                    }
                });
            }
            
            stopwatch.Stop();
            
            // Print matrices
            if (printMatrices)
            {
                Console.WriteLine("Similarity matrix:");
                PrintMatrix(similarityMatrix, numberOfPoints);
                Console.WriteLine();
                Console.WriteLine("Responsibility matrix:");
                PrintMatrix(responsibilityMatrix, numberOfPoints);
                Console.WriteLine();
                Console.WriteLine("Availability matrix:");
                PrintMatrix(availabilityMatrix, numberOfPoints);
                Console.WriteLine();
                Console.WriteLine("Criteria matrix:");
                PrintMatrix(criterionMatrix, numberOfPoints);
                Console.WriteLine();
            }
            
            // Print results
            PrintClusters(criterionMatrix, numberOfPoints);
            Console.WriteLine("Elapsed time: " + stopwatch.ElapsedMilliseconds + " ms");
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            Environment.Exit(1);
        }
    }
    
    private static void PrintMatrix(in double[,] matrix, int numberOfPoints)
    {
        for (var i = 0; i < numberOfPoints; i++)
        {
            for (var j = 0; j < numberOfPoints; j++)
            {
                Console.Write(matrix[i, j] + " ");
            }
            Console.WriteLine();
        }
    }
    
    private static void PrintClusters(in double[,] criterionMatrix, int numberOfPoints)
    {
        var clusters = new Dictionary<int, HashSet<int>>();

        for (var x1 = 0; x1 < numberOfPoints; x1++)
        {
            var x2 = 0;
            var maxValue = criterionMatrix[x1, 0];
            for (var j = 1; j < numberOfPoints; j++)
            {
                if (criterionMatrix[x1, j] <= maxValue) continue;
                maxValue = criterionMatrix[x1, j];
                x2 = j;
            }
            
            HashSet<int>? cluster1 = null, cluster2 = null;
            foreach (var cluster in clusters.Values)
            {
                if (cluster.Contains(x1)) cluster1 = cluster;
                if (cluster.Contains(x2)) cluster2 = cluster;
            }
            if (cluster1 == null && cluster2 == null)
            {
                var group = new HashSet<int> {x1, x2};
                clusters[x1] = group;
                clusters[x2] = group;
            }
            else if (cluster1 != null && cluster2 == null)
            {
                cluster1.Add(x2);
                clusters[x2] = cluster1;
            }
            else if (cluster1 == null && cluster2 != null)
            {
                cluster2.Add(x1);
                clusters[x1] = cluster2;
            }
            else if (cluster1 != cluster2 && cluster1 != null && cluster2 != null)
            {
                cluster1.UnionWith(cluster2);
                foreach (var i in cluster2)
                {
                    clusters[i] = cluster1;
                }
            }
        }
        
        var uniqueClusters = new HashSet<HashSet<int>>(clusters.Values);
            
        Console.WriteLine("Clusters:");
        var index = 1;
        foreach (var cluster in uniqueClusters)
        {
            Console.WriteLine($"Cluster {index++}: {{{string.Join(", ", cluster)}}}");
        }
        Console.WriteLine("Number of clusters: " + uniqueClusters.Count);
    }
}