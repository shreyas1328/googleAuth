using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentResults;
using Google.Apis.Auth;
using Google.Apis.Http;
using GoogleAuth.Data.Interface;
using GoogleAuth.Data.Model;
using GoogleAuth.Data.Model.Entity;
using GoogleAuth.Data.Model.Request;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace GoogleAuth.Data.Repository
{
	public class AuthenticationRepository: IAuthenticationRepository
    {
        private readonly UserManager<User> _userManager;
        private readonly IConfiguration _configuration;

        public AuthenticationRepository(
        UserManager<User> userManager,
        IConfiguration configuration)
        {
            _userManager = userManager;
            _configuration = configuration;
        }

        public async Task<Result<string>> SocialLogin(LoginRequest request)
        {
            var tokenValidationResult = await ValidateSocialToken(request);

            if (tokenValidationResult.IsFailed)
            {
                return tokenValidationResult;
            }

            var user = await _userManager.FindByEmailAsync(request.Email);

            if (user is null)
            {
                var registerResult = await RegisterSocialUser(request);

                if (registerResult.IsFailed)
                {
                    return tokenValidationResult;
                }

                user = registerResult.Value;
            }

            if (user.Provider != request.Provider)
            {
                return Result.Fail($"User was registered via {user.Provider} and cannot be logged via {request.Provider}.");
            }

            var token = GetToken(await GetClaims(user));

            return Result.Ok(new JwtSecurityTokenHandler().WriteToken(token));
        }

        private string GetErrorsText(IEnumerable<IdentityError> errors)
        {
            return string.Join(", ", errors.Select(error => error.Description).ToArray());
        }

        private JwtSecurityToken GetToken(IEnumerable<Claim> authClaims)
        {
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));

            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:ValidIssuer"],
                audience: _configuration["JWT:ValidAudience"],
                expires: DateTime.Now.AddHours(3),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256));

            return token;
        }

        private async Task<Result> ValidateSocialToken(LoginRequest request)
        {
            return request.Provider switch
            {
                //Consts.LoginProviders.Facebook => await ValidateFacebookToken(request),
                Consts.LoginProviders.Google => await ValidateGoogleToken(request),
                _ => Result.Fail($"{request.Provider} provider is not supported.")
            };
        }

        private async Task<Result> ValidateGoogleToken(LoginRequest request)
        {
            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new List<string> { _configuration["SocialLogin:Google:TokenAudience"] }
                };
                var result = await GoogleJsonWebSignature.ValidateAsync(request.AccessToken, settings);

            }
            catch (InvalidJwtException _)
            {
                return Result.Fail($"{request.Provider} access token is not valid.");
            }

            return Result.Ok();
        }

        private async Task<Result<User>> RegisterSocialUser(LoginRequest request)
        {
            var user = new User()
            {
                Email = request.Email,
                UserName = request.Email,
                SecurityStamp = Guid.NewGuid().ToString(),
                Provider = request.Provider!
            };

            var result = await _userManager.CreateAsync(user, $"Pass!1{Guid.NewGuid().ToString()}");

            if (!result.Succeeded)
            {
                return Result.Fail($"Unable to register user {request.Email}, errors: {GetErrorsText(result.Errors)}");
            }

            await _userManager.AddToRoleAsync(user, Role.User);

            return Result.Ok(user);
        }

        private async Task<List<Claim>> GetClaims(User user)
        {
            var authClaims = new List<Claim>
        {
            new(ClaimTypes.Name, user.Email!),
            new(ClaimTypes.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

            var userRoles = await _userManager.GetRolesAsync(user);

            if (userRoles is not null && userRoles.Any())
            {
                authClaims.AddRange(userRoles.Select(userRole => new Claim(ClaimTypes.Role, userRole)));
            }

            return authClaims;
        }
    }
}

