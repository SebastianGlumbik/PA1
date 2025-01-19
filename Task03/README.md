# PageRank algorithm
- Assignment: [Task_03.pdf](Task03.pdf)
- Input data are specified in the assignment.
  - The input file must be in following format:
    ```
    685230
    1 2
    1 5
    1 7
    1 8
    1 9
    .
    .
    . 
    ```
    - The first line contains the number of nodes.
    - The following lines contain edges between nodes. Nodes can be divided by space or tab.

## How to run
There are two ways to run the code:
1. **Build the project first** `dotnet build -c Release`
   * Navigate to the output directory
   * Run the code using: `dotnet Task03.dll --file input.txt`
     * You can try `dotnet Task03.dll --help` to see all available options.
2. **Run the code directly** `dotnet run -c Release --file input.txt`.