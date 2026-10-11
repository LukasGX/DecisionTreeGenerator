using System.Text.Json;
using System.Text;
using System.Diagnostics;
using DecisionTreeGenerator.Data;
using DecisionTreeGenerator.Nodes;
using System.ComponentModel;

namespace DecisionTreeGenerator.TreeNS;

public class Tree(Node rootNode)
{
    public Node RootNode = rootNode;

    public bool Test(DataPool dataPool)
    {
        bool status = true;

        foreach (Element testElement in dataPool.TestData)
        {
            Outcome prediction = TreeGenerator.Predict(this, testElement);

            if (prediction.Name != testElement.Label)
                status = false;
        }

        return status;
    }

    public void Print()
    {
        PrintNode(RootNode, "");
    }

    private static void PrintNode(Node node, string indent)
    {
        if (node is LeafNode leaf)
        {
            Console.WriteLine($"{indent}[{leaf.Outcome.Name}]");
            return;
        }

        if (node is not Decision decision)
            throw new Exception("Unknown node type.");

        Console.WriteLine($"{indent}{decision.Attribute.Name}");

        foreach (Outcome outcome in decision.PossibleOutcomes)
        {
            Console.WriteLine($"{indent}├── {outcome.Name}");

            if (decision.FollowingNodes.TryGetValue(outcome, out Node? child))
            {
                PrintNode(child, indent + "│   ");
            }
            else
            {
                Console.WriteLine($"{indent}│   [No child node]");
            }
        }
    }

    public void ExportDot(string filename)
    {
        StringBuilder dot = GenerateDot();

        File.WriteAllText(
            filename,
            dot.ToString(),
            new UTF8Encoding(false)
        );
    }

    public void ExportSvg(string filename)
    {
        StringBuilder dot = GenerateDot();

        ProcessStartInfo startInfo = new()
        {
            FileName = "dot",
            ArgumentList = { "-Tsvg", "-o", filename },
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using Process process = new()
        {
            StartInfo = startInfo
        };

        try {
            process.Start();
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 2)
        {
            throw new InvalidOperationException(
                "Graphviz is not installed or 'dot' was not found in PATH. " +
                "Install it with: sudo dnf install graphviz",
                ex);
        }

        process.StandardInput.Write(dot.ToString());
        process.StandardInput.Close();

        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Graphviz dot exited with code {process.ExitCode}.");
        }
    }

    public StringBuilder GenerateDot()
    {
        StringBuilder dot = new();

        dot.AppendLine("digraph DecisionTree {");
        dot.AppendLine("    rankdir=TB;");
        dot.AppendLine("    graph [ordering=out];");
        dot.AppendLine("    node [shape=box, style=\"rounded,filled\", fontname=\"Sans\"];");
        dot.AppendLine("    edge [fontname=\"Sans\"];");

        int nextId = 0;
        WriteNode(dot, RootNode, ref nextId);

        dot.AppendLine("}");

        return dot;
    }

    private static string WriteNode(
        StringBuilder dot,
        Node node,
        ref int nextId)
    {
        string id = $"n{nextId++}";

        switch (node)
        {
            case LeafNode leaf:
                dot.AppendLine(
                    $"    {id} [label=\"{Escape(leaf.Outcome.Name)}\", " +
                    "fillcolor=\"#dcfce7\"];");
                break;

            case Decision decision:
                dot.AppendLine(
                    $"    {id} [label=\"{Escape(decision.Attribute.Name)}?\", " +
                    "fillcolor=\"#dbeafe\"];");

                foreach (Outcome outcome in decision.PossibleOutcomes)
                {
                    if (!decision.FollowingNodes.TryGetValue(
                            outcome, out Node? child))
                    {
                        continue;
                    }

                    string childId = WriteNode(dot, child, ref nextId);

                    dot.AppendLine(
                        $"    {id} -> {childId} " +
                        $"[label=\"{Escape(outcome.Name)}\"];");
                }

                break;

            default:
                throw new InvalidOperationException(
                    $"Unknown node type: {node.GetType().Name}");
        }

        return id;
    }

    private static string Escape(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", "")
            .Replace("\n", "\\n");
    }

    public void ExportJSON(string filename, ElementSchema schema)
    {
        var model = new
        {
            schema = schema.Attributes.Select(attribute => new
            {
                name = attribute.Name,
                outcomes = attribute.Outcomes
                    .Select(outcome => outcome.Name)
                    .ToList()
            }).ToList(),

            root = SerializeNode(RootNode)
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        string json = JsonSerializer.Serialize(model, options);
        File.WriteAllText(filename, json, new System.Text.UTF8Encoding(false));
    }

    private static object SerializeNode(Node node)
    {
        if (node is LeafNode leaf)
        {
            return new
            {
                type = "leaf",
                outcome = leaf.Outcome.Name
            };
        }

        if (node is Decision decision)
        {
            return new
            {
                type = "decision",
                attribute = decision.Attribute.Name,
                branches = decision.PossibleOutcomes
                    .Where(outcome =>
                        decision.FollowingNodes.ContainsKey(outcome))
                    .Select(outcome => new
                    {
                        outcome = outcome.Name,
                        node = SerializeNode(
                            decision.FollowingNodes[outcome])
                    })
                    .ToList()
            };
        }

        throw new InvalidOperationException(
            $"Unknown node type: {node.GetType().Name}");
    }

    public static (Tree, ElementSchema) LoadFromJSON(string filename)
    {
        using JsonDocument document = JsonDocument.Parse(
            File.ReadAllText(filename));

        JsonElement model = document.RootElement;

        // Schema laden
        List<DataAttribute> attributes = [];

        foreach (JsonElement attribute in model.GetProperty("schema").EnumerateArray())
        {
            string name = attribute.GetProperty("name").GetString()
                ?? throw new FormatException("Missing attribute name.");

            List<Outcome> outcomes = attribute.GetProperty("outcomes")
                .EnumerateArray()
                .Select(o => new Outcome(
                    o.GetString() ?? throw new FormatException("Invalid outcome.")))
                .ToList();

            attributes.Add(new DataAttribute(name, outcomes));
        }

        ElementSchema schema = new(attributes);

        // Baum laden
        Node root = DeserializeNode(model.GetProperty("root"));

        return (new Tree(root), schema);
    }

    private static Node DeserializeNode(JsonElement element)
    {
        string type = element.GetProperty("type").GetString()
            ?? throw new InvalidDataException("Missing node type.");

        string? attributeName = type == "decision"
            ? element.GetProperty("attribute").GetString()
            : null;

        switch (type)
        {
            case "leaf":
            {
                string outcomeName = element
                    .GetProperty("outcome")
                    .GetString()
                    ?? throw new InvalidDataException("Missing outcome.");

                return new LeafNode(new Outcome(outcomeName));
            }

            case "decision":
            {
                List<Outcome> outcomes = [];
                Dictionary<Outcome, Node> children = [];

                foreach (JsonElement branch in
                        element.GetProperty("branches").EnumerateArray())
                {
                    string outcomeName = branch
                        .GetProperty("outcome")
                        .GetString()
                        ?? throw new InvalidDataException("Missing branch outcome.");

                    Outcome outcome = new(outcomeName);
                    Node child = DeserializeNode(branch.GetProperty("node"));

                    outcomes.Add(outcome);
                    children.Add(outcome, child);
                }

                if (attributeName is null) throw new Exception("Internal Error.");

                Decision decision = new(
                    new DataAttribute(attributeName, outcomes),
                    outcomes);

                foreach (var child in children)
                    decision.FollowingNodes.Add(child.Key, child.Value);

                return decision;
            }

            default:
                throw new InvalidDataException(
                    $"Unknown node type: {type}");
        }
    }
}