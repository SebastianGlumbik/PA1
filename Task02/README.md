# Affinity Propagation clustering algorithm
- Assignment: [Task_02.pdf](Task02.pdf)
- Input data: [input1.csv](input1.csv) and [input2.csv](input2.csv)

## How to run
There are two ways to run the code:
1. **Build the project first** `dotnet build -c Release`
   * Navigate to the output directory
   * Run the code using: `dotnet Task02.dll --file input1.csv -n 100 --iterations 1` or `dotnet Task02.dll --file input2.csv --print-matrices true`
     * You can try `dotnet Task02.dll --help` to see all available options.
2. **Run the code directly** `dotnet run -c Release --file input2.csv`.