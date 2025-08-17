using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace GymManagement.Web.Authorization
{
    /// <summary>
    /// Defines the operations that can be performed on Booking resources
    /// </summary>
    public static class BookingOperations
    {
        public static OperationAuthorizationRequirement Create = 
            new OperationAuthorizationRequirement { Name = nameof(Create) };
            
        public static OperationAuthorizationRequirement Read = 
            new OperationAuthorizationRequirement { Name = nameof(Read) };
            
        public static OperationAuthorizationRequirement Update = 
            new OperationAuthorizationRequirement { Name = nameof(Update) };
            
        public static OperationAuthorizationRequirement Delete = 
            new OperationAuthorizationRequirement { Name = nameof(Delete) };
            
        public static OperationAuthorizationRequirement Cancel = 
            new OperationAuthorizationRequirement { Name = nameof(Cancel) };
            
        public static OperationAuthorizationRequirement ViewAll = 
            new OperationAuthorizationRequirement { Name = nameof(ViewAll) };
    }
}
