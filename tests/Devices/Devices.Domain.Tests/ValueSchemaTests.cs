using System.Text.Json;
using Devices.Domain.Descriptors;

namespace Devices.Domain.Tests;

public sealed class ValueSchemaTests
{
    private static string? Check(string schema, string value) => ValueSchema.Check(schema, JsonDocument.Parse(value).RootElement);

    [Theory]
    [InlineData("""{ "type": "boolean" }""", "true")]
    [InlineData("""{ "type": "boolean" }""", "false")]
    [InlineData("""{ "type": "integer" }""", "3")]
    [InlineData("""{ "type": "integer" }""", "3.0")]
    [InlineData("""{ "type": "integer" }""", "-7")]
    [InlineData("""{ "type": "number" }""", "3.5")]
    [InlineData("""{ "type": "number" }""", "3")]
    [InlineData("""{ "type": "string" }""", "\"x\"")]
    [InlineData("""{ "type": "object" }""", "{}")]
    [InlineData("""{ "type": "array" }""", "[1,2]")]
    [InlineData("""{ "type": "null" }""", "null")]
    [InlineData("""{ "type": "madeup" }""", "1")]
    [InlineData("""{ }""", "\"anything\"")]
    [InlineData("""true""", "1")]
    public void A_value_of_the_declared_type_fits(string schema, string value)
    {
        Assert.Null(Check(schema, value));
    }

    [Theory]
    [InlineData("""{ "type": "boolean" }""", "1")]
    [InlineData("""{ "type": "boolean" }""", "\"true\"")]
    [InlineData("""{ "type": "integer" }""", "3.5")]
    [InlineData("""{ "type": "integer" }""", "\"3\"")]
    [InlineData("""{ "type": "number" }""", "\"3\"")]
    [InlineData("""{ "type": "number" }""", "null")]
    [InlineData("""{ "type": "string" }""", "3")]
    [InlineData("""{ "type": "object" }""", "[]")]
    [InlineData("""{ "type": "array" }""", "{}")]
    [InlineData("""{ "type": "null" }""", "0")]
    public void A_value_of_another_type_is_refused(string schema, string value)
    {
        Assert.Contains("must be of type", Check(schema, value), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("""{ "enum": ["eco", "boost"] }""", "\"eco\"", true)]
    [InlineData("""{ "enum": ["eco", "boost"] }""", "\"turbo\"", false)]
    [InlineData("""{ "enum": [1, 2, 3] }""", "2", true)]
    [InlineData("""{ "enum": [1, 2, 3] }""", "2.0", true)]
    [InlineData("""{ "enum": [1, 2, 3] }""", "4", false)]
    [InlineData("""{ "enum": [1, 2, 3] }""", "\"2\"", false)]
    [InlineData("""{ "enum": [] }""", "1", false)]
    [InlineData("""{ "enum": [null, true] }""", "null", true)]
    [InlineData("""{ "enum": [{ "a": 1 }] }""", "{ \"a\": 1 }", true)]
    [InlineData("""{ "type": "string", "enum": ["a"] }""", "\"b\"", false)]
    public void An_enum_lists_the_allowed_values(string schema, string value, bool fits)
    {
        Assert.Equal(fits, Check(schema, value) is null);
    }

    [Theory]
    [InlineData("""{ "const": 5 }""", "5", true)]
    [InlineData("""{ "const": 5 }""", "6", false)]
    [InlineData("""{ "const": "on" }""", "\"on\"", true)]
    [InlineData("""{ "const": "on" }""", "\"off\"", false)]
    [InlineData("""{ "const": false }""", "false", true)]
    [InlineData("""{ "const": false }""", "0", false)]
    public void A_const_allows_only_itself(string schema, string value, bool fits)
    {
        Assert.Equal(fits, Check(schema, value) is null);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(3, true)]
    [InlineData(1.5, true)]
    [InlineData(-0.001, false)]
    [InlineData(3.001, false)]
    public void Minimum_and_maximum_are_inclusive(double value, bool fits)
    {
        var problem = ValueSchema.Check("""{ "type": "number", "minimum": 0, "maximum": 3 }""", JsonDocument.Parse(value.ToString(System.Globalization.CultureInfo.InvariantCulture)).RootElement);

        Assert.Equal(fits, problem is null);
    }

    [Fact]
    public void The_number_limits_say_which_side_was_broken()
    {
        Assert.Contains("at least", Check("""{ "minimum": 0 }""", "-1"), StringComparison.Ordinal);
        Assert.Contains("at most", Check("""{ "maximum": 0 }""", "1"), StringComparison.Ordinal);
    }

    [Fact]
    public void A_limit_that_is_not_a_number_is_passed_on()
    {
        Assert.Null(Check("""{ "minimum": "low", "maximum": null }""", "5"));
        Assert.Null(Check("""{ "minLength": "one", "maxLength": true }""", "\"abc\""));
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("a", true)]
    [InlineData("abc", true)]
    [InlineData("abcd", false)]
    public void String_length_is_limited(string text, bool fits)
    {
        Assert.Equal(fits, Check("""{ "type": "string", "minLength": 1, "maxLength": 3 }""", JsonSerializer.Serialize(text)) is null);
    }

    [Fact]
    public void Limits_of_one_kind_do_not_apply_to_another()
    {
        Assert.Null(Check("""{ "minLength": 5, "minimum": 5 }""", "true"));
        Assert.Null(Check("""{ "minimum": 5 }""", "\"x\""));
        Assert.Null(Check("""{ "maxLength": 1 }""", "100"));
    }

    private const string Nested = """
        { "type": "object", "required": ["count"],
          "properties": {
            "count": { "type": "integer", "minimum": 0 },
            "tag": { "type": "string", "enum": ["a", "b"] },
            "inner": { "type": "object", "required": ["x"], "properties": { "x": { "type": "number", "maximum": 10 } } } } }
        """;

    [Theory]
    [InlineData("""{ "count": 1 }""")]
    [InlineData("""{ "count": 1, "tag": "a", "inner": { "x": 10 } }""")]
    [InlineData("""{ "count": 1, "extra": "not enforced" }""")]
    public void An_object_with_its_required_properties_fits(string value)
    {
        Assert.Null(Check(Nested, value));
    }

    [Theory]
    [InlineData("""{ }""", "needs 'count'")]
    [InlineData("""{ "count": -1 }""", "value.count must be at least")]
    [InlineData("""{ "count": "1" }""", "value.count must be of type integer")]
    [InlineData("""{ "count": 1, "tag": "z" }""", "value.tag must be one of")]
    [InlineData("""{ "count": 1, "inner": { } }""", "value.inner needs 'x'")]
    [InlineData("""{ "count": 1, "inner": { "x": 11 } }""", "value.inner.x must be at most")]
    [InlineData("""{ "count": 1, "inner": [] }""", "value.inner must be of type object")]
    public void An_object_problem_names_the_path(string value, string problem)
    {
        Assert.Contains(problem, Check(Nested, value), StringComparison.Ordinal);
    }

    [Fact]
    public void Array_items_are_not_enforced()
    {
        Assert.Null(Check("""{ "type": "array", "items": { "type": "integer" }, "minItems": 5 }""", """["x"]"""));
    }

    [Fact]
    public void The_first_problem_is_reported_const_then_enum_then_type()
    {
        Assert.Contains("must be 1", Check("""{ "const": 1, "enum": [1], "type": "string" }""", "2"), StringComparison.Ordinal);
        Assert.Contains("one of", Check("""{ "enum": [1], "type": "string" }""", "2"), StringComparison.Ordinal);
    }

    [Fact]
    public void A_schema_that_is_not_json_throws()
    {
        Assert.ThrowsAny<JsonException>(() => Check("{ not json", "1"));
    }
}
