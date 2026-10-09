using DecisionTreeGenerator.Data;
using DecisionTreeGenerator.Nodes;
using DecisionTreeGenerator.TreeNS;

namespace DecisionTreeGenerator;

public static class TreeGenerator
{
    public static Tree GenerateTree(
        ElementSchema elementSchema,
        DataPool dataPool
    )
    {
        if (!elementSchema.ValidateList(dataPool.TrainingData))
            throw new Exception("Error in training data");

        if (!elementSchema.ValidateList(dataPool.TestData))
            throw new Exception("Error in test data");

        Node rootNode = BuildTree(elementSchema, dataPool.TrainingData, elementSchema.Attributes);

        return new Tree(rootNode);
    }

    private static Node BuildTree(
        ElementSchema elementSchema,
        List<Element> elements,
        List<DataAttribute> availableAttributes
    )
    {
        if (elements.Count == 0)
            throw new Exception("Empty subset");

        string[] labels = elements
            .Select(e => e.Label)
            .Distinct()
            .ToArray();

        if (labels.Length == 1)
            return new LeafNode(new(labels[0]));

        (DataAttribute? bestSplit, double informationGain, List<DataAttribute> availableAttributesNew) =
            GetBestSplit(elements, availableAttributes);

        if (bestSplit is null || informationGain <= 0)
        {
            string majorityLabel = elements
                .GroupBy(e => e.Label)
                .OrderByDescending(g => g.Count())
                .First()
                .Key;

            return new LeafNode(new(majorityLabel));
        }

        Decision decision = new(bestSplit, bestSplit.Outcomes);

        foreach (Outcome outcome in bestSplit.Outcomes)
        {
            List<Element> subset = [.. elements.Where(e =>
                e.Attributes.Any(a =>
                    a.Name == bestSplit.Name &&
                    a.Outcome.Name == outcome.Name))];

            if (subset.Count == 0)
            {
                string majorityLabel = elements
                    .GroupBy(e => e.Label)
                    .OrderByDescending(g => g.Count())
                    .First()
                    .Key;

                decision.FollowingNodes[outcome] =
                    new LeafNode(new(majorityLabel));
            }
            else
            {
                decision.FollowingNodes[outcome] =
                    BuildTree(elementSchema, subset, availableAttributesNew);
            }
        }

        return decision;
    }

    
    public static double CalculateInformationGain(
        DataAttribute attribute,
        List<Element> elements)
    {
        if (elements.Count == 0)
            return 0.0;

        double entropyBefore = CalculateEntropy(elements);
        double entropyAfter = 0.0;

        foreach (Outcome outcome in attribute.Outcomes)
        {
            List<Element> subset = elements
                .Where(e => e.Attributes.Any(a =>
                    a.Name == attribute.Name &&
                    a.Outcome.Name == outcome.Name))
                .ToList();

            if (subset.Count == 0)
                continue;

            double weight = (double)subset.Count / elements.Count;

            entropyAfter += weight * CalculateEntropy(subset);
        }

        return entropyBefore - entropyAfter;
    }

    private static double CalculateEntropy(List<Element> elements)
    {
        if (elements.Count == 0)
            return 0.0;

        return elements
            .GroupBy(e => e.Label)
            .Select(group =>
            {
                double p = (double)group.Count() / elements.Count;
                return -p * Math.Log2(p);
            })
            .Sum();
    }

    private static (
        DataAttribute?,
        double,
        List<DataAttribute>
    ) GetBestSplit(
        List<Element> elements,
        List<DataAttribute> availableAttributes)
    {
        DataAttribute? bestSplit = null;
        double bestInformationGain = 0;

        foreach (DataAttribute attribute in availableAttributes)
        {
            double informationGain =
                CalculateInformationGain(attribute, elements);

            if (informationGain > bestInformationGain)
            {
                bestSplit = attribute;
                bestInformationGain = informationGain;
            }
        }

        List<DataAttribute> remainingAttributes = [.. availableAttributes];

        if (bestSplit is not null)
            remainingAttributes.Remove(bestSplit);

        return (bestSplit, bestInformationGain, remainingAttributes);
    }

    public static Outcome Predict(Tree tree, Element element)
    {
        Node currentNode = tree.RootNode;

        while (currentNode is Decision decision)
        {
            SetAttribute? attribute = element.Attributes
                .FirstOrDefault(a => a.Name == decision.Attribute.Name);

            if (attribute is null)
                throw new Exception(
                    $"Missing attribute: {decision.Attribute.Name}");

            Outcome? outcome = decision.PossibleOutcomes
                .FirstOrDefault(o => o.Name == attribute.Outcome.Name);

            if (outcome is null)
                throw new Exception(
                    $"Unknown outcome: {attribute.Outcome.Name}");

            if (!decision.FollowingNodes.TryGetValue(outcome, out Node? nextNode))
                throw new Exception(
                    $"No child node for outcome: {outcome.Name}");

            currentNode = nextNode;
        }

        if (currentNode is LeafNode leaf)
            return leaf.Outcome;

        throw new Exception("Invalid tree node");
    }
}