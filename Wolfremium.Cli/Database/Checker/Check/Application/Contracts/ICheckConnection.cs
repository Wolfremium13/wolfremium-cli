using System.Threading.Tasks;
using LanguageExt;
using LanguageExt.Common;
using Wolfremium.Cli.Database.Checker.Check.Domain.Models;

namespace Wolfremium.Cli.Database.Checker.Check.Application.Contracts;

public interface ICheckConnection
{
    Task<Either<Error, string>> ExecuteAsync(ConnectionCheckRequest request);
}

public record ConnectionCheckRequest(
    ConnectionSettings Settings
);
