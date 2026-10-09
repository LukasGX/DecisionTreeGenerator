using DecisionTreeGenerator.Data;
using DecisionTreeGenerator;
using Xunit;
using DecisionTreeGenerator.TreeNS;

public class GeneratorTests
{
    [Fact]
    public void CalculateInformationGain_PerfectSplit_ReturnsOne()
    {
        ElementSchema schema = new([
            new("Color", [new("Red"), new("Green")])
        ]);

        List<Element> elements =
        [
            new("A", [new("Color", new("Red"))]),
            new("A", [new("Color", new("Red"))]),
            new("B", [new("Color", new("Green"))]),
            new("B", [new("Color", new("Green"))])
        ];

        double gain = TreeGenerator.CalculateInformationGain(
            schema.Attributes[0], elements);

        Assert.Equal(1.0, gain, precision: 10);
    }

    [Fact]
    public void CalculateInformationGain_NoUsefulSplit_ReturnsZero()
    {
        ElementSchema schema = new([
            new("Color", [new("Red"), new("Green")])
        ]);

        List<Element> elements =
        [
            new("A", [new("Color", new("Red"))]),
            new("B", [new("Color", new("Red"))]),
            new("A", [new("Color", new("Green"))]),
            new("B", [new("Color", new("Green"))])
        ];

        double gain = TreeGenerator.CalculateInformationGain(
            schema.Attributes[0], elements);

        Assert.Equal(0.0, gain, precision: 10);
    }

    [Fact]
    public void CalculateInformationGain_EmptyDataset_ReturnsZero()
    {
        ElementSchema schema = new([
            new("Color", [new("Red"), new("Green")])
        ]);

        double gain = TreeGenerator.CalculateInformationGain(
            schema.Attributes[0], []);

        Assert.Equal(0.0, gain);
    }

    [Fact]
    public void CalculateInformationGain_PureDataset_ReturnsZero()
    {
        ElementSchema schema = new([
            new("Color", [new("Red"), new("Green")])
        ]);

        List<Element> elements =
        [
            new("A", [new("Color", new("Red"))]),
            new("A", [new("Color", new("Green"))]),
            new("A", [new("Color", new("Red"))])
        ];

        double gain = TreeGenerator.CalculateInformationGain(
            schema.Attributes[0], elements);

        Assert.Equal(0.0, gain, precision: 10);
    }

    [Fact]
    public void GenerateTree_PerfectlySeparableData_CreatesCorrectPredictions()
    {
        ElementSchema schema = new([
            new("Color", [new("Red"), new("Green")])
        ]);

        DataPool dataPool = new();

        dataPool.TrainingData.AddRange([
            new("A", [new("Color", new("Red"))]),
            new("A", [new("Color", new("Red"))]),
            new("B", [new("Color", new("Green"))]),
            new("B", [new("Color", new("Green"))])
        ]);

        Tree tree = TreeGenerator.GenerateTree(schema, dataPool);

        Element redElement = new("A", [
            new("Color", new("Red"))
        ]);

        Element greenElement = new("B", [
            new("Color", new("Green"))
        ]);

        Assert.Equal("A", TreeGenerator.Predict(tree, redElement).Name);
        Assert.Equal("B", TreeGenerator.Predict(tree, greenElement).Name);
    }

    [Fact]
    public void GenerateTree_NoUsefulSplit_ReturnsMajorityLabel()
    {
        ElementSchema schema = new([
            new("Color", [new("Red"), new("Green")])
        ]);

        DataPool dataPool = new();

        dataPool.TrainingData.AddRange([
            new("A", [new("Color", new("Red"))]),
            new("A", [new("Color", new("Green"))]),
            new("A", [new("Color", new("Red"))]),
            new("B", [new("Color", new("Red"))])
        ]);

        Tree tree = TreeGenerator.GenerateTree(schema, dataPool);

        Element testElement = new("A", [
            new("Color", new("Green"))
        ]);

        Outcome prediction = TreeGenerator.Predict(tree, testElement);

        Assert.Equal("A", prediction.Name);
    }

    [Fact]
    public void GenerateTree_TwoRelevantAttributes_PredictsCorrectly()
    {
        ElementSchema schema = new([
            new("Color", [new("Red"), new("Green")]),
            new("Shape", [new("Circle"), new("Square")])
        ]);

        DataPool dataPool = new();

        dataPool.TrainingData.AddRange([
            new("A", [new("Color", new("Red")),   new("Shape", new("Circle"))]),
            new("A", [new("Color", new("Red")),   new("Shape", new("Square"))]),
            new("B", [new("Color", new("Green")), new("Shape", new("Circle"))]),
            new("A", [new("Color", new("Green")), new("Shape", new("Square"))]),
            new("B", [new("Color", new("Green")), new("Shape", new("Circle"))])
        ]);

        Tree tree = TreeGenerator.GenerateTree(schema, dataPool);

        Assert.Equal("A", TreeGenerator.Predict(tree,
            new("A", [new("Color", new("Red")), new("Shape", new("Circle"))])).Name);

        Assert.Equal("A", TreeGenerator.Predict(tree,
            new("A", [new("Color", new("Red")), new("Shape", new("Square"))])).Name);

        Assert.Equal("B", TreeGenerator.Predict(tree,
            new("B", [new("Color", new("Green")), new("Shape", new("Circle"))])).Name);

        Assert.Equal("A", TreeGenerator.Predict(tree,
            new("A", [new("Color", new("Green")), new("Shape", new("Square"))])).Name);
    }

    [Fact]
    public void GenerateTree_EmptyTrainingData_ThrowsException()
    {
        ElementSchema schema = new([
            new("Color", [new("Red"), new("Green")])
        ]);

        DataPool dataPool = new();

        Assert.Throws<Exception>(() =>
            TreeGenerator.GenerateTree(schema, dataPool));
    }
}