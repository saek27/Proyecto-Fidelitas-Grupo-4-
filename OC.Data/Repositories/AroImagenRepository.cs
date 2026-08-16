using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OC.Core.Contracts.IRepositories;
using OC.Core.Domain.Entities;
using OC.Data.Context;

namespace OC.Data.Repositories
{
    public class AroImagenRepository : IAroImagenRepository
    {
        private readonly AppDbContext _context;

        public AroImagenRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<AroImagen>> GetActivasByAroIdAsync(int aroId)
        {
            return await _context.AroImagenes
                .AsNoTracking()
                .Where(i => i.AroId == aroId && i.Activo)
                .OrderByDescending(i => i.EsPrincipal)
                .ThenBy(i => i.Orden)
                .ThenBy(i => i.Id)
                .ToListAsync();
        }

        public async Task<List<AroImagen>> GetAllByAroIdAsync(int aroId)
        {
            return await _context.AroImagenes
                .AsNoTracking()
                .Where(i => i.AroId == aroId)
                .OrderByDescending(i => i.Activo)
                .ThenByDescending(i => i.EsPrincipal)
                .ThenBy(i => i.Orden)
                .ThenBy(i => i.Id)
                .ToListAsync();
        }

        public async Task<AroImagen?> GetPrincipalAsync(int aroId)
        {
            return await _context.AroImagenes
                .AsNoTracking()
                .Where(i => i.AroId == aroId && i.Activo && i.EsPrincipal)
                .FirstOrDefaultAsync();
        }

        public async Task<int> CountActivasAsync(int aroId)
        {
            return await _context.AroImagenes
                .AsNoTracking()
                .CountAsync(i => i.AroId == aroId && i.Activo);
        }

        public async Task MarcarPrincipalAsync(int imagenId)
        {
            var imagen = await _context.AroImagenes
                .FirstOrDefaultAsync(i => i.Id == imagenId);
            if (imagen == null) return;

            var otras = await _context.AroImagenes
                .Where(i => i.AroId == imagen.AroId && i.Id != imagenId && i.EsPrincipal)
                .ToListAsync();
            foreach (var o in otras) o.EsPrincipal = false;

            imagen.EsPrincipal = true;
            await _context.SaveChangesAsync();
        }

        public async Task AddAsync(AroImagen imagen)
        {
            await _context.AroImagenes.AddAsync(imagen);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int imagenId)
        {
            var imagen = await _context.AroImagenes
                .FirstOrDefaultAsync(i => i.Id == imagenId);
            if (imagen == null) return;

            _context.AroImagenes.Remove(imagen);
            await _context.SaveChangesAsync();
        }

        public async Task SoftDeleteAsync(int imagenId)
        {
            var imagen = await _context.AroImagenes
                .FirstOrDefaultAsync(i => i.Id == imagenId);
            if (imagen == null) return;

            imagen.Activo = false;
            imagen.EsPrincipal = false;
            await _context.SaveChangesAsync();
        }

        public async Task RestoreAsync(int imagenId)
        {
            var imagen = await _context.AroImagenes
                .FirstOrDefaultAsync(i => i.Id == imagenId);
            if (imagen == null) return;

            imagen.Activo = true;
            await _context.SaveChangesAsync();
        }

        public async Task UpdateOrdenAsync(int imagenId, int orden)
        {
            var imagen = await _context.AroImagenes
                .FirstOrDefaultAsync(i => i.Id == imagenId);
            if (imagen == null) return;

            imagen.Orden = orden;
            await _context.SaveChangesAsync();
        }

        public async Task<AroImagen?> GetByIdAsync(int imagenId)
        {
            return await _context.AroImagenes
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == imagenId);
        }
    }
}
