using System.Linq.Expressions;
using System.CommandLine;

using DecisionTreeGenerator.Data;
using DecisionTreeGenerator.Nodes;
using DecisionTreeGenerator.TreeNS;

namespace DecisionTreeGenerator;

public static class Program
{
    public static int Main(string[] args)
    {
        DataPool dataPool = new();
        ElementSchema elSchema = new([]);

        Option<string> schemaOption = new("-s", "--schema")
        {
            Description = "Schema: csv:filename, json:filename or autodetect",
            DefaultValueFactory = _ => "autodetect"
        };

        Option<string> trainingOption = new("-e", "--trainingdata")
        {
            Description = "Trainingdata: csv:filename, json:filename or manual",
            DefaultValueFactory = _ => "manual"
        };

        Option<string> testOption = new("-t", "--testdata")
        {
            Description = "Testdata: csv:filename, json:filename or manual",
            DefaultValueFactory = _ => "manual"
        };

        RootCommand rootCommand = new("DecisionTreeGenerator");

        rootCommand.Options.Add(schemaOption);
        rootCommand.Options.Add(trainingOption);
        rootCommand.Options.Add(testOption);

        rootCommand.SetAction(parseResult =>
        {
            string schemaArg = parseResult.GetValue(schemaOption) ?? "autodetect";
            string trainingArg = parseResult.GetValue(trainingOption) ?? "manual";
            string testArg = parseResult.GetValue(testOption) ?? "manual";

            try {
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
                if (trainingArg.Equals("manual", StringComparison.OrdinalIgnoreCase))
                {
                    dataPool.Manual(elSchema, dataPool, false);
                }
                else
                {
                    LoadFileArgument(trainingArg, "training data", (format, filename) =>
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
                if (testArg.Equals("manual", StringComparison.OrdinalIgnoreCase))
                {
                    dataPool.Manual(elSchema, dataPool, false);
                }
                else
                {
                    LoadFileArgument(testArg, "test data", (format, filename) =>
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
            catch (FileNotFoundException e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"File not found: {e.FileName}");
                Console.ResetColor();
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: {e.Message}");
                Console.ResetColor();
            }
        });

        return rootCommand.Parse(args).Invoke();
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