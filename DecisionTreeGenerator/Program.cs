using System.Linq.Expressions;
using System.CommandLine;

using DecisionTreeGenerator.Data;
using DecisionTreeGenerator.Nodes;
using DecisionTreeGenerator.TreeNS;
using System.Diagnostics;
using System.ComponentModel;

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

        // report command options
        Option<string> outputOption = new("-o", "--output")
        {
            Description = "Output to file",
            Required = true
        };

        Option<bool> dontOpenOption = new("-X")
        {
            Description = "Do not auto-open report",
            Required = false
        };

        Option<string> baseCommandOption = new("-b", "--base-command")
        {
            Description = "Base command for opening report",
            DefaultValueFactory = _ => "xdg-open",
            Required = false
        };

        RootCommand rootCommand = new("dtg")
        {
            Description = "DecisionTreeGenerator"
        };

        Command trainCommand = new("train", "Train a decision tree");
        Command predictCommand = new("predict", "Predict outcomes using a trained tree");
        Command validateCommand = new("validate", "Validate given data using schema");
        Command inspectCommand = new("inspect", "Inspect given tree");
        Command reportCommand = new("report", "Generate html report using a trained tree");

        rootCommand.Subcommands.Add(trainCommand);
        rootCommand.Subcommands.Add(predictCommand);
        rootCommand.Subcommands.Add(validateCommand);
        rootCommand.Subcommands.Add(inspectCommand);
        rootCommand.Subcommands.Add(reportCommand);

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

        inspectCommand.Options.Add(loadOption);

        reportCommand.Options.Add(loadOption);
        reportCommand.Options.Add(outputOption);
        reportCommand.Options.Add(dontOpenOption);
        reportCommand.Options.Add(baseCommandOption);

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
                Console.Error.WriteLine($"File not found: {e.FileName}");
                Console.ResetColor();
                Environment.Exit(1);
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine($"Error: {e.Message}");
                Console.ResetColor();
                Environment.Exit(1);
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
                Console.Error.WriteLine($"File not found: {e.FileName}");
                Console.ResetColor();
                Environment.Exit(1);
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine($"Error: {e.Message}");
                Console.ResetColor();
                Environment.Exit(1);
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
                Console.Error.WriteLine($"File not found: {e.FileName}");
                Console.ResetColor();
                Environment.Exit(1);
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine($"Error: {e.Message}");
                Console.ResetColor();
                Environment.Exit(1);
            }
        });

        inspectCommand.SetAction(parseResult =>
        {
            try {
                string loadArg = parseResult.GetValue(loadOption) ?? throw new Exception("No tree file specified");

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

                tree.Inspect();
            }
            catch (FileNotFoundException e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine($"File not found: {e.FileName}");
                Console.ResetColor();
                Environment.Exit(1);
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine($"Error: {e.Message}");
                Console.ResetColor();
                Environment.Exit(1);
            }
        });

        reportCommand.SetAction(parseResult =>
        {
            try {
                string loadArg = parseResult.GetValue(loadOption) ?? throw new Exception("No tree file specified");
                string outputArg = parseResult.GetValue(outputOption) ?? throw new Exception("No output file specified");
                bool dontOpenArg = parseResult.GetValue(dontOpenOption);
                string baseCommandArg = parseResult.GetValue(baseCommandOption) ?? "xdg-open";

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

                string[] TrustedBaseCommands =
                [
                    "xdg-open",
                    "firefox",
                    "chrome",
                    "chromium",
                    "brave",
                    "nautilus",
                    "dolphin",
                    "thunar",
                    "nemo"
                ];

                // output
                LoadFileArgument(outputArg, "output parameter", [".html"], false, (format, filename) =>
                {
                    switch (format)
                    {
                        case "html":
                        {
                            string html = tree.GenerateHtmlReport();
                            File.WriteAllText(filename, html);

                            if (!dontOpenArg) {
                                bool confirmed = false;
                                bool trustedCommand = false;

                                if (!TrustedBaseCommands.Contains(baseCommandArg))
                                {
                                    Console.Write($"Do you really want to execute:\n\n{baseCommandArg} {filename}\n\n[y/N] ");
                                    string? answer = Console.ReadLine();
                                    if (answer is null)
                                        confirmed = false;
                                    else if (answer.Equals("y", StringComparison.CurrentCultureIgnoreCase) || answer.Equals("yes", StringComparison.CurrentCultureIgnoreCase))
                                        confirmed = true;
                                    else
                                        confirmed = false;
                                }
                                else
                                {
                                    confirmed = true;
                                }

                                if (confirmed) {
                                    ProcessStartInfo startInfo = new()
                                    {
                                        FileName = baseCommandArg,
                                        ArgumentList = { filename },
                                        UseShellExecute = false
                                    };

                                    try
                                    {
                                        Process.Start(startInfo);
                                    }
                                    catch (Win32Exception ex) when (ex.NativeErrorCode == 2)
                                    {
                                        throw new InvalidOperationException(
                                            $"{baseCommandArg} {filename} could not be executed.",
                                            ex);
                                    }

                                    Console.WriteLine($"Executed \"{baseCommandArg} {filename}\"");
                                    if (trustedCommand) Console.WriteLine("Because \"{baseCommandArg}\" is trusted.");
                                    else Console.WriteLine("Because command was confirmed");
                                }
                                else
                                    Console.WriteLine($"Did not execute \"{baseCommandArg} {filename}\"\nBecause command was not confirmed");
                            }
                            break;
                        }
                    }
                });
            }
            catch (FileNotFoundException e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine($"File not found: {e.FileName}");
                Console.ResetColor();
                Environment.Exit(1);
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine($"Error: {e.Message}");
                Console.ResetColor();
                Environment.Exit(1);
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