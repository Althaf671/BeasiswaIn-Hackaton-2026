

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;

namespace BeasiswaIn.Web.Infrastructures.Persistance.Exceptions;

public sealed class PostgresExceptionTranslatorInterceptor : SaveChangesInterceptor
{
    public override Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData, 
        CancellationToken cancellationToken = default)
    {
        if (eventData.Exception is DbUpdateException { InnerException: PostgresException pgException })
        {
            Exception translated = pgException.SqlState switch
            {
                "23505" => new UniqueConstraintViolationException(
                    MapConstraintToMessage(pgException.ConstraintName)),

                "23503" => new ForeignKeyViolationException(
                    "Related resource does not exist."),

                "23502" => new RequiredFieldMissingException(
                    MapColumnToMessage(pgException.ColumnName)),
                
                "23514" => new CheckConstraintViolationException(
                    $"Value violation constraint: {pgException.ConstraintName}."),

                "22001" => new FieldTooLongException(
                    "One or more fields exceed the maximum allowed length."),

                "22003" => new NumericOverflowException(
                    "Numeric value is out of the allowed range."),

                "40001" or "40P01" => new ConcurrencyConflictException(
                    "The operation could not complete due to a concurrent update, please retry."),

                _ => eventData.Exception
            };

            if (translated != eventData.Exception)
            {
                throw translated;
            }
        }

        if (eventData.Exception is DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException(
                "The record was modified by another process, please reload and retry.");
        }

        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }  

    private static string MapConstraintToMessage(string? constraintName)
    {
        return constraintName switch
        {
            "ix_users_email" => "Email already in use.",
            "ix_users_username" => "Username already taken.",
            _=> "A conflict occured with existing data."
            // add later, i mean like kalau ada case lain
        };
    }

    private static string MapColumnToMessage(string? columnName)
    {
        return columnName is not null
            ? $"Field '{columnName}' is required."
            : "A required field is missing.";
    }
}