using System.Collections.Generic;
using System.Threading.Tasks;
using OC.Core.Domain.Entities;

namespace OC.Core.Contracts.IRepositories
{
    /// <summary>
    /// Acceso a LandingItemDestacado. Cada fila = un slot fijo (1..8) de la sección destacados.
    /// Mismo patrón que el carrusel pero para 8 slots.
    /// </summary>
    public interface ILandingDestacadoRepository
    {
        /// <summary>Devuelve los slots de destacados (vacíos no incluidos), con Producto/Aro eager-loaded, ordenados por Posicion.</summary>
        Task<List<LandingItemDestacado>> GetAllAsync();

        /// <summary>Cantidad de slots actualmente ocupados.</summary>
        Task<int> CountAsync();

        /// <summary>
        /// Reemplaza TODOS los slots de destacados con la lista recibida.
        /// items puede tener hasta 8 elementos. Posicion se asigna 1..items.Count.
        /// Operación atómica.
        /// </summary>
        Task ReplaceAllAsync(List<LandingItemInput> items);

        /// <summary>Persiste un slot individual (uso interno / seed).</summary>
        Task AddAsync(LandingItemDestacado item);
    }
}