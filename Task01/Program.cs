using System.Diagnostics;

namespace Task01;

internal static class Program
{
    private static class Result
    {
        private static readonly Lock LockObject = new();

        public static double Value { get; private set; } = double.MaxValue;

        public static int[] Permutation { get; private set; } = [];

        public static void Set(double value, int[] permutation)
        {
            lock (LockObject)
            {
                if (value >= Value) return;
                Value = value;
                Permutation = permutation.ToArray();
            }
        }
    }
    
    private static void Main(string[] args)
    {
        var stopwatch = new Stopwatch();
        var inputFileName = args.Length >= 1 ? args[0] : "input.txt";
        try 
        {
            stopwatch.Start();
            
            // Read file
            using var reader = new StreamReader(inputFileName);
            
            // Get dimension / number of devices
            var dimension = int.Parse(reader.ReadLine() ?? throw new Exception("File is empty"));
    
            // Get facility and their widths
            var facilities = new int[dimension];
            var widths = new int[dimension];
            var widthValues = (reader.ReadLine() ?? throw new Exception("File is too short")).Split(' ');

            // Get matrix / weights of transitions between locations
            var weights = new int[dimension, dimension];
            for (var i = 0; i < dimension; i++)
            {
                // Assign facility number and width
                facilities[i] = i;
                widths[i] = int.Parse(widthValues[i]);
                
                // Assign weights
                var weightValues = (reader.ReadLine() ?? throw new Exception("File is too short")).Split(' ');
                for (var j = i + 1; j < dimension; j++)
                {
                    // Symmetric matrix
                    var value = int.Parse(weightValues[j]);
                    weights[i, j] = value;
                    weights[j, i] = value;
                }
            }
            
            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount
            };
            
            // Solve the problem using branch and bound
            Parallel.ForEach(facilities, options, facility =>
            {
                var permutation = new int[dimension];
                permutation[0] = facility;
                Process(ref permutation, 1,ref facilities, ref widths, ref weights);
            });
            
            stopwatch.Stop();
            Console.WriteLine("Best permutation: [" + string.Join(" ", Result.Permutation) + "] with result: " + Result.Value);
            Console.WriteLine("Elapsed time: " + stopwatch.ElapsedMilliseconds + " ms");
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            Environment.Exit(1);
        }
    }

    private static void Process(ref int[] permutation, int permutationLength, ref int[] allValues, ref int[] widths, ref int[,] weights)
    {
        if (permutationLength == allValues.Length)
        {
             Result.Set(SRFLP(ref permutation, permutationLength, ref widths, ref weights), permutation);
             return;
        }

        foreach (var value in allValues)
        {
            if (permutation.AsSpan(..permutationLength).Contains(value))
            {
                continue;
            }
            
            if (SRFLP(ref permutation, permutationLength, ref widths, ref weights) >= Result.Value)
            {
                // Bound
                return;
            }
            
            permutation[permutationLength] = value;
            Process(ref permutation, permutationLength+1, ref allValues, ref widths, ref weights);
        }
    }

    private static double SRFLP(ref int[] permutation, int permutationLength, ref int[] widths, ref int[,] weights)
    {
        var sum = 0.0;
        for (var i = 0; i < permutationLength; i++)
        {
            var indexI = permutation[i];
            for (var j = i + 1; j < permutationLength; j++)
            {
                var indexJ = permutation[j];
                sum += weights[indexI, indexJ] * Distance(i, j, ref permutation, ref widths);
                if (sum >= Result.Value)
                {
                    return double.MaxValue;
                }
            }
        }

        return sum;
    }

    private static double Distance(int i, int j, ref int[] permutation, ref int[] widths)
    {
        var sum = (widths[permutation[i]] + widths[permutation[j]]) / (double)2;
        for (var k = i; k <= j; k++)
        {
            sum += widths[permutation[k]];
        }

        return sum;
    }
}