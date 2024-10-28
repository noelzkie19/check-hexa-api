using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CheckHexaApi.Controllers
{
    /// <summary>
    /// Handles user authentication tasks.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]

    public class AuthenticateController : ControllerBase
    {
        /// <summary>
        /// Handles user authentication tasks.
        /// </summary>
        public AuthenticateController()
        {

        }
        /// <summary>
        /// Check Authentication
        /// </summary>
        /// <response code="400">Bad Request - Invalid Request</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="500">HTTP Internal Server Error</response>
        [HttpGet("check")]
        public IActionResult CheckAuthentication()
        {
            return Ok(new { IsAuthenticated = User.Identity.IsAuthenticated });
        }
        /// <summary>
        /// CIDA Callback
        /// </summary>
        /// <response code="400">Bad Request - Invalid Request</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="500">HTTP Internal Server Error</response>
        [AllowAnonymous]
        [HttpGet("auth/callback")]
        public async Task<IActionResult> Callback(string returnUrl = null)
        {
            var result = await HttpContext.AuthenticateAsync("oidc");
            if (!result.Succeeded)
            {
                return Redirect("https://webqa.mbtcheck.com/CIDA"); // Handle failure
            }

            // Sign in the user and create a session
            await HttpContext.SignInAsync(result.Principal);

            // Check if ReturnUrl is valid and not an external URL
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl); // Redirect to the return URL
            }

            return Redirect("/");
        }
    }
}
