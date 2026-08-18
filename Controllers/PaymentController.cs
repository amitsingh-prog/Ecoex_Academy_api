using Ecoeex_Academy_Api.Data;
using Ecoeex_Academy_Api.Model;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ecoeex_Academy_Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PaymentController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var payments = await _context.tb_Payments
                .Include(p => p.Order)
                    .ThenInclude(o => o.PayerUser)
                .AsNoTracking()
                .Select(p => new
                {
                    p.PaymentId,
                    p.OrderId,
                    p.Utr,
                    p.Status,
                    p.SubmittedAt,
                    p.ReviewedByAdminEmail,
                    p.ReviewedAt,
                    p.RejectionReason,
                    Order = new
                    {
                        p.Order.OrderId,
                        p.Order.TotalAmount,
                        p.Order.Status,
                        Payer = new
                        {
                            p.Order.PayerUser.UserId,
                            p.Order.PayerUser.Name,
                            p.Order.PayerUser.Email,
                            p.Order.PayerUser.Mobile
                        }
                    }
                })
                .ToListAsync();

            return Ok(payments);
        }

        [HttpGet("order/{orderId:int}")]
        public async Task<IActionResult> GetByOrderId(int orderId)
        {
            var payment = await _context.tb_Payments
                .Include(p => p.Order)
                    .ThenInclude(o => o.PayerUser)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.OrderId == orderId);

            if (payment == null)
            {
                return NotFound(new { message = "Payment not found for the specified order." });
            }

            return Ok(new
            {
                payment.PaymentId,
                payment.OrderId,
                payment.Utr,
                payment.Status,
                payment.SubmittedAt,
                payment.ReviewedByAdminEmail,
                payment.ReviewedAt,
                payment.RejectionReason,
                Order = new
                {
                    payment.Order.OrderId,
                    payment.Order.TotalAmount,
                    payment.Order.Status,
                    Payer = new
                    {
                        payment.Order.PayerUser.UserId,
                        payment.Order.PayerUser.Name,
                        payment.Order.PayerUser.Email,
                        payment.Order.PayerUser.Mobile
                    }
                }
            });
        }



        [HttpPost("{paymentId:int}/approve")]
        public async Task<IActionResult> Approve(int paymentId, ReviewPaymentRequest request)
        {
            var payment = await _context.tb_Payments.FindAsync(paymentId);

            if (payment == null)
            {
                return NotFound(new { message = "Payment not found." });
            }

            if (payment.Status == "Approved")
            {
                return BadRequest(new { message = "Payment already approved." });
            }

            payment.Status = "Approved";
            payment.ReviewedByAdminEmail = request.AdminEmail;
            payment.ReviewedAt = DateTime.UtcNow;
            payment.RejectionReason = null;

            // Optionally update order status
            var order = await _context.tb_Orders.FindAsync(payment.OrderId);
            if (order != null)
            {
                order.Status = "Paid";
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Payment approved." });
        }


        [HttpPost("{paymentId:int}/reject")]
        public async Task<IActionResult> Reject(int paymentId, ReviewPaymentRequest request)
        {
            var payment = await _context.tb_Payments.FindAsync(paymentId);

            if (payment == null)
            {
                return NotFound(new { message = "Payment not found." });
            }

            if (payment.Status == "Rejected")
            {
                return BadRequest(new { message = "Payment already rejected." });
            }

            payment.Status = "Rejected";
            payment.ReviewedByAdminEmail = request.AdminEmail;
            payment.ReviewedAt = DateTime.UtcNow;
            payment.RejectionReason = request.RejectionReason?.Trim();

            // Optionally update order status
            var order = await _context.tb_Orders.FindAsync(payment.OrderId);
            if (order != null)
            {
                order.Status = "Payment Rejected";
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Payment rejected." });
        }
    }



    public class ReviewPaymentRequest
    {
        public string AdminEmail { get; set; } = null!;
        public string? RejectionReason { get; set; }
    }
}
