using BeasiswaIn.Web.Common.Abstractions;
using BeasiswaIn.Web.Common.Enums;
using BeasiswaIn.Web.Services.Competitions;

namespace BeasiswaIn.Web.Common.Errors;

internal static class CompetitionServiceError
{
    private const string Domain = nameof(CompetitionService);

    public static readonly Error NameDuplicated =
        Error.Create(
            ErrorType.NotFound,
            $"{Domain}.NameDuplicated",
            "Nama ini sudah digunakan",
            Domain) with { Field = "Name"};

    public static readonly Error NotFound =
        Error.Create(
            ErrorType.NotFound,
            $"{Domain}.NotFound",
            "Lomba ini tidak ditemukan",
            Domain);

    public static readonly Error CurrentlyInUse =
        Error.Create(
            ErrorType.NotFound,
            $"{Domain}.CurrentlyInUse",
            "Kategori tidak bisa dihapus karena sedang digunakan",
            Domain);

    public static readonly Error CategoryNotFound = 
        Error.Create(
            ErrorType.NotFound,
            $"{Domain}.CategoryNotFound",
            "Kategori ttidak ditemukan",
            Domain) with { Field = "CategoryId" };
}