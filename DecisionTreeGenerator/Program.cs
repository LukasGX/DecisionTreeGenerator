using System.Linq.Expressions;
using DecisionTreeGenerator.Data;
using DecisionTreeGenerator.Nodes;
using DecisionTreeGenerator.TreeNS;
namespace DecisionTreeGenerator;

public static class Program
{
    public static void Main(string[] args)
    {
        string? trainingDataArg = null;

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] is "--trainingdata" or "-e")
            {
                if (trainingDataArg is not null)
                    throw new ArgumentException(
                        "The training data parameter was specified more than once.");

                if (i + 1 >= args.Length)
                    throw new ArgumentException(
                        "Expected a value: csv:filename or json:filename.");

                trainingDataArg = args[++i];
            }
        }

        if (trainingDataArg is not null)
        {
            string[] parts = trainingDataArg.Split(':', 2);

            if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[1]))
                throw new ArgumentException(
                    "Expected format: csv:filename or json:filename.");

            string format = parts[0].ToLowerInvariant();
            string filename = parts[1];

            switch (format)
            {
                case "csv":
                    Console.WriteLine($"Load CSV: {filename}");
                    break;

                case "json":
                    Console.WriteLine($"Load JSON: {filename}");
                    break;

                default:
                    throw new ArgumentException(
                        $"Unsupported training data format: {format}");
            }
        }

        DataPool dataPool = new();

        ElementSchema elSchema = new([new("Color", [new("Red"), new("Green")]), new("Shape", [new("Circle"), new("Square"), new("Rectangle")])]);

        // insert training data
        dataPool.TrainingData.Add(new("A", [new("Color", new("Red")),   new("Shape", new("Circle"))]));
        dataPool.TrainingData.Add(new("A", [new("Color", new("Green")), new("Shape", new("Circle"))]));
        dataPool.TrainingData.Add(new("A", [new("Color", new("Red")),   new("Shape", new("Circle"))]));
        dataPool.TrainingData.Add(new("A", [new("Color", new("Green")), new("Shape", new("Circle"))]));
        dataPool.TrainingData.Add(new("A", [new("Color", new("Red")),   new("Shape", new("Circle"))]));

        dataPool.TrainingData.Add(new("A", [new("Color", new("Red")),   new("Shape", new("Square"))]));
        dataPool.TrainingData.Add(new("A", [new("Color", new("Green")), new("Shape", new("Square"))]));
        dataPool.TrainingData.Add(new("A", [new("Color", new("Red")),   new("Shape", new("Square"))]));
        dataPool.TrainingData.Add(new("A", [new("Color", new("Green")), new("Shape", new("Square"))]));
        dataPool.TrainingData.Add(new("A", [new("Color", new("Green")), new("Shape", new("Square"))]));

        dataPool.TrainingData.Add(new("B", [new("Color", new("Red")),   new("Shape", new("Rectangle"))]));
        dataPool.TrainingData.Add(new("B", [new("Color", new("Green")), new("Shape", new("Rectangle"))]));
        dataPool.TrainingData.Add(new("B", [new("Color", new("Red")),   new("Shape", new("Rectangle"))]));
        dataPool.TrainingData.Add(new("B", [new("Color", new("Green")), new("Shape", new("Rectangle"))]));
        dataPool.TrainingData.Add(new("B", [new("Color", new("Red")),   new("Shape", new("Rectangle"))]));

        // insert test data
        dataPool.TestData.Add(new("A", [
            new("Color", new("Green")),
            new("Shape", new("Circle"))
        ]));

        dataPool.TestData.Add(new("A", [
            new("Color", new("Red")),
            new("Shape", new("Square"))
        ]));

        dataPool.TestData.Add(new("B", [
            new("Color", new("Green")),
            new("Shape", new("Rectangle"))
        ]));

        // generate tree
        Tree tree = TreeGenerator.GenerateTree(elSchema, dataPool);
        tree.Test(dataPool);
        tree.Print();
    }
}