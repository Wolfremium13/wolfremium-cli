using System.Threading.Tasks;
using LanguageExt;
using LanguageExt.Common;
using Wolfremium.Cli.Database.Configuration.Load.Application.Contracts;
using Wolfremium.Cli.Database.Configuration.Load.Domain.Models;
using Wolfremium.Cli.Database.Configuration.Load.Domain.Ports;

namespace Wolfremium.Cli.Database.Configuration.Load.Application.UseCases;

public class LoadConfigurationUseCase : ILoadConfiguration
{
    private readonly IConfigurationLoader _configurationLoader;

    public LoadConfigurationUseCase(IConfigurationLoader configurationLoader)
    {
        _configurationLoader = configurationLoader;
    }

    public async Task<Either<Error, ApplicationConfiguration>> ExecuteAsync(ConfigurationLoadRequest request)
    {
        return await _configurationLoader.LoadAsync();
    }
}
