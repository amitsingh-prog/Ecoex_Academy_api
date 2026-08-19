using Microsoft.AspNetCore.Mvc;
using static Ecoeex_Academy_Api.Services.Email_Services;

namespace Ecoeex_Academy_Api.Services
{
    public interface IEmail_Services
    {
        public Task<Response> SendOtpAsync(  string email,    string purpose,   string targetType);
        public Task<Response> VerifyOtpAsync(  string email,   string otp,   string purpose);

        public Task<Response> SendRegistrationSuccessEmailAsync(  string email,  string name);

        Task<Response> SendPaymentSuccessEmailAsync(int PaymentId);
        Task<Response> SendPaymentApprovedEmailAsync(int PaymentId);
        Task<Response> SendPaymentRejectEmailAsync(int PaymentId);



    }
}
