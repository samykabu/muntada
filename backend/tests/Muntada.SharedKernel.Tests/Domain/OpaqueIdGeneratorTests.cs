using Muntada.SharedKernel.Domain;

namespace Muntada.SharedKernel.Tests.Domain;

public class OpaqueIdGeneratorTests
{
    [Fact]
    public void Generate_should_return_id_with_prefix()
    {
        var id = OpaqueIdGenerator.Generate("usr");

        id.Should().StartWith("usr_");
    }

    [Fact]
    public void Generate_should_produce_unique_ids()
    {
        var ids = Enumerable.Range(0, 100)
            .Select(_ => OpaqueIdGenerator.Generate("usr"))
            .ToList();

        ids.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Generate_should_reject_empty_prefix()
    {
        var act = () => OpaqueIdGenerator.Generate("");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Generate_should_reject_prefix_too_short()
    {
        var act = () => OpaqueIdGenerator.Generate("a");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Generate_should_reject_prefix_too_long()
    {
        var act = () => OpaqueIdGenerator.Generate("toolongprefix");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Generate_should_reject_uppercase_prefix()
    {
        var act = () => OpaqueIdGenerator.Generate("USR");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void TryParse_should_succeed_for_valid_id()
    {
        var id = OpaqueIdGenerator.Generate("room");

        var result = OpaqueIdGenerator.TryParse(id, out var prefix, out var encoded);

        result.Should().BeTrue();
        prefix.Should().Be("room");
        encoded.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void TryParse_should_fail_for_invalid_input()
    {
        OpaqueIdGenerator.TryParse("invalid", out _, out _).Should().BeFalse();
        OpaqueIdGenerator.TryParse("", out _, out _).Should().BeFalse();
        OpaqueIdGenerator.TryParse("a_encoded", out _, out _).Should().BeFalse();
        OpaqueIdGenerator.TryParse("USR_encoded", out _, out _).Should().BeFalse();
    }

    [Fact]
    public void Generated_id_should_be_url_safe()
    {
        var id = OpaqueIdGenerator.Generate("msg");

        id.Should().MatchRegex(@"^[a-z]{2,8}_[a-zA-Z0-9]+$");
    }
}
