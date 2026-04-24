using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OC.Core.Contracts.IRepositories;
using OC.Core.Domain.Entities;
using OC.Web.ViewModels;

namespace OC.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ReportesController : Controller
    {
        private readonly IGenericRepository<Venta> _ventaRepo;
        private readonly IGenericRepository<Cita> _citaRepo;
        private readonly IGenericRepository<Sucursal> _sucursalRepo;

        public ReportesController(
            IGenericRepository<Venta> ventaRepo,
            IGenericRepository<Cita> citaRepo,
            IGenericRepository<Sucursal> sucursalRepo)
        {
            _ventaRepo = ventaRepo;
            _citaRepo = citaRepo;
            _sucursalRepo = sucursalRepo;
        }

        public IActionResult Index() => View();

        // ── VENTAS ──────────────────────────────────────────────────────────

        public async Task<IActionResult> Ventas(DateTime? desde, DateTime? hasta, int? sucursalId)
        {
            try
            {
                var fechaDesde = desde ?? DateTime.Now.AddMonths(-1);
                var fechaHasta = (hasta ?? DateTime.Now).Date.AddDays(1).AddTicks(-1);

                var sucursales = (await _sucursalRepo.GetPagedAsync(1, 100)).Items.ToList();

                var raw = await _ventaRepo.GetPagedAsync(
                    1, 10000,
                    v => v.FechaVenta >= fechaDesde && v.FechaVenta <= fechaHasta
                         && (!sucursalId.HasValue || v.SucursalId == sucursalId),
                    includeProperties: "Paciente,Usuario,Sucursal,Detalles");

                var ventas = raw.Items.ToList();

                var porFecha = ventas
                    .GroupBy(v => v.FechaVenta.Date)
                    .OrderBy(g => g.Key)
                    .ToList();

                var porSucursal = ventas
                    .GroupBy(v => v.Sucursal?.Nombre ?? "Sin sucursal")
                    .OrderBy(g => g.Key)
                    .ToList();

                var porMetodo = ventas
                    .GroupBy(v => v.MetodoPago.ToString())
                    .ToList();

                var topProductos = ventas
                    .SelectMany(v => v.Detalles)
                    .GroupBy(d => d.DescripcionSnapshot)
                    .Select(g => new ProductoTopItem
                    {
                        Nombre = g.Key,
                        CantidadVendida = g.Sum(d => d.Cantidad),
                        TotalGenerado = g.Sum(d => d.Subtotal)
                    })
                    .OrderByDescending(x => x.TotalGenerado)
                    .Take(10)
                    .ToList();

                var vm = new ReporteVentasViewModel
                {
                    Desde = fechaDesde,
                    Hasta = hasta ?? DateTime.Now,
                    SucursalId = sucursalId,
                    Sucursales = sucursales,
                    TotalGeneral = ventas.Sum(v => v.Total),
                    TotalTransacciones = ventas.Count,
                    PromedioVenta = ventas.Any() ? ventas.Average(v => v.Total) : 0,
                    EtiquetasFecha = porFecha.Select(g => g.Key.ToString("dd/MM")).ToList(),
                    TotalesPorFecha = porFecha.Select(g => g.Sum(v => v.Total)).ToList(),
                    EtiquetasSucursal = porSucursal.Select(g => g.Key).ToList(),
                    TotalesPorSucursal = porSucursal.Select(g => g.Sum(v => v.Total)).ToList(),
                    EtiquetasMetodo = porMetodo.Select(g => g.Key).ToList(),
                    ConteosPorMetodo = porMetodo.Select(g => g.Count()).ToList(),
                    TopProductos = topProductos
                };

                return View(vm);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al generar el reporte: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        public async Task<IActionResult> ExportarVentasExcel(DateTime? desde, DateTime? hasta, int? sucursalId)
        {
            try
            {
                var fechaDesde = desde ?? DateTime.Now.AddMonths(-1);
                var fechaHasta = (hasta ?? DateTime.Now).Date.AddDays(1).AddTicks(-1);

                var raw = await _ventaRepo.GetPagedAsync(
                    1, 10000,
                    v => v.FechaVenta >= fechaDesde && v.FechaVenta <= fechaHasta
                         && (!sucursalId.HasValue || v.SucursalId == sucursalId),
                    includeProperties: "Paciente,Usuario,Sucursal,Detalles");

                var ventas = raw.Items.ToList();

                using var wb = new XLWorkbook();

                // ── Hoja 1: Listado ─────────────────────────────────────────
                var ws = wb.Worksheets.Add("Ventas");

                ws.Cell(1, 1).Value = "Reporte de Ventas — Óptica Comunal";
                ws.Cell(1, 1).Style.Font.Bold = true;
                ws.Cell(1, 1).Style.Font.FontSize = 14;
                ws.Range(1, 1, 1, 7).Merge();

                ws.Cell(2, 1).Value = $"Período: {fechaDesde:dd/MM/yyyy} al {(hasta ?? DateTime.Now):dd/MM/yyyy}";
                ws.Range(2, 1, 2, 7).Merge();

                string[] headers = { "Factura", "Fecha", "Paciente", "Cajero", "Sucursal", "Método", "Total (₡)" };
                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = ws.Cell(4, i + 1);
                    cell.Value = headers[i];
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3c6e");
                    cell.Style.Font.FontColor = XLColor.White;
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }

                int row = 5;
                foreach (var v in ventas)
                {
                    ws.Cell(row, 1).Value = v.NumeroFactura;
                    ws.Cell(row, 2).Value = v.FechaVenta.ToString("dd/MM/yyyy HH:mm");
                    ws.Cell(row, 3).Value = v.Paciente?.NombreCompleto;
                    ws.Cell(row, 4).Value = v.Usuario?.Nombre;
                    ws.Cell(row, 5).Value = v.Sucursal?.Nombre;
                    ws.Cell(row, 6).Value = v.MetodoPago.ToString();
                    ws.Cell(row, 7).Value = v.Total;
                    ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00";
                    if (row % 2 == 0)
                        ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#f4f6fb");
                    row++;
                }

                ws.Cell(row, 6).Value = "TOTAL";
                ws.Cell(row, 6).Style.Font.Bold = true;
                ws.Cell(row, 7).Value = ventas.Sum(v => v.Total);
                ws.Cell(row, 7).Style.Font.Bold = true;
                ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00";
                ws.Columns().AdjustToContents();

                // ── Hoja 2: Por método ──────────────────────────────────────
                var ws2 = wb.Worksheets.Add("Por Método");
                ws2.Cell(1, 1).Value = "Ventas por Método de Pago";
                ws2.Cell(1, 1).Style.Font.Bold = true;

                string[] h2 = { "Método", "Transacciones", "Total (₡)", "Promedio (₡)" };
                for (int i = 0; i < h2.Length; i++)
                {
                    ws2.Cell(3, i + 1).Value = h2[i];
                    ws2.Cell(3, i + 1).Style.Font.Bold = true;
                    ws2.Cell(3, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3c6e");
                    ws2.Cell(3, i + 1).Style.Font.FontColor = XLColor.White;
                }

                int r2 = 4;
                foreach (var g in ventas.GroupBy(v => v.MetodoPago.ToString()))
                {
                    ws2.Cell(r2, 1).Value = g.Key;
                    ws2.Cell(r2, 2).Value = g.Count();
                    ws2.Cell(r2, 3).Value = g.Sum(v => v.Total);
                    ws2.Cell(r2, 3).Style.NumberFormat.Format = "#,##0.00";
                    ws2.Cell(r2, 4).Value = g.Average(v => v.Total);
                    ws2.Cell(r2, 4).Style.NumberFormat.Format = "#,##0.00";
                    r2++;
                }
                ws2.Columns().AdjustToContents();

                using var stream = new MemoryStream();
                wb.SaveAs(stream);
                return File(stream.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"ReporteVentas_{DateTime.Now:yyyyMMdd}.xlsx");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al exportar: {ex.Message}";
                return RedirectToAction("Ventas", new { desde, hasta, sucursalId });
            }
        }

        // ── FIDELIZACIÓN ────────────────────────────────────────────────────

        public async Task<IActionResult> Fidelizacion(DateTime? desde, DateTime? hasta, int? sucursalId)
        {
            try
            {
                var fechaDesde = desde ?? DateTime.Now.AddMonths(-12);
                var fechaHasta = (hasta ?? DateTime.Now).Date.AddDays(1).AddTicks(-1);
                var haceUnMes = fechaHasta.AddMonths(-1);

                var sucursales = (await _sucursalRepo.GetPagedAsync(1, 100)).Items.ToList();

                var raw = await _citaRepo.GetPagedAsync(
                    1, 10000,
                    c => c.FechaHora >= fechaDesde && c.FechaHora <= fechaHasta
                         && c.Estado == EstadoCita.Atendida
                         && (!sucursalId.HasValue || c.SucursalId == sucursalId),
                    includeProperties: "Paciente");

                var detalle = raw.Items
                    .GroupBy(c => c.PacienteId)
                    .Select(g =>
                    {
                        var paciente = g.First().Paciente;
                        int visitas = g.Count();
                        bool esNuevo = paciente.FechaRegistro >= haceUnMes;

                        string clasificacion = esNuevo
                            ? "Nuevo"
                            : visitas >= 4
                                ? "Frecuente"
                                : visitas >= 2
                                    ? "Regular"
                                    : "Esporádico";

                        return new FidelizacionPacienteItem
                        {
                            NombreCompleto = paciente.NombreCompleto,
                            Cedula = paciente.Cedula,
                            Telefono = paciente.Telefono,
                            Email = paciente.Email,
                            VisitasEnPeriodo = visitas,
                            Clasificacion = clasificacion,
                            UltimaVisita = g.Max(c => c.FechaHora)
                        };
                    })
                    .OrderByDescending(x => x.VisitasEnPeriodo)
                    .ToList();

                var vm = new ReporteFidelizacionViewModel
                {
                    Desde = desde ?? DateTime.Now.AddMonths(-12),
                    Hasta = hasta ?? DateTime.Now,
                    SucursalId = sucursalId,
                    Sucursales = sucursales,
                    TotalNuevos = detalle.Count(x => x.Clasificacion == "Nuevo"),
                    TotalRegulares = detalle.Count(x => x.Clasificacion == "Regular"),
                    TotalFrecuentes = detalle.Count(x => x.Clasificacion == "Frecuente"),
                    TotalEsporadicos = detalle.Count(x => x.Clasificacion == "Esporádico"),
                    Detalle = detalle
                };

                return View(vm);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al generar el reporte: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        public async Task<IActionResult> ExportarFidelizacionExcel(DateTime? desde, DateTime? hasta, int? sucursalId)
        {
            try
            {
                var fechaDesde = desde ?? DateTime.Now.AddMonths(-12);
                var fechaHasta = (hasta ?? DateTime.Now).Date.AddDays(1).AddTicks(-1);
                var haceUnMes = fechaHasta.AddMonths(-1);

                var raw = await _citaRepo.GetPagedAsync(
                    1, 10000,
                    c => c.FechaHora >= fechaDesde && c.FechaHora <= fechaHasta
                         && c.Estado == EstadoCita.Atendida
                         && (!sucursalId.HasValue || c.SucursalId == sucursalId),
                    includeProperties: "Paciente");

                var detalle = raw.Items
                    .GroupBy(c => c.PacienteId)
                    .Select(g =>
                    {
                        var paciente = g.First().Paciente;
                        int visitas = g.Count();
                        bool esNuevo = paciente.FechaRegistro >= haceUnMes;
                        string cls = esNuevo ? "Nuevo" : visitas >= 4 ? "Frecuente" : visitas >= 2 ? "Regular" : "Esporádico";
                        return new FidelizacionPacienteItem
                        {
                            NombreCompleto = paciente.NombreCompleto,
                            Cedula = paciente.Cedula,
                            Telefono = paciente.Telefono,
                            Email = paciente.Email,
                            VisitasEnPeriodo = visitas,
                            Clasificacion = cls,
                            UltimaVisita = g.Max(c => c.FechaHora)
                        };
                    })
                    .OrderByDescending(x => x.VisitasEnPeriodo)
                    .ToList();

                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("Fidelización");

                ws.Cell(1, 1).Value = "Reporte de Fidelización — Óptica Comunal";
                ws.Cell(1, 1).Style.Font.Bold = true;
                ws.Cell(1, 1).Style.Font.FontSize = 14;
                ws.Range(1, 1, 1, 7).Merge();

                string[] headers = { "Paciente", "Cédula", "Teléfono", "Correo", "Visitas en Período", "Última Visita", "Clasificación" };
                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = ws.Cell(3, i + 1);
                    cell.Value = headers[i];
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3c6e");
                    cell.Style.Font.FontColor = XLColor.White;
                }

                int row = 4;
                foreach (var p in detalle)
                {
                    ws.Cell(row, 1).Value = p.NombreCompleto;
                    ws.Cell(row, 2).Value = p.Cedula;
                    ws.Cell(row, 3).Value = p.Telefono ?? "";
                    ws.Cell(row, 4).Value = p.Email ?? "";
                    ws.Cell(row, 5).Value = p.VisitasEnPeriodo;
                    ws.Cell(row, 6).Value = p.UltimaVisita.ToString("dd/MM/yyyy");
                    ws.Cell(row, 7).Value = p.Clasificacion;

                    var color = p.Clasificacion switch
                    {
                        "Frecuente" => XLColor.FromHtml("#d4edda"),
                        "Regular" => XLColor.FromHtml("#cce5ff"),
                        "Nuevo" => XLColor.FromHtml("#fff3cd"),
                        _ => XLColor.FromHtml("#f8f9fa")
                    };
                    ws.Row(row).Style.Fill.BackgroundColor = color;
                    row++;
                }

                ws.Columns().AdjustToContents();

                using var stream = new MemoryStream();
                wb.SaveAs(stream);
                return File(stream.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"ReporteFidelizacion_{DateTime.Now:yyyyMMdd}.xlsx");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al exportar: {ex.Message}";
                return RedirectToAction("Fidelizacion", new { desde, hasta, sucursalId });
            }
        }

        // ── DEMANDA ─────────────────────────────────────────────────────────

        public async Task<IActionResult> Demanda(DateTime? desde, DateTime? hasta, int? sucursalId)
        {
            try
            {
                var fechaDesde = desde ?? DateTime.Now.AddMonths(-3);
                var fechaHasta = (hasta ?? DateTime.Now).Date.AddDays(1).AddTicks(-1);

                var sucursales = (await _sucursalRepo.GetPagedAsync(1, 100)).Items.ToList();

                var raw = await _citaRepo.GetPagedAsync(
                    1, 10000,
                    c => c.FechaHora >= fechaDesde && c.FechaHora <= fechaHasta
                         && (!sucursalId.HasValue || c.SucursalId == sucursalId));

                var citas = raw.Items.ToList();

                var porMes = citas
                    .GroupBy(c => new { c.FechaHora.Year, c.FechaHora.Month })
                    .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                    .ToList();

                var vm = new ReporteDemandaViewModel
                {
                    Desde = desde ?? DateTime.Now.AddMonths(-3),
                    Hasta = hasta ?? DateTime.Now,
                    SucursalId = sucursalId,
                    Sucursales = sucursales,
                    TotalAgendadas = citas.Count,
                    TotalAtendidas = citas.Count(c => c.Estado == EstadoCita.Atendida),
                    TotalCanceladas = citas.Count(c => c.Estado == EstadoCita.Cancelada),
                    TotalPendientes = citas.Count(c => c.Estado == EstadoCita.Pendiente || c.Estado == EstadoCita.Confirmada),
                    EtiquetasMes = porMes.Select(g => $"{g.Key.Month:00}/{g.Key.Year}").ToList(),
                    AtendidaPorMes = porMes.Select(g => g.Count(c => c.Estado == EstadoCita.Atendida)).ToList(),
                    CanceladaPorMes = porMes.Select(g => g.Count(c => c.Estado == EstadoCita.Cancelada)).ToList(),
                    PendientePorMes = porMes.Select(g => g.Count(c => c.Estado == EstadoCita.Pendiente || c.Estado == EstadoCita.Confirmada)).ToList()
                };

                return View(vm);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al generar el reporte: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        public async Task<IActionResult> ExportarDemandaExcel(DateTime? desde, DateTime? hasta, int? sucursalId)
        {
            try
            {
                var fechaDesde = desde ?? DateTime.Now.AddMonths(-3);
                var fechaHasta = (hasta ?? DateTime.Now).Date.AddDays(1).AddTicks(-1);

                var raw = await _citaRepo.GetPagedAsync(
                    1, 10000,
                    c => c.FechaHora >= fechaDesde && c.FechaHora <= fechaHasta
                         && (!sucursalId.HasValue || c.SucursalId == sucursalId),
                    includeProperties: "Paciente,Sucursal");

                var citas = raw.Items.OrderBy(c => c.FechaHora).ToList();

                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("Demanda");

                ws.Cell(1, 1).Value = "Reporte de Demanda — Óptica Comunal";
                ws.Cell(1, 1).Style.Font.Bold = true;
                ws.Cell(1, 1).Style.Font.FontSize = 14;
                ws.Range(1, 1, 1, 5).Merge();

                string[] headers = { "Fecha", "Paciente", "Sucursal", "Estado", "Motivo" };
                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = ws.Cell(3, i + 1);
                    cell.Value = headers[i];
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3c6e");
                    cell.Style.Font.FontColor = XLColor.White;
                }

                int row = 4;
                foreach (var c in citas)
                {
                    ws.Cell(row, 1).Value = c.FechaHora.ToString("dd/MM/yyyy HH:mm");
                    ws.Cell(row, 2).Value = c.Paciente?.NombreCompleto;
                    ws.Cell(row, 3).Value = c.Sucursal?.Nombre;
                    ws.Cell(row, 4).Value = c.Estado;
                    ws.Cell(row, 5).Value = c.MotivoConsulta;

                    var color = c.Estado switch
                    {
                        EstadoCita.Atendida => XLColor.FromHtml("#d4edda"),
                        EstadoCita.Cancelada => XLColor.FromHtml("#f8d7da"),
                        _ => XLColor.FromHtml("#fff3cd")
                    };
                    ws.Row(row).Style.Fill.BackgroundColor = color;
                    row++;
                }

                ws.Columns().AdjustToContents();

                using var stream = new MemoryStream();
                wb.SaveAs(stream);
                return File(stream.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"ReporteDemanda_{DateTime.Now:yyyyMMdd}.xlsx");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al exportar: {ex.Message}";
                return RedirectToAction("Demanda", new { desde, hasta, sucursalId });
            }
        }
    }
}