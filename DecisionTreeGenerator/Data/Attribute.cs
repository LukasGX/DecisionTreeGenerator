using System.Xml.Schema;

namespace DecisionTreeGenerator.Data;

public class DataAttribute(string name, List<Outcome> outcomes, bool numeric)
{
    public string Name = name;
    public List<Outcome> Outcomes = outcomes;
    public bool Numeric = numeric;
}