using Ecoex_Academy_Api.DTO;

namespace Ecoex_Academy_Api.Services
{
    public interface ICertificateServices
    {
        Task SendCertificatesAsync(int courseId, CancellationToken cancellationToken);
        Task SendCertificates_userAsync(int courseId, List<Get_Participants> obj_participants, CancellationToken cancellationToken);
    }
}
