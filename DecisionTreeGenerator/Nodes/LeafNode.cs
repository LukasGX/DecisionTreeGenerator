using DecisionTreeGenerator.Data;
namespace DecisionTreeGenerator.Nodes;

public class LeafNode(Outcome outcome) : Node
{
    public Outcome Outcome = outcome;
}