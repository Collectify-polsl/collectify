using Collectify.Model.Entities;

namespace Collectify.Model.Interfaces;

/// <summary>
/// Repository interface dedicated to <see cref="Template"/> entities.
/// </summary>
public interface ITemplateRepository : IRepository<Template>
{
    /// <summary>
    /// Asynchronously retrieves a template with its field definitions.
    /// </summary>
    /// <param name="id">Identifier of the template.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// Task that results in the template instance or null if not found.
    /// </returns>
    Task<Template?> GetWithFieldsAsync(int id, CancellationToken cancellationToken = default);
}