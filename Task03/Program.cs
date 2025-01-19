using System.Diagnostics;

namespace Task03;

internal static class Program
{
    private class Graph
    {
        public class Node
        {
            private readonly Lock _lock = new();
            private HashSet<int>? _in;
            private int _out;
            
            public void IncrementOut()
            {
                Interlocked.Increment(ref _out);
            }
            
            public bool AddIn(int node)
            {
                lock (_lock)
                {
                    _in ??= [];
                    return _in.Add(node);
                }
            }
            
            public int Out => _out;

            public IEnumerable<int> InNodes()
            {
                lock (_lock)
                {
                    return _in?.AsEnumerable() ?? [];
                }
            }
        }

        private readonly Node[] _nodes;
        private int _edges;
        
        public Graph(int size)
        {
            _nodes = new Node[size];
            for (var i = 0; i < _nodes.Length; i++)
            {
                _nodes[i] = new Node();
            }
        }
        
        public void AddEdge(int from, int to)
        {
            if (!_nodes[to - 1].AddIn(from)) return;
            _nodes[from - 1].IncrementOut();
            Interlocked.Increment(ref _edges);
        }
        
        public Node this[int node] => _nodes[node-1];
        public int Edges => _edges;
        public int Nodes => _nodes.Length;
    }
    
    /// <summary>
    /// PageRank algorithm
    /// </summary>
    /// <param name="file">Input file, first line contains number of nodes, the rest contains edges in format `from to`</param>
    /// <param name="threshold">Threshold</param>
    /// <param name="top">How many top nodes to print</param>
    /// <exception cref="Exception"></exception>
    private static void Main(string? file, double threshold = 1e-6, int top = 5)
    {
        var stopwatch = new Stopwatch();
        try
        {
            stopwatch.Start();
            // Calculate chunk size for each thread
            var fileSize = new FileInfo(file ?? throw new Exception("Specify input file.")).Length;
            var chunkSize = (long)Math.Ceiling((double)fileSize / Environment.ProcessorCount);
            var nodes = int.Parse(new StreamReader(file).ReadLine() ?? "0");
            Console.WriteLine("Number of nodes: " + nodes);
            var graph = new Graph(nodes);
            
            // Read edges
            Parallel.For(0, Environment.ProcessorCount, i =>
            {
                ReadEdges(file, i * chunkSize, chunkSize, ref graph);
            });
            Console.WriteLine("Number of edges: " + graph.Edges);
            
            // Parallel options
            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount
            };
            
            // Initialize PageRank
            var pageRank = new double[graph.Nodes];
            const double d = 0.85;
            Parallel.For(0, graph.Nodes, options, i =>
            {
                pageRank[i] = 1.0 / graph.Nodes;
            });
            
            // Calculate PageRank
            var end = false;
            var iterations = 0;
            while (!end)
            {
                var newPageRank = new double[graph.Nodes];
                Parallel.For(1, graph.Nodes+1, options, u =>
                {
                    newPageRank[u - 1] = (1 - d) / graph.Nodes + d * graph[u].InNodes().Sum(v => pageRank[v-1] / graph[v].Out);
                });

                end = true;
                Parallel.For(0, graph.Nodes, options, i =>
                {
                    if (Math.Abs(newPageRank[i] - pageRank[i]) >= threshold)
                    {
                        end = false;
                    }
                    pageRank[i] = newPageRank[i];
                });
                iterations++;
            }
            
            stopwatch.Stop();
            
            // Print results
            Console.WriteLine("Iterations: " + iterations);
            var i = 0;
            foreach (var pr in pageRank.AsParallel().Select((value, index) => (value, index)).OrderByDescending(x => x.value).Take(top))
            {
                Console.WriteLine(++i + ". Node: " + (pr.index + 1) + " PageRank: " + pr.value);
            }
            Console.WriteLine("Elapsed time: " + stopwatch.ElapsedMilliseconds + " ms");
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e.Message);
            Console.Error.WriteLine("Try `" + AppDomain.CurrentDomain.FriendlyName + " --help`");
            Environment.Exit(1);
        }
    }
    
    private static void ReadEdges(in string file, long start, long size, ref Graph graph)
    {
        using var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var bufferedStream = new BufferedStream(fileStream);
        using var streamReader = new StreamReader(bufferedStream);
        if (start > 0)
        {
            bufferedStream.Seek(start - 1, SeekOrigin.Begin);
        }
        streamReader.ReadLine(); // Skip the first line

        var read = 0;
        while (read < start + size)
        {
            var line = streamReader.ReadLine();
            if (line == null)
            {
                break;
            }
            read += line.Length;
            var values = line.Split([' ', '\t'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            var from = int.Parse(values[0]);
            var to = int.Parse(values[1]);
            graph.AddEdge(from, to);
        }
    }
}