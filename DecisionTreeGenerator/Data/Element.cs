using System.Reflection.Emit;

namespace DecisionTreeGenerator.Data;

public class Element(string label, List<SetAttribute> attributes)
{
    public string Label = label;
    public List<SetAttribute> Attributes = attributes;
}