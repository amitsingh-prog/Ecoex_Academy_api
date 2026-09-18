using Ecoeex_Academy_Api.Data;
using Ecoeex_Academy_Api.Model;
using Ecoeex_Academy_Api.Services;
using Ecoex_Academy_Api.DTO;
using Ecoex_Academy_Api.Enums;
using Ecoex_Academy_Api.Model;
using Ecoex_Academy_Api.Models;
using Ecoex_Academy_Api.Services;
using Hangfire;
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

namespace Ecoex_Academy_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SessionParticipantController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IEmail_Services _emailService;
        private readonly ICertificateServices _certificateServices;
        public SessionParticipantController(AppDbContext context, IEmail_Services emailService, ICertificateServices certificateServices)
        {
            _context = context;
            _emailService = emailService;
            _certificateServices = certificateServices;
        }

        // =========================================================
        // GET ALL REGISTERED USERS
        // =========================================================

        [HttpGet("Alluser_course1")]
        public async Task<IActionResult> GetAlluser_course1([FromQuery] int Tempc)
        {
            try
            {
                var participants = await (
                    from u in _context.tb_Users
                    where u.temp_c == Tempc
                    select new Get_Participants
                    {
                        UserId = u.UserId,
                        ParticipantId = 0,
                        Email = u.Email,
                        Name = u.Name
                    }
                ).ToListAsync();

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


        [HttpGet("AllRegistreredUser")]
        public async Task<IActionResult> GetAllUserDetail([FromQuery] int courseID)
        {
            try
            {
                var participants = await (
                    from participant in _context.tb_SessionParticipant
                    join user in _context.tb_Users
                        on participant.UserID equals user.UserId
                    where participant.CourseID == courseID
                    select new Get_Participants
                    {
                        UserId = user.UserId,
                        ParticipantId = participant.Id,
                        Email = user.Email,
                        Name = user.Name
                    }
                ).ToListAsync();

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
                            await _emailService.SendCourse1ZoomEmail(
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
        // SEND ZOOM REMINDER
        // =========================================================

        [HttpPost("send-reminder")]
        public async Task<IActionResult> SendReminder([FromQuery] int courseID)
        {
            if (courseID <= 0)
            {
                return BadRequest("Invalid Course ID.");
            }

            try
            {
                // -------------------------------------------------
                // 1. Check Course
                // -------------------------------------------------

                var course = await _context.tb_Courses
                    .FirstOrDefaultAsync(x => x.CourseID == courseID);

                if (course == null)
                {
                    return NotFound($"Course with ID {courseID} not found.");
                }

                // -------------------------------------------------
                // 2. Get all registered users for this course
                // -------------------------------------------------

                var participants = await (
                    from participant in _context.tb_SessionParticipant
                    join user in _context.tb_Users
                        on participant.UserID equals user.UserId
                    where participant.CourseID == courseID
                    select new
                    {
                        UserId = user.UserId,
                        ParticipantId = participant.Id,
                        Email = user.Email,
                        Name = user.Name,
                        StartDateTime = participant.StartDateTime
                    }
                ).ToListAsync();

                if (participants.Count == 0)
                {
                    return NotFound(
                        $"No registered participants found for Course ID {courseID}."
                    );
                }

                int total = participants.Count;
                int sent = 0;
                int failed = 0;
                int skipped = 0;

                // -------------------------------------------------
                // 3. Process every participant
                // -------------------------------------------------

                foreach (var participant in participants)
                {
                    tb_joining_reminder? reminder = null;

                    try
                    {
                        // -----------------------------------------
                        // Validate email
                        // -----------------------------------------

                        if (string.IsNullOrWhiteSpace(participant.Email))
                        {
                            skipped++;
                            continue;
                        }

                        // -----------------------------------------
                        // Check maximum 2 reminders
                        // for this user + course
                        // -----------------------------------------

                        var reminderCount = await (
                            from reminderRecord in _context.tb_joining_reminder
                            join sessionParticipant in _context.tb_SessionParticipant
                                on reminderRecord.SessionParticipantID
                                equals sessionParticipant.Id
                            where sessionParticipant.CourseID == courseID
                                  && sessionParticipant.UserID == participant.UserId
                            select reminderRecord
                        ).CountAsync();

                        if (reminderCount >= 2)
                        {
                            skipped++;
                            continue;
                        }

                        // -----------------------------------------
                        // 4. Create reminder DB record
                        // -----------------------------------------

                        reminder = new tb_joining_reminder
                        {
                            SessionParticipantID = participant.ParticipantId,
                            ReminderEmailStatus = ZoomEmailStatus.Processing,
                            ReminderSentAt = null,
                            ReminderEmailResponse = null
                        };

                        _context.tb_joining_reminder.Add(reminder);

                        await _context.SaveChangesAsync();

                        // -----------------------------------------
                        // 5. Send email immediately
                        // -----------------------------------------

                        var emailResult = await _emailService
                            .SendCourse1ReminderEmail(
                                participant.UserId,
                                new DateTime(2026, 9, 19, 11, 0, 0),
                            new DateTime(2026, 9, 19, 13, 0, 0)
                            );

                        // -----------------------------------------
                        // 6. Update DB based on email result
                        // -----------------------------------------

                        if (emailResult.Success)
                        {
                            reminder.ReminderEmailStatus =
                                ZoomEmailStatus.Sent;

                            reminder.ReminderSentAt =
                                DateTime.UtcNow;

                            reminder.ReminderEmailResponse =
                                emailResult.Message;

                            sent++;
                        }
                        else
                        {
                            reminder.ReminderEmailStatus =
                                ZoomEmailStatus.Failed;

                            reminder.ReminderSentAt = null;

                            reminder.ReminderEmailResponse =
                                emailResult.Message;

                            failed++;
                        }

                        await _context.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        failed++;

                        // -----------------------------------------
                        // Update DB as Failed
                        // -----------------------------------------

                        if (reminder != null)
                        {
                            reminder.ReminderEmailStatus =
                                ZoomEmailStatus.Failed;

                            reminder.ReminderSentAt = null;

                            reminder.ReminderEmailResponse =
                                ex.Message;

                            await _context.SaveChangesAsync();
                        }
                    }
                }

                // -------------------------------------------------
                // 7. Final Response
                // -------------------------------------------------

                return Ok(new
                {
                    Success = true,
                    Message = "Reminder email processing completed.",
                    CourseID = courseID,
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
                    $"Error sending reminder emails: {ex.Message}"
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

        [HttpPost("send-certificate_userwise")]
        public async Task<IActionResult> SendCertificate1(
   [FromQuery] int courseID,
      [FromBody] List<Get_Participants> obj_participants,
   CancellationToken cancellationToken)
        {
            try
            {
                if (courseID <= 0)
                {
                    return BadRequest("Invalid Course ID.");
                }

                await _certificateServices.SendCertificates_userAsync(
                    courseID,
                    obj_participants,
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

        [HttpPost("schedule-reminder")]
        public async Task<IActionResult> ScheduleReminder(
            [FromQuery] int courseID)
        {
            if (courseID <= 0)
            {
                return BadRequest("Invalid Course ID.");
            }
            try
            {
                // -----------------------------------------
                // Check Course
                // -----------------------------------------

                var course = await _context.tb_Courses
                    .FirstOrDefaultAsync(x => x.CourseID == courseID);

                if (course == null)
                {
                    return NotFound(
                        $"Course with ID {courseID} not found.");
                }
                // -----------------------------------------
                // Get registered users
                // -----------------------------------------

                var participants = await (
                    from participant in _context.tb_SessionParticipant
                    join user in _context.tb_Users
                        on participant.UserID equals user.UserId
                    where participant.CourseID == courseID
                    select new
                    {
                        UserId = user.UserId,
                        ParticipantId = participant.Id,
                        Email = user.Email
                    }
                ).ToListAsync();

                if (participants.Count == 0)
                {
                    return NotFound(
                        $"No registered participants found for Course ID {courseID}.");
                }

                // -----------------------------------------
                // Calculate reminder times
                // -----------------------------------------

                var indiaTimeZone =
                    TimeZoneInfo.FindSystemTimeZoneById(
                        "India Standard Time");

                var nowIndia = TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.UtcNow,
                    indiaTimeZone);

                //// Tomorrow 10:00 AM
                //var tomorrow10AM = nowIndia.Date
                //    .AddDays(1)
                //    .AddHours(10);

                //// Day after tomorrow 10:00 AM
                //var dayAfterTomorrow10AM = nowIndia.Date
                //    .AddDays(2)
                //    .AddHours(10);

                // First reminder → 2:22 PM today
                var reminder1 = nowIndia.Date
                    .AddDays(1)
                    .AddHours(10);

                // Second reminder → 2:24 PM today
                var reminder2 = nowIndia.Date
                    .AddDays(2)
                    .AddHours(10);

                // -----------------------------------------
                // Schedule jobs
                // -----------------------------------------

                int scheduled = 0;

                foreach (var participant in participants)
                {
                    if (string.IsNullOrWhiteSpace(participant.Email))
                    {
                        continue;
                    }

                    // Reminder #1 → Session 1
                    BackgroundJob.Schedule<ReminderService>(
                        service => service.SendScheduledReminder(
                            participant.UserId,
                            participant.ParticipantId,
                            courseID,
                            new DateTime(2026, 9, 19, 11, 0, 0),
                            new DateTime(2026, 9, 19, 13, 0, 0)
                        ),
                        reminder1 - nowIndia
                    );

                    // Reminder #2 → Session 2
                    BackgroundJob.Schedule<ReminderService>(
                        service => service.SendScheduledReminder(
                            participant.UserId,
                            participant.ParticipantId,
                            courseID,
                            new DateTime(2026, 9, 20, 11, 0, 0),
                            new DateTime(2026, 9, 20, 13, 0, 0)
                        ),
                        reminder2 - nowIndia
                    );

                    scheduled++;
                }

                return Ok(new
                {
                    Success = true,
                    Message = "Reminder jobs scheduled successfully.",
                    CourseID = courseID,
                    Participants = scheduled,
                    Reminder1 = reminder1,
                    Reminder2 = reminder2
                });
            }
            catch (Exception ex)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    $"Error scheduling reminders: {ex.Message}");
            }
        }


        //        [HttpPost("send-certificate_Again_To_Sameuser")]
        //        public async Task<IActionResult> SendCertificate2(
        //[FromQuery] int courseID,
        //[FromBody] List<Get_Participants> obj_participants,
        //CancellationToken cancellationToken)
        //        {
        //            try
        //            {
        //                if (courseID <= 0)
        //                {
        //                    return BadRequest("Invalid Course ID.");
        //                }

        //                await _certificateServices.SendCertificates_userAsync(
        //                    courseID,
        //                    obj_participants,
        //                    cancellationToken
        //                );

        //                return Ok(new
        //                {
        //                    Success = true,
        //                    Message = "Certificate processing completed.",
        //                    CourseID = courseID
        //                });
        //            }
        //            catch (OperationCanceledException)
        //            {
        //                return StatusCode(
        //                    StatusCodes.Status499ClientClosedRequest,
        //                    "Certificate processing was cancelled."
        //                );
        //            }
        //            catch (Exception ex)
        //            {
        //                return StatusCode(
        //                    StatusCodes.Status500InternalServerError,
        //                    $"Error sending certificates: {ex.Message}"
        //                );
        //            }
        //        }



    }
}