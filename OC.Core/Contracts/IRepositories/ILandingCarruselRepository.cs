using System.Collections.Generic;
using System.Threading.Tasks;
using OC.Core.Domain.Entities;

namespace OC.Core.Contracts.IRepositories
{
    /// <summary>
    /// Acceso a LandingItemCarrusel. Cada fila = un slot fijo (1..6) del carrusel principal.
    /// El admin reordena arrastrando, el repo hace ReplaceAll en una transacción.
    /// </summary>
    public interface ILandingCarruselRepository
    {
        /// <summary>Devuelve los slots del carrusel (vacíos no incluidos), con Producto/Aro eager-loaded, ordenados por Posicion.</summary>
        Task<List<LandingItemCarrusel>> GetAllAsync();

        /// <summary>Cantidad de slots actualmente ocupados.</summary>
        Task<int> CountAsync();

        /// <summary>
        /// Reemplaza TODOS los slots del carrusel con la lista recibida.
        /// items puede tener hasta 6 elementos. Posicion se asigna 1..items.Count.
        /// Operación atómica: dentro de una transacción, RemoveRange + AddRange + SaveChanges.
        /// </summary>
        Task ReplaceAllAsync(List<LandingItemInput> items);

        /// <summary>Persiste un slot individual (uso interno / seed).</summary>
        Task AddAsync(LandingItemCarrusel item);
    }

    /// <summary>DTO de entrada para repos de landing. Tipo="Producto"|"Aro", Id=Id del Producto o Aro.</summary>
    public record LandingItemInput(string Tipo, int Id);
}