using BCrypt.Net;
using Ecoeex_Academy_Api.Services;
using Ecoeex_Academy_Api.Data;
using Ecoeex_Academy_Api.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Mail;
namespace Ecoeex_Academy_Api.Services
{
    public class Email_Services : IEmail_Services
    {

        public AppDbContext _context { get; set; }
        public Email_Services(AppDbContext db)
        {
            _context = db;
        }

        public class Response
        {
            public bool Success { get; set; }
            public string Message { get; set; } = "";
        }


        private readonly string brevokey = "xsmtpsib-647c52389c8d0102ec471fa36e5027450a5f6fbaf1f665709b1494c04c43aae4-nq3qXtUwop04JqDY";
        public async Task<Response> SendOtpAsync(string email, string purpose, string targetType)
        {
            try
            {
                string otp = new Random()
                    .Next(100000, 999999)
                    .ToString();


                var otpEntry = new OtpRequest
                {
                    TargetType = targetType,
                    TargetValue = email,
                    OtpCodeHash = BCrypt.Net.BCrypt.HashPassword(otp),
                    Purpose = purpose,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                    CreatedAt = DateTime.UtcNow
                };

                _context.tb_OtpRequests.Add(otpEntry);

                await _context.SaveChangesAsync();


                // 📧 Brevo SMTP Config (same as your leave API)
                var smtp = new SmtpClient
                {
                    Host = "smtp-relay.brevo.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(
                        "info@ecoex.market",
                       brevokey
                    )
                };

                var fromAddress =
                    new MailAddress(
                        "info@ecoex.market",
                        "Ecoex Academy"
                    );


                var toAddress =
                    new MailAddress(email);


                string htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{
            font-family: Arial, Helvetica, sans-serif;
            background-color: #f5f7fb;
            margin: 0;
            padding: 20px;
        }}

        .container {{
            max-width: 600px;
            margin: auto;
            background: #ffffff;
            border-radius: 12px;
            padding: 30px;
            box-shadow: 0 5px 20px rgba(0,0,0,0.08);
        }}

        .header {{
            text-align: center;
            color: #198754;
        }}

        .logo {{
            font-size: 28px;
            font-weight: bold;
        }}

        .otp-box {{
            background: #f0fdf4;
            border: 2px dashed #198754;
            padding: 20px;
            text-align: center;
            border-radius: 10px;
            margin: 25px 0;
        }}

        .otp {{
            font-size: 36px;
            font-weight: bold;
            letter-spacing: 8px;
            color: #198754;
        }}

        .footer {{
            margin-top: 25px;
            font-size: 13px;
            color: #777;
            text-align: center;
        }}

        p {{
            color: #444;
            font-size: 15px;
        }}
    </style>
</head>

<body>

<div class='container'>

    <div class='header'>
        <div class='logo'>Ecoex Academy</div>
        <p>Learning | Sustainability | Future Skills</p>
    </div>


    <hr>


    <h3>Hello,</h3>

    <p>
        We received a request to verify your email address
        for Ecoex Academy.
    </p>


    <div class='otp-box'>

        <p>Your One-Time Password (OTP) is</p>

        <div class='otp'>
            {otp}
        </div>

        <p>
            This OTP is valid for <b>5 minutes</b>.
        </p>

    </div>


    <p>
        Please do not share this OTP with anyone.
        Our team will never ask for your verification code.
    </p>


    <p>
        If you did not request this verification,
        you can safely ignore this email.
    </p>


    <div class='footer'>

        © {DateTime.Now.Year} Ecoex Academy<br>

        Empowering professionals with knowledge and skills.

    </div>


</div>

</body>
</html>";


                using (var message = new MailMessage(
                    fromAddress,
                    toAddress))
                {
                    message.Subject =
                        "Your OTP Code - Ecoex Academy";
                    message.Body = htmlBody;
                    message.IsBodyHtml = true;
                    await smtp.SendMailAsync(message);

                }



                return new Response
                {
                    Success = true,
                    Message = "OTP sent successfully."
                };

            }
            catch (Exception ex)
            {
                return new Response
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }
        public async Task<Response> VerifyOtpAsync(string email, string otp, string purpose)
        {
            try
            {

                var request =
                    await _context.tb_OtpRequests
                    .Where(x =>
                        x.TargetValue == email &&
                        x.Purpose == purpose &&
                        x.ConsumedAt == null
                    )
                    .OrderByDescending(x => x.CreatedAt)
                    .FirstOrDefaultAsync();

                if (request == null)
                {
                    return new Response
                    {
                        Success = false,
                        Message = "OTP not found."
                    };
                }



                if (request.ExpiresAt < DateTime.UtcNow)
                {
                    return new Response
                    {
                        Success = false,
                        Message = "OTP expired."
                    };
                }



                bool valid =
                    BCrypt.Net.BCrypt.Verify(
                        otp,
                        request.OtpCodeHash);



                if (!valid)
                {
                    return new Response
                    {
                        Success = false,
                        Message = "Invalid OTP."
                    };
                }



                request.ConsumedAt =
                    DateTime.UtcNow;


                await _context.SaveChangesAsync();



                return new Response
                {
                    Success = true,
                    Message = "OTP verified successfully."
                };

            }
            catch (Exception ex)
            {
                return new Response
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }


        public async Task<Response> SendRegistrationSuccessEmailAsync(string email, string name)
        {
            try
            {
                var smtp = new SmtpClient
                {
                    Host = "smtp-relay.brevo.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(
                        "info@ecoex.market",
                        brevokey
                        )
                };


                var fromAddress = new MailAddress(
                    "info@ecoex.market",
                    "Ecoex Academy"
                );


                var toAddress = new MailAddress(email);


                string htmlBody = $@"
<!DOCTYPE html>
<html>

<head>
<style>

body {{
    font-family: Arial, sans-serif;
    background:#f8f9fa;
}}

.container {{
    max-width:600px;
    margin:auto;
    background:white;
    padding:35px;
    border-radius:15px;
}}

.header {{
    text-align:center;
    color:#198754;
}}

.logo {{
    font-size:30px;
    font-weight:bold;
}}

.success-box {{
    background:#f0fff4;
    border:2px solid #198754;
    padding:25px;
    text-align:center;
    border-radius:12px;
    margin:25px 0;
}}

.title {{
    color:#198754;
    font-size:24px;
    font-weight:bold;
}}

.footer {{
    margin-top:30px;
    text-align:center;
    color:#777;
    font-size:13px;
}}

p {{
    color:#444;
    font-size:15px;
}}

</style>
</head>


<body>

<div class='container'>


<div class='header'>

<div class='logo'>
Ecoex Academy
</div>

<p>
Learning | Sustainability | Future Skills
</p>

</div>


<hr>


<h3>Hello {name},</h3>


<p>
Welcome to <b>Ecoex Academy</b>.
Your registration has been completed successfully.
</p>


<div class='success-box'>

<div class='title'>
🎉 Registration Successful
</div>

<p>
Your account has been created successfully.
</p>

<p>
You can now access courses and start your learning journey.
</p>

</div>


<p>
Thank you for joining Ecoex Academy.
We are excited to have you with us.
</p>


<p>
If you need any assistance, feel free to contact our support team.
</p>


<div class='footer'>

© {DateTime.Now.Year} Ecoex Academy<br>

Empowering professionals with knowledge and skills.

</div>


</div>


</body>

</html>";


                using (var message = new MailMessage(
                    fromAddress,
                    toAddress))
                {
                    message.Subject =
                        "Registration Successful - Ecoex Academy";

                    message.Body = htmlBody;

                    message.IsBodyHtml = true;


                    await smtp.SendMailAsync(message);
                }


                return new Response
                {
                    Success = true,
                    Message = "Registration email sent successfully."
                };

            }
            catch (Exception ex)
            {
                return new Response
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }
        public async Task<Response> SendPaymentSuccessEmailAsync(int PaymentId)
        {
            try
            {
                // ==========================================
                // GET PAYMENT DETAILS
                // ==========================================

                var payment = await _context.tb_Payments
                    .Include(x => x.Order)
                        .ThenInclude(x => x.PayerUser)
                    .FirstOrDefaultAsync(x => x.PaymentId == PaymentId);


                if (payment == null)
                {
                    return new Response
                    {
                        Success = false,
                        Message = "Payment not found."
                    };
                }


                // ==========================================
                // GET USER
                // ==========================================

                var user = payment.Order.PayerUser;


                if (user == null)
                {
                    return new Response
                    {
                        Success = false,
                        Message = "User not found."
                    };
                }


                // ==========================================
                // SMTP CONFIGURATION
                // ==========================================

                var smtp = new SmtpClient
                {
                    Host = "smtp-relay.brevo.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(
                        "info@ecoex.market",
                        brevokey
                    )
                };


                var fromAddress = new MailAddress(
                    "info@ecoex.market",
                    "Ecoex Academy"
                );


                var toAddress = new MailAddress(
                    user.Email,
                    user.Name
                );


                // ==========================================
                // PAYMENT DETAILS
                // ==========================================

                string paymentDate =
                    payment.SubmittedAt.ToLocalTime()
                        .ToString("dd MMM yyyy, hh:mm tt");


                string amount =
                    payment.Order.TotalAmount
                        .ToString("N2");


                string orderId =
                    payment.Order.OrderId.ToString();


                // ==========================================
                // EMAIL HTML
                // ==========================================

                string htmlBody = $@"
<!DOCTYPE html>

<html>

<head>

    <meta charset='UTF-8'>

    <style>

        body {{
            font-family: Arial, sans-serif;
            background: #f8f9fa;
            margin: 0;
            padding: 20px;
        }}

        .container {{
            max-width: 600px;
            margin: auto;
            background: #ffffff;
            padding: 35px;
            border-radius: 15px;
            box-shadow: 0 5px 20px rgba(0,0,0,0.08);
        }}

        .header {{
            text-align: center;
            color: #198754;
        }}

        .logo {{
            font-size: 30px;
            font-weight: bold;
        }}

        .success-box {{
            background: #f0fff4;
            border: 2px solid #198754;
            padding: 25px;
            text-align: center;
            border-radius: 12px;
            margin: 25px 0;
        }}

        .title {{
            color: #198754;
            font-size: 24px;
            font-weight: bold;
        }}

        .details {{
            background: #f8f9fa;
            padding: 20px;
            border-radius: 10px;
            margin-top: 20px;
        }}

        .row {{
            display: flex;
            justify-content: space-between;
            padding: 8px 0;
            border-bottom: 1px solid #eeeeee;
        }}

        .row:last-child {{
            border-bottom: none;
        }}

        .label {{
            color: #777777;
        }}

        .value {{
            font-weight: bold;
            color: #333333;
        }}

        .notice {{
            background: #fff8e1;
            border-left: 4px solid #ffc107;
            padding: 15px;
            margin-top: 25px;
            color: #555555;
        }}

        .footer {{
            margin-top: 30px;
            text-align: center;
            color: #777777;
            font-size: 13px;
        }}

        p {{
            color: #444444;
            font-size: 15px;
            line-height: 1.6;
        }}

    </style>

</head>


<body>

<div class='container'>

    <div class='header'>

        <div class='logo'>
            Ecoex Academy
        </div>

        <p>
            Learning | Sustainability | Future Skills
        </p>

    </div>


    <hr>


    <h3>
        Hello {user.Name},
    </h3>


    <div class='success-box'>

        <div class='title'>
            Payment Submitted Successfully
        </div>

        <p>
            We have received your payment details
            and UTR successfully.
        </p>

    </div>


    <p>
        Thank you for registering with
        <b>Ecoex Academy</b>.
    </p>


    <p>
        Your payment is currently under
        <b>manual verification</b>.
        Our team will verify your payment details
        and notify you within <b>48 hours</b>.
    </p>


    <div class='details'>

        <div class='row'>

            <span class='label'>
                Order ID
            </span>

            <span class='value'>
                #{orderId}
            </span>

        </div>


        <div class='row'>

            <span class='label'>
                Amount
            </span>

            <span class='value'>
                ₹{amount}
            </span>

        </div>


        <div class='row'>

            <span class='label'>
                UTR / Transaction ID
            </span>

            <span class='value'>
                {payment.Utr}
            </span>

        </div>


        <div class='row'>

            <span class='label'>
                Submitted On
            </span>

            <span class='value'>
                {paymentDate}
            </span>

        </div>


        <div class='row'>

            <span class='label'>
                Status
            </span>

            <span class='value'>
                Pending Verification
            </span>

        </div>

    </div>


    <div class='notice'>

        <b>What happens next?</b>

        <p style='margin-bottom:0;'>

            Our team will manually verify your payment.
            You will receive an email notification once
            the verification is completed.

        </p>

    </div>


    <p>

        Please keep your transaction details safely
        until the payment verification is completed.

    </p>


    <div class='footer'>

        © {DateTime.Now.Year} Ecoex Academy
        <br>

        Empowering professionals with knowledge and skills.

    </div>

</div>

</body>

</html>
";


                // ==========================================
                // SEND EMAIL
                // ==========================================

                using (var message = new MailMessage(
                    fromAddress,
                    toAddress))
                {
                    message.Subject =
                        $"Payment Submitted - Order #{orderId} | Ecoex Academy";

                    message.Body = htmlBody;

                    message.IsBodyHtml = true;

                    await smtp.SendMailAsync(message);
                }


                return new Response
                {
                    Success = true,
                    Message = "Payment submission email sent successfully."
                };
            }
            catch (Exception ex)
            {
                return new Response
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }










    }




}
