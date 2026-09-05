using Ecoeex_Academy_Api.Data;
using Ecoeex_Academy_Api.Model;
using Ecoeex_Academy_Api.Services;
using Ecoex_Academy_Api.DTO;
using Ecoex_Academy_Api.Enums;
using Ecoex_Academy_Api.Model;
using Ecoex_Academy_Api.Models;
using Ecoex_Academy_Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

//using SixLabors.Fonts;
//using SixLabors.ImageSharp;
//using SixLabors.ImageSharp.Drawing;
//using SixLabors.ImageSharp.Drawing.Processing;
//using SixLabors.ImageSharp.PixelFormats;
//using SixLabors.ImageSharp.Processing;


using SkiaSharp;

namespace Ecoex_Academy_Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class SessionParticipantController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IEmail_Services _emailService;
        private readonly ICertificateServices _certificateServices;
        public SessionParticipantController(
            AppDbContext context,
            IEmail_Services emailService,
            ICertificateServices certificateServices)
        {
            _context = context;
            _emailService = emailService;
            _certificateServices = certificateServices;
        }


        // =========================================================
        // GET ALL REGISTERED USERS
        // =========================================================

        [HttpGet("AllRegistreredUser")]
        public async Task<IActionResult> GetAllUserDetail()
        {
            try
            {
                var participants = await _context.tb_Users
                    .Where(x => x.EmailVerified == true)
                    .Select(x => new Get_Participants
                    {
                        UserId = x.UserId,
                        Email = x.Email,
                        Name = x.Name
                    })
                    .ToListAsync();

                return Ok(participants);
            }
            catch (Exception ex)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    $"Error retrieving registered users: {ex.Message}"
                );
            }
        }


        // =========================================================
        // SEND ZOOM LINK
        // =========================================================

        [HttpPost("send-zoom-link")]
        public async Task<IActionResult> SendZoomLink(
            [FromBody] List<Get_Participants> obj_participants,
            [FromQuery] int courseID)
        {
            if (obj_participants == null ||
                obj_participants.Count == 0)
            {
                return BadRequest("No participants were provided.");
            }

            if (courseID <= 0)
            {
                return BadRequest("Invalid Course ID.");
            }

            try
            {
                var course = await _context.tb_Courses
                    .FirstOrDefaultAsync(x =>
                        x.CourseID == courseID);

                if (course == null)
                {
                    return NotFound(
                        $"Course with ID {courseID} not found."
                    );
                }

                if (string.IsNullOrWhiteSpace(course.ZoomMeetingId))
                {
                    return BadRequest(
                        $"Zoom meeting link is not configured for course {courseID}."
                    );
                }

                int total = obj_participants.Count;
                int sent = 0;
                int failed = 0;
                int skipped = 0;

                foreach (var participant in obj_participants)
                {
                    try
                    {
                        if (participant.UserId <= 0)
                        {
                            skipped++;
                            continue;
                        }

                        var user = await _context.tb_Users
                            .FirstOrDefaultAsync(
                                x => x.UserId == participant.UserId
                            );

                        if (user == null)
                        {
                            skipped++;
                            continue;
                        }

                        var existingParticipant =
                            await _context.tb_SessionParticipant
                                .FirstOrDefaultAsync(x =>
                                    x.UserID == user.UserId &&
                                    x.CourseID == course.CourseID
                                );

                        SessionParticipant sessionParticipant;

                        if (existingParticipant != null)
                        {
                            sessionParticipant = existingParticipant;

                            if (sessionParticipant.ZoomEmailStatus ==
                                ZoomEmailStatus.Sent)
                            {
                                skipped++;
                                continue;
                            }

                            sessionParticipant.ZoomLink =
                                course.ZoomMeetingId;

                            sessionParticipant.ZoomEmailStatus =
                                ZoomEmailStatus.Processing;

                            sessionParticipant.ZoomEmailSentAt = null;

                            sessionParticipant.ZoomEmailResponse = null;

                            sessionParticipant.UpdatedAt =
                                DateTime.UtcNow;
                        }
                        else
                        {
                            sessionParticipant =
                                new SessionParticipant
                                {
                                    UserID = user.UserId,

                                    CourseID = course.CourseID,

                                    StartDateTime =
                                        Convert.ToDateTime(
                                            course.BatchStartDate
                                        ),

                                    EndDateTime = null,

                                    ZoomLink =
                                        course.ZoomMeetingId,

                                    ZoomEmailStatus =
                                        ZoomEmailStatus.Processing,

                                    ZoomEmailSentAt = null,

                                    ZoomEmailResponse = null,

                                    CreatedAt = DateTime.UtcNow,

                                    UpdatedAt = null
                                };

                            _context.tb_SessionParticipant
                                .Add(sessionParticipant);
                        }

                        await _context.SaveChangesAsync();

                        var emailResult =
                            await _emailService.SendZoomLinkEmail(
                                user.UserId,
                                sessionParticipant.ZoomLink!,
                                course.Name,
                                sessionParticipant.StartDateTime,
                                sessionParticipant.EndDateTime
                            );

                        if (emailResult.Success)
                        {
                            sessionParticipant.ZoomEmailStatus =
                                ZoomEmailStatus.Sent;

                            sessionParticipant.ZoomEmailSentAt =
                                DateTime.UtcNow;

                            sessionParticipant.ZoomEmailResponse =
                                emailResult.Message;

                            sessionParticipant.UpdatedAt =
                                DateTime.UtcNow;

                            sent++;
                        }
                        else
                        {
                            sessionParticipant.ZoomEmailStatus =
                                ZoomEmailStatus.Failed;

                            sessionParticipant.ZoomEmailSentAt =
                                null;

                            sessionParticipant.ZoomEmailResponse =
                                emailResult.Message;

                            sessionParticipant.UpdatedAt =
                                DateTime.UtcNow;

                            failed++;
                        }

                        await _context.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        failed++;

                        var failedParticipant =
                            await _context.tb_SessionParticipant
                                .FirstOrDefaultAsync(x =>
                                    x.UserID == participant.UserId &&
                                    x.CourseID == course.CourseID
                                );

                        if (failedParticipant != null)
                        {
                            failedParticipant.ZoomEmailStatus =
                                ZoomEmailStatus.Failed;

                            failedParticipant.ZoomEmailResponse =
                                ex.Message;

                            failedParticipant.ZoomEmailSentAt =
                                null;

                            failedParticipant.UpdatedAt =
                                DateTime.UtcNow;

                            await _context.SaveChangesAsync();
                        }

                        continue;
                    }
                }

                return Ok(new
                {
                    Success = true,
                    Message = "Zoom email processing completed.",
                    Total = total,
                    Sent = sent,
                    Failed = failed,
                    Skipped = skipped
                });
            }
            catch (Exception ex)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    $"Error sending Zoom link emails: {ex.Message}"
                );
            }
        }


        // =========================================================
        // SEND CERTIFICATE
        // =========================================================

        [HttpPost("send-certificate")]
        public async Task<IActionResult> SendCertificate(
     [FromQuery] int courseID,
     CancellationToken cancellationToken)
        {
            try
            {
                if (courseID <= 0)
                {
                    return BadRequest("Invalid Course ID.");
                }

                await _certificateServices.SendCertificatesAsync(
                    courseID,
                    cancellationToken
                );

                return Ok(new
                {
                    Success = true,
                    Message = "Certificate processing completed.",
                    CourseID = courseID
                });
            }
            catch (OperationCanceledException)
            {
                return StatusCode(
                    StatusCodes.Status499ClientClosedRequest,
                    "Certificate processing was cancelled."
                );
            }
            catch (Exception ex)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    $"Error sending certificates: {ex.Message}"
                );
            }
        }





    }
}