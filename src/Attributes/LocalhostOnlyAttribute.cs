using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Net;

namespace VehicleMaintenanceInvoiceSystem.Attributes;

/// <summary>
/// Authorization filter that restricts access to localhost only
/// Used to secure internal API endpoints from external access
/// </summary>
public class LocalhostOnlyAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var remoteIp = context.HttpContext.Connection.RemoteIpAddress;
        
        // Allow localhost access (IPv4 and IPv6)
        if (remoteIp != null && 
            !IPAddress.IsLoopback(remoteIp) && 
            !remoteIp.Equals(IPAddress.Parse("127.0.0.1")) &&
            !remoteIp.Equals(IPAddress.Parse("::1")))
        {
            context.Result = new ForbidResult("Access denied - Internal API endpoints are restricted to localhost only");
            return;
        }
        
        base.OnActionExecuting(context);
    }
}
