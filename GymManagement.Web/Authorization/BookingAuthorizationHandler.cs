using GymManagement.Web.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using System.Security.Claims;

namespace GymManagement.Web.Authorization
{
    /// <summary>
    /// Authorization handler for Booking resources
    /// Implements resource-based authorization to ensure users can only access their own bookings
    /// </summary>
    public class BookingAuthorizationHandler : AuthorizationHandler<OperationAuthorizationRequirement, Booking>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            OperationAuthorizationRequirement requirement,
            Booking booking)
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId))
            {
                return Task.CompletedTask; // Fail - no user ID
            }

            // Admin can do everything
            if (context.User.IsInRole("Admin"))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            // Handle different operations
            switch (requirement.Name)
            {
                case nameof(BookingOperations.ViewAll):
                    // Only Admin and Trainer can view all bookings
                    if (context.User.IsInRole("Trainer"))
                    {
                        context.Succeed(requirement);
                    }
                    break;

                case nameof(BookingOperations.Read):
                case nameof(BookingOperations.Update):
                case nameof(BookingOperations.Cancel):
                    // Members can only access their own bookings
                    if (context.User.IsInRole("Member"))
                    {
                        if (IsOwner(context.User, booking))
                        {
                            context.Succeed(requirement);
                        }
                    }
                    // Trainers can read bookings for classes they teach
                    else if (context.User.IsInRole("Trainer") && requirement.Name == nameof(BookingOperations.Read))
                    {
                        // TODO: Add logic to check if trainer owns the class
                        // For now, allow trainers to read all bookings
                        context.Succeed(requirement);
                    }
                    break;

                case nameof(BookingOperations.Create):
                    // Members can create bookings for themselves
                    if (context.User.IsInRole("Member"))
                    {
                        if (IsOwner(context.User, booking))
                        {
                            context.Succeed(requirement);
                        }
                    }
                    break;

                case nameof(BookingOperations.Delete):
                    // Only Admin can delete bookings
                    // Members and Trainers should use Cancel instead
                    break;

                default:
                    // Unknown operation - fail
                    break;
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Checks if the current user is the owner of the booking
        /// </summary>
        private static bool IsOwner(ClaimsPrincipal user, Booking booking)
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId))
                return false;

            // Check if the booking belongs to the current user
            // We need to compare with the TaiKhoanId from the booking's ThanhVien
            return booking.ThanhVien?.TaiKhoan?.Id == userId;
        }
    }

    /// <summary>
    /// Authorization handler for operations that don't require a specific booking resource
    /// </summary>
    public class BookingOperationAuthorizationHandler : AuthorizationHandler<OperationAuthorizationRequirement>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            OperationAuthorizationRequirement requirement)
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId))
            {
                return Task.CompletedTask; // Fail - no user ID
            }

            // Admin can do everything
            if (context.User.IsInRole("Admin"))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            // Handle different operations
            switch (requirement.Name)
            {
                case nameof(BookingOperations.ViewAll):
                    // Only Admin and Trainer can view all bookings
                    if (context.User.IsInRole("Trainer"))
                    {
                        context.Succeed(requirement);
                    }
                    break;

                case nameof(BookingOperations.Create):
                    // Members can create bookings
                    if (context.User.IsInRole("Member"))
                    {
                        context.Succeed(requirement);
                    }
                    break;

                default:
                    // For other operations, require a specific resource
                    break;
            }

            return Task.CompletedTask;
        }
    }
}
