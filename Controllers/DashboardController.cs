using Ecoeex_Academy_Api.Data;
using Ecoeex_Academy_Api.Model;
using Microsoft.AspNetCore.Http;
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

        [HttpGet("registration-revenue/cards")]
        public async Task<ActionResult<RegistrationRevenueCardsResponse>> GetRegistrationRevenueCards(
          CancellationToken cancellationToken = default)
        {
            try
            {
                // ============================================================
                // DATE RANGE FOR TODAY
                // ============================================================

                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);


                // ============================================================
                // TODAY - PAID + APPROVED ORDERS
                // ============================================================

                var todayOrders = _context.tb_Orders
                    .AsNoTracking()
                    .Where(o =>
                        o.CreatedAt >= today &&
                        o.CreatedAt < tomorrow &&
                        o.Status != null &&
                        o.Status.ToLower() == OrderPaid &&
                        o.Payment != null &&
                        o.Payment.Status != null &&
                        o.Payment.Status.ToLower() == PaymentApproved);


                // ============================================================
                // 1. TODAY REGISTRATION
                //    Breakdown by Google / Email
                // ============================================================

                var todayRegistration = await todayOrders
                    .Join(
                        _context.tb_Users.AsNoTracking(),
                        o => o.PayerUserId,
                        u => u.UserId,
                        (o, u) => new
                        {
                            u.UserId,
                            AuthProvider = string.IsNullOrWhiteSpace(u.AuthProvider)
                                ? "email"
                                : u.AuthProvider
                        })
                    .Distinct()
                    .ToListAsync(cancellationToken);


                var todayRegistrationByProvider = todayRegistration
                    .GroupBy(x => x.AuthProvider.ToLower())
                    .Select(g => new RegistrationProviderItem
                    {
                        Provider = g.Key,
                        Count = g.Count()
                    })
                    .OrderByDescending(x => x.Count)
                    .ToList();


                var todayRegistrationTotal =
                    todayRegistrationByProvider.Sum(x => x.Count);


                // ============================================================
                // 2. TODAY REVENUE
                // ============================================================

                var todayRevenueData = await todayOrders
                    .Select(o => new
                    {
                        o.OrderId,
                        o.TotalAmount,
                        o.GstAmount
                    })
                    .ToListAsync(cancellationToken);


                // Remove duplicate order rows
                var todayRevenueOrders = todayRevenueData
                    .GroupBy(x => x.OrderId)
                    .Select(g => g.First())
                    .ToList();


                var todayTotalRevenue =
                    todayRevenueOrders.Sum(x => x.TotalAmount);

                var todayGst =
                    todayRevenueOrders.Sum(x => x.GstAmount);


                // ============================================================
                // 2A. TODAY REVENUE BY COURSE
                // ============================================================

                var todayRevenueByCourseRaw = await _context.tb_OrderCourses
                    .AsNoTracking()
                    .Where(oc =>
                        oc.Order.CreatedAt >= today &&
                        oc.Order.CreatedAt < tomorrow &&
                        oc.Order.Status != null &&
                        oc.Order.Status.ToLower() == OrderPaid &&
                        oc.Order.Payment != null &&
                        oc.Order.Payment.Status != null &&
                        oc.Order.Payment.Status.ToLower() == PaymentApproved)
                    .Select(oc => new
                    {
                        oc.CourseID,
                        CourseName = oc.Course.Name,
                        oc.OrderId,
                        oc.Order.TotalAmount,

                        OrderCourseCount = _context.tb_OrderCourses
                            .Count(x => x.OrderId == oc.OrderId)
                    })
                    .ToListAsync(cancellationToken);


                var todayRevenueByCourse = todayRevenueByCourseRaw
                    .GroupBy(x => new
                    {
                        x.CourseID,
                        x.CourseName
                    })
                    .Select(g => new CourseRevenueItem
                    {
                        CourseId = g.Key.CourseID,
                        Course = g.Key.CourseName ?? "Unknown",

                        // Divide order amount between courses
                        Revenue = Math.Round(
                            g.Sum(x =>
                                x.OrderCourseCount > 0
                                    ? x.TotalAmount / x.OrderCourseCount
                                    : 0),
                            2)
                    })
                    .OrderByDescending(x => x.Revenue)
                    .ToList();


                // ============================================================
                // OVERALL - PAID + APPROVED ORDERS
                // ============================================================

                var allOrders = _context.tb_Orders
                    .AsNoTracking()
                    .Where(o =>
                        o.Status != null &&
                        o.Status.ToLower() == OrderPaid &&
                        o.Payment != null &&
                        o.Payment.Status != null &&
                        o.Payment.Status.ToLower() == PaymentApproved);




                // ============================================================
                // TOTAL REGISTRATION
                // ALL VERIFIED USERS
                // Breakdown by Google / Email
                // ============================================================

                var totalRegistrationUsers = await _context.tb_Users
                    .AsNoTracking()
                    .Where(u =>
                        u.EmailVerified == true ||
                        u.MobileVerified == true)
                    .Select(u => new
                    {
                        u.UserId,
                        AuthProvider = string.IsNullOrWhiteSpace(u.AuthProvider)
                            ? "email"
                            : u.AuthProvider
                    })
                    .ToListAsync(cancellationToken);


                var totalRegistrationByProvider = totalRegistrationUsers
                    .GroupBy(x => x.AuthProvider.ToLower())
                    .Select(g => new RegistrationProviderItem
                    {
                        Provider = g.Key,
                        Count = g.Count()
                    })
                    .OrderByDescending(x => x.Count)
                    .ToList();


                var totalRegistration =
                    totalRegistrationByProvider.Sum(x => x.Count);


                // ============================================================
                // 4. TOTAL REVENUE
                // ============================================================

                var totalRevenueData = await allOrders
                    .Select(o => new
                    {
                        o.OrderId,
                        o.TotalAmount,
                        o.GstAmount
                    })
                    .ToListAsync(cancellationToken);


                // Remove duplicate order rows
                var totalRevenueOrders = totalRevenueData
                    .GroupBy(x => x.OrderId)
                    .Select(g => g.First())
                    .ToList();


                var totalRevenue =
                    totalRevenueOrders.Sum(x => x.TotalAmount);

                var totalGst =
                    totalRevenueOrders.Sum(x => x.GstAmount);


                // ============================================================
                // 4A. TOTAL REVENUE BY COURSE
                // ============================================================

                var totalRevenueByCourseRaw = await _context.tb_OrderCourses
                    .AsNoTracking()
                    .Where(oc =>
                        oc.Order.Status != null &&
                        oc.Order.Status.ToLower() == OrderPaid &&
                        oc.Order.Payment != null &&
                        oc.Order.Payment.Status != null &&
                        oc.Order.Payment.Status.ToLower() == PaymentApproved)
                    .Select(oc => new
                    {
                        oc.CourseID,
                        CourseName = oc.Course.Name,
                        oc.OrderId,
                        oc.Order.TotalAmount,

                        OrderCourseCount = _context.tb_OrderCourses
                            .Count(x => x.OrderId == oc.OrderId)
                    })
                    .ToListAsync(cancellationToken);


                var totalRevenueByCourse = totalRevenueByCourseRaw
                    .GroupBy(x => new
                    {
                        x.CourseID,
                        x.CourseName
                    })
                    .Select(g => new CourseRevenueItem
                    {
                        CourseId = g.Key.CourseID,
                        Course = g.Key.CourseName ?? "Unknown",

                        Revenue = Math.Round(
                            g.Sum(x =>
                                x.OrderCourseCount > 0
                                    ? x.TotalAmount / x.OrderCourseCount
                                    : 0),
                            2)
                    })
                    .OrderByDescending(x => x.Revenue)
                    .ToList();


                // ============================================================
                // FINAL RESPONSE
                // ============================================================

                var response = new RegistrationRevenueCardsResponse
                {
                    // --------------------------------------------------------
                    // TODAY REGISTRATION
                    // --------------------------------------------------------

                    TodayRegistration = new TodayRegistrationCard
                    {
                        Total = todayRegistrationTotal,
                        ByProvider = todayRegistrationByProvider
                    },


                    // --------------------------------------------------------
                    // TODAY REVENUE
                    // --------------------------------------------------------

                    TodayRevenue = new TodayRevenueCard
                    {
                        Total = decimal.Round(todayTotalRevenue, 2),

                        WithoutGst = decimal.Round(
                            todayTotalRevenue - todayGst,
                            2),

                        ByCourse = todayRevenueByCourse
                    },


                    // --------------------------------------------------------
                    // TOTAL REGISTRATION
                    // --------------------------------------------------------

                    TotalRegistration = new TotalRegistrationCard
                    {
                        Total = totalRegistration,
                        ByProvider = totalRegistrationByProvider
                    },


                    // --------------------------------------------------------
                    // TOTAL REVENUE
                    // --------------------------------------------------------

                    TotalRevenue = new TotalRevenueCard
                    {
                        Total = decimal.Round(totalRevenue, 2),

                        WithoutGst = decimal.Round(
                            totalRevenue - totalGst,
                            2),

                        ByCourse = totalRevenueByCourse
                    }
                };


                return Ok(response);
            }
            catch (OperationCanceledException)
            {
                return BadRequest(new
                {
                    message = "Request was cancelled."
                });
            }
            catch (Exception ex)
            {
                return Problem(
                    detail: ex.Message,
                    statusCode: 500);
            }
        }


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


        [HttpGet("registration/cards")]
        public async Task<ActionResult<RegistrationCardsResponse>> GetRegistrationCards(CancellationToken cancellationToken = default)
        {
            try
            {
                var accounts = await _context.tb_Users
                    .AsNoTracking()
                    .CountAsync(cancellationToken);


                var verified = await _context.tb_Users
                    .AsNoTracking()
                    .CountAsync(u => u.EmailVerified || u.MobileVerified, cancellationToken);


                var orderedUsers = await _context.tb_Orders
                    .AsNoTracking()
                    .Select(o => o.PayerUserId)
                    .Distinct()
                    .CountAsync(cancellationToken);

                var paidUsers = await _context.tb_Orders
                    .AsNoTracking()
                    .Where(o =>
                        o.Status != null &&
                        o.Status.ToLower() == OrderPaid &&
                        o.Payment != null &&
                        o.Payment.Status != null &&
                        o.Payment.Status.ToLower() == PaymentApproved)
                    .Select(o => o.PayerUserId)
                    .Distinct()
                    .CountAsync(cancellationToken);

                var funnelPercent = accounts == 0
                    ? 0
                    : Math.Round(verified * 100m / accounts, 1);


                var signupToPurchasePercent = accounts == 0
                    ? 0
                    : Math.Round(paidUsers * 100m / accounts, 1);


                var providerGroups = await _context.tb_Users
                    .AsNoTracking()
                    .GroupBy(u => u.AuthProvider ?? "unknown")
                    .Select(g => new ProviderBreakdownItem
                    {
                        Provider = g.Key,
                        Count = g.Count()
                    })
                    .ToListAsync(cancellationToken);

                foreach (var p in providerGroups)
                {
                    p.Percent = accounts == 0 ? 0 : Math.Round(p.Count * 100m / accounts, 1);
                }

                var response = new RegistrationCardsResponse
                {
                    RegistrationFunnelPercent = funnelPercent,
                    Started = accounts - verified,
                    Verified = verified,
                    Accounts = accounts,

                    SignupToPurchasePercent = signupToPurchasePercent,
                    AccountsWithOrders = orderedUsers,
                    AccountsPaid = paidUsers,

                    ProviderBreakdown = providerGroups
                        .OrderByDescending(x => x.Count)
                        .ToList()
                };

                return Ok(response);
            }
            catch (OperationCanceledException)
            {
                return BadRequest(new { message = "Request was cancelled." });
            }
            catch (Exception)
            {
                return Problem(detail: "An unexpected error occurred while computing registration cards.", statusCode: 500);
            }
        }





        [HttpGet("registration/list")]
        public async Task<ActionResult> GetRegistrationList([FromQuery] string? type, [FromQuery] int page = 1,
          [FromQuery] int pageSize = 50,
          [FromQuery] int samplePerProvider = 5,
          CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(type))
            {
                return BadRequest(new { message = "Query parameter 'type' is required. Allowed: started, verified, accounts, ordered, paid, providers." });
            }

            type = type.Trim().ToLowerInvariant();

            if (page < 1) page = 1;
            pageSize = Math.Clamp(pageSize, 1, 200);

            try
            {
                switch (type)
                {
                    case "funnel":
                        {
                            var query = _context.tb_Users
                                .AsNoTracking()
                                .OrderByDescending(x => x.CreatedAt);

                            var total = await query.CountAsync(cancellationToken);

                            var items = await query
                                .Skip((page - 1) * pageSize)
                                .Take(pageSize)
                                .Select(u => new
                                {
                                    u.UserId,
                                    u.Name,
                                    u.Email,
                                    u.Mobile,
                                    u.AuthProvider,
                                    u.RegistrationType,
                                    u.UserType,
                                    u.EmailVerified,
                                    u.MobileVerified,
                                    u.CreatedAt,
                                    FunnelStatus = u.EmailVerified || u.MobileVerified ? "verified" : "started"
                                })
                                .ToListAsync(cancellationToken);

                            return Ok(new PagedListResponse<object>
                            {
                                Total = total,
                                Page = page,
                                PageSize = pageSize,
                                Items = items
                            });
                        }

                    case "signup-to-purchase conversion":
                        {
                            var grouped = _context.tb_Orders
                                .AsNoTracking()
                                .Where(o =>
                                    o.Status != null &&
                                    o.Status.ToLower() == OrderPaid &&
                                    o.Payment != null &&
                                    o.Payment.Status != null &&
                                    o.Payment.Status.ToLower() == PaymentApproved)
                                .GroupBy(o => o.PayerUserId)
                                .Select(g => new
                                {
                                    UserId = g.Key,
                                    Orders = g.Count(),
                                    LatestOrder = g.Max(o => o.CreatedAt)
                                });

                            var joined = grouped
                                .Join(
                                    _context.tb_Users.AsNoTracking(),
                                    g => g.UserId,
                                    u => u.UserId,
                                    (g, u) => new
                                    {
                                        u.UserId,
                                        u.Name,
                                        u.Email,
                                        u.Mobile,
                                        Orders = g.Orders,
                                        LatestOrder = g.LatestOrder,
                                        u.AuthProvider,
                                        u.CreatedAt,
                                        OrderStatus = "Paid"
                                    })
                                .OrderByDescending(x => x.LatestOrder);

                            var total = await joined.CountAsync(cancellationToken);

                            var items = await joined
                                .Skip((page - 1) * pageSize)
                                .Take(pageSize)
                                .ToListAsync(cancellationToken);

                            return Ok(new PagedListResponse<object>
                            {
                                Total = total,
                                Page = page,
                                PageSize = pageSize,
                                Items = items
                            });
                        }


                    case "providers":
                        {
                            var accounts = await _context.tb_Users.AsNoTracking().CountAsync(cancellationToken);

                            var groups = await _context.tb_Users
                                .AsNoTracking()
                                .GroupBy(u => u.AuthProvider ?? "unknown")
                                .Select(g => new
                                {
                                    Provider = g.Key,
                                    Count = g.Count()
                                })
                                .OrderByDescending(x => x.Count)
                                .ToListAsync(cancellationToken);

                            var result = groups
                                .Select(g => new ProviderWithSamples
                                {
                                    Provider = g.Provider,
                                    Count = g.Count,
                                    Percent = accounts == 0 ? 0 : Math.Round(g.Count * 100m / accounts, 1),
                                    Samples = _context.tb_Users
                                        .AsNoTracking()
                                        .Where(u => (u.AuthProvider ?? "unknown") == g.Provider)
                                        .OrderByDescending(u => u.CreatedAt)
                                        .Take(samplePerProvider)
                                        .Select(u => new { u.UserId, u.Name, u.Email, u.CreatedAt })
                                        .ToList()
                                })
                                .ToList();

                            return Ok(new { total = groups.Count, page = 1, pageSize = groups.Count, items = result });
                        }

                    default:
                        return BadRequest(new { message = "Invalid type. Allowed: started, verified, accounts, ordered, paid, providers." });
                }
            }
            catch (OperationCanceledException)
            {
                return BadRequest(new { message = "Request was cancelled." });
            }
            catch (Exception)
            {
                return Problem(detail: "An unexpected error occurred while fetching registration list.", statusCode: 500);
            }
        }





        [HttpGet("course/cards")]
        public async Task<ActionResult<CourseCardsResponse>> GetCourseCards(
            CancellationToken cancellationToken = default)
        {
            try
            {

                var courseDemand = await _context.tb_OrderCourses
                    .AsNoTracking()
                    .GroupBy(oc => new
                    {
                        oc.CourseID,
                        oc.Course.Name
                    })
                    .Select(g => new CourseDemandItem
                    {
                        CourseId = g.Key.CourseID,
                        Course = g.Key.Name ?? "Unknown",

                        Paid = g.Count(oc =>
                            oc.Order.Status != null &&
                            oc.Order.Status.ToLower() == OrderPaid &&
                            oc.Order.Payment != null &&
                            oc.Order.Payment.Status != null &&
                            oc.Order.Payment.Status.ToLower() == PaymentApproved),

                        Pending = g.Count(oc =>
                            oc.Order.Status != null &&
                            oc.Order.Status.ToLower() == OrderPending)
                    })
                    .OrderByDescending(x => x.Paid)
                    .ToListAsync(cancellationToken);


                var spaUserIds = await _context.tb_Users
                    .AsNoTracking()
                    .Where(u =>
                        u.Email != null &&
                        u.Email.ToLower().EndsWith("@spa.ac.in"))
                    .Select(u => u.UserId)
                    .ToListAsync(cancellationToken);


                var spaPaidOrders = await _context.tb_Orders
                    .AsNoTracking()
                    .Where(o =>
                        spaUserIds.Contains(o.PayerUserId) &&
                        o.Status != null &&
                        o.Status.ToLower() == OrderPaid &&
                        o.Payment != null &&
                        o.Payment.Status != null &&
                        o.Payment.Status.ToLower() == PaymentApproved)
                    .ToListAsync(cancellationToken);


                var spaTotal = spaUserIds.Count;

                var spaPaid = spaPaidOrders
                    .Select(o => o.PayerUserId)
                    .Distinct()
                    .Count();

                var revenueByCourse = await _context.tb_OrderCourses
                    .AsNoTracking()
                    .Where(oc =>
                        oc.Order.Status != null &&
                        oc.Order.Status.ToLower() == OrderPaid &&
                        oc.Order.Payment != null &&
                        oc.Order.Payment.Status != null &&
                        oc.Order.Payment.Status.ToLower() == PaymentApproved)
                    .GroupBy(oc => new
                    {
                        oc.CourseID,
                        oc.Course.Name
                    })
                    .Select(g => new CourseRevenueItem
                    {
                        CourseId = g.Key.CourseID,
                        Course = g.Key.Name ?? "Unknown",

                        Revenue = Math.Round(
                            g.Sum(oc => oc.Order.TotalAmount),
                            2)
                    })
                    .OrderByDescending(x => x.Revenue)
                    .ToListAsync(cancellationToken);


                var leadingCourse = courseDemand
                    .OrderByDescending(x => x.Paid)
                    .FirstOrDefault();


                var response = new CourseCardsResponse
                {
                    LeadingCourse = leadingCourse?.Course ?? "N/A",

                    DemandByCourse = courseDemand,

                    SpaStudentSegment = new SpaStudentSegment
                    {
                        Total = spaTotal,
                        Paid = spaPaid,
                        DiscountGiven = 0
                    },

                    RevenueByCourse = revenueByCourse
                };

                return Ok(response);
            }
            catch (OperationCanceledException)
            {
                return BadRequest(new
                {
                    message = "Request was cancelled."
                });
            }
            catch (Exception)
            {
                return Problem(detail: "An unexpected error occurred while computing course cards.",
                    statusCode: 500);
            }
        }

        [HttpGet("course/list")]
        public async Task<ActionResult> GetCourseList(
        [FromQuery] string? type,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(type))
            {
                return BadRequest(new
                {
                    message = "Query parameter 'type' is required. Allowed: demand, spa, revenue."
                });
            }

            type = type.Trim().ToLowerInvariant();

            if (page < 1)
                page = 1;

            pageSize = Math.Clamp(pageSize, 1, 200);

            try
            {


                if (type == "demand")
                {
                    var query = _context.tb_OrderCourses
                        .AsNoTracking()
                        .GroupBy(oc => new
                        {
                            oc.CourseID,
                            oc.Course.Name
                        })
                        .Select(g => new
                        {
                            courseId = g.Key.CourseID,

                            course = g.Key.Name ?? "Unknown",

                            paid = g.Count(oc =>
                                oc.Order.Status != null &&
                                oc.Order.Status.ToLower() == OrderPaid &&
                                oc.Order.Payment != null &&
                                oc.Order.Payment.Status != null &&
                                oc.Order.Payment.Status.ToLower() == PaymentApproved),

                            pending = g.Count(oc =>
                                oc.Order.Status != null &&
                                oc.Order.Status.ToLower() == OrderPending),

                            total = g.Count()
                        })
                        .OrderByDescending(x => x.paid);

                    var total = await query.CountAsync(cancellationToken);

                    var items = await query
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToListAsync(cancellationToken);

                    return Ok(new
                    {
                        total,
                        page,
                        pageSize,
                        items
                    });
                }



                if (type == "spa")
                {
                    var query = _context.tb_Users
                        .AsNoTracking()
                        .Where(u =>
                            u.Email != null &&
                            u.Email.ToLower().EndsWith("@spa.ac.in"))
                        .Select(u => new
                        {
                            u.UserId,
                            u.Name,
                            u.Email,
                            u.Mobile,
                            u.CreatedAt,

                            Paid = _context.tb_Orders.Any(o =>
                                o.PayerUserId == u.UserId &&
                                o.Status != null &&
                                o.Status.ToLower() == OrderPaid &&
                                o.Payment != null &&
                                o.Payment.Status != null &&
                                o.Payment.Status.ToLower() == PaymentApproved)
                        })
                        .OrderByDescending(x => x.CreatedAt);

                    var total = await query.CountAsync(cancellationToken);

                    var items = await query
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToListAsync(cancellationToken);

                    return Ok(new
                    {
                        total,
                        page,
                        pageSize,
                        items
                    });
                }



                if (type == "revenue")
                {
                    var query = _context.tb_OrderCourses
                        .AsNoTracking()
                        .Where(oc =>
                            oc.Order.Status != null &&
                            oc.Order.Status.ToLower() == OrderPaid &&
                            oc.Order.Payment != null &&
                            oc.Order.Payment.Status != null &&
                            oc.Order.Payment.Status.ToLower() == PaymentApproved)
                        .GroupBy(oc => new
                        {
                            oc.CourseID,
                            oc.Course.Name
                        })
                        .Select(g => new
                        {
                            courseId = g.Key.CourseID,

                            course = g.Key.Name ?? "Unknown",

                            orders = g.Select(x => x.OrderId)
                                .Distinct()
                                .Count(),

                            revenue = Math.Round(
                                g.Sum(x => x.Order.TotalAmount),
                                2)
                        })
                        .OrderByDescending(x => x.revenue);

                    var total = await query.CountAsync(cancellationToken);

                    var items = await query
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToListAsync(cancellationToken);

                    return Ok(new
                    {
                        total,
                        page,
                        pageSize,
                        items
                    });
                }


                return BadRequest(new
                {
                    message =
                        "Invalid type. Allowed: demand, spa, revenue."
                });
            }
            catch (OperationCanceledException)
            {
                return BadRequest(new
                {
                    message = "Request was cancelled."
                });
            }
            catch (Exception)
            {
                return Problem(detail: "An unexpected error occurred while fetching course list.",
                    statusCode: 500);
            }
        }






        [HttpGet("group/cards")]
        public async Task<ActionResult<GroupSalesCardsResponse>> GetGroupCards(
       CancellationToken cancellationToken = default)
        {
            try
            {
                var paidGroupIds = await _context.tb_Orders
                    .AsNoTracking()
                    .Where(o =>
                        o.GroupId != null &&
                        o.Status != null &&
                        o.Status.ToLower() == OrderPaid &&
                        o.Payment != null &&
                        o.Payment.Status != null &&
                        o.Payment.Status.ToLower() == PaymentApproved)
                    .Select(o => o.GroupId!.Value)
                    .Distinct()
                    .ToListAsync(cancellationToken);


                var unpaidGroups = _context.tb_RegistrationGroups
                    .AsNoTracking()
                    .Where(g =>
                        !paidGroupIds.Contains(g.GroupId));

                var groupsCreatedNeverPaid =
                    await unpaidGroups.CountAsync(cancellationToken);

                var unpaidGroupMemberCount =
                    await _context.tb_RegistrationGroupMembers
                        .AsNoTracking()
                        .Where(m =>
                            !paidGroupIds.Contains(m.GroupId))
                        .CountAsync(cancellationToken);

                var oldestGroup = await unpaidGroups
                    .OrderBy(g => g.CreatedAt)
                    .Select(g => (DateTime?)g.CreatedAt)
                    .FirstOrDefaultAsync(cancellationToken);

                var oldestDays = 0;

                if (oldestGroup.HasValue)
                {
                    oldestDays = Math.Max(
                        0,
                        (int)(DateTime.UtcNow - oldestGroup.Value).TotalDays);
                }


                return Ok(new GroupSalesCardsResponse
                {
                    GroupsCreatedNeverPaid = groupsCreatedNeverPaid,
                    Members = unpaidGroupMemberCount,
                    OldestDays = oldestDays
                });
            }
            catch (OperationCanceledException)
            {
                return BadRequest(new
                {
                    message = "Request was cancelled."
                });
            }
            catch (Exception)
            {
                return Problem(
                    detail: "An unexpected error occurred while computing group sales cards.",
                    statusCode: 500);
            }
        }

        [HttpGet("group/list")]
        public async Task<ActionResult> GetGroupList(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            CancellationToken cancellationToken = default)
        {
            if (page < 1)
                page = 1;

            pageSize = Math.Clamp(pageSize, 1, 200);

            try
            {

                var paidGroupIds = _context.tb_Orders
                    .AsNoTracking()
                    .Where(o =>
                        o.GroupId != null &&
                        o.Status != null &&
                        o.Status.ToLower() == OrderPaid &&
                        o.Payment != null &&
                        o.Payment.Status != null &&
                        o.Payment.Status.ToLower() == PaymentApproved)
                    .Select(o => o.GroupId!.Value);

                var query = _context.tb_RegistrationGroups
                    .AsNoTracking()
                    .Where(g =>
                        !paidGroupIds.Contains(g.GroupId))
                    .Select(g => new
                    {
                        groupId = g.GroupId,

                        groupCode = g.GroupCode,

                        status = g.Status,

                        createdAt = g.CreatedAt,

                        members = _context.tb_RegistrationGroupMembers
                            .Count(m => m.GroupId == g.GroupId),

                        ageDays =
                            (int)(DateTime.UtcNow - g.CreatedAt).TotalDays
                    })
                    .OrderByDescending(x => x.createdAt);


                var total = await query.CountAsync(cancellationToken);


                var items = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);


                return Ok(new
                {
                    total,
                    page,
                    pageSize,
                    items
                });
            }
            catch (OperationCanceledException)
            {
                return BadRequest(new
                {
                    message = "Request was cancelled."
                });
            }
            catch (Exception)
            {
                return Problem(
                    detail: "An unexpected error occurred while fetching group list.",
                    statusCode: 500);
            }
        }

        [HttpGet("graph")]
        public async Task<ActionResult> GetGraphData(CancellationToken cancellationtoken)
        {

            DateTime today = DateTime.Now;
            DateTime lastsixthday = DateTime.Now.AddDays(-6);
            var graphData_revenue = await _context.tb_Orders
                .AsNoTracking()
                .Where(o =>
                    o.Status != null &&
                    o.Status.ToLower() == OrderPaid &&
                    o.Payment != null &&
                    o.Payment.Status != null &&
                    o.Payment.Status.ToLower() == PaymentApproved &&
                    o.CreatedAt >= lastsixthday && o.CreatedAt <= today
                    )
                .GroupBy(g => g.CreatedAt.Date)
                .Select(g => new
                {
                    CreatedAt = g.Key,
                    TotalRevenue = g.Sum(x => x.TotalAmount),
                })
                .OrderBy(x => x.CreatedAt)
                .ToListAsync(cancellationtoken);


            var graphData_registration = await _context.tb_Users
               .AsNoTracking()
               .Where(o =>
               (o.EmailVerified == true || o.MobileVerified == true) &&
                   o.CreatedAt >= lastsixthday && o.CreatedAt <= today
                   )
               .GroupBy(g => g.CreatedAt.Date)
               .Select(g => new
               {
                   CreatedAt = g.Key,
                   TotalRegistration = g.Count()
               })
               .OrderBy(x => x.CreatedAt)
               .ToListAsync(cancellationtoken);


            return Ok(new
            {
                revenue = graphData_revenue,
                registrations = graphData_registration

            });
        }

        public class RegistrationRevenueCardsResponse
        {
            public TodayRegistrationCard TodayRegistration { get; set; } = new();

            public TodayRevenueCard TodayRevenue { get; set; } = new();

            public TotalRegistrationCard TotalRegistration { get; set; } = new();

            public TotalRevenueCard TotalRevenue { get; set; } = new();
        }


        // ============================================================
        // TODAY REGISTRATION
        // ============================================================

        public class TodayRegistrationCard
        {
            public int Total { get; set; }

            public List<RegistrationProviderItem> ByProvider { get; set; } = new();
        }


        // ============================================================
        // REGISTRATION PROVIDER
        // ============================================================

        public class RegistrationProviderItem
        {
            public string Provider { get; set; } = string.Empty;

            public int Count { get; set; }
        }


        // ============================================================
        // TODAY REVENUE
        // ============================================================

        public class TodayRevenueCard
        {
            public decimal Total { get; set; }

            public decimal WithoutGst { get; set; }

            public List<CourseRevenueItem> ByCourse { get; set; } = new();
        }


        // ============================================================
        // TOTAL REGISTRATION
        // ============================================================

        public class TotalRegistrationCard
        {
            public int Total { get; set; }

            public List<RegistrationProviderItem> ByProvider { get; set; } = new();
        }


        // ============================================================
        // TOTAL REVENUE
        // ============================================================

        public class TotalRevenueCard
        {
            public decimal Total { get; set; }

            public decimal WithoutGst { get; set; }

            public List<CourseRevenueItem> ByCourse { get; set; } = new();
        }


        // ============================================================
        // COURSE REVENUE
        // ============================================================

        public class CourseRevenueItem
        {
            public int? CourseId { get; set; }

            public string Course { get; set; } = string.Empty;

            public decimal Revenue { get; set; }
        }
        public class CourseCardsResponse
        {
            public string LeadingCourse { get; set; } = string.Empty;

            public List<CourseDemandItem> DemandByCourse { get; set; } = new();

            public SpaStudentSegment SpaStudentSegment { get; set; } = new();

            public List<CourseRevenueItem> RevenueByCourse { get; set; } = new();
        }


        public class CourseDemandItem
        {
            public int? CourseId { get; set; }

            public string Course { get; set; } = string.Empty;

            public int Paid { get; set; }

            public int Pending { get; set; }
        }


        public class SpaStudentSegment
        {
            public int Total { get; set; }

            public int Paid { get; set; }

            public decimal DiscountGiven { get; set; }
        }





        public class GroupSalesCardsResponse
        {
            public int GroupsCreatedNeverPaid { get; set; }

            public int Members { get; set; }

            public int OldestDays { get; set; }
        }


        public class RegistrationCardsResponse
        {

            public decimal RegistrationFunnelPercent { get; set; }
            public int Started { get; set; }
            public int Verified { get; set; }
            public int Accounts { get; set; }

            // Signup-to-purchase
            public decimal SignupToPurchasePercent { get; set; }
            public int AccountsWithOrders { get; set; }
            public int AccountsPaid { get; set; }

            // Provider breakdown
            public List<ProviderBreakdownItem> ProviderBreakdown { get; set; } = new();
        }

        public class ProviderBreakdownItem
        {
            public string Provider { get; set; } = string.Empty;
            public int Count { get; set; }
            public decimal Percent { get; set; }
        }
        public class PagedListResponse<T>
        {
            public int Total { get; init; }
            public int Page { get; init; }
            public int PageSize { get; init; }
            public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
        }

        public class ProviderWithSamples
        {
            public string Provider { get; set; } = string.Empty;
            public int Count { get; set; }
            public decimal Percent { get; set; }
            public IEnumerable<object> Samples { get; set; } = Enumerable.Empty<object>();
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