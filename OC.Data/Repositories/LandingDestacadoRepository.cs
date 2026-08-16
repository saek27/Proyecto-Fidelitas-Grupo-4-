using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OC.Core.Contracts.IRepositories;
using OC.Core.Domain.Entities;
using OC.Data.Context;

namespace OC.Data.Repositories
{
    public class LandingDestacadoRepository : ILandingDestacadoRepository
    {
        private readonly AppDbContext _context;

        public LandingDestacadoRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<LandingItemDestacado>> GetAllAsync()
        {
            return await _context.Set<LandingItemDestacado>()
                .AsNoTracking()
                .Include(d => d.Producto)
                .Include(d => d.Aro)
                .OrderBy(d => d.Posicion)
                .ToListAsync();
        }

        public async Task<int> CountAsync()
        {
            return await _context.Set<LandingItemDestacado>().CountAsync();
        }

        public async Task AddAsync(LandingItemDestacado item)
        {
            await _context.Set<LandingItemDestacado>().AddAsync(item);
            await _context.SaveChangesAsync();
        }

        public async Task ReplaceAllAsync(List<LandingItemInput> items)
        {
            if (items == null) items = new List<LandingItemInput>();
            if (items.Count > 8)
                throw new System.InvalidOperationException("Destacados admite máximo 8 items.");

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var existentes = await _context.Set<LandingItemDestacado>().ToListAsync();
                if (existentes.Any())
                    _context.Set<LandingItemDestacado>().RemoveRange(existentes);

                byte pos = 1;
                foreach (var input in items)
                {
                    if (string.IsNullOrWhiteSpace(input.Tipo)) continue;
                    if (input.Tipo != "Producto" && input.Tipo != "Aro") continue;

                    var entity = new LandingItemDestacado
                    {
                        Posicion = pos++,
                        Tipo = input.Tipo,
                        ProductoId = input.Tipo == "Producto" ? input.Id : null,
                        AroId = input.Tipo == "Aro" ? input.Id : null
                    };
                    await _context.Set<LandingItemDestacado>().AddAsync(entity);
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