using System;
using System.Security.Claims;
using ExpenseHub.Api.Auth;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Auth;

/// <summary>
/// Testes de <see cref="CurrentUser"/>.
/// </summary>
[TestClass]
public sealed class CurrentUserTests
{
    /// <summary>
    /// O identificador e as roles são lidos das claims do token.
    /// </summary>
    [TestMethod]
    public void FromPrincipal_TokenClaims_ExtractsIdAndRoles()
    {
        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "user-1"),
                new Claim(ClaimTypes.Role, Roles.Employee),
                new Claim(ClaimTypes.Role, Roles.Approver),
            ],
            "test");

        CurrentUser user = CurrentUser.FromPrincipal(new ClaimsPrincipal(identity));

        Assert.AreEqual("user-1", user.Id);
        Assert.IsTrue(user.IsInRole(Roles.Employee));
        Assert.IsTrue(user.IsInRole(Roles.Approver));
        Assert.IsFalse(user.IsInRole(Roles.Finance));
    }

    /// <summary>
    /// Sem o identificador no token, não há usuário atual.
    /// </summary>
    [TestMethod]
    public void FromPrincipal_MissingNameIdentifier_Throws()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, Roles.Employee)], "test"));

        Assert.ThrowsExactly<InvalidOperationException>(() => CurrentUser.FromPrincipal(principal));
    }

    /// <summary>
    /// O nome da role é comparado sem ignorar maiúsculas.
    /// </summary>
    [TestMethod]
    public void IsInRole_DifferentCase_ReturnsFalse()
    {
        var user = new CurrentUser("user-1", [Roles.Employee]);

        Assert.IsFalse(user.IsInRole("employee"));
    }
}
