namespace Ecoex_Academy_Api.Services
{
    public interface ICertificateServices
    {
        Task SendCertificatesAsync(int courseId, CancellationToken cancellationToken);
    }
}
