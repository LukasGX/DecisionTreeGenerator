namespace DecisionTreeGenerator.Data;

public class DataAttribute(string name, List<Outcome> outcomes)
{
    public string Name = name;
    public List<Outcome> Outcomes = outcomes;
}