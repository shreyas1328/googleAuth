using System;
using FluentResults;
using GoogleAuth.Data.Model.Request;

namespace GoogleAuth.Data.Interface
{
	public interface IAuthenticationRepository
	{
        Task<Result<string>> SocialLogin(LoginRequest request);
    }
}

