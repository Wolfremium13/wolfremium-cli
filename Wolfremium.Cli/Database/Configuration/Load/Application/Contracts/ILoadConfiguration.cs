using System.Threading.Tasks;
using LanguageExt;
using LanguageExt.Common;
using Wolfremium.Cli.Database.Configuration.Load.Domain.Models;

namespace Wolfremium.Cli.Database.Configuration.Load.Application.Contracts;

public interface ILoadConfiguration
{
    Task<Either<Error, ApplicationConfiguration>> ExecuteAsync(ConfigurationLoadRequest request);
}

public record ConfigurationLoadRequest();
