using Intravision.Models;
namespace Intravision.Services.Contracts;

public record SaveDrinkCommand(int Id, DrinkDetails Details, string? RowVersion, byte[]? Image);
public record SaveCoinCommand(decimal Denomination, int Quantity, bool IsBlocked, string? RowVersion);
public enum AdminStatus { Success, Invalid, NotFound, Conflict }
public record AdminResult(AdminStatus Status, IReadOnlyList<ValidationIssue> Errors)
{
    public static AdminResult Success() => new(AdminStatus.Success, []);
    public static AdminResult Missing() => new(AdminStatus.NotFound, []);
    public static AdminResult Conflict() => new(AdminStatus.Conflict, []);
    public static AdminResult Invalid(params ValidationIssue[] errors) => new(AdminStatus.Invalid, errors);
}
public interface IAdminService
{
    Task<AdminResult> SaveDrinkAsync(SaveDrinkCommand command, CancellationToken cancellationToken);
    Task<AdminResult> DeleteDrinkAsync(int id, string? rowVersion, CancellationToken cancellationToken);
    Task<AdminResult> SaveCoinsAsync(IReadOnlyList<SaveCoinCommand> commands, CancellationToken cancellationToken);
}
