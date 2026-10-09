using System.Reflection.Metadata.Ecma335;

namespace DecisionTreeGenerator.Data;

public class ElementSchema(List<DataAttribute> attributes)
{
    public List<DataAttribute> Attributes = attributes;

    public bool Validate(Element element)
    {
        foreach (DataAttribute attr in Attributes)
        {
            SetAttribute? matching = element.Attributes
                .FirstOrDefault(a => a.Name == attr.Name);

            if (matching == null)
                return false;

            if (!attr.Outcomes.Any(o => o.Name == matching.Outcome.Name))
                return false;
        }

        return true;
    }

    public bool ValidateList(List<Element> elements)
    {
        bool valid = true;

        foreach (Element el in elements)
        {
            if (Validate(el) == false)
                valid = false;
        }

        return valid;
    }
}