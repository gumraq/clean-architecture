using Cargo.Contract;
using Cargo.Infrastructure.Data;
using IDeal.Common.Components;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Text;

namespace Cargo.ServiceHost
{
    public class MessageInjectorAttribute : ActionFilterAttribute
	{
        public override void OnActionExecuting(ActionExecutingContext context)
		{
			base.OnActionExecuting(context);
			try
			{
				string authHeader = context.HttpContext.Request.Headers["Authorization"];
				if (authHeader != null && authHeader.StartsWith("Bearer"))
				{
					var identity = (ClaimsIdentity)context.HttpContext.User.Identity;
					var authenticatedMessageObj = context.ActionArguments.Values
						.SingleOrDefault(a => a.GetType().GetInterfaces().Any(t => t == typeof(IAuthenticatedMessage)));
					if (authenticatedMessageObj is IAuthenticatedMessage authenticatedMessage)
					{
						Claim claim = identity.Claims.FirstOrDefault(c => c.Type == "contragentId");
						if (claim != null && !string.IsNullOrEmpty(claim.Value))
						{
							int agentId = 0;
							if (int.TryParse(claim.Value, out agentId))
							{
								authenticatedMessage.AgentId ??= agentId;
							}
						}

						claim = identity.Claims.FirstOrDefault(c => c.Type == "customerId");
						int customerId = 55;
						if (claim != null && !string.IsNullOrEmpty(claim.Value))
						{
							if (int.TryParse(claim.Value, out customerId))
							{
								authenticatedMessage.CustomerId = customerId;
							}
						}
					}
				}
			}
			catch
			{ }
		}
	}
}
