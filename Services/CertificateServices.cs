using Ecoeex_Academy_Api.Data;
using Ecoeex_Academy_Api.Services;
using Ecoex_Academy_Api.Enums;
using Ecoex_Academy_Api.Model;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;

namespace Ecoex_Academy_Api.Services
{
    public class CertificateServices : ICertificateServices
    {

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<CertificateServices> _logger;
        public CertificateServices(
            IServiceScopeFactory scopeFactory,
            ILogger<CertificateServices> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task SendCertificatesAsync(
              int courseId,
              CancellationToken cancellationToken)
        {

            using IServiceScope scope =
                _scopeFactory.CreateScope();

            var context =
                scope.ServiceProvider
                    .GetRequiredService<AppDbContext>();

            var emailService =
                scope.ServiceProvider
                    .GetRequiredService<IEmail_Services>();

            try
            {

                // -------------------------------------------------
                // GET COURSES
                // -------------------------------------------------

                DateTime date = DateTime.UtcNow;




                var courses = await context.tb_Courses
                    .Where(x => x.CourseID == courseId)
                    .ToListAsync(cancellationToken);

                if (courses.Count == 0)
                {
                    _logger.LogInformation(
                        "No courses found."
                    );
                    return;
                }

                foreach (var course in courses)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    // -------------------------------------------------
                    // CHECK COURSE
                    // -------------------------------------------------

                    _logger.LogInformation(
                        "Processing Course ID: {CourseId}, Name: {CourseName}",
                        course.CourseID,
                        course.Name
                    );


                    // -------------------------------------------------
                    // GET SESSION PARTICIPANTS
                    // -------------------------------------------------

                    var participants =
                        await context.tb_SessionParticipant
                            .Where(x =>
                                x.CourseID == course.CourseID)
                            .ToListAsync(cancellationToken);

                    if (participants.Count == 0)
                    {
                        _logger.LogInformation(
                            "No participants found for Course ID: {CourseId}",
                            course.CourseID
                        );

                        continue;
                    }


                    int sent = 0;
                    int failed = 0;
                    int skipped = 0;


                    // -------------------------------------------------
                    // PROCESS EACH PARTICIPANT
                    // -------------------------------------------------

                    foreach (var sessionParticipant in participants)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            break;

                        try
                        {
                            // -----------------------------------------
                            // GET USER
                            // -----------------------------------------

                            var user =
                                await context.tb_Users
                                    .FirstOrDefaultAsync(
                                        x =>
                                            x.UserId ==
                                            sessionParticipant.UserID,
                                        cancellationToken
                                    );

                            if (user == null)
                            {
                                skipped++;

                                _logger.LogWarning(
                                    "User not found. UserId: {UserId}",
                                    sessionParticipant.UserID
                                );

                                continue;
                            }


                            // -----------------------------------------
                            // CHECK EXISTING CERTIFICATE
                            // -----------------------------------------

                            var certificate =
                                await context.tb_Certificate
                                    .FirstOrDefaultAsync(
                                        x =>
                                            x.ParticipantId ==
                                            sessionParticipant.Id
                                            &&
                                            x.CourseID ==
                                            course.CourseID,
                                        cancellationToken
                                    );


                            // -----------------------------------------
                            // ALREADY SENT
                            // -----------------------------------------

                            if (certificate != null &&
                                certificate.CertificateEmailStatus ==
                                CertificateEmailStatus.Sent)
                            {
                                skipped++;

                                _logger.LogInformation(
                                    "Certificate already sent. UserId: {UserId}, CourseId: {CourseId}",
                                    user.UserId,
                                    course.CourseID
                                );

                                continue;
                            }


                            // -----------------------------------------
                            // CREATE CERTIFICATE
                            // -----------------------------------------

                            if (certificate == null)
                            {
                                certificate =
                                    new Certificate
                                    {
                                        ParticipantId =
                                            sessionParticipant.Id,

                                        CourseID =
                                            course.CourseID,

                                        CertificateId =
                                            $"ECOEX-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"
                                                .ToUpper(),

                                        CertificateFilePath =
                                            null,

                                        IssuedAt =
                                            DateTime.UtcNow,

                                        CertificateEmailStatus =
                                            CertificateEmailStatus.Processing,

                                        CertificateEmailSentAt =
                                            null,

                                        CertificateEmailResponse =
                                            null,

                                        CreatedAt =
                                            DateTime.UtcNow,

                                        UpdatedAt =
                                            null
                                    };

                                context.tb_Certificate.Add(
                                    certificate
                                );
                            }
                            else
                            {
                                certificate.CertificateEmailStatus =
                                    CertificateEmailStatus.Processing;

                                certificate.CertificateEmailSentAt =
                                    null;

                                certificate.CertificateEmailResponse =
                                    null;

                                certificate.UpdatedAt =
                                    DateTime.UtcNow;
                            }


                            // -----------------------------------------
                            // SAVE BEFORE GENERATING
                            // -----------------------------------------

                            await context.SaveChangesAsync(
                                cancellationToken
                            );


                            // -----------------------------------------
                            // GENERATE CERTIFICATE
                            // -----------------------------------------

                            string certificatePath =
                                await GenerateCertificateTemplate(
                                    course.CourseID,
                                    course.Name,
                                    user.Name,
                                    user.UserId,
                                    certificate.CertificateId
                                );


                            certificate.CertificateFilePath =
                                certificatePath;


                            await context.SaveChangesAsync(
                                cancellationToken
                            );


                            // -----------------------------------------
                            // SEND EMAIL
                            // -----------------------------------------

                            var emailResult =
                                await emailService
                                    .SendCertificateEmail(
                                        user.UserId,
                                        certificate.CertificateId,
                                        course.Name,
                                        certificate.CertificateFilePath
                                    );


                            // -----------------------------------------
                            // EMAIL SUCCESS
                            // -----------------------------------------

                            if (emailResult.Success)
                            {
                                certificate.CertificateEmailStatus =
                                    CertificateEmailStatus.Sent;

                                certificate.CertificateEmailSentAt =
                                    DateTime.UtcNow;

                                certificate.CertificateEmailResponse =
                                    emailResult.Message;

                                certificate.UpdatedAt =
                                    DateTime.UtcNow;

                                sent++;

                                _logger.LogInformation(
                                    "Certificate sent successfully. UserId: {UserId}, CourseId: {CourseId}",
                                    user.UserId,
                                    course.CourseID
                                );
                            }


                            // -----------------------------------------
                            // EMAIL FAILED
                            // -----------------------------------------

                            else
                            {
                                certificate.CertificateEmailStatus =
                                    CertificateEmailStatus.Failed;

                                certificate.CertificateEmailSentAt =
                                    null;

                                certificate.CertificateEmailResponse =
                                    emailResult.Message;

                                certificate.UpdatedAt =
                                    DateTime.UtcNow;

                                failed++;

                                _logger.LogWarning(
                                    "Certificate email failed. UserId: {UserId}, CourseId: {CourseId}, Error: {Error}",
                                    user.UserId,
                                    course.CourseID,
                                    emailResult.Message
                                );
                            }


                            await context.SaveChangesAsync(
                                cancellationToken
                            );
                        }
                        catch (Exception ex)
                        {
                            failed++;

                            _logger.LogError(
                                ex,
                                "Error processing certificate for UserId: {UserId}, CourseId: {CourseId}",
                                sessionParticipant.UserID,
                                course.CourseID
                            );


                            // -----------------------------------------
                            // UPDATE FAILED CERTIFICATE
                            // -----------------------------------------

                            try
                            {
                                var failedCertificate =
                                    await context.tb_Certificate
                                        .FirstOrDefaultAsync(
                                            x =>
                                                x.ParticipantId ==
                                                sessionParticipant.Id
                                                &&
                                                x.CourseID ==
                                                course.CourseID,
                                            cancellationToken
                                        );

                                if (failedCertificate != null)
                                {
                                    failedCertificate
                                        .CertificateEmailStatus =
                                        CertificateEmailStatus.Failed;

                                    failedCertificate
                                        .CertificateEmailResponse =
                                        ex.Message;

                                    failedCertificate
                                        .CertificateEmailSentAt =
                                        null;

                                    failedCertificate.UpdatedAt =
                                        DateTime.UtcNow;

                                    await context.SaveChangesAsync(
                                        cancellationToken
                                    );
                                }
                            }
                            catch (Exception dbEx)
                            {
                                _logger.LogError(
                                    dbEx,
                                    "Could not update failed certificate status."
                                );
                            }
                        }
                    }


                    _logger.LogInformation(
                        "Course {CourseId} certificate processing completed. Sent: {Sent}, Failed: {Failed}, Skipped: {Skipped}",
                        course.CourseID,
                        sent,
                        failed,
                        skipped
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error while processing certificates."
                );
            }
        }


        // =========================================================
        // GENERATE CERTIFICATE
        // =========================================================

        private async Task<string> GenerateCertificateTemplate(
            int CourseId,
            string courseName,
            string participantName,
            int userId,
            string certificateId)
        {
            // TEMPLATE PATH
            string templatePath = System.IO.Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "Template",
                "certificate.png"
            );

            if (!System.IO.File.Exists(templatePath))
                throw new FileNotFoundException("Certificate template PNG not found.", templatePath);

            // USER FOLDER
            string safeUserName = string.Join(
                "_",
                participantName.Split(System.IO.Path.GetInvalidFileNameChars())
            );

            string userFolderName = $"{safeUserName}_{userId}";

            string certificateDirectory = System.IO.Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "certificates",
                userFolderName
            );

            if (!System.IO.Directory.Exists(certificateDirectory))
                System.IO.Directory.CreateDirectory(certificateDirectory);

            // OUTPUT FILE
            string fileName = $"{certificateId}.png";
            string outputPath = System.IO.Path.Combine(certificateDirectory, fileName);

            // FONT PATH (optional custom font)
            string fontPath = System.IO.Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "font",
                "Alice-Regular.ttf"
            );

            // DATE (example logic; keep as-is or update)
            string date = "conducted on 12-13 Sep 2026";
            if (CourseId == 1)
            {
                date = "conducted on 19-20 Sep 2026";
            }

            // Run CPU-bound image work on background thread
            await Task.Run(() =>
            {
                using var templateBitmap = SKBitmap.Decode(templatePath);
                if (templateBitmap == null)
                    throw new Exception("Unable to load certificate template.");

                var info = new SKImageInfo(templateBitmap.Width, templateBitmap.Height);
                using var surface = SKSurface.Create(info);
                var canvas = surface.Canvas;

                using var templateImage = SKImage.FromBitmap(templateBitmap);
                canvas.Clear(SKColors.Transparent);
                canvas.DrawImage(templateImage, 0, 0);

                SKTypeface typeface = SKTypeface.Default;
                if (System.IO.File.Exists(fontPath))
                {
                    try { typeface = SKTypeface.FromFile(fontPath); }
                    catch { typeface = SKTypeface.Default; }
                }

                static float FitTextSize(SKTypeface tf, string text, float maxWidth, float startingSize, float minSize = 10f)
                {
                    using var font = new SKFont(tf, startingSize);
                    float width = font.MeasureText(text);
                    if (width <= maxWidth) return startingSize;

                    float low = minSize;
                    float high = startingSize;
                    float size = startingSize;
                    for (int i = 0; i < 8; i++)
                    {
                        float mid = (low + high) / 2f;
                        font.Size = mid;
                        width = font.MeasureText(text);
                        if (width > maxWidth) high = mid;
                        else low = mid;
                        size = mid;
                    }
                    return Math.Max(minSize, size);
                }

                using var namePaint = new SKPaint { Color = SKColors.Black, IsAntialias = true };
                using var coursePaint = new SKPaint { Color = SKColors.Black, IsAntialias = true };
                using var datePaint = new SKPaint { Color = SKColors.Black, IsAntialias = true };
                using var certificateIdPaint = new SKPaint { Color = SKColors.Black, IsAntialias = true };

                // Center X is the same for every row: the true midpoint of the template.
                float pageCenterX = templateBitmap.Width / 2f;

                // Keep only the Y positions per row, plus a margin that defines the
                // max width text is allowed to occupy before it auto-shrinks.
                float sideMargin = 150f;
                float maxTextWidth = templateBitmap.Width - (sideMargin * 2f);

                float nameTop = 690f, nameBottom = 790f;
                float courseTop = 930f, courseBottom = 1010f;
                float dateTop = 1010f, dateBottom = 1090f;
                float certTop = 1940f, certBottom = 2010f;

                float nameSize = FitTextSize(typeface, participantName, maxTextWidth, startingSize: 48f, minSize: 18f);
                float courseSize = FitTextSize(typeface, courseName, maxTextWidth, startingSize: 48f, minSize: 18f);
                float dateSize = FitTextSize(typeface, date, maxTextWidth, startingSize: 48f, minSize: 18f);
                string certIdText = $"Certificate ID: {certificateId}";
                float certIdSize = FitTextSize(typeface, certIdText, maxTextWidth, startingSize: 22f, minSize: 12f);

                using var nameFont = new SKFont(typeface, nameSize);
                using var courseFont = new SKFont(typeface, courseSize);
                using var dateFont = new SKFont(typeface, dateSize);
                using var certIdFont = new SKFont(typeface, certIdSize);

                SKFontMetrics nm = nameFont.Metrics;
                float nameY = (nameTop + nameBottom) / 2f - (nm.Ascent + nm.Descent) / 2f;

                SKFontMetrics cm = courseFont.Metrics;
                float courseY = (courseTop + courseBottom) / 2f - (cm.Ascent + cm.Descent) / 2f;

                SKFontMetrics dm = dateFont.Metrics;
                float dateY = (dateTop + dateBottom) / 2f - (dm.Ascent + dm.Descent) / 2f;

                SKFontMetrics cim = certIdFont.Metrics;
                float certY = (certTop + certBottom) / 2f - (cim.Ascent + cim.Descent) / 2f;

                canvas.DrawText(participantName, pageCenterX, nameY, SKTextAlign.Center, nameFont, namePaint);
                canvas.DrawText(courseName, pageCenterX, courseY, SKTextAlign.Center, courseFont, coursePaint);
                canvas.DrawText(date, pageCenterX, dateY, SKTextAlign.Center, dateFont, datePaint);
                canvas.DrawText(certIdText, pageCenterX, certY, SKTextAlign.Center, certIdFont, certificateIdPaint);

                using var img = surface.Snapshot();
                using var data = img.Encode(SKEncodedImageFormat.Png, 100);

                using var fs = System.IO.File.Open(outputPath, System.IO.FileMode.Create, System.IO.FileAccess.Write);
                data.SaveTo(fs);
            }); ;

            // RETURN RELATIVE PATH
            return System.IO.Path.Combine("certificates", userFolderName, fileName).Replace("\\", "/");
        }
    }
}
