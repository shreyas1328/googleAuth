using GoogleAuth.Data.Interface;
using GoogleAuth.Data.Model.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace GoogleAuth.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IAuthenticationRepository _authenticationRepository;

        public UserController(IAuthenticationRepository authenticationRepository)
        {
            _authenticationRepository = authenticationRepository;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> SocialLogin([FromBody] LoginRequest request)
        {
            var result = await _authenticationRepository.SocialLogin(request);

            var resultDto = result.ToResult();

            if (!resultDto.IsSuccess)
            {
                return BadRequest(resultDto);
            }
            return Ok(resultDto);
        }

    }
}

