using System.Threading.Tasks;
using LanguageExt;
using LanguageExt.Common;
using Wolfremium.Cli.Database.Checker.Check.Application.Contracts;
using Wolfremium.Cli.Database.Checker.Check.Domain.Ports;

namespace Wolfremium.Cli.Database.Checker.Check.Application.UseCases;

public class CheckConnectionUseCase : ICheckConnection
{
    private readonly IConnectionCheckerService _connectionCheckerService;

    public CheckConnectionUseCase(IConnectionCheckerService connectionCheckerService)
    {
        _connectionCheckerService = connectionCheckerService;
    }

    public async Task<Either<Error, string>> ExecuteAsync(ConnectionCheckRequest request)
    {
        return await _connectionCheckerService.CheckConnectionAsync(request.Settings);
    }
}
