
 
namespace BeasiswaIn.Web.Common.Constants;

internal static class AuditActions
{
    public const string InquiryCreated = "Inquiry.Created";
    public const string InquiryUpdated = "Inquiry.Updated";
    public const string InquiryStatusChanged = "Inquiry.StatusChanged";
    public const string InquiryDeleted = "Inquiry.Deleted";
    public const string InquiryPaymentProofUploaded = "Inquiry.PaymentProofUploaded";

    public const string InvoiceCreated = "Invoice.Created";

    public const string PackageCreated = "Package.Created";
    public const string PackageUpdated = "Package.Updated";
    public const string PackageDeleted = "Package.Deleted";
    public const string PackageActivated = "Package.Activated";
    public const string PackageDeactivated = "Package.Deactivated";

    public const string PortfolioCategoryCreated = "PortfolioCategory.Created";
    public const string PortfolioCategoryUpdated = "PortfolioCategory.Updated";
    public const string PortfolioCategoryDeleted = "PortfolioCatergoy.Deleted";
    public const string PortfolioItemCreated = "PortfolioItem.Created";
    public const string PortfolioItemUpdated = "PortfolioItem.Updated";
    public const string PortfolioItemDeleted = "PortfolioItem.Deleted";
    public const string PortfolioItemPublished = "PortfolioItem.Published";
    public const string PortfolioItemUnpublished = "PortfolioItem.Unpublished";

    public const string AuthLoginSucceeded = "Auth.LoginSucceeded";
    public const string AuthLoginFailed = "Auth.LoginFailed";
    public const string AuthLoggedOut = "Auth.LoggedOut";
}