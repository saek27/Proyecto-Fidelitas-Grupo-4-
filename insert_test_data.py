import pymssql
import hashlib

conn = pymssql.connect(server='localhost', database='SistemaOpticaDB', user='sa', password='Dani4421!')
cur = conn.cursor()

print("=== INSERTANDO DATOS DE PRUEBA ===\n")

# 2 SUCURSALES
print("[1/8] Sucursal Heredia...")
cur.execute("SET IDENTITY_INSERT Sucursales ON")
cur.execute("""
    IF NOT EXISTS (SELECT 1 FROM Sucursales WHERE Id = 2)
    INSERT INTO Sucursales (Id, Nombre, Direccion, Telefono, Activo, HorarioAtencion, Latitud, Longitud, TelefonoAdicional)
    VALUES (2, 'Sucursal Heredia', 'Centro Comercial Heredia Plaza, Local 45, Heredia', '2260-4401', 1, 'Lun-Vie 8:00-18:00, Sab 9:00-14:00', 10.0194, -84.1167, '2260-4402')
""")
cur.execute("SET IDENTITY_INSERT Sucursales OFF")
conn.commit()
print("[OK] Sucursal Heredia")

# 5 USUARIOS
print("[2/8] 5 Usuarios...")
pwd_hash = hashlib.sha256(b'Password123!').hexdigest()
cur.execute("SET IDENTITY_INSERT Usuarios ON")

usuarios = [
    (2, 'Laura Mendez', 'laura.mendez@optica.com', 1, 2, 1, '205420890', '2023-03-15', 450000),
    (3, 'Roberto Sanchez', 'roberto.sanchez@optica.com', 1, 3, 1, '402340567', '2023-06-01', 350000),
    (4, 'Maria Fernandez', 'maria.fernandez@optica.com', 1, 5, 2, '701230456', '2024-01-10', 500000),
    (5, 'Pedro Ramirez', 'pedro.ramirez@optica.com', 1, 2, 2, '104560789', '2023-09-20', 450000),
    (6, 'Ana Gomez', 'ana.gomez@optica.com', 1, 3, 1, '303670145', '2024-02-01', 350000),
]
for u in usuarios:
    cur.execute("""
        IF NOT EXISTS (SELECT 1 FROM Usuarios WHERE Id = %d)
        INSERT INTO Usuarios (Id, Nombre, Correo, Contrasena, Activo, RolId, SucursalId, Cedula, FechaContratacion, NumeroCuentaIBAN, SalarioBase)
        VALUES (%d, '%s', '%s', '%s', %d, %d, %d, '%s', '%s', '', %s)
    """ % (u[0], u[0], u[1], u[2], pwd_hash, u[3], u[4], u[5], u[6], u[7], u[8]))

cur.execute("SET IDENTITY_INSERT Usuarios OFF")
conn.commit()
print("[OK] 5 Usuarios")

# 12 CITAS
print("[3/8] Creando SolicitudesCitas y luego 12 Citas...")
citas_data = [
    (1, 2, '2026-05-20 09:00', 'Examen de Vision', 'Examen completo', 'Completada', 1),
    (1, 2, '2026-06-05 10:30', 'Seguimiento', 'Control post-examen', 'Programada', 1),
    (2, 5, '2026-05-22 14:00', 'Examen de Vision', 'Primera vez', 'Completada', 2),
    (3, 2, '2026-05-25 11:00', 'Examen completo', 'Consulta general', 'Completada', 1),
    (3, 5, '2026-06-08 09:00', 'Adaptacion lentes', 'Seguimiento adaptacion', 'Programada', 2),
    (4, 2, '2026-05-18 15:30', 'Examen de Vision', 'Chequeo anual', 'Completada', 1),
    (4, 2, '2026-06-12 16:00', 'Revision lentes', 'Problemas con armazon', 'Programada', 1),
    (5, 5, '2026-05-28 08:30', 'Examen completo', 'Evaluacion completa', 'Completada', 2),
    (6, 2, '2026-06-01 10:00', 'Examen de Vision', 'Primera cita', 'Programada', 1),
    (7, 5, '2026-05-15 13:00', 'Revision', 'Chequeo general', 'Completada', 2),
    (7, 2, '2026-06-10 11:30', 'Seguimiento', 'Post-tratamiento', 'Programada', 1),
    (8, 5, '2026-05-10 09:00', 'Examen de Vision', 'Evaluacion inicial', 'Cancelada', 2),
]

for idx, c in enumerate(citas_data, 1):
    cur.execute("""
        INSERT INTO SolicitudesCitas (PacienteId, FechaSolicitud, Motivo, Estado, FechaAprobacion, UsuarioAprobadorId)
        VALUES (%d, '%s', '%s', 'Aprobada', '%s', %d)
    """ % (c[0], c[2], c[3], c[2], c[1]))
    cur.execute("SELECT @@IDENTITY")
    sol_id = cur.fetchone()[0]
    cur.execute("""
        INSERT INTO Citas (PacienteId, SolicitudCitaId, FechaHora, MotivoConsulta, ObservacionesEspecialista, Estado, UsuarioAsignadoId, SucursalId, FechaCreacion, NotificacionesActivas, CanalNotificacion)
        VALUES (%d, %d, '%s', '%s', '%s', '%s', %d, %d, GETDATE(), 1, 'Email')
    """ % (c[0], sol_id, c[2], c[3], c[4], c[5], c[1], c[6]))
conn.commit()
print("[OK] 12 Citas (con solicitudes vinculadas)")

# 6 PROVEEDORES
print("[4/8] 6 Proveedores...")
proveedores = [
    ("Lentes Plus S.A.", 88881234, 'contacto@lentesplus.com'),
    ("OpticalSupply CR", 22225678, 'ventas@opticalsupplycr.com'),
    ("Vision Tech International", 77779012, 'orders@visiontech.com'),
    ("MarcoOptica S.A.", 33333456, 'compras@marcooptica.com'),
    ("ClearVision Imports", 66667890, 'info@clearvisioncr.com'),
    ("Optica Mayorista CR", 44449012, 'optica@mayoristacr.com'),
]
for p in proveedores:
    cur.execute("INSERT INTO Proveedores (Nombre, Activo, NumeroTelefonico, Correo) VALUES ('%s', 1, %d, '%s')" % (p[0], p[1], p[2]))
conn.commit()
print("[OK] 6 Proveedores")

# 8 PEDIDOS
print("[5/8] 8 Pedidos...")
pedidos = [
    (2, '2026-05-01', '2026-05-15', '2026-05-14', 1, 1),
    (3, '2026-05-20', '2026-06-05', None, 2, 2),
    (4, '2026-05-05', '2026-05-20', '2026-05-19', 1, 3),
    (5, '2026-05-10', '2026-05-25', None, 2, 4),
    (6, '2026-05-12', '2026-05-28', '2026-05-27', 1, 5),
    (7, '2026-05-18', '2026-06-01', None, 2, 6),
    (8, '2026-05-22', '2026-06-08', None, 3, 7),
    (9, '2026-05-25', '2026-06-10', None, 2, 8),
]
for p in pedidos:
    fecha_real = "NULL" if p[3] is None else "'" + p[3] + "'"
    cur.execute("""
        INSERT INTO Pedidos (ProveedorId, FechaPedido, FechaEntregaEstimada, FechaEntregaReal, Activo, Descripcion, Estado, Indicador)
        VALUES (%d, '%s', '%s', %s, 1, 'Pedido de productos opticos', %d, %d)
    """ % (p[0], p[1], p[2], fecha_real, p[4], p[5]))
conn.commit()
print("[OK] 8 Pedidos")

# 6 ORDENES DE TRABAJO
print("[6/8] 6 Ordenes de Trabajo...")
ordenes = [
    (1, 1, None, 'Pendiente', 'Orden para examenes', '2026-05-15'),
    (2, 2, None, 'EnProceso', 'Armazon de lectura', '2026-05-18'),
    (3, 1, 5, 'Completada', 'Lentes de sol', '2026-05-10'),
    (4, 2, None, 'Pendiente', 'Revision optica', '2026-05-22'),
    (5, 1, 8, 'Completada', 'Examen completo', '2026-05-05'),
    (6, 2, 12, 'EnProceso', 'Adaptacion lentes', '2026-05-25'),
]
for o in ordenes:
    venta_id = "NULL" if o[2] is None else str(o[2])
    cur.execute("""
        INSERT INTO OrdenesTrabajo (PacienteId, SucursalId, VentaId, Estado, Referencia, FechaCreacion, FechaLista)
        VALUES (%d, %d, %s, '%s', '%s', '%s', NULL)
    """ % (o[0], o[1], venta_id, o[3], o[4], o[5]))
conn.commit()
print("[OK] 6 Ordenes de Trabajo")

# 10 PLANILLAS
print("[7/8] 10 Planillas...")
planillas_data = [
    (2, 5, 2026, 160, 176, 10, 6, 25000, 50000, 487500),
    (3, 5, 2026, 160, 168, 8, 0, 15000, 30000, 385000),
    (4, 5, 2026, 160, 180, 12, 8, 30000, 0, 562000),
    (5, 5, 2026, 160, 172, 6, 6, 20000, 40000, 478000),
    (6, 5, 2026, 160, 160, 0, 0, 10000, 20000, 362000),
    (2, 6, 2026, 160, 168, 8, 0, 20000, 50000, 445000),
    (3, 6, 2026, 160, 176, 16, 0, 18000, 30000, 398000),
    (4, 6, 2026, 160, 184, 14, 10, 35000, 0, 598000),
    (5, 6, 2026, 160, 164, 4, 0, 15000, 40000, 442000),
    (6, 6, 2026, 160, 172, 12, 0, 12000, 20000, 375000),
]

for p in planillas_data:
    num = "PLAN-2026-" + str(p[1]).zfill(2) + "-" + str(p[0]).zfill(4)
    sal_ord = p[3] * 2500
    val_extra = p[5] * 5000
    val_doble = p[6] * 5000
    total_ing = sal_ord + val_extra + val_doble + p[7]
    ccss = (sal_ord + val_extra + val_doble) * 0.0917
    solidarista = total_ing * 0.015

    sql = """
    INSERT INTO Planillas (UsuarioId, Mes, Año, FechaCalculo, HorasBase, TotalHoras, 
        HorasVacaciones, HorasIncapacidadParcial, HorasIncapacidadTotal, HorasPermiso,
        HorasExtras, HorasDobles, Comisiones, Prestamos, EmbargosPensiones, 
        CuentasPorCobrar, AdelantoQuincena, PorcentajeCCSS, PorcentajeSolidarista,
        NumeroComprobante, SalarioOrdinario, ValorHorasExtras, ValorHorasDobles,
        ValorVacaciones, ValorIncapacidadParcial, ValorIncapacidadTotal,
        TotalIngresos, MontoCCSS, MontoImpuestoRenta, MontoSolidarista, 
        TotalDeducciones, SalarioNeto)
    VALUES (%d, %d, %d, GETDATE(), %d, %d, 
        0, 0, 0, 0,
        %d, %d, %d, %d, 0, 
        0, 0, 9.17, 1.5,
        '%s', %d, %d, 0,
        0, 0, 0,
        %d, %f, 0, %f, 
        0, %d)
    """ % (
        p[0], p[1], p[2], p[3], p[4],
        p[5], p[6], p[7], p[8],
        num, sal_ord, val_extra + val_doble,
        total_ing, ccss, solidarista, p[9]
    )
    cur.execute(sql)

conn.commit()
print("[OK] 10 Planillas")

print("\n=== VERIFICACION ===")
cur.execute("SELECT COUNT(*) FROM Usuarios WHERE Id > 1"); print("  Usuarios nuevos: " + str(cur.fetchone()[0]))
cur.execute("SELECT COUNT(*) FROM Citas"); print("  Citas: " + str(cur.fetchone()[0]))
cur.execute("SELECT COUNT(*) FROM Proveedores"); print("  Proveedores: " + str(cur.fetchone()[0]))
cur.execute("SELECT COUNT(*) FROM Pedidos"); print("  Pedidos: " + str(cur.fetchone()[0]))
cur.execute("SELECT COUNT(*) FROM OrdenesTrabajo"); print("  Ordenes de Trabajo: " + str(cur.fetchone()[0]))
cur.execute("SELECT COUNT(*) FROM Planillas"); print("  Planillas: " + str(cur.fetchone()[0]))
cur.execute("SELECT COUNT(*) FROM Sucursales"); print("  Sucursales: " + str(cur.fetchone()[0]))

print("\n[OK] Todos los datos insertados!")
conn.close()