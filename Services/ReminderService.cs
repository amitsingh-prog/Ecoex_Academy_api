using Ecoeex_Academy_Api.Data;
using Ecoeex_Academy_Api.Services;
using Ecoex_Academy_Api.Enums;
using Ecoex_Academy_Api.Model;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace Ecoex_Academy_Api.Services
{
    public class ReminderService
    {
        private readonly AppDbContext _context;
        private readonly IEmail_Services _emailService;

        public ReminderService(
            AppDbContext context,
            IEmail_Services emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task SendScheduledReminder(
     int userId,
     int participantId,
     int courseID,
     DateTime sessionStartDateTime,
     DateTime sessionEndDateTime)
        {
            // -----------------------------------------
            // Get participant
            // -----------------------------------------

            var participant = await _context.tb_SessionParticipant
                .FirstOrDefaultAsync(x =>
                    x.Id == participantId &&
                    x.UserID == userId &&
                    x.CourseID == courseID);

            if (participant == null)
            {
                return;
            }

            // -----------------------------------------
            // Get user
            // -----------------------------------------

            var user = await _context.tb_Users
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (user == null || string.IsNullOrWhiteSpace(user.Email))
            {
                return;
            }

            // -----------------------------------------
            // Check maximum 2 reminders
            // -----------------------------------------

            var reminderCount = await (
                from reminder in _context.tb_joining_reminder
                join sessionParticipant in _context.tb_SessionParticipant
                    on reminder.SessionParticipantID equals sessionParticipant.Id
                where sessionParticipant.CourseID == courseID
                      && sessionParticipant.UserID == userId
                select reminder
            ).CountAsync();

            if (reminderCount >= 2)
            {
                return;
            }

            // -----------------------------------------
            // Create DB reminder
            // -----------------------------------------

            var reminderRecord = new tb_joining_reminder
            {
                SessionParticipantID = participantId,
                ReminderEmailStatus = ZoomEmailStatus.Processing,
                ReminderSentAt = null,
                ReminderEmailResponse = null
            };

            _context.tb_joining_reminder.Add(reminderRecord);

            await _context.SaveChangesAsync();

            try
            {
                // -----------------------------------------
                // Send Email
                // -----------------------------------------
                var emailResult =
                    await _emailService.SendCourse1ReminderEmail(
                        userId,
                        sessionStartDateTime,
                        sessionEndDateTime
                    );

                // -----------------------------------------
                // Update DB
                // -----------------------------------------

                if (emailResult.Success)
                {
                    reminderRecord.ReminderEmailStatus =
                        ZoomEmailStatus.Sent;

                    reminderRecord.ReminderSentAt =
                        DateTime.UtcNow;

                    reminderRecord.ReminderEmailResponse =
                        emailResult.Message;
                }
                else
                {
                    reminderRecord.ReminderEmailStatus =
                        ZoomEmailStatus.Failed;

                    reminderRecord.ReminderEmailResponse =
                        emailResult.Message;
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                reminderRecord.ReminderEmailStatus =
                    ZoomEmailStatus.Failed;

                reminderRecord.ReminderEmailResponse =
                    ex.Message;

                await _context.SaveChangesAsync();

                throw;
            }
        }
    }
}
