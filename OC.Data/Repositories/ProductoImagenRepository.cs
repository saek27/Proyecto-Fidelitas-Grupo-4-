using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OC.Core.Contracts.IRepositories;
using OC.Core.Domain.Entities;
using OC.Data.Context;

namespace OC.Data.Repositories
{
    public class ProductoImagenRepository : IProductoImagenRepository
    {
        private readonly AppDbContext _context;

        public ProductoImagenRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<ProductoImagen>> GetActivasByProductoIdAsync(int productoId)
        {
            return await _context.ProductoImagenes
                .AsNoTracking()
                .Where(i => i.ProductoId == productoId && i.Activo)
                .OrderByDescending(i => i.EsPrincipal)
                .ThenBy(i => i.Orden)
                .ThenBy(i => i.Id)
                .ToListAsync();
        }

        public async Task<List<ProductoImagen>> GetAllByProductoIdAsync(int productoId)
        {
            return await _context.ProductoImagenes
                .AsNoTracking()
                .Where(i => i.ProductoId == productoId)
                .OrderByDescending(i => i.Activo)
                .ThenByDescending(i => i.EsPrincipal)
                .ThenBy(i => i.Orden)
                .ThenBy(i => i.Id)
                .ToListAsync();
        }

        public async Task<ProductoImagen?> GetPrincipalAsync(int productoId)
        {
            return await _context.ProductoImagenes
                .AsNoTracking()
                .Where(i => i.ProductoId == productoId && i.Activo && i.EsPrincipal)
                .FirstOrDefaultAsync();
        }

        public async Task<int> CountActivasAsync(int productoId)
        {
            return await _context.ProductoImagenes
                .AsNoTracking()
                .CountAsync(i => i.ProductoId == productoId && i.Activo);
        }

        public async Task MarcarPrincipalAsync(int imagenId)
        {
            var imagen = await _context.ProductoImagenes
                .FirstOrDefaultAsync(i => i.Id == imagenId);
            if (imagen == null) return;

            // Desmarcar las demás del mismo producto
            var otras = await _context.ProductoImagenes
                .Where(i => i.ProductoId == imagen.ProductoId && i.Id != imagenId && i.EsPrincipal)
                .ToListAsync();
            foreach (var o in otras) o.EsPrincipal = false;

            imagen.EsPrincipal = true;
            await _context.SaveChangesAsync();
        }

        public async Task AddAsync(ProductoImagen imagen)
        {
            await _context.ProductoImagenes.AddAsync(imagen);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int imagenId)
        {
            var imagen = await _context.ProductoImagenes
                .FirstOrDefaultAsync(i => i.Id == imagenId);
            if (imagen == null) return;

            _context.ProductoImagenes.Remove(imagen);
            await _context.SaveChangesAsync();
        }

        public async Task SoftDeleteAsync(int imagenId)
        {
            var imagen = await _context.ProductoImagenes
                .FirstOrDefaultAsync(i => i.Id == imagenId);
            if (imagen == null) return;

            imagen.Activo = false;
            imagen.EsPrincipal = false; // ya no puede ser principal si está inactiva
            await _context.SaveChangesAsync();
        }

        public async Task RestoreAsync(int imagenId)
        {
            var imagen = await _context.ProductoImagenes
                .FirstOrDefaultAsync(i => i.Id == imagenId);
            if (imagen == null) return;

            imagen.Activo = true;
            await _context.SaveChangesAsync();
        }

        public async Task UpdateOrdenAsync(int imagenId, int orden)
        {
            var imagen = await _context.ProductoImagenes
                .FirstOrDefaultAsync(i => i.Id == imagenId);
            if (imagen == null) return;

            imagen.Orden = orden;
            await _context.SaveChangesAsync();
        }

        public async Task<ProductoImagen?> GetByIdAsync(int imagenId)
        {
            return await _context.ProductoImagenes
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == imagenId);
        }
    }
}
