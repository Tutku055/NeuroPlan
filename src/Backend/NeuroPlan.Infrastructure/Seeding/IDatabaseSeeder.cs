using System.Threading;
using System.Threading.Tasks;

namespace NeuroPlan.Infrastructure.Seeding;

public interface IDatabaseSeeder
{
    Task SeedAsync(bool isDevelopment, CancellationToken cancellationToken = default);
}
