using System.Security.Claims;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using MyBlog.Web.Security;

namespace MyBlog.Unit.Tests.Security;

public class RoleClaimsHelperTests
{
    [Fact]
    public void GetRoleClaimTypes_UsesConfiguredDistinctValues_WhenPresent()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth0:RoleClaimTypes:0"] = "roles",
                ["Auth0:RoleClaimTypes:1"] = "https://myblog/roles",
                ["Auth0:RoleClaimTypes:2"] = "roles"
            })
            .Build();

        var result = RoleClaimsHelper.GetRoleClaimTypes(configuration);

        result.Should().BeEquivalentTo(["roles", "https://myblog/roles"]);
    }

    [Fact]
    public void GetRoleClaimTypes_ReturnsDefaults_WhenConfigurationIsMissing()
    {
        var configuration = new ConfigurationBuilder().Build();

        var result = RoleClaimsHelper.GetRoleClaimTypes(configuration);

        result.Should().BeEquivalentTo(RoleClaimsHelper.DefaultRoleClaimTypes);
    }

    [Theory]
    [InlineData("Admin", new[] { "Admin" })]
    [InlineData("Admin,Author", new[] { "Admin", "Author" })]
    [InlineData(" [\"Admin\",\"Author\"] ", new[] { "Admin", "Author" })]
    public void ExpandRoleValues_ParsesSupportedFormats(string input, string[] expected)
    {
        var result = RoleClaimsHelper.ExpandRoleValues(input);

        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void AddRoleClaims_AddsExpandedRoleClaimsWithoutDuplicates()
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim("roles", "Admin,Author"),
            new Claim(ClaimTypes.Role, "Admin")
        }, "TestAuth", ClaimTypes.Name, ClaimTypes.Role);

        RoleClaimsHelper.AddRoleClaims(identity, ["roles"]);

        identity.FindAll(ClaimTypes.Role)
            .Select(claim => claim.Value)
            .Should()
            .BeEquivalentTo(["Admin", "Author"]);
    }

    [Fact]
    public void GetRoles_CollectsDistinctRolesAcrossMultipleClaimTypes()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim("https://myblog/roles", "[\"Author\",\"Admin\"]"),
            new Claim("roles", "Editor,Author")
        }, "TestAuth", ClaimTypes.Name, ClaimTypes.Role));

        var result = RoleClaimsHelper.GetRoles(principal);

        result.Should().Equal("Admin", "Author", "Editor");
    }
}
