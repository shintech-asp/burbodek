using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace burbodek.Filters
{
    public class RedirectIfAuthenticatedAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.HttpContext.User.Identity?.IsAuthenticated == true)
            {
                var user = context.HttpContext.User;

                if (user.IsInRole("Admin"))
                    context.Result = new RedirectToActionResult("Index", "Home", null);
                else if (user.IsInRole("Client"))
                    context.Result = new RedirectToActionResult("Index", "Employee", null);
                else if (user.IsInRole("Employer"))
                    context.Result = new RedirectToActionResult("Index", "Employer", null);
            }

            base.OnActionExecuting(context);
        }
    }
}