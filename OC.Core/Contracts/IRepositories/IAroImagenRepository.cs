using System.Collections.Generic;
using System.Threading.Tasks;
using OC.Core.Domain.Entities;

namespace OC.Core.Contracts.IRepositories
{
    /// <summary>
    /// Acceso a AroImagen. Mismo contrato que IProductoImagenRepository,
    /// parametrizado para Aro.
    /// </summary>
    public interface IAroImagenRepository
    {
        Task<List<AroImagen>> GetActivasByAroIdAsync(int aroId);
        Task<List<AroImagen>> GetAllByAroIdAsync(int aroId);
        Task<AroImagen?> GetPrincipalAsync(int aroId);
        Task<int> CountActivasAsync(int aroId);
        Task MarcarPrincipalAsync(int imagenId);
        Task AddAsync(AroImagen imagen);
        Task DeleteAsync(int imagenId);
        Task SoftDeleteAsync(int imagenId);
        Task RestoreAsync(int imagenId);
        Task UpdateOrdenAsync(int imagenId, int orden);
        Task<AroImagen?> GetByIdAsync(int imagenId);
    }
}
