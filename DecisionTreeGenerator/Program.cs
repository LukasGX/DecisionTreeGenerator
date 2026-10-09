using System.Linq.Expressions;
using DecisionTreeGenerator.Data;
using DecisionTreeGenerator.Nodes;
using DecisionTreeGenerator.TreeNS;
namespace DecisionTreeGenerator;

public static class Program
{
    public static void Main(string[] args)
    {
        DataPool dataPool = new();
        ElementSchema elSchema = new([]);

        string schemaArg = "autodetect";
        string trainingDataArg = "manual";
        string testDataArg = "manual";

        HashSet<string> usedOptions = [];

        for (int i = 0; i < args.Length; i++)
        {
            string? target = args[i] switch
            {
                "-s" or "--schema" => "schema",
                "-e" or "--trainingdata" => "trainingdata",
                "-t" or "--testdata" => "testdata",
                _ => null
            };

            if (target is null)
                throw new ArgumentException($"Unknown argument: {args[i]}");

            if (!usedOptions.Add(target))
                throw new ArgumentException(
                    $"The parameter '{args[i]}' was specified more than once.");

            if (i + 1 >= args.Length || args[i + 1].StartsWith('-'))
                throw new ArgumentException(
                    $"Missing value for parameter '{args[i]}'.");

            string value = args[++i];

            switch (target)
            {
                case "schema":
                    schemaArg = value;
                    break;

                case "trainingdata":
                    trainingDataArg = value;
                    break;

                case "testdata":
                    testDataArg = value;
                    break;
            }
        }

        // schema
        if (schemaArg.Equals("autodetect", StringComparison.OrdinalIgnoreCase))
        {
            // TODO: autodetect
            throw new NotImplementedException();
        }
        else
        {
            LoadFileArgument(schemaArg, "schema", (format, filename) =>
            {
                switch (format)
                {
                    case "csv":
                        elSchema = ElementSchema.LoadCSV(filename);
                        break;

                    case "json":
                        elSchema = ElementSchema.LoadJSON(filename);
                        break;
                }
            });
        }

        // trainingdata
        if (trainingDataArg.Equals("manual", StringComparison.OrdinalIgnoreCase))
        {
            // TODO: manual
            throw new NotImplementedException();
        }
        else
        {
            LoadFileArgument(trainingDataArg, "training data", (format, filename) =>
            {
                switch (format)
                {
                    case "csv":
                        dataPool.LoadCSV(filename, elSchema);
                        break;

                    case "json":
                        dataPool.LoadJSON(filename, elSchema);
                        break;
                }
            });
        }

        // testdata
        if (testDataArg.Equals("manual", StringComparison.OrdinalIgnoreCase))
        {
            // TODO: manual
        }
        else
        {
            LoadFileArgument(testDataArg, "test data", (format, filename) =>
            {
                switch (format)
                {
                    case "csv":
                        dataPool.LoadCSV(filename, elSchema, true);
                        break;

                    case "json":
                        dataPool.LoadJSON(filename, elSchema, true);
                        break;
                }
            });
        }

        // generate tree
        Tree tree = TreeGenerator.GenerateTree(elSchema, dataPool);
        tree.Test(dataPool);
        tree.Print();
    }

    static void LoadFileArgument(
        string value,
        string parameterName,
        Action<string, string> load
    )
    {
        string[] parts = value.Split(':', 2);

        if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[1]))
            throw new ArgumentException(
                $"Invalid value for {parameterName}. " +
                "Expected csv:filename or json:filename.");

        string format = parts[0].ToLowerInvariant();
        string filename = parts[1];

        if (format is not ("csv" or "json"))
            throw new ArgumentException(
                $"Unsupported format '{format}' for {parameterName}.");

        load(format, filename);
    }
}