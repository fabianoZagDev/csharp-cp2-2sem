using System;
using System.Linq;
using System.Reflection;
using ExpenseHub.Api.Auth;
using ExpenseHub.Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Controllers;

/// <summary>
/// Garante, por reflexão, a matriz de roles das rotas de <see cref="ExpensesController"/>.
/// </summary>
[TestClass]
public sealed class ExpensesControllerAuthorizationTests
{
    /// <summary>
    /// Nenhuma rota de escrita aceita Auditor ou Admin.
    /// </summary>
    [TestMethod]
    public void WriteActions_Roles_NeverIncludeAuditorOrAdmin()
    {
        foreach (MethodInfo action in ActionsWhere(IsWrite))
        {
            string[] roles = RolesOf(action);

            Assert.IsNotEmpty(roles, $"{action.Name} deve restringir roles.");
            Assert.DoesNotContain(Roles.Auditor, roles, $"{action.Name} não pode aceitar Auditor.");
            Assert.DoesNotContain(Roles.Admin, roles, $"{action.Name} não pode aceitar Admin.");
        }
    }

    /// <summary>
    /// As rotas de leitura aceitam Employee, Approver, Finance e Auditor, e nunca Admin.
    /// </summary>
    [TestMethod]
    public void ReadActions_Roles_AreTheFourFunctionalRolesWithoutAdmin()
    {
        string[] expected = [Roles.Employee, Roles.Approver, Roles.Finance, Roles.Auditor];

        foreach (MethodInfo action in ActionsWhere(method => !IsWrite(method)))
        {
            CollectionAssert.AreEquivalent(expected, RolesOf(action), action.Name);
        }
    }

    /// <summary>
    /// O controller inteiro exige autenticação.
    /// </summary>
    [TestMethod]
    public void Controller_Class_RequiresAuthorization()
    {
        Assert.IsNotNull(typeof(ExpensesController).GetCustomAttribute<AuthorizeAttribute>());
    }

    /// <summary>
    /// Criar, editar e enviar são exclusivos da role Employee.
    /// </summary>
    [TestMethod]
    public void WriteActions_Roles_AreEmployeeOnly()
    {
        foreach (MethodInfo action in ActionsWhere(IsWrite))
        {
            CollectionAssert.AreEqual(new[] { Roles.Employee }, RolesOf(action), action.Name);
        }
    }

    private static bool IsWrite(MethodInfo method)
    {
        return method.GetCustomAttributes<HttpMethodAttribute>()
            .Any(attribute => attribute.HttpMethods.Any(verb => verb != "GET"));
    }

    private static MethodInfo[] ActionsWhere(Func<MethodInfo, bool> predicate)
    {
        return typeof(ExpensesController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => method.GetCustomAttributes<HttpMethodAttribute>().Any())
            .Where(predicate)
            .ToArray();
    }

    private static string[] RolesOf(MethodInfo action)
    {
        return action.GetCustomAttributes<AuthorizeAttribute>()
            .Where(attribute => !string.IsNullOrEmpty(attribute.Roles))
            .SelectMany(attribute => attribute.Roles!.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            .ToArray();
    }
}
