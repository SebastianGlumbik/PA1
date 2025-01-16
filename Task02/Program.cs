using System.Diagnostics;

namespace Task02;

internal static class Program
{
    /// <summary>
    /// Affinity Propagation clustering algorithm
    /// </summary>
    /// <param name="file">Input file</param>
    /// <param name="n">Number of points (0 = all)</param>
    /// <param name="iterations">Number of iterations</param>
    /// <param name="printMatrices">Print matrices</param>
    private static void Main(string? file, uint n = 0, uint iterations = 1, bool printMatrices = false)
    {
        var stopwatch = new Stopwatch();
        try
        {
            stopwatch.Start();
            // Read whole file, skip header and take N lines, for each line skip first value (label) and parse the rest
            var allValues = File.ReadLines(file  ?? throw new Exception("Specify input file.")).Skip(1).Take(n == 0 ? Range.All : Range.EndAt((int)n)).Select(line => line.Split(',').Skip(1).Select(int.Parse).ToArray()).ToArray();
            n = (uint)allValues.Length; // Update number of points to actual value
            Console.WriteLine("Number of points: " + n);
            
            var similarityMatrix = new double[n, n];
            
            // Parallel options
            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount
            };
            
            // Calculate similarity matrix
            var minValue = double.MaxValue;
            object lockObject = new();
            Parallel.For(0, allValues.Length, options, () => minValue, (i, _, localMin) =>
            {
                for (var j = i + 1; j < allValues.Length; j++)
                {
                    var valueSum = 0.0;
                    for (var k = 0; k < allValues[i].Length; k++)
                    {
                        valueSum += Math.Pow(allValues[i][k] - allValues[j][k], 2);
                    }
                    similarityMatrix[i, j] = similarityMatrix[j, i] = -valueSum;
                    localMin = Math.Min(localMin, -valueSum);
                }
                return localMin;
            }, localMin =>
            {
                lock (lockObject)
                {
                    minValue = Math.Min(minValue, localMin);
                }
            });

            // Set diagonal to minimum value, change from task
            Parallel.For(0, n, options, i =>
            {
                similarityMatrix[i, i] = minValue;
            });

            // Initialize responsibility, availability and criterion matrices
            var responsibilityMatrix = new double[n, n];
            var availabilityMatrix = new double[n, n];
            var criterionMatrix = new double[n, n];
            
            // Main loop
            for (var x = 0; x < iterations; x++)
            {
                // Calculate responsibility matrix R(i,j) = S(i,j) - max{a(i,k) + S(i,k)} for k != j
                Parallel.For(0, n, options, i  =>
                {
                    for (var j = 0; j < n; j++)
                    {
                        var max = double.MinValue;
                        for (var k = 0; k < n; k++)
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
                Parallel.For(0, n, options, i  =>
                {
                    for (var j = 0; j < n; j++)
                    {
                        var sum = 0.0;
                        if (i == j) // A(j, j) = sum(max{0, R(k, j}) for k != j
                        {
                            for (var k = 0; k < n; k++)
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
                            for (var k = 0; k < n; k++)
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
                Parallel.For(0, n, options, i  =>
                {
                    for (var j = 0; j < n; j++)
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
                PrintMatrix(similarityMatrix, n);
                Console.WriteLine();
                Console.WriteLine("Responsibility matrix:");
                PrintMatrix(responsibilityMatrix, n);
                Console.WriteLine();
                Console.WriteLine("Availability matrix:");
                PrintMatrix(availabilityMatrix, n);
                Console.WriteLine();
                Console.WriteLine("Criteria matrix:");
                PrintMatrix(criterionMatrix, n);
                Console.WriteLine();
            }
            
            // Print results
            PrintClusters(criterionMatrix, n);
            Console.WriteLine("Elapsed time: " + stopwatch.ElapsedMilliseconds + " ms");
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e.Message);
            Console.Error.WriteLine("Try `" + AppDomain.CurrentDomain.FriendlyName + " --help`");
            Environment.Exit(1);
        }
    }
    
    private static void PrintMatrix(in double[,] matrix, uint n)
    {
        for (var i = 0; i < n; i++)
        {
            for (var j = 0; j < n; j++)
            {
                Console.Write(matrix[i, j] + " ");
            }
            Console.WriteLine();
        }
    }
    
    private static void PrintClusters(in double[,] criterionMatrix, uint n)
    {
        var clusters = new Dictionary<int, HashSet<int>>();

        for (var x1 = 0; x1 < n; x1++)
        {
            var x2 = 0;
            var maxValue = criterionMatrix[x1, 0];
            for (var j = 1; j < n; j++)
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