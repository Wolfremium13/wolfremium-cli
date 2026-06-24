using System.Threading.Tasks;
using LanguageExt;
using LanguageExt.Common;
using Wolfremium.Cli.Database.Checker.Check.Domain.Models;

namespace Wolfremium.Cli.Database.Checker.Check.Domain.Ports;

public interface IConnectionCheckerService
{
    Task<Either<Error, string>> CheckConnectionAsync(ConnectionSettings settings);
}
