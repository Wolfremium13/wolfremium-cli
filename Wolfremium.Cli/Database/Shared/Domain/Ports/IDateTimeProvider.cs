using System;

namespace Wolfremium.Cli.Database.Shared.Domain.Ports;

public interface IDateTimeProvider
{
    DateTime Now { get; }
    DateTime UtcNow { get; }
}
