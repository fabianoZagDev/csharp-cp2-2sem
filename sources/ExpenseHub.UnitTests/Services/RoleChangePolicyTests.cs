using System.Linq;
using ExpenseHub.Api.Auth;
using ExpenseHub.Api.Errors;
using ExpenseHub.Api.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Services;

/// <summary>
/// Testes de <see cref="RoleChangePolicy"/>.
/// </summary>
[TestClass]
public sealed class RoleChangePolicyTests
{
    /// <summary>
    /// Roles conhecidas são aceitas e repetições são removidas.
    /// </summary>
    [TestMethod]
    public void Resolve_KnownRolesWithDuplicates_ReturnsDistinctRoles()
    {
        var result = RoleChangePolicy.Resolve("admin", "user", [], [Roles.Employee, Roles.Employee, Roles.Approver]);

        CollectionAssert.AreEquivalent(new[] { Roles.Employee, Roles.Approver }, result.ToArray());
    }

    /// <summary>
    /// Role desconhecida gera erro de validação.
    /// </summary>
    [TestMethod]
    public void Resolve_UnknownRole_ThrowsDomainValidation()
    {
        Assert.ThrowsExactly<DomainValidationException>(
            () => RoleChangePolicy.Resolve("admin", "user", [], ["Manager"]));
    }

    /// <summary>
    /// O Admin não pode remover a própria role Admin.
    /// </summary>
    [TestMethod]
    public void Resolve_AdminRemovingOwnAdminRole_ThrowsForbidden()
    {
        Assert.ThrowsExactly<ForbiddenOperationException>(
            () => RoleChangePolicy.Resolve("admin", "admin", [Roles.Admin], [Roles.Employee]));
    }

    /// <summary>
    /// O Admin pode manter a própria role Admin e acumular outras.
    /// </summary>
    [TestMethod]
    public void Resolve_AdminKeepingOwnAdminRole_Succeeds()
    {
        var result = RoleChangePolicy.Resolve("admin", "admin", [Roles.Admin], [Roles.Admin, Roles.Employee]);

        Assert.HasCount(2, result);
    }

    /// <summary>
    /// Um Admin pode retirar a role Admin de outro usuário.
    /// </summary>
    [TestMethod]
    public void Resolve_AdminRemovingAdminFromAnotherUser_Succeeds()
    {
        var result = RoleChangePolicy.Resolve("admin", "other-admin", [Roles.Admin], []);

        Assert.IsEmpty(result);
    }
}
