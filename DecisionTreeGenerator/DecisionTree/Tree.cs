using System.ComponentModel.DataAnnotations;
using System.Xml;
using DecisionTreeGenerator.Data;
using DecisionTreeGenerator.Nodes;

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
            throw new Exception("Unknown node type");

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
}