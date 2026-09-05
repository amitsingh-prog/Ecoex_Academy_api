using BCrypt.Net;
using Ecoeex_Academy_Api.Data;
using Ecoeex_Academy_Api.Model;
using Ecoeex_Academy_Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Drawing;
using System.Net;
using System.Net.Mail;
using static System.Net.Mime.MediaTypeNames;


namespace Ecoeex_Academy_Api.Services
{
    public class Email_Services : IEmail_Services
    {

        public AppDbContext _context { get; set; }
        private readonly IWebHostEnvironment _environment;
        public Email_Services(AppDbContext db, IWebHostEnvironment environment)
        {
            _context = db;
            _environment = environment;
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




        public async Task<Response> SendPaymentApprovedEmailAsync(int PaymentId)
        {
            try
            {
                string logoPath = Path.Combine(
    _environment.WebRootPath,
    "images",
    "ecoex-logo.png"
);
                // ==========================================
                // GET PAYMENT DETAILS
                // ==========================================

                var payment = await _context.tb_Payments
                    .Include(x => x.Order)
                        .ThenInclude(x => x.PayerUser)
                    .FirstOrDefaultAsync(x => x.PaymentId == PaymentId);

                if (payment == null)
                {
                    return new Response { Success = false, Message = "Payment not found." };
                }

                var user = payment.Order.PayerUser;

                if (user == null)
                {
                    return new Response { Success = false, Message = "User not found." };
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
                    Credentials = new NetworkCredential("info@ecoex.market", brevokey)
                };

                var fromAddress = new MailAddress("info@ecoex.market", "Ecoex Academy");
                var toAddress = new MailAddress(user.Email, user.Name);

                // ==========================================
                // PAYMENT DETAILS
                // ==========================================

                string paymentDate = payment.SubmittedAt.ToLocalTime().ToString("dd MMM yyyy, hh:mm tt");
                string amount = payment.Order.TotalAmount.ToString("N2");
                string orderId = payment.Order.OrderId.ToString();
                string paymentIdStr = payment.PaymentId.ToString();
                string utr = payment.Utr;

                // ==========================================
                // GENERATE RECEIPT PDF
                // ==========================================

                byte[] receiptPdfBytes = GenerateReceiptPdf(
        payerName: user.Name,
        payerEmail: user.Email,
        orderId: orderId,
        paymentId: paymentIdStr,
        utr: utr,
        submittedOn: paymentDate,
        amount: amount,
        logoPath: logoPath
    );

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

        .icon {{
            font-size: 45px;
            margin-bottom: 10px;
        }}

        /* PAYMENT DETAILS */

        .details {{
            background: #f8f9fa;
            padding: 20px 25px;
            border-radius: 10px;
            margin-top: 20px;
        }}

        .details-table {{
            width: 100%;
            border-collapse: collapse;
            table-layout: fixed;
        }}

        .details-table td {{
            padding: 13px 0;
            border-bottom: 1px solid #eeeeee;
            vertical-align: middle;
        }}

        .details-table .label {{
            width: 40%;
            color: #777777;
            text-align: left;
            padding-right: 25px;
            font-size: 14px;
        }}

        .details-table .value {{
            width: 60%;
            font-weight: bold;
            color: #333333;
            text-align: right;
            padding-left: 20px;
            font-size: 14px;
            word-break: break-word;
        }}

        .details-table tr:last-child td {{
            border-bottom: none;
        }}

        .approved {{
            color: #198754 !important;
        }}

        /* LOGO */

        .logo-space {{
            text-align: center;
            padding: 5px 0 20px 0;
        }}

        .logo-space img {{
            width: 120px;
            height: auto;
            display: inline-block;
        }}

        /* NOTICE */

        .notice {{
            background: #e8f5e9;
            border-left: 4px solid #198754;
            padding: 15px;
            margin-top: 25px;
            color: #444444;
        }}

        /* NEXT STEP */

        .next-step {{
            background: #f8f9fa;
            padding: 20px;
            border-radius: 10px;
            margin-top: 20px;
        }}

        /* FOOTER */

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

    <!-- HEADER -->

    <div class='header'>

        <div class='logo'>
            Ecoex Academy
        </div>

        <p>
            Learning | Sustainability | Future Skills
        </p>

    </div>

    <hr>

    <!-- GREETING -->

    <h3>
        Hello {user.Name},
    </h3>


    <!-- SUCCESS BOX -->

    <div class='success-box'>

        <div class='icon'>
            ✓
        </div>

        <div class='title'>
            Payment Approved Successfully
        </div>

        <p>
            Your payment has been successfully verified and approved by our team.
        </p>

    </div>


    <!-- MESSAGE -->

    <p>
        Congratulations! 🎉
    </p>

    <p>
        Your payment for <b>Ecoex Academy</b> has been verified successfully.
        Your registration is now confirmed.
        A copy of your receipt is attached to this email.
    </p>


    <!-- PAYMENT DETAILS -->

    <div class='details'>
 

        <!-- DETAILS TABLE -->

        <table class='details-table'>

            <tr>

                <td class='label'>
                    Payer Name
                </td>

                <td class='value'>
                    {user.Name}
                </td>

            </tr>


            <tr>

                <td class='label'>
                    Payer Email
                </td>

                <td class='value'>
                    {user.Email}
                </td>

            </tr>


            <tr>

                <td class='label'>
                    Order ID
                </td>

                <td class='value'>
                    #{orderId}
                </td>

            </tr>


            <tr>

                <td class='label'>
                    Payment ID
                </td>

                <td class='value'>
                    #{paymentIdStr}
                </td>

            </tr>


            <tr>

                <td class='label'>
                    UTR / Transaction ID
                </td>

                <td class='value'>
                    {utr}
                </td>

            </tr>


            <tr>

                <td class='label'>
                    Payment Date
                </td>

                <td class='value'>
                    {paymentDate}
                </td>

            </tr>


            <tr>

                <td class='label'>
                    Amount Paid
                </td>

                <td class='value'>
                    ₹{amount}
                </td>

            </tr>


            <tr>

                <td class='label'>
                    Status
                </td>

                <td class='value approved'>
                    Payment Approved
                </td>

            </tr>

        </table>

    </div>


    <!-- PAYMENT VERIFIED -->

    <div class='notice'>

        <b>
            ✓ Payment Verified
        </b>

        <p style='margin-bottom:0;'>

            Your payment has been reviewed and verified by the
            Ecoex Academy team.
            Your registration is successfully confirmed.

        </p>

    </div>


    <!-- NEXT STEPS -->

    <div class='next-step'>

        <b>
            What's next?
        </b>

        <p>

            You can now access your registered course and continue
            your learning journey with Ecoex Academy.

        </p>

        <p style='margin-bottom:0;'>

            If your course includes live sessions or additional resources,
            the relevant details will be shared with you separately.

        </p>

    </div>


    <!-- CLOSING -->

    <p>

        Thank you for choosing <b>Ecoex Academy</b>.
        We look forward to being part of your learning journey.

    </p>


    <!-- FOOTER -->

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
                // SEND EMAIL WITH ATTACHMENT
                // ==========================================

                using (var message = new MailMessage(fromAddress, toAddress))
                using (var pdfStream = new MemoryStream(receiptPdfBytes))
                {
                    message.Subject = $"Payment Approved - Order #{orderId} | Ecoex Academy";
                    message.Body = htmlBody;
                    message.IsBodyHtml = true;

                    var attachment = new Attachment(pdfStream, $"Receipt_Payment_{paymentIdStr}.pdf", "application/pdf");
                    message.Attachments.Add(attachment);

                    await smtp.SendMailAsync(message);
                }

                return new Response { Success = true, Message = "Payment approval email sent successfully." };
            }
            catch (Exception ex)
            {
                return new Response { Success = false, Message = ex.Message };
            }
        }

        // ==========================================
        // PDF RECEIPT GENERATOR (QuestPDF)
        // ==========================================
        private byte[] GenerateReceiptPdf(
     string payerName,
     string payerEmail,
     string orderId,
     string paymentId,
     string utr,
     string submittedOn,
     string amount,
     string logoPath)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);

                    page.DefaultTextStyle(x =>
                        x.FontSize(11)
                         .FontColor(Colors.Grey.Darken3));

                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            // ECOEX LOGO
                            row.RelativeItem()
                                .Height(45)
                                .Image(logoPath)
                                .FitHeight();

                            // PAYMENT RECEIPT
                            row.RelativeItem()
                                .AlignRight()
                                .AlignMiddle()
                                .Text("PAYMENT RECEIPT")
                                .FontSize(18)
                                .Bold();
                        });

                        col.Item()
                            .PaddingTop(2)
                            .Text("EcoEx Academy  |  academy.ecoex.market")
                            .FontSize(9)
                            .FontColor(Colors.Grey.Medium);

                        col.Item()
                            .Text("Karma Ecotech Limited, operating as EcoEx")
                            .FontSize(9)
                            .FontColor(Colors.Grey.Medium);

                        col.Item()
                            .PaddingTop(10)
                            .LineHorizontal(0.5f)
                            .LineColor(Colors.Grey.Lighten1);
                    });

                    page.Content()
                        .PaddingTop(20)
                        .Column(col =>
                        {
                            col.Item()
                                .Text($"Receipt for Payment #{paymentId}")
                                .Bold()
                                .FontSize(13);

                            col.Item().PaddingTop(10);

                            void Row(string label, string value)
                            {
                                col.Item()
                                    .PaddingVertical(6)
                                    .Row(row =>
                                    {
                                        row.RelativeItem()
                                            .Text(label)
                                            .FontColor(Colors.Grey.Medium);

                                        row.RelativeItem()
                                            .AlignRight()
                                            .Text(value)
                                            .Bold();
                                    });

                                col.Item()
                                    .LineHorizontal(0.5f)
                                    .LineColor(Colors.Grey.Lighten2);
                            }

                            Row("Payer Name", payerName);
                            Row("Payer Email", payerEmail);
                            Row("Order ID", $"#{orderId}");
                            Row("Payment ID", $"#{paymentId}");
                            Row("UTR / Transaction Reference", utr);
                            Row("Submitted On", submittedOn);

                            col.Item()
                                .PaddingTop(20)
                                .Background(Colors.Green.Lighten4)
                                .Padding(15)
                                .Row(row =>
                                {
                                    row.RelativeItem()
                                        .Text("Amount Paid (incl. GST)")
                                        .FontColor(Colors.Grey.Darken1);

                                    row.RelativeItem()
                                        .AlignRight()
                                        .Text($"Rs. {amount}")
                                        .FontSize(18)
                                        .Bold()
                                        .FontColor(Colors.Green.Darken2);
                                });
                        });

                    page.Footer()
                        .PaddingTop(20)
                        .Column(col =>
                        {
                            col.Item()
                                .LineHorizontal(0.5f)
                                .LineColor(Colors.Grey.Lighten1);

                            col.Item()
                                .PaddingTop(8)
                                .AlignCenter()
                                .Text("This is a system-generated receipt and does not require a signature.")
                                .FontSize(8)
                                .FontColor(Colors.Grey.Medium);

                            col.Item()
                                .AlignCenter()
                                .Text("For questions about this payment, contact support@ecoex.market")
                                .FontSize(8)
                                .FontColor(Colors.Grey.Medium);

                            col.Item()
                                .AlignCenter()
                                .Text("Karma Ecotech Limited (operating as EcoEx) — academy.ecoex.market")
                                .FontSize(8)
                                .FontColor(Colors.Grey.Medium);
                        });
                });
            });

            return document.GeneratePdf();
        }

        public async Task<Response> SendPaymentRejectEmailAsync(int PaymentId)
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
            color: #dc3545;
        }}

        .logo {{
            font-size: 30px;
            font-weight: bold;
        }}

        .reject-box {{
            background: #fff5f5;
            border: 2px solid #dc3545;
            padding: 25px;
            text-align: center;
            border-radius: 12px;
            margin: 25px 0;
        }}

        .title {{
            color: #dc3545;
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
            padding: 10px 0;
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

    <div class='reject-box'>

        <div class='title'>
            Payment Verification Unsuccessful
        </div>

        <p>
            Unfortunately, we were unable to verify your
            payment with the transaction details submitted.
        </p>

    </div>

    <p>
        Thank you for registering with
        <b>Ecoex Academy</b>.
    </p>

    <p>
        We have reviewed the payment details submitted
        for your registration. Unfortunately, the payment
        could not be approved at this time.
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

            <span class='value' style='color:#dc3545;'>
                Payment Rejected
            </span>

        </div>

    </div>

    <div class='notice'>

        <b>What should you do next?</b>

        <p style='margin-bottom:0;'>

            Please check your transaction details and make sure
            that the payment was successfully completed.
            If you believe this rejection was made in error,
            please contact the Ecoex Academy support team
            with your Order ID and transaction details.

        </p>

    </div>

    <p>

        If you have not completed the payment successfully,
        please make the payment again and submit the correct
        transaction details.

    </p>

    <p>

        We are happy to help if you have any questions
        regarding your payment or registration.

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
             $"Payment Verification Failed - Order #{orderId} | Ecoex Academy";

                    message.Body = htmlBody;

                    message.IsBodyHtml = true;

                    await smtp.SendMailAsync(message);
                }

                return new Response
                {
                    Success = true,
                    Message = "Payment rejection email sent successfully."
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


        public async Task<Response> SendZoomLinkEmail(
          int userId,
          string zoomLink,
          string courseName,
          DateTime startDateTime,
          DateTime? endDateTime)
        {
            try
            {
                var user = await _context.tb_Users
                    .FirstOrDefaultAsync(x => x.UserId == userId);

                if (user == null)
                {
                    return new Response
                    {
                        Success = false,
                        Message = "User not found."
                    };
                }

                if (string.IsNullOrWhiteSpace(user.Email))
                {
                    return new Response
                    {
                        Success = false,
                        Message = "User email address is not available."
                    };
                }

                if (string.IsNullOrWhiteSpace(zoomLink))
                {
                    return new Response
                    {
                        Success = false,
                        Message = "Zoom link is not available."
                    };
                }

                // ---------------------------------------------------------
                // Format date and time
                // ---------------------------------------------------------

                string formattedDate =
                    startDateTime.ToLocalTime().ToString("dddd, dd MMMM yyyy");

                string formattedStartTime =
                   startDateTime.ToString("hh:mm tt");

                string formattedTime;

                formattedTime = formattedStartTime;


                // ---------------------------------------------------------
                // SMTP Configuration
                // ---------------------------------------------------------

                using var smtp = new SmtpClient
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

                // ---------------------------------------------------------
                // Email HTML
                // ---------------------------------------------------------

                var htmlBody = $@"

<!DOCTYPE html>
<html lang='en'>

<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <meta http-equiv='X-UA-Compatible' content='IE=edge'>

    <title>Ecoex Academy - Session Details</title>
</head>

<body style='
    margin:0;
    padding:0;
    background-color:#f3f5f7;
    font-family:Arial, Helvetica, sans-serif;
    color:#202124;
'>

<!-- Outer Wrapper -->
<table width='100%'
       cellpadding='0'
       cellspacing='0'
       border='0'
       role='presentation'
       style='
           width:100%;
           background-color:#f3f5f7;
           padding:40px 15px;
       '>

    <tr>
        <td align='center'>

            <!-- Main Email Container -->
            <table width='620'
                   cellpadding='0'
                   cellspacing='0'
                   border='0'
                   role='presentation'
                   style='
                       width:100%;
                       max-width:620px;
                       background-color:#ffffff;
                       border-radius:12px;
                       overflow:hidden;
                       box-shadow:0 3px 12px rgba(0,0,0,0.08);
                   '>


                <!-- ================================================= -->
                <!-- HEADER -->
                <!-- ================================================= -->

                <tr>
                    <td style='
                        padding:28px 40px;
                        background-color:#ffffff;
                        border-bottom:1px solid #e8eaed;
                    '>

                        <table width='100%'
                               cellpadding='0'
                               cellspacing='0'
                               border='0'
                               role='presentation'>

                            <tr>

                                <td align='left'>

                                    <div style='
                                        font-size:27px;
                                        font-weight:700;
                                        letter-spacing:1px;
                                        color:#176b3a;
                                    '>
                                        ECOEX ACADEMY
                                    </div>
 

                                </td>

                                <td align='right'
                                    valign='middle'>

                                 

                                </td>

                            </tr>

                        </table>

                    </td>
                </tr>


                <!-- ================================================= -->
                <!-- HERO -->
                <!-- ================================================= -->

                <tr>
                    <td style='
                        padding:40px 40px 30px 40px;
                        background-color:#f7fbf8;
                    '>

                        <div style='
                            display:inline-block;
                            padding:7px 12px;
                            background-color:#e2f2e8;
                            color:#176b3a;
                            font-size:11px;
                            font-weight:700;
                            letter-spacing:0.8px;
                            border-radius:20px;
                        '>
                            SESSION CONFIRMED
                        </div>

                        <h1 style='
                            margin:18px 0 10px 0;
                            font-size:28px;
                            line-height:1.3;
                            font-weight:700;
                            color:#202124;
                        '>
                            Your Session Details
                        </h1>

                        <p style='
                            margin:0;
                            font-size:15px;
                            line-height:1.7;
                            color:#5f6368;
                        '>
                    Here are the details for your upcoming session. You can join the session using the Zoom link provided below.
                        </p>

                    </td>
                </tr>


                <!-- ================================================= -->
                <!-- GREETING -->
                <!-- ================================================= -->

                <tr>
                    <td style='
                        padding:32px 40px 10px 40px;
                    '>

                        <p style='
                            margin:0 0 12px 0;
                            font-size:16px;
                            line-height:1.6;
                            color:#202124;
                        '>
                            Dear
                            <strong>
                                {System.Net.WebUtility.HtmlEncode(user.Name)}
                            </strong>,
                        </p>

                        <p style='
                            margin:0;
                            font-size:15px;
                            line-height:1.7;
                            color:#5f6368;
                        '>
                            Thank you for registering with
                            <strong style='color:#333333;'>
                                Ecoex Academy
                            </strong>.
                            We are pleased to share the details of your
                            upcoming online learning session.
                        </p>

                    </td>
                </tr>


                <!-- ================================================= -->
                <!-- COURSE CARD -->
                <!-- ================================================= -->

                <tr>
                    <td style='
                        padding:25px 40px 10px 40px;
                    '>

                        <table width='100%'
                               cellpadding='0'
                               cellspacing='0'
                               border='0'
                               role='presentation'
                               style='
                                   border:1px solid #e1e6e3;
                                   border-radius:10px;
                                   background-color:#ffffff;
                               '>

                            <!-- Course -->
                            <tr>

                                <td style='
                                    padding:22px 24px;
                                    border-bottom:1px solid #edf0ee;
                                '>

                                    <div style='
                                        font-size:11px;
                                        font-weight:700;
                                        letter-spacing:1px;
                                        color:#7a817d;
                                        text-transform:uppercase;
                                    '>
                                        COURSE
                                    </div>

                                    <div style='
                                        margin-top:7px;
                                        font-size:18px;
                                        font-weight:700;
                                        line-height:1.4;
                                        color:#202124;
                                    '>
                                        {System.Net.WebUtility.HtmlEncode(courseName)}
                                    </div>

                                </td>

                            </tr>


                            <!-- Date -->
                            <tr>

                                <td style='
                                    padding:20px 24px;
                                    border-bottom:1px solid #edf0ee;
                                '>

                                    <table width='100%'
                                           cellpadding='0'
                                           cellspacing='0'
                                           border='0'
                                           role='presentation'>

                                        <tr>

                                            <td width='45'
                                                valign='top'>

                                                <div style='
                                                    width:34px;
                                                    height:34px;
                                                    line-height:34px;
                                                    text-align:center;
                                                    background-color:#e8f4ec;
                                                    border-radius:7px;
                                                    font-size:16px;
                                                '>
                                                    📅
                                                </div>

                                            </td>

                                            <td valign='middle'>

                                                <div style='
                                                    font-size:11px;
                                                    font-weight:700;
                                                    letter-spacing:0.8px;
                                                    color:#7a817d;
                                                '>
                                                    DATE
                                                </div>

                                                <div style='
                                                    margin-top:4px;
                                                    font-size:15px;
                                                    font-weight:600;
                                                    color:#202124;
                                                '>
                                                    {formattedDate}
                                                </div>

                                            </td>

                                        </tr>

                                    </table>

                                </td>

                            </tr>


                            <!-- Time -->
                            <tr>

                                <td style='
                                    padding:20px 24px;
                                '>

                                    <table width='100%'
                                           cellpadding='0'
                                           cellspacing='0'
                                           border='0'
                                           role='presentation'>

                                        <tr>

                                            <td width='45'
                                                valign='top'>

                                                <div style='
                                                    width:34px;
                                                    height:34px;
                                                    line-height:34px;
                                                    text-align:center;
                                                    background-color:#e8f4ec;
                                                    border-radius:7px;
                                                    font-size:16px;
                                                '>
                                                    🕐
                                                </div>

                                            </td>

                                            <td valign='middle'>

                                                <div style='
                                                    font-size:11px;
                                                    font-weight:700;
                                                    letter-spacing:0.8px;
                                                    color:#7a817d;
                                                '>
                                                    START TIME
                                                </div>

                                                <div style='
                                                    margin-top:4px;
                                                    font-size:15px;
                                                    font-weight:600;
                                                    color:#202124;
                                                '>
                                                    {formattedTime}
                                                </div>

                                            </td>

                                        </tr>

                                    </table>

                                </td>

                            </tr>

                        </table>

                    </td>
                </tr>


                <!-- ================================================= -->
                <!-- ZOOM CTA -->
                <!-- ================================================= -->

                <tr>
                    <td style='
                        padding:30px 40px;
                    '>

                        <table width='100%'
                               cellpadding='0'
                               cellspacing='0'
                               border='0'
                               role='presentation'
                               style='
                                   background-color:#176b3a;
                                   border-radius:10px;
                               '>

                            <tr>

                                <td align='center'
                                    style='padding:30px 25px;'>

                                    <div style='
                                        font-size:19px;
                                        font-weight:700;
                                        color:#ffffff;
                                    '>
                                        Ready to Join?
                                    </div>

                                    <p style='
                                        margin:9px 0 22px 0;
                                        font-size:14px;
                                        line-height:1.6;
                                        color:#dcefe3;
                                    '>
                                        Join the session using the secure
                                        Zoom meeting link below.
                                    </p>

                                    <!-- Button -->

                                    <table cellpadding='0'
                                           cellspacing='0'
                                           border='0'
                                           role='presentation'>

                                        <tr>

                                            <td align='center'
                                                style='
                                                    background-color:#ffffff;
                                                    border-radius:6px;
                                                '>

                                                <a href='{System.Net.WebUtility.HtmlEncode(zoomLink)}'
                                                   style='
                                                       display:inline-block;
                                                       padding:14px 30px;
                                                       color:#176b3a;
                                                       text-decoration:none;
                                                       font-size:15px;
                                                       font-weight:700;
                                                       letter-spacing:0.2px;
                                                   '>
                                                    JOIN SESSION
                                                </a>

                                            </td>

                                        </tr>

                                    </table>

                                </td>

                            </tr>

                        </table>

                    </td>
                </tr>


                <!-- ================================================= -->
                <!-- JOINING INSTRUCTIONS -->
                <!-- ================================================= -->

                <tr>
                    <td style='
                        padding:0 40px 30px 40px;
                    '>

                        <table width='100%'
                               cellpadding='0'
                               cellspacing='0'
                               border='0'
                               role='presentation'
                               style='
                                   background-color:#f8f9fa;
                                   border-radius:8px;
                               '>

                            <tr>

                                <td style='
                                    padding:22px 24px;
                                '>

                                    <div style='
                                        font-size:14px;
                                        font-weight:700;
                                        color:#202124;
                                        margin-bottom:12px;
                                    '>
                                        Before You Join
                                    </div>

                                    <div style='
                                        font-size:13px;
                                        line-height:1.8;
                                        color:#5f6368;
                                    '>

                                        • Please join the session
                                        <strong>5–10 minutes early</strong>.<br>

                                        • Ensure you have a stable
                                        internet connection.<br>

                                        • Please use your registered name
                                        when joining the session.<br>

                                        • Keep your microphone muted unless
                                        you are asked to speak.

                                    </div>

                                </td>

                            </tr>

                        </table>

                    </td>
                </tr>




                <!-- ================================================= -->
                <!-- CLOSING -->
                <!-- ================================================= -->

                <tr>
                    <td style='
                        padding:5px 40px 35px 40px;
                    '>

                        <p style='
                            margin:0;
                            font-size:14px;
                            line-height:1.7;
                            color:#5f6368;
                        '>
                            We look forward to having you with us.
                        </p>

                        <p style='
                            margin:18px 0 0 0;
                            font-size:14px;
                            line-height:1.6;
                            color:#202124;
                        '>
                            Best regards,<br>
                            <strong>Ecoex Academy Team</strong>
                        </p>

                    </td>
                </tr>


                <!-- ================================================= -->
                <!-- FOOTER -->
                <!-- ================================================= -->

                <tr>

                    <td align='center'
                        style='
                            padding:28px 35px;
                            background-color:#f5f6f5;
                            border-top:1px solid #e6e8e6;
                        '>

                        <div style='
                            font-size:14px;
                            font-weight:700;
                            color:#333333;
                        '>
                            ECOEX ACADEMY
                        </div>

                        <div style='
                            margin-top:7px;
                            font-size:12px;
                            color:#777777;
                            line-height:1.6;
                        '>
                            Sustainability • Circular Economy • ESG
                        </div>

                        <div style='
                            margin-top:15px;
                            font-size:11px;
                            line-height:1.6;
                            color:#999999;
                        '>
                            This is an automated communication from
                            Ecoex Academy. Please do not reply to this email.
                        </div>

                        <div style='
                            margin-top:10px;
                            font-size:11px;
                            color:#aaaaaa;
                        '>
                            © {DateTime.Now.Year} Ecoex Academy.
                            All rights reserved.
                        </div>

                    </td>

                </tr>

            </table>

        </td>
    </tr>

</table>
</body>
</html>";



                // ---------------------------------------------------------
                // Create Email
                // ---------------------------------------------------------

                using var message = new MailMessage(
                    fromAddress,
                    toAddress
                );
                message.Subject = "Ecoex Academy | Upcoming Session Details";

                message.Body = htmlBody;

                message.IsBodyHtml = true;

                // Important email headers
                message.Headers.Add(
                    "X-Mailer",
                    "Ecoex Academy"
                );

                message.Headers.Add(
                    "X-Auto-Response-Suppress",
                    "All"
                );


                // ---------------------------------------------------------
                // Send Email
                // ---------------------------------------------------------

                await smtp.SendMailAsync(message);


                return new Response
                {
                    Success = true,
                    Message = "Zoom link email sent successfully."
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



        public async Task<Response> SendCertificateEmail(int userId, string certificateId, string courseName, string? certificateFilePath)
        {
            try
            {
                // -----------------------------------------
                // Get User
                // -----------------------------------------

                var user = await _context.tb_Users
                    .FirstOrDefaultAsync(x => x.UserId == userId);

                if (user == null)
                {
                    return new Response
                    {
                        Success = false,
                        Message = "User not found."
                    };
                }

                if (string.IsNullOrWhiteSpace(user.Email))
                {
                    return new Response
                    {
                        Success = false,
                        Message = "User email address is missing."
                    };
                }


                // -----------------------------------------
                // SMTP Configuration
                // -----------------------------------------

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


                // -----------------------------------------
                // Email Addresses
                // -----------------------------------------

                var fromAddress = new MailAddress(
                    "info@ecoex.market",
                    "Ecoex Academy"
                );

                var toAddress = new MailAddress(
                    user.Email,
                    user.Name
                );


                // -----------------------------------------
                // Certificate Link
                // -----------------------------------------

                string certificateSection = string.Empty;

                if (!string.IsNullOrWhiteSpace(certificateFilePath))
                {
                    certificateSection = $@"
                <div style='
                    margin:30px 0;
                    text-align:center;
                '>

                    <a href='{certificateFilePath}'
                       style='
                           display:inline-block;
                           padding:13px 28px;
                           background:#1a73e8;
                           color:#ffffff;
                           text-decoration:none;
                           border-radius:6px;
                           font-size:14px;
                           font-weight:600;
                       '>
                        View Certificate
                    </a>

                </div>";
                }


                // -----------------------------------------
                // Email Body
                // -----------------------------------------

                string htmlBody = $@"
<!DOCTYPE html>

<html>

<head>

    <meta charset='UTF-8' />

    <meta name='viewport'
          content='width=device-width, initial-scale=1.0' />

</head>

<body style='
    margin:0;
    padding:0;
    background:#f4f6f8;
    font-family:Arial,Helvetica,sans-serif;
'>

<table width='100%'
       cellpadding='0'
       cellspacing='0'
       border='0'
       style='background:#f4f6f8;padding:30px 10px;'>

<tr>

<td align='center'>

<table width='600'
       cellpadding='0'
       cellspacing='0'
       border='0'
       style='
           max-width:600px;
           width:100%;
           background:#ffffff;
           border-radius:10px;
           overflow:hidden;
       '>


<!-- HEADER -->

<tr>

<td style='
    background:#111827;
    padding:24px 30px;
    text-align:center;
'>

    <div style='
        color:#ffffff;
        font-size:24px;
        font-weight:700;
    '>
        Ecoex Academy
    </div>

    <div style='
        color:#d1d5db;
        font-size:13px;
        margin-top:5px;
    '>
        Learning • Knowledge • Growth
    </div>

</td>

</tr>


<!-- CONTENT -->

<tr>

<td style='padding:35px 35px 20px 35px;'>

    <p style='
        margin:0 0 18px 0;
        font-size:16px;
        color:#111827;
    '>
        Dear <strong>{user.Name}</strong>,
    </p>


    <p style='
        margin:0 0 18px 0;
        font-size:15px;
        line-height:1.7;
        color:#4b5563;
    '>

        Congratulations on successfully completing the
        <strong>{courseName}</strong> session with
        <strong>Ecoex Academy</strong>.

    </p>


    <p style='
        margin:0 0 25px 0;
        font-size:15px;
        line-height:1.7;
        color:#4b5563;
    '>

        Your certificate has been issued and is now available
        for your records.

    </p>


    <!-- CERTIFICATE DETAILS -->

    <table width='100%'
           cellpadding='0'
           cellspacing='0'
           border='0'
           style='
               background:#f8fafc;
               border:1px solid #e5e7eb;
               border-radius:8px;
           '>

        <tr>

            <td style='padding:18px 20px;'>

                <div style='
                    font-size:12px;
                    color:#6b7280;
                    margin-bottom:5px;
                '>
                    COURSE
                </div>

                <div style='
                    font-size:15px;
                    color:#111827;
                    font-weight:600;
                '>
                    {courseName}
                </div>

            </td>

        </tr>


        <tr>

            <td style='
                padding:0 20px 18px 20px;
            '>

                <div style='
                    font-size:12px;
                    color:#6b7280;
                    margin-bottom:5px;
                '>
                    CERTIFICATE ID
                </div>

                <div style='
                    font-size:14px;
                    color:#111827;
                    font-weight:600;
                '>
                    {certificateId}
                </div>

            </td>

        </tr>


        <tr>

            <td style='
                padding:0 20px 18px 20px;
            '>

                <div style='
                    font-size:12px;
                    color:#6b7280;
                    margin-bottom:5px;
                '>
                    ISSUE DATE
                </div>

                <div style='
                    font-size:14px;
                    color:#111827;
                    font-weight:600;
                '>
                    {DateTime.UtcNow:dd MMMM yyyy}
                </div>

            </td>

        </tr>

    </table>


    {certificateSection}


    <p style='
        margin:20px 0 0 0;
        font-size:14px;
        line-height:1.7;
        color:#6b7280;
    '>

        Please retain this certificate for your future reference.
        The Certificate ID can be used to verify the authenticity
        of your certificate.

    </p>


    <p style='
        margin:25px 0 0 0;
        font-size:15px;
        color:#374151;
    '>

        Best regards,<br />

        <strong>Ecoex Academy</strong><br />

        Ecoex Market

    </p>

</td>

</tr>


<!-- FOOTER -->

<tr>

<td style='
    background:#f8fafc;
    border-top:1px solid #e5e7eb;
    padding:22px 30px;
    text-align:center;
'>

    <p style='
        margin:0;
        font-size:12px;
        color:#9ca3af;
        line-height:1.6;
    '>

        This is an automated email from Ecoex Academy.
        Please do not reply directly to this email.

    </p>

    <p style='
        margin:8px 0 0 0;
        font-size:12px;
        color:#9ca3af;
    '>

        © {DateTime.UtcNow.Year} Ecoex Academy. All rights reserved.

    </p>

</td>

</tr>


</table>

</td>

</tr>

</table>

</body>

</html>
";


                // -----------------------------------------
                // Create Email
                // -----------------------------------------

                using var message = new MailMessage(
                    fromAddress,
                    toAddress
                );

                message.Subject =
                    "Certificate of Completion | Ecoex Academy";

                message.Body = htmlBody;

                message.IsBodyHtml = true;


                // -----------------------------------------
                // Send Email
                // -----------------------------------------

                await smtp.SendMailAsync(message);


                // -----------------------------------------
                // Success
                // -----------------------------------------

                return new Response
                {
                    Success = true,
                    Message = "Certificate email sent successfully."
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
