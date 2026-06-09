using FluentAssertions;
using WideEvents.Core.Builder;
using Xunit;

namespace WideEvents.Core.Tests.Builder;

public sealed class WideEventStructureBuilderTests
{
    [Fact]
    public void Build_FlatKey_ReturnsSingleLevel()
    {
        var flat = new Dictionary<string, object?> { ["status"] = 200 };
        var result = WideEventStructureBuilder.Build(flat);
        result["status"].Should().Be(200);
    }

    [Fact]
    public void Build_DottedKey_ReturnsNestedDictionary()
    {
        var flat = new Dictionary<string, object?>
        {
            ["user.id"] = "u-1",
            ["user.name"] = "alice"
        };
        var result = WideEventStructureBuilder.Build(flat);
        var user = result["user"].Should().BeAssignableTo<Dictionary<string, object?>>().Subject;
        user["id"].Should().Be("u-1");
        user["name"].Should().Be("alice");
    }

    [Fact]
    public void Build_DeepNesting_ReturnsMultipleLevels()
    {
        var flat = new Dictionary<string, object?> { ["a.b.c"] = "deep" };
        var result = WideEventStructureBuilder.Build(flat);
        var a = result["a"].Should().BeAssignableTo<Dictionary<string, object?>>().Subject;
        var b = a["b"].Should().BeAssignableTo<Dictionary<string, object?>>().Subject;
        b["c"].Should().Be("deep");
    }

    [Fact]
    public void Build_SiblingKeys_ShareParentDictionary()
    {
        var flat = new Dictionary<string, object?>
        {
            ["payment.method"] = "card",
            ["payment.provider"] = "stripe"
        };
        var result = WideEventStructureBuilder.Build(flat);
        var payment = result["payment"].Should().BeAssignableTo<Dictionary<string, object?>>().Subject;
        payment.Should().HaveCount(2);
    }

    [Fact]
    public void Build_EmptyInput_ReturnsEmptyDictionary()
    {
        var result = WideEventStructureBuilder.Build(new Dictionary<string, object?>());
        result.Should().BeEmpty();
    }
}
