using ITQS.SupportOperationsCenter.Models.Administration.GdapAdminLinks;

namespace ITQS.SupportOperationsCenter.Services.Interfaces;

public interface IGdapMailSenderService
{
    Task SendAsync(GdapMailPreviewModel message);
}
