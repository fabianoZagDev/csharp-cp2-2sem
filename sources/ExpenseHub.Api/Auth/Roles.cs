using System.Collections.Generic;

namespace ExpenseHub.Api.Auth;

/// <summary>
/// Nomes das roles do sistema.
/// </summary>
public static class Roles
{
    /// <summary>
    /// Administrador: gerencia usuários e roles.
    /// </summary>
    public const string Admin = "Admin";

    /// <summary>
    /// Colaborador: cria e envia os próprios reembolsos.
    /// </summary>
    public const string Employee = "Employee";

    /// <summary>
    /// Aprovador: aprova ou reprova reembolsos enviados.
    /// </summary>
    public const string Approver = "Approver";

    /// <summary>
    /// Financeiro: paga reembolsos aprovados.
    /// </summary>
    public const string Finance = "Finance";

    /// <summary>
    /// Auditor: leitura de todos os reembolsos.
    /// </summary>
    public const string Auditor = "Auditor";

    /// <summary>
    /// Todas as roles do sistema.
    /// </summary>
    public static readonly IReadOnlyList<string> All = [Admin, Employee, Approver, Finance, Auditor];
}
