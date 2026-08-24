using Ecoeex_Academy_Api.Data;
using Ecoeex_Academy_Api.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ecoex_Academy_Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }


        private const string OrderPaid = "paid";
        private const string OrderPending = "pending";

        private const string PaymentApproved = "approved";
        private const string PaymentPendingVerification = "pending verification";

        /// <summary>
        /// Card values for dashboard (top-level small cards).
        /// Returns concise numeric summaries without item lists.
        /// </summary>
        [HttpGet("cards")]
        public async Task<ActionResult<CardValuesResponse>> GetCardValues(CancellationToken cancellationToken)
        {
            try
            {
                var orders = _context.tb_Orders.AsNoTracking();

                var confirmed = await GetConfirmedRevenueAsync(orders, cancellationToken);
                var claimed = await GetClaimedNotYetVerifiedAsync(orders, cancellationToken);
                var abandoned = await GetAbandonedAtPaymentAsync(orders, cancellationToken);
                var avg = await GetAverageOrderValueAsync(orders, cancellationToken);

                var response = new CardValuesResponse
                {
                    ConfirmedAmount = confirmed.Amount,
                    ConfirmedOrders = confirmed.Orders,
                    ConfirmedNetOfGst = confirmed.NetOfGst,

                    ClaimedAmount = claimed.Amount,
                    ClaimedOrders = claimed.Orders,
                    ClaimedOldestHoursAgo = claimed.OldestHoursAgo,

                    AbandonedAmount = abandoned.Amount,
                    AbandonedOrders = abandoned.Orders,
                    AbandonedPercentage = abandoned.PercentageOfAllOrders,

                    AvgOrderValueOverall = avg.Overall,
                    AvgOrderValueIndividual = avg.Individual,
                    AvgOrderValueGroup = avg.Group
                };

                return Ok(response);
            }
            catch (OperationCanceledException)
            {
                return BadRequest(new { message = "Request was cancelled." });
            }
            catch (Exception)
            {
                return Problem(detail: "An unexpected error occurred while building card values.", statusCode: 500);
            }
        }

        private static DashboardOrderItem MapOrder(Order o)
        {
            return new DashboardOrderItem
            {
                OrderId = o.OrderId,
                Payer = o.PayerUser?.Name ?? string.Empty,
                Amount = o.TotalAmount,
                Utr = o.Payment != null ? o.Payment.Utr : null,
                OrderType = o.OrderType,
                Status = o.Status,
                CreatedAt = o.CreatedAt
            };
        }

        private async Task<RevenueSummary> GetConfirmedRevenueAsync(
            IQueryable<Order> orders,
            CancellationToken cancellationToken)
        {
            var confirmedOrders = await orders
                .Where(o =>
                    o.Status != null &&
                    o.Status.ToLower() == OrderPaid &&
                    o.Payment != null &&
                    o.Payment.Status != null &&
                    o.Payment.Status.ToLower() == PaymentApproved)
                .Include(o => o.PayerUser)
                .Include(o => o.Payment)
                .ToListAsync(cancellationToken);

            var amount = confirmedOrders.Sum(o => o.TotalAmount);
            var gst = confirmedOrders.Sum(o => o.GstAmount);

            return new RevenueSummary
            {
                Amount = decimal.Round(amount, 2),
                Orders = confirmedOrders.Count,
                NetOfGst = decimal.Round(amount - gst, 2),

                Items = confirmedOrders
                    .OrderByDescending(o => o.CreatedAt)
                    .Select(MapOrder)
                    .ToList()
            };
        }

        private async Task<ClaimedSummary> GetClaimedNotYetVerifiedAsync(
            IQueryable<Order> orders,
            CancellationToken cancellationToken)
        {
            var claimedOrders = await orders
                .Where(o =>
                    o.Payment != null &&
                    o.Payment.Status != null &&
                    o.Payment.Status.ToLower() == PaymentPendingVerification)
                .Include(o => o.PayerUser)
                .Include(o => o.Payment)
                .ToListAsync(cancellationToken);

            var amount = claimedOrders.Sum(o => o.TotalAmount);

            var oldest = claimedOrders
                .Where(o => o.Payment != null)
                .OrderBy(o => o.Payment!.SubmittedAt)
                .FirstOrDefault();

            var oldestHours = 0;

            if (oldest?.Payment != null)
            {
                oldestHours = Math.Max(
                    0,
                    (int)(DateTime.UtcNow - oldest.Payment.SubmittedAt).TotalHours
                );
            }

            return new ClaimedSummary
            {
                Amount = decimal.Round(amount, 2),
                Orders = claimedOrders.Count,
                OldestHoursAgo = oldestHours,

                Items = claimedOrders
                    .OrderBy(o => o.Payment!.SubmittedAt)
                    .Select(MapOrder)
                    .ToList()
            };
        }

        private async Task<AbandonedSummary> GetAbandonedAtPaymentAsync(
            IQueryable<Order> orders,
            CancellationToken cancellationToken)
        {
            var abandonedOrders = await orders
                .Where(o =>
                    o.Status != null &&
                    o.Status.ToLower() == OrderPending)
                .Include(o => o.PayerUser)
                .Include(o => o.Payment)
                .ToListAsync(cancellationToken);

            var amount = abandonedOrders.Sum(o => o.TotalAmount);

            var allOrdersCount = await orders.CountAsync(cancellationToken);

            var percentage = allOrdersCount == 0
                ? 0
                : Math.Round(
                    abandonedOrders.Count * 100m / allOrdersCount,
                    1);

            return new AbandonedSummary
            {
                Amount = decimal.Round(amount, 2),
                Orders = abandonedOrders.Count,
                PercentageOfAllOrders = percentage,

                Items = abandonedOrders
                    .OrderByDescending(o => o.CreatedAt)
                    .Select(MapOrder)
                    .ToList()
            };
        }

        private async Task<AverageOrderValueSummary> GetAverageOrderValueAsync(
            IQueryable<Order> orders,
            CancellationToken cancellationToken)
        {
            var confirmedOrders = await orders
                .Where(o =>
                    o.Status != null &&
                    o.Status.ToLower() == OrderPaid)
                .Include(o => o.PayerUser)
                .Include(o => o.Payment)
                .ToListAsync(cancellationToken);

            var individual = confirmedOrders
                .Where(o =>
                    o.OrderType != null &&
                    o.OrderType.ToLower() == "individual")
                .ToList();

            var group = confirmedOrders
                .Where(o =>
                    o.OrderType != null &&
                    o.OrderType.ToLower() == "group")
                .ToList();

            return new AverageOrderValueSummary
            {
                Overall = confirmedOrders.Count == 0
                    ? 0
                    : Math.Round(confirmedOrders.Average(o => o.TotalAmount), 2),

                Individual = individual.Count == 0
                    ? 0
                    : Math.Round(individual.Average(o => o.TotalAmount), 2),

                Group = group.Count == 0
                    ? 0
                    : Math.Round(group.Average(o => o.TotalAmount), 2),

                Items = confirmedOrders
                    .OrderByDescending(o => o.CreatedAt)
                    .Select(MapOrder)
                    .ToList()
            };
        }

        /// <summary>
        /// Returns a paged list of dashboard orders filtered by type.
        /// Query param `type` is required and must be one of: revenue, claimed, abandoned, average.
        /// Optional pagination: page (>=1), pageSize (1..200).
        /// </summary>


        [HttpGet("list")]
        public async Task<ActionResult> GetDashboardOrders(
          [FromQuery] string? type,
          [FromQuery] int page = 1,
          [FromQuery] int pageSize = 50,
          CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(type))
            {
                return BadRequest(new
                {
                    message = "Query parameter 'type' is required. Allowed: revenue, claimed, abandoned, average."
                });
            }

            type = type.Trim().ToLowerInvariant();

            if (page < 1)
                page = 1;

            pageSize = Math.Clamp(pageSize, 1, 200);

            var orders = _context.tb_Orders.AsNoTracking();

            // ============================================================
            // AVERAGE ORDER VALUE
            // ============================================================

            if (type == "aov")
            {
                var averageRows = await orders
                    .Where(o =>
                        o.Status != null &&
                        o.Status.ToLower() == OrderPaid)
                    .GroupBy(o => o.OrderType)
                    .Select(g => new
                    {
                        orderType = g.Key,
                        orders = g.Count(),
                        average = Math.Round(g.Average(o => o.TotalAmount), 2)
                    })
                    .OrderBy(x => x.orderType)
                    .ToListAsync(cancellationToken);

                var total = averageRows.Count;

                var items = averageRows
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return Ok(new
                {
                    total,
                    page,
                    pageSize,
                    items
                });
            }

            // ============================================================
            // OTHER DASHBOARD REPORTS
            // ============================================================

            IQueryable<Order> query = type switch
            {
                "revenue" => orders.Where(o =>
                    o.Status != null &&
                    o.Status.ToLower() == OrderPaid &&
                    o.Payment != null &&
                    o.Payment.Status != null &&
                    o.Payment.Status.ToLower() == PaymentApproved),

                "claimed" => orders.Where(o =>
                    o.Payment != null &&
                    o.Payment.Status != null &&
                    o.Payment.Status.ToLower() == PaymentPendingVerification),

                "abandoned" => orders.Where(o =>
                    o.Status != null &&
                    o.Status.ToLower() == OrderPending),

                _ => null
            };

            if (query == null)
            {
                return BadRequest(new
                {
                    message = "Invalid dashboard type. Allowed: revenue, claimed, abandoned, average."
                });
            }

            var totalOrders = await query.CountAsync(cancellationToken);

            var itemsOrders = await query
                .Include(o => o.PayerUser)
                .Include(o => o.Payment)
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new DashboardOrderItem
                {
                    OrderId = o.OrderId,
                    Payer = o.PayerUser != null
                        ? o.PayerUser.Name
                        : string.Empty,
                    Amount = o.TotalAmount,
                    Utr = o.Payment != null
                        ? o.Payment.Utr
                        : null,
                    OrderType = o.OrderType,
                    Status = o.Status,
                    CreatedAt = o.CreatedAt
                })
                .ToListAsync(cancellationToken);

            return Ok(new OrdersListResponse
            {
                Total = totalOrders,
                Page = page,
                PageSize = pageSize,
                Items = itemsOrders
            });
        }

        public class CardValuesResponse
        {
            public decimal ConfirmedAmount { get; set; }
            public int ConfirmedOrders { get; set; }
            public decimal ConfirmedNetOfGst { get; set; }

            public decimal ClaimedAmount { get; set; }
            public int ClaimedOrders { get; set; }
            public int ClaimedOldestHoursAgo { get; set; }

            public decimal AbandonedAmount { get; set; }
            public int AbandonedOrders { get; set; }
            public decimal AbandonedPercentage { get; set; }

            public decimal AvgOrderValueOverall { get; set; }
            public decimal AvgOrderValueIndividual { get; set; }
            public decimal AvgOrderValueGroup { get; set; }
        }
        public class OrdersListResponse
        {
            public int Total { get; set; }
            public int Page { get; set; }
            public int PageSize { get; set; }
            public List<DashboardOrderItem> Items { get; set; } = new();
        }

        public class DashboardResponse
        {
            public RevenueSummary ConfirmedRevenue { get; set; } = new();
            public ClaimedSummary ClaimedNotYetVerified { get; set; } = new();
            public AbandonedSummary AbandonedAtPayment { get; set; } = new();
            public AverageOrderValueSummary AvgOrderValueByType { get; set; } = new();
        }

        public class DashboardOrderItem
        {
            public int OrderId { get; set; }
            public string Payer { get; set; } = string.Empty;
            public decimal Amount { get; set; }
            public string? Utr { get; set; }
            public string OrderType { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
        }

        public class RevenueSummary
        {
            public decimal Amount { get; set; }
            public int Orders { get; set; }
            public decimal NetOfGst { get; set; }
            public List<DashboardOrderItem> Items { get; set; } = new();
        }

        public class ClaimedSummary
        {
            public decimal Amount { get; set; }
            public int Orders { get; set; }
            public int OldestHoursAgo { get; set; }
            public List<DashboardOrderItem> Items { get; set; } = new();
        }

        public class AbandonedSummary
        {
            public decimal Amount { get; set; }
            public int Orders { get; set; }
            public decimal PercentageOfAllOrders { get; set; }
            public List<DashboardOrderItem> Items { get; set; } = new();
        }

        public class AverageOrderValueSummary
        {
            public decimal Overall { get; set; }
            public decimal Individual { get; set; }
            public decimal Group { get; set; }
            public List<DashboardOrderItem> Items { get; set; } = new();
        }
    }
}