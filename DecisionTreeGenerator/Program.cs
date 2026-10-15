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
            Description = "Schema: csv file, json file or autodetect",
            DefaultValueFactory = _ => "autodetect",
            Required = false
        };

        Option<string> trainingOption = new("-l", "--trainingdata")
        {
            Description = "Trainingdata: csv file, json file or manual",
            DefaultValueFactory = _ => "manual",
            Required = false
        };

        Option<string> testOption = new("-t", "--testdata")
        {
            Description = "Testdata: csv file, json file or manual",
            DefaultValueFactory = _ => "manual",
            Required = false
        };

        Option<bool> printOption = new("-p", "--print")
        {
            Description = "Print tree to console",
            DefaultValueFactory = _ => false,
            Required = false
        };

        Option<bool> confusionMatrixOption = new("--confusion-matrix")
        {
            Description = "Show confusion matrix",
            DefaultValueFactory = _ => false,
            Required = false
        };

        Option<string> exportConfusionMatrixOption = new("--export-confusion-matrix")
        {
            Description = "Export confusion matrix: md file",
            Required = false
        };

        Option<string> exportOption = new("-e", "--export")
        {
            Description = "Export: dot file, svg file or json file",
            Required = false
        };

        // predict command options
        Option<string> loadOption = new("-l", "--load")
        {
            Description = "Load tree: json file",
            Required = true
        };

        Option<string> predictDataOption = new("-d", "--data")
        {
            Description = "Data to predict: json file, csv file or manual",
            DefaultValueFactory = _ => "manual",
            Required = false
        };

        Option<bool> verboseOption = new("-v", "--verbose")
        {
            Description = "Verbose",
            DefaultValueFactory = _ => false,
            Required = false
        };

        // validate command options
        Option<string> restrictedSchemaOption = new("-s", "--schema")
        {
            Description = "Schema: csv file or json file",
            Required = true
        };

        Option<string> validateDataOption = new("-d", "--data")
        {
            Description = "Data to validate: json file, csv file or manual",
            DefaultValueFactory = _ => "manual",
            Required = false
        };

        RootCommand rootCommand = new("DecisionTreeGenerator");

        Command trainCommand = new("train", "Train a decision tree");
        Command predictCommand = new("predict", "Predict outcomes using a trained tree");
        Command validateCommand = new("validate", "Validate given data using schema");

        rootCommand.Subcommands.Add(trainCommand);
        rootCommand.Subcommands.Add(predictCommand);
        rootCommand.Subcommands.Add(validateCommand);

        trainCommand.Options.Add(schemaOption);
        trainCommand.Options.Add(trainingOption);
        trainCommand.Options.Add(testOption);
        trainCommand.Options.Add(printOption);
        trainCommand.Options.Add(exportOption);
        trainCommand.Options.Add(verboseOption);

        predictCommand.Options.Add(loadOption);
        predictCommand.Options.Add(predictDataOption);
        predictCommand.Options.Add(verboseOption);
        predictCommand.Options.Add(confusionMatrixOption);
        predictCommand.Options.Add(exportConfusionMatrixOption);

        validateCommand.Options.Add(restrictedSchemaOption);
        validateCommand.Options.Add(validateDataOption);

        trainCommand.SetAction(parseResult =>
        {
            try {
                string schemaArg = parseResult.GetValue(schemaOption) ?? "autodetect";
                string trainingArg = parseResult.GetValue(trainingOption) ?? "manual";
                string testArg = parseResult.GetValue(testOption) ?? "manual";
                string exportArg = parseResult.GetValue(exportOption) ?? "none";
                bool printArg = parseResult.GetValue(printOption);
                bool verboseArg = parseResult.GetValue(verboseOption);

                // schema
                if (schemaArg.Equals("autodetect", StringComparison.OrdinalIgnoreCase))
                {
                    // TODO: autodetect
                    throw new NotImplementedException();
                }
                else
                {
                    LoadFileArgument(schemaArg, "schema", [".csv", ".json"], true, (format, filename) =>
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
                    LoadFileArgument(trainingArg, "training data", [".csv", ".json"], true, (format, filename) =>
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
                    LoadFileArgument(testArg, "test data", [".csv", ".json"], true, (format, filename) =>
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
                Tree tree = TreeGenerator.GenerateTree(elSchema, dataPool, verboseArg);
                tree.Test(dataPool);

                if (printArg) tree.Print();

                // export
                if (!exportArg.Equals("none", StringComparison.OrdinalIgnoreCase))
                {
                    LoadFileArgument(exportArg, "export parameter", [".dot", ".svg", ".json"], false, (format, filename) =>
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
                bool verboseArg = parseResult.GetValue(verboseOption);
                bool confusionMatrixArg = parseResult.GetValue(confusionMatrixOption);
                string exportConfusionMatrixArg = parseResult.GetValue(exportConfusionMatrixOption) ?? "";

                Tree? tree = null;

                // load
                LoadFileArgument(loadArg, "load parameter", [".json"], true, (format, filename) =>
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
                    LoadFileArgument(predictDataArg, "predict data", [".csv", ".json"], true, (format, filename) =>
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

                // export confusion matrix
                string? file = null;
                if (exportConfusionMatrixArg != "")
                {
                    LoadFileArgument(exportConfusionMatrixArg, "confusion matrix export", [".md", ".csv"], false, (format, filename) =>
                    {
                        file = filename;
                    });
                }

                TreeGenerator.Predict(tree, dataPool, verboseArg, file is not null || confusionMatrixArg, file);
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

        validateCommand.SetAction(parseResult =>
        {
            try {
                string restrictedSchemaArg = parseResult.GetValue(restrictedSchemaOption) ?? throw new Exception("No schema specified");
                string validateDataArg = parseResult.GetValue(validateDataOption) ?? "manual";

                // schema
                LoadFileArgument(restrictedSchemaArg, "schema", [".csv", ".json"], true, (format, filename) =>
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

                // validate data
                if (validateDataArg.Equals("manual", StringComparison.OrdinalIgnoreCase))
                {
                    dataPool.Manual(elSchema, dataPool, false);
                }
                else
                {
                    LoadFileArgument(validateDataArg, "validate data", [".csv", ".json"], true, (format, filename) =>
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
                
                bool isValid = elSchema.ValidateList(dataPool.TrainingData);

                if (isValid)
                    Console.WriteLine("The data is valid.");
                else
                    Console.WriteLine("The data is invalid.");
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
        string filename,
        string parameterName,
        string[] allowedFileExtensions,
        bool requireFileExisting,
        Action<string, string> load
    )
    {
        string extension = Path.GetExtension(filename);

        if (!allowedFileExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw new FormatException($"File extension '{extension}' is not allowed for {parameterName}.");
        }

        if (!File.Exists(filename) && requireFileExisting)
        {
            throw new FileNotFoundException(
                $"File '{filename}' does not exist.",
                filename
            );
        }

        load(extension.TrimStart('.'), filename);
    }
}