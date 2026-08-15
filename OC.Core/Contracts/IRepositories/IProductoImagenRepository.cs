using System.Collections.Generic;
using System.Threading.Tasks;
using OC.Core.Domain.Entities;

namespace OC.Core.Contracts.IRepositories
{
    /// <summary>
    /// Acceso a ProductoImagen. Centraliza las consultas que el genérico
    /// no cubre: listado ordenado por principal+orden, conteo de activas,
    /// marcado de principal y soft/hard delete.
    /// </summary>
    public interface IProductoImagenRepository
    {
        /// <summary>
        /// Devuelve las imágenes ACTIVAS de un producto, ordenadas con la
        /// principal primero y el resto por Orden ascendente.
        /// </summary>
        Task<List<ProductoImagen>> GetActivasByProductoIdAsync(int productoId);

        /// <summary>
        /// Devuelve TODAS las imágenes del producto (activas e inactivas),
        /// ordenadas por Activo DESC, EsPrincipal DESC, Orden ASC, Id ASC.
        /// Útil para la pantalla de gestión de imágenes.
        /// </summary>
        Task<List<ProductoImagen>> GetAllByProductoIdAsync(int productoId);

        /// <summary>Imagen marcada como principal del producto (o null si no hay).</summary>
        Task<ProductoImagen?> GetPrincipalAsync(int productoId);

        /// <summary>Cantidad de imágenes activas del producto.</summary>
        Task<int> CountActivasAsync(int productoId);

        /// <summary>Marca una imagen como EsPrincipal=1 y desmarca las demás del mismo producto.</summary>
        Task MarcarPrincipalAsync(int imagenId);

        /// <summary>Persiste una nueva imagen.</summary>
        Task AddAsync(ProductoImagen imagen);

        /// <summary>Elimina físicamente una imagen (hard delete) — usado solo desde /Inventory/GestionImagenes.</summary>
        Task DeleteAsync(int imagenId);

        /// <summary>Soft delete: marca Activo=false.</summary>
        Task SoftDeleteAsync(int imagenId);

        /// <summary>Reactivar una imagen inactiva (Activo=true).</summary>
        Task RestoreAsync(int imagenId);

        /// <summary>Actualiza el Orden de una imagen.</summary>
        Task UpdateOrdenAsync(int imagenId, int orden);

        /// <summary>Busca una imagen por Id.</summary>
        Task<ProductoImagen?> GetByIdAsync(int imagenId);
    }
}
