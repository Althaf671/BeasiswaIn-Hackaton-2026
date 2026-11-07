using BeasiswaIn.Web.Common.Abstractions;
using BeasiswaIn.Web.Common.Enums;

namespace BeasiswaIn.Web.Common.Errors;

internal static class ScholarBotErrors
{
    public static readonly Error Unauthorized = Error.Create(ErrorType.Unauthorized,
        "Unauthorized", "Silakan masuk untuk menggunakan ScholarBot.", "ScholarBot");
    public static readonly Error InvalidRequest = Error.Create(ErrorType.BadRequest,
        "InvalidRequest", "Pesan atau riwayat percakapan tidak valid. Batasi pesan/esai hingga 12.000 karakter dan mulai percakapan baru jika riwayat terlalu panjang.", "ScholarBot");
    public static readonly Error Unavailable = Error.Create(ErrorType.ServiceUnavailable,
        "Unavailable", "ScholarBot belum dapat menghubungi layanan AI. Silakan coba lagi nanti.", "ScholarBot");
    public static readonly Error Timeout = Error.Create(ErrorType.RequestTimeout,
        "Timeout", "Permintaan ScholarBot melewati batas waktu. Coba pertanyaan yang lebih singkat.", "ScholarBot");
    public static readonly Error BudgetExceeded = Error.Create(ErrorType.BadRequest,
        "BudgetExceeded", "Batas pemrosesan tercapai. Sempitkan pertanyaan atau mulai percakapan baru.", "ScholarBot");
    public static readonly Error InvalidResponse = Error.Create(ErrorType.ServiceUnavailable,
        "InvalidResponse", "Jawaban AI belum lengkap atau tidak dapat digunakan. Silakan coba lagi.", "ScholarBot");
}
