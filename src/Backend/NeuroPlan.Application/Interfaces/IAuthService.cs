using System.Threading.Tasks;
using NeuroPlan.Application.DTOs;

namespace NeuroPlan.Application.Interfaces;

public interface IAuthService
{
    Task<AuthenticatedUserDto?> AuthenticateAsync(LoginRequestDto request);
}
