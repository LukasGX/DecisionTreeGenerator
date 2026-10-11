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

        // train command options
        Option<string> schemaOption = new("-s", "--schema")
        {
            Description = "Schema: csv:filename, json:filename or autodetect",
            DefaultValueFactory = _ => "autodetect"
        };

        Option<string> trainingOption = new("-l", "--trainingdata")
        {
            Description = "Trainingdata: csv:filename, json:filename or manual",
            DefaultValueFactory = _ => "manual"
        };

        Option<string> testOption = new("-t", "--testdata")
        {
            Description = "Testdata: csv:filename, json:filename or manual",
            DefaultValueFactory = _ => "manual"
        };

        Option<bool> printOption = new("-p", "--print")
        {
            Description = "Print tree to console",
            DefaultValueFactory = _ => false
        };

        Option<string> exportOption = new("-e", "--export")
        {
            Description = "Export: dot:filename, svg:filename or json:filename",
            DefaultValueFactory = _ => "dot:tree.dot"
        };

        // predict command options
        Option<string> loadOption = new("-l", "--load")
        {
            Description = "Load tree: json:filename",
            DefaultValueFactory = _ => "json:tree.json"
        };

        Option<string> predictDataOption = new("-d", "--data")
        {
            Description = "Data to predict: json:filename, csv:filename or manual",
            DefaultValueFactory = _ => "manual"
        };

        RootCommand rootCommand = new("DecisionTreeGenerator");

        Command trainCommand = new("train", "Train a decision tree");
        Command predictCommand = new("predict", "Predict outcomes using a trained tree");

        rootCommand.Subcommands.Add(trainCommand);
        rootCommand.Subcommands.Add(predictCommand);

        trainCommand.Options.Add(schemaOption);
        trainCommand.Options.Add(trainingOption);
        trainCommand.Options.Add(testOption);
        trainCommand.Options.Add(printOption);
        trainCommand.Options.Add(exportOption);

        predictCommand.Options.Add(loadOption);
        predictCommand.Options.Add(predictDataOption);

        trainCommand.SetAction(parseResult =>
        {
            try {
                string schemaArg = parseResult.GetValue(schemaOption) ?? "autodetect";
                string trainingArg = parseResult.GetValue(trainingOption) ?? "manual";
                string testArg = parseResult.GetValue(testOption) ?? "manual";
                string exportArg = parseResult.GetValue(exportOption) ?? "none";
                bool printArg = parseResult.GetValue(printOption);

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

                if (printArg) tree.Print();

                // export
                if (!exportArg.Equals("none", StringComparison.OrdinalIgnoreCase))
                {
                    LoadFileArgument(exportArg, "export parameter", (format, filename) =>
                    {
                        switch (format)
                        {
                            case "dot":
                                tree.ExportDot(filename);
                                break;
                            case "svg":
                                tree.ExportSvg(filename);
                                break;
                            case "json":
                                tree.ExportJSON(filename, elSchema);
                                break;
                        }
                    });
                }
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

        predictCommand.SetAction(parseResult =>
        {
            try {
                string loadArg = parseResult.GetValue(loadOption) ?? throw new Exception("No tree file specified");
                string predictDataArg = parseResult.GetValue(predictDataOption) ?? "manual";

                Tree? tree = null;

                // load
                LoadFileArgument(loadArg, "load parameter", (format, filename) =>
                {
                    switch (format)
                    {
                        case "json":
                            (Tree tr, ElementSchema els) = Tree.LoadFromJSON(filename);
                            tree = tr;
                            elSchema = els;
                            break;
                    }
                });

                if (tree is null) throw new Exception("Internal error.");

                // predict data
                if (predictDataArg.Equals("manual", StringComparison.OrdinalIgnoreCase))
                {
                    dataPool.Manual(elSchema, dataPool, false);
                }
                else
                {
                    LoadFileArgument(predictDataArg, "predict data", (format, filename) =>
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

                TreeGenerator.Predict(tree, dataPool);
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

        if (format is not ("csv" or "json" or "dot" or "svg"))
            throw new ArgumentException(
                $"Unsupported format '{format}' for {parameterName}.");

        load(format, filename);
    }
}