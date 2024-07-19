using Microsoft.AspNetCore.Mvc.Filters;

namespace Auction.Service.Filters;

public class ExceptionsFilter: IActionFilter
{
	public void OnActionExecuting(ActionExecutingContext context)
	{
		throw new NotImplementedException();
	}

	public void OnActionExecuted(ActionExecutedContext context)
	{
		throw new NotImplementedException();
	}
}