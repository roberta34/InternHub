using InternHub.Api.Domain.Auth;
using InternHub.Api.Features.Shared;
using InternHub.Api.Infrastructure.Persistence;
using InternHub.Domain.Students;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace InternHub.Api.Infrastructure.Auth;

public sealed class IdentityStudentAccountRegistration(
    InternHubDbContext db,
    UserManager<ApplicationUser> users,
    RoleManager<IdentityRole<Guid>> roles) : IStudentAccountRegistration
{
    private const string DuplicateEmailMessage = "An account with this email already exists.";
    private const string StudentRole = "Student";

    public async Task<Result<RegisteredAccount>> RegisterAsync(
        string fullName,
        string email,
        string password,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var student = new Student(Guid.NewGuid(), fullName, email);

        if (await db.Students.AsNoTracking().AnyAsync(existing => existing.Email == student.Email, ct)
            || await users.FindByEmailAsync(student.Email) is not null)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            return Result<RegisteredAccount>.Conflict(DuplicateEmailMessage);
        }

        db.Students.Add(student);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException exception) when (IsDuplicateStudentEmail(exception))
        {
            await transaction.RollbackAsync(CancellationToken.None);
            return Result<RegisteredAccount>.Conflict(DuplicateEmailMessage);
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = student.Email,
            Email = student.Email,
            StudentId = student.Id
        };

        IdentityResult createUserResult;
        try
        {
            ct.ThrowIfCancellationRequested();
            createUserResult = await users.CreateAsync(user, password);
        }
        catch (DbUpdateException exception) when (IsDuplicateUserEmail(exception))
        {
            await transaction.RollbackAsync(CancellationToken.None);
            return Result<RegisteredAccount>.Conflict(DuplicateEmailMessage);
        }

        if (!createUserResult.Succeeded)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            if (createUserResult.Errors.Any(IsDuplicateEmailError))
                return Result<RegisteredAccount>.Conflict(DuplicateEmailMessage);

            return Result<RegisteredAccount>.Validation(GetIdentityErrorMessage(createUserResult));
        }

        ct.ThrowIfCancellationRequested();
        var role = await roles.FindByNameAsync(StudentRole);
        if (role is null)
        {
            var createRoleResult = await roles.CreateAsync(new IdentityRole<Guid>
            {
                Id = Guid.NewGuid(),
                Name = StudentRole
            });
            if (!createRoleResult.Succeeded)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                return Result<RegisteredAccount>.Validation(GetIdentityErrorMessage(createRoleResult));
            }
        }

        ct.ThrowIfCancellationRequested();
        var addRoleResult = await users.AddToRoleAsync(user, StudentRole);
        if (!addRoleResult.Succeeded)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            return Result<RegisteredAccount>.Validation(GetIdentityErrorMessage(addRoleResult));
        }

        await transaction.CommitAsync(ct);
        return Result<RegisteredAccount>.Success(
            new RegisteredAccount(user.Id, student.Id, student.Email));
    }

    private static bool IsDuplicateStudentEmail(DbUpdateException exception) =>
        exception.InnerException is SqliteException { SqliteErrorCode: 19 } sqliteException
        && sqliteException.Message.Contains("Students.Email", StringComparison.OrdinalIgnoreCase);

    private static bool IsDuplicateUserEmail(DbUpdateException exception) =>
        exception.InnerException is SqliteException { SqliteErrorCode: 19 } sqliteException
        && (sqliteException.Message.Contains(
                "AspNetUsers.NormalizedEmail",
                StringComparison.OrdinalIgnoreCase)
            || sqliteException.Message.Contains(
                "AspNetUsers.NormalizedUserName",
                StringComparison.OrdinalIgnoreCase));

    private static bool IsDuplicateEmailError(IdentityError error) =>
        error.Code is "DuplicateEmail" or "DuplicateUserName";

    private static string GetIdentityErrorMessage(IdentityResult result)
    {
        var message = string.Join(" ", result.Errors.Select(error => error.Description));
        return string.IsNullOrWhiteSpace(message)
            ? throw new InvalidOperationException("Identity returned a failed result without errors.")
            : message;
    }
}
