using DecisionTreeGenerator.Data;
namespace DecisionTreeGenerator.Nodes;

public class Decision(
    DataAttribute attribute,
    List<Outcome> possibleOutcomes
) : Node
{
    public DataAttribute Attribute = attribute;
    public List<Outcome> PossibleOutcomes = possibleOutcomes;

    public Dictionary<Outcome, Node> FollowingNodes = [];
}