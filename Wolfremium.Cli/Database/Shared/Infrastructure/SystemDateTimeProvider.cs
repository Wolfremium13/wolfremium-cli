using System;
using Wolfremium.Cli.Database.Shared.Domain.Ports;

namespace Wolfremium.Cli.Database.Shared.Infrastructure;

public class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime Now => DateTime.Now;
    public DateTime UtcNow => DateTime.UtcNow;
}
