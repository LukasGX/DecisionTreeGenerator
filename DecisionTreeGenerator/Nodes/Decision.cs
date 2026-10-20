using DecisionTreeGenerator.Data;
namespace DecisionTreeGenerator.Nodes;

public class Decision(
    DataAttribute attribute,
    bool numeric
) : Node
{
    public DataAttribute Attribute = attribute;
    public List<Outcome> PossibleOutcomes = [];
    public bool Numeric = numeric;
    public double Treshold = 0.0;

    public Dictionary<Outcome, Node> FollowingNodes = [];
}