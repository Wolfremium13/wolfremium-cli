using System.Threading.Tasks;
using LanguageExt;
using LanguageExt.Common;
using Wolfremium.Cli.Database.Configuration.Load.Domain.Models;

namespace Wolfremium.Cli.Database.Configuration.Load.Domain.Ports;

public interface IConfigurationLoader
{
    Task<Either<Error, ApplicationConfiguration>> LoadAsync();
}
