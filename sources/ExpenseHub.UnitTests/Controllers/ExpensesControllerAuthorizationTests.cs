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
    /// Cada rota de escrita aceita uma única role: Employee cria, edita e envia; Approver aprova e reprova.
    /// </summary>
    /// <param name="actionName">Nome da ação do controller.</param>
    /// <param name="role">Única role aceita.</param>
    [TestMethod]
    [DataRow(nameof(ExpensesController.Create), Roles.Employee)]
    [DataRow(nameof(ExpensesController.Update), Roles.Employee)]
    [DataRow(nameof(ExpensesController.Submit), Roles.Employee)]
    [DataRow(nameof(ExpensesController.Approve), Roles.Approver)]
    [DataRow(nameof(ExpensesController.Reject), Roles.Approver)]
    public void WriteAction_Roles_MatchAuthorizationMatrix(string actionName, string role)
    {
        MethodInfo action = ActionsWhere(IsWrite).Single(method => method.Name == actionName);

        CollectionAssert.AreEqual(new[] { role }, RolesOf(action), actionName);
    }

    /// <summary>
    /// Toda rota de escrita do controller está coberta pela matriz acima.
    /// </summary>
    [TestMethod]
    public void WriteActions_All_AreCoveredByMatrix()
    {
        string[] expected =
        [
            nameof(ExpensesController.Create),
            nameof(ExpensesController.Update),
            nameof(ExpensesController.Submit),
            nameof(ExpensesController.Approve),
            nameof(ExpensesController.Reject),
        ];

        CollectionAssert.AreEquivalent(expected, ActionsWhere(IsWrite).Select(action => action.Name).ToArray());
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
