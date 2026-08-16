using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OC.Core.Contracts.IRepositories;
using OC.Core.Domain.Entities;
using OC.Data.Context;

namespace OC.Data.Repositories
{
    public class LandingCarruselRepository : ILandingCarruselRepository
    {
        private readonly AppDbContext _context;

        public LandingCarruselRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<LandingItemCarrusel>> GetAllAsync()
        {
            return await _context.Set<LandingItemCarrusel>()
                .AsNoTracking()
                .Include(c => c.Producto)
                .Include(c => c.Aro)
                .OrderBy(c => c.Posicion)
                .ToListAsync();
        }

        public async Task<int> CountAsync()
        {
            return await _context.Set<LandingItemCarrusel>().CountAsync();
        }

        public async Task AddAsync(LandingItemCarrusel item)
        {
            await _context.Set<LandingItemCarrusel>().AddAsync(item);
            await _context.SaveChangesAsync();
        }

        public async Task ReplaceAllAsync(List<LandingItemInput> items)
        {
            if (items == null) items = new List<LandingItemInput>();
            if (items.Count > 6)
                throw new System.InvalidOperationException("El carrusel admite máximo 6 items.");

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Borrar todos los slots existentes
                var existentes = await _context.Set<LandingItemCarrusel>().ToListAsync();
                if (existentes.Any())
                    _context.Set<LandingItemCarrusel>().RemoveRange(existentes);

                // 2. Insertar nuevos con Posicion 1..N
                byte pos = 1;
                foreach (var input in items)
                {
                    if (string.IsNullOrWhiteSpace(input.Tipo)) continue;
                    if (input.Tipo != "Producto" && input.Tipo != "Aro") continue;

                    var entity = new LandingItemCarrusel
                    {
                        Posicion = pos++,
                        Tipo = input.Tipo,
                        ProductoId = input.Tipo == "Producto" ? input.Id : null,
                        AroId = input.Tipo == "Aro" ? input.Id : null
                    };
                    await _context.Set<LandingItemCarrusel>().AddAsync(entity);
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
    }
}