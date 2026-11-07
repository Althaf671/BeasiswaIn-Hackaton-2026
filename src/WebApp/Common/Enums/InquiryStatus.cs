

using System.ComponentModel.DataAnnotations;

namespace BeasiswaIn.Web.Common.Enums;

public enum InquiryStatus
{
    [Display(Name = "Menunggu")]
    Pending,

    [Display(Name = "Dihubungi")]
    Contacted,

    [Display(Name = "Ditolak")]
    Rejected,

    [Display(Name = "Selesai")]
    Completed,
}