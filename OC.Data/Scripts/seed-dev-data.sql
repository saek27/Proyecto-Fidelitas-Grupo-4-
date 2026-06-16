-- ============================================================
-- Script: seed-dev-data.sql
-- Propósito: Sembrar la base de datos de desarrollo con datos
--            realistas para una óptica costarricense. Pensado
--            para correr DESPUÉS de clean-dev-data.sql.
--
-- ⚠ EJECUTAR SOLO EN DESARROLLO.
--
-- Volumen:
--   6  Proveedores
--   25 Pacientes
--   50 SolicitudesCitas (43 Aprobadas, 7 Rechazadas)
--   50 Citas (23 Atendidas, 13 Confirmadas, 6 Pendientes,
--             8 Canceladas)
--   23 Expedientes + 23 ValoresClinicos
--   20 Ventas  + 47 DetalleVentas
--   15 OrdenesTrabajo
--   50 EnviosNotificacion
--   10 Pedidos + 27 DetallePedidos
--
-- Convenciones:
--   - Fechas pasadas:   2026-03-15 .. 2026-06-05
--   - Fechas futuras:   2026-06-10 .. 2026-06-22
--   - Cédulas en formato costarricense 0-0000-0000
--   - Teléfonos 8 dígitos (formato CR: 6000-0000 / 7000-0000 / 8000-0000)
--   - IDs deterministas con IDENTITY_INSERT (1..N)
--   - Contrasenas placeholder: 'SEED_TEMP_PASS' (no son válidas para login)
-- ============================================================

-- (SET XACT_ABORT removed: partial failures OK)

SET NOCOUNT ON;

-- (BEGIN TRY removed: keep going on errors)

-- (BEGIN TRANSACTION removed)

-- ============================================================
-- 0) ESQUEMA: garantiza columnas nuevas de Proveedor (idempotente)
--    Es un espejo de lo que hace DbInitializer.EnsureProveedorSchema
--    al arranque de la app, para que el seed corra sin depender de
--    que la app se haya iniciado antes.
-- ============================================================
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Proveedores') AND name = 'NumeroTelefonico' AND system_type_id = 56)
    ALTER TABLE [dbo].[Proveedores] ALTER COLUMN [NumeroTelefonico] nvarchar(9) NOT NULL;

IF OBJECT_ID('dbo.CK_Proveedores_NumeroTelefonico','C') IS NOT NULL
    ALTER TABLE [dbo].[Proveedores] DROP CONSTRAINT [CK_Proveedores_NumeroTelefonico];
IF OBJECT_ID('dbo.CK_Proveedores_Correo','C') IS NOT NULL
    ALTER TABLE [dbo].[Proveedores] DROP CONSTRAINT [CK_Proveedores_Correo];

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Proveedores') AND name = 'ContactoAdicionalNombre')
    ALTER TABLE [dbo].[Proveedores] ADD [ContactoAdicionalNombre] nvarchar(100) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Proveedores') AND name = 'ContactoAdicionalTelefono')
    ALTER TABLE [dbo].[Proveedores] ADD [ContactoAdicionalTelefono] nvarchar(20) NULL;

-- ============================================================
-- ============================================================
-- 0b) CATÁLOGO DE PRODUCTOS (24) — necesario para FKs de DetalleVentas y DetallePedidos
--     Editado por Hermes para que el seed corra en una BD nueva
-- ============================================================
PRINT '0b) Insertando Productos...';
SET IDENTITY_INSERT Productos ON;
INSERT INTO Productos (Id, Nombre, SKU, CostoUnitario, Stock, Activo, DescripcionCorta, Destacado, Categoria, PrecioPublico) VALUES
(1,  N'Aro Metálico Redondo',             N'ARO-MET-001', 18000.00, 12, 1, N'Aro metálico redondo clásico, varilla estándar.', 0, N'Aros',         35000.00),
(2,  N'Lentes BlueCut Digital',           N'LEN-BLU-002', 30000.00, 25, 1, N'Lentes con filtro blue cut digital para uso prolongado de pantallas.', 1, N'Lentes',       65000.00),
(3,  N'Aro Cat-Eye Femenino',             N'ARO-CAT-003', 28000.00,  8, 1, N'Aro cat-eye femenino acetato.', 0, N'Aros',                 55000.00),
(4,  N'Aro Deportivo Wrap',               N'ARO-DEP-004', 20000.00, 10, 1, N'Aro deportivo tipo wrap, sujeción firme.', 0, N'Aros',            40000.00),
(5,  N'Aro Vintage Oval',                 N'ARO-VIN-005', 19000.00,  6, 1, N'Aro vintage oval, acetato.', 0, N'Aros',                     38000.00),
(6,  N'Aro Infantil Disney',              N'ARO-INF-006', 12000.00, 14, 1, N'Aro infantil con licencia, material hipoalergénico.', 0, N'Aros', 25000.00),
(7,  N'Aro Wayfarer Clásico',             N'ARO-WAY-007', 22000.00,  9, 1, N'Aro wayfarer clásico, varilla reforzada.', 0, N'Aros',          45000.00),
(8,  N'Aro Oversize Grande',              N'ARO-OVE-008', 21000.00,  5, 1, N'Aro oversize, marco grueso.', 0, N'Aros',                    42000.00),
(9,  N'Aro Metálico Cuadrado',            N'ARO-MET-009', 16000.00, 11, 1, N'Aro metálico cuadrado, unisex.', 0, N'Aros',                  32000.00),
(10, N'Aro Flex Silicona',                N'ARO-FLX-010', 14000.00, 13, 1, N'Aro de silicona flexible, ideal para niños y deportes.', 1, N'Aros', 28000.00),
(11, N'Lentes Monofocáticos Elite',       N'LEN-MON-011', 22000.00, 30, 1, N'Lentes monofocales CR-39 con anti-reflejo.', 0, N'Lentes', 45000.00),
(12, N'Lentes Monofocáticos Plus',        N'LEN-MON-012', 26000.00, 22, 1, N'Lentes monofocales con blue cut.', 0, N'Lentes', 50000.00),
(13, N'Lentes Progresivos Liberty Plus',  N'LEN-PRO-013', 58000.00, 15, 1, N'Lentes progresivos premium, diseño Liberty Plus.', 1, N'Lentes', 125000.00),
(14, N'Lentes Antifatiga Digital Plus',   N'LEN-AFG-014', 32000.00, 18, 1, N'Lentes anti-fatiga para trabajo prolongado en pantallas.', 0, N'Lentes', 68000.00),
(15, N'Lentes Transitions Gen 8',         N'LEN-TRN-015', 44000.00,  9, 1, N'Lentes fotocromáticos Transitions Gen 8.', 1, N'Lentes', 95000.00),
(16, N'Lentes High Index 1.74 Ultra',     N'LEN-HI-016',  65000.00,  7, 1, N'Lentes alto índice 1.74, ultra delgados.', 0, N'Lentes', 140000.00),
(17, N'Lentes Bifocales Office Pro',      N'LEN-BIF-017', 41000.00, 10, 1, N'Lentes bifocales office, segmento amplio.', 0, N'Lentes', 88000.00),
(18, N'Lentes Deportivos Recetados',      N'LEN-DEP-018', 38000.00,  8, 1, N'Lentes deportivos recetados, policarbonato.', 0, N'Lentes', 82000.00),
(19, N'Estuche Rígido Premium Negro',     N'ACC-EST-019',  8500.00, 40, 1, N'Estuche rígido premium, color negro.', 0, N'Accesorios', 18000.00),
(20, N'Paño de Limpieza Microfibra',      N'ACC-PAN-020',  2500.00, 80, 1, N'Paño de limpieza microfibra, pack individual.', 0, N'Accesorios', 5000.00),
(21, N'Kit de Limpieza Completo',         N'ACC-KIT-021',  7000.00, 35, 1, N'Kit completo: spray + paño + estuche.', 0, N'Accesorios', 15000.00),
(22, N'Cordón de Nylon Premium',          N'ACC-COR-022',  3800.00, 50, 1, N'Cordón de nylon para sujeción de aros.', 0, N'Accesorios', 8000.00),
(23, N'Gotas Hidratantes Oftálmicas',     N'ACC-GOT-023',  5500.00, 60, 1, N'Gotas hidratantes para lentes de contacto.', 0, N'Accesorios', 12000.00),
(24, N'Lentes de Sol Sport Pro MTB',      N'LEN-SOL-024', 35000.00, 12, 1, N'Lentes de sol polarizados para ciclismo MTB.', 0, N'Lentes', 75000.00);
SET IDENTITY_INSERT Productos OFF;

PRINT '0c) Aros y TecnologiaLente: no requieren seed (el script referencia Productos.ProductoId).';


-- 1) PROVEEDORES (6)
-- ============================================================
PRINT '1) Insertando Proveedores...';
SET IDENTITY_INSERT Proveedores ON;
INSERT INTO Proveedores (Id, Nombre, Activo, NumeroTelefonico, Correo, ContactoAdicionalNombre, ContactoAdicionalTelefono) VALUES
(1, N'OptiLens Internacional S.A.',     1, N'2233-4455', N'ventas@optilens.cr',         N'Marcela Solís Vargas',   N'8844-1122'),
(2, N'Distribuidora Visual CR S.A.',    1, N'2445-5667', N'contacto@distvisual.cr',     N'Pablo Arias Méndez',     N'8999-3344'),
(3, N'Lentes del Valle S.A.',           1, N'2667-7889', N'info@lentesdelvalle.cr',    NULL,                      NULL),
(4, N'VisionLab Centroamerica',         1, N'2289-9011', N'cr@visionlabca.com',         N'Lucía Fernández Quesada', N'8766-5544'),
(5, N'Lux Óptica Mayorista',            1, N'2433-2211', N'pedidos@luxoptica.cr',       NULL,                      NULL),
(6, N'Andes Lentes Premium',            1, N'2255-4433', N'comercial@andeslentes.cr',   N'Esteban Rojas Castillo', N'8322-9911');
SET IDENTITY_INSERT Proveedores OFF;

-- ============================================================
-- 2) PACIENTES (25) — datos costarricenses realistas
-- ============================================================
PRINT '2) Insertando Pacientes...';
SET IDENTITY_INSERT Pacientes ON;
INSERT INTO Pacientes
  (Id, Nombres, Apellidos, Cedula, Telefono, Email, Contrasena,
   FechaNacimiento, FechaRegistro, IntentosFallidosLogin, BloqueadoPermanentemente, TotpHabilitado)
VALUES
(1,  N'María Fernanda',   N'Rodríguez Solís',     N'102340567', N'88445511', N'mfernanda.rodriguez@gmail.com',  N'SEED_TEMP_PASS', '1984-03-15', '2025-08-10', 0, 0, 0),
(2,  N'Carlos Andrés',    N'Hernández Vargas',    N'108761234', N'87654321', N'carlos.hernandezv@gmail.com',     N'SEED_TEMP_PASS', '1991-07-22', '2025-09-04', 0, 0, 0),
(3,  N'Ana Lucía',        N'González Quesada',    N'212345678', N'86554433', N'analu.gq@hotmail.com',            N'SEED_TEMP_PASS', '1998-11-10', '2025-10-12', 0, 0, 0),
(4,  N'Luis Diego',       N'Jiménez Brenes',      N'114567890', N'89992211', N'ldiego.jimenez@outlook.com',      N'SEED_TEMP_PASS', '1971-05-18', '2025-07-22', 0, 0, 0),
(5,  N'Carmen Patricia',  N'Sánchez Mora',        N'116789012', N'87112233', N'carmen.sanchez@gmail.com',        N'SEED_TEMP_PASS', '2007-02-14', '2026-05-28', 0, 0, 0),
(6,  N'José Miguel',      N'Castro Aguilar',      N'118901234', N'85556677', N'jmiguel.castro@gmail.com',        N'SEED_TEMP_PASS', '1959-09-30', '2025-06-15', 0, 0, 0),
(7,  N'Rosa Isabel',      N'Vargas Cordero',      N'220123456', N'83334455', N'rosa.vargas.c@gmail.com',         N'SEED_TEMP_PASS', '1981-12-05', '2025-11-20', 0, 0, 0),
(8,  N'Francisco Javier', N'Espinoza López',      N'122345678', N'88776655', N'francisco.espinoza@gmail.com',    N'SEED_TEMP_PASS', '1995-04-22', '2025-10-30', 0, 0, 0),
(9,  N'Isabel Cristina',  N'Pérez Ramírez',       N'124567890', N'84556677', N'isabel.perez.r@gmail.com',        N'SEED_TEMP_PASS', '2018-08-11', '2025-09-18', 0, 0, 0),
(10, N'Pedro Antonio',    N'Brenes Martínez',     N'126789012', N'89991122', N'pedro.brenes@gmail.com',          N'SEED_TEMP_PASS', '1974-06-28', '2025-08-05', 0, 0, 0),
(11, N'Lucía Gabriela',   N'Monge Calderón',      N'228901234', N'87774455', N'lucia.monge@gmail.com',           N'SEED_TEMP_PASS', '2002-01-20', '2025-12-02', 0, 0, 0),
(12, N'Miguel Ángel',     N'Cordero Solís',       N'130123456', N'86665544', N'miguel.cordero@gmail.com',        N'SEED_TEMP_PASS', '1988-10-08', '2026-01-14', 0, 0, 0),
(13, N'Teresa de Jesús',  N'Chaves Hernández',    N'132345678', N'89997788', N'teresa.chaves@gmail.com',         N'SEED_TEMP_PASS', '1956-03-25', '2025-05-10', 0, 0, 0),
(14, N'Ricardo José',     N'Quesada Vargas',      N'134567890', N'88771122', N'ricardo.quesada@gmail.com',       N'SEED_TEMP_PASS', '2013-05-12', '2025-09-25', 0, 0, 0),
(15, N'Patricia Elena',   N'Jiménez López',       N'236789012', N'85553344', N'patricia.jimenezl@gmail.com',     N'SEED_TEMP_PASS', '1978-08-17', '2026-02-08', 0, 0, 0),
(16, N'Daniel Eduardo',   N'Castro Brenes',       N'138901234', N'87775566', N'daniel.castro.b@gmail.com',       N'SEED_TEMP_PASS', '1997-11-28', '2026-01-22', 0, 0, 0),
(17, N'Gabriela Beatriz', N'Rodríguez Mora',      N'140123456', N'86664455', N'gabriela.rodriguezm@gmail.com',   N'SEED_TEMP_PASS', '1970-02-09', '2025-07-30', 0, 0, 0),
(18, N'Andrés Felipe',    N'Solís Aguilar',       N'142345678', N'89990011', N'andres.solis.a@gmail.com',        N'SEED_TEMP_PASS', '2004-09-15', '2026-05-30', 0, 0, 0),
(19, N'Sofía Carolina',   N'Vargas Espinoza',     N'244567890', N'88773344', N'sofia.vargas.e@gmail.com',        N'SEED_TEMP_PASS', '1993-12-03', '2025-11-08', 0, 0, 0),
(20, N'Diego Alejandro',  N'Pérez Quesada',       N'146789012', N'85551122', N'diego.perez.q@gmail.com',         N'SEED_TEMP_PASS', '1985-04-28', '2026-03-01', 0, 0, 0),
(21, N'Valentina',        N'Jiménez Hernández',   N'148901234', N'86662233', N'valentina.jimenezh@gmail.com',    N'SEED_TEMP_PASS', '1966-07-14', '2025-06-20', 0, 0, 0),
(22, N'Esteban Manuel',   N'Cordero Brenes',      N'150123456', N'89994455', N'esteban.cordero@gmail.com',       N'SEED_TEMP_PASS', '2000-03-22', '2025-12-15', 0, 0, 0),
(23, N'Adriana Lucía',    N'Monge Solís',         N'252345678', N'88776633', N'adriana.monge.s@gmail.com',       N'SEED_TEMP_PASS', '1977-11-19', '2025-10-05', 0, 0, 0),
(24, N'Sebastián',        N'Ramírez Vargas',      N'154567890', N'87775588', N'sebastian.ramirezv@gmail.com',    N'SEED_TEMP_PASS', '2010-01-07', '2026-02-20', 0, 0, 0),
(25, N'Camila Andrea',    N'López Castro',        N'156789012', N'86663344', N'camila.lopez.c@gmail.com',        N'SEED_TEMP_PASS', '1989-08-25', '2025-11-25', 0, 0, 0);
SET IDENTITY_INSERT Pacientes OFF;

-- ============================================================
-- 3) SOLICITUDESCITAS (50) — todas aprobadas, 1 por cita
-- ============================================================
PRINT '3) Insertando SolicitudesCitas...';
SET IDENTITY_INSERT SolicitudesCitas ON;
INSERT INTO SolicitudesCitas (Id, PacienteId, FechaSolicitud, Motivo, Estado, FechaAprobacion, UsuarioAprobadorId) VALUES

(1, 1, '2026-03-10 09:15:00', N'Control anual y renovación de receta', N'Aprobada', '2026-03-10 10:00:00', 1),
(2, 1, '2026-04-22 14:00:00', N'Renovación de lentes progresivos', N'Aprobada', '2026-04-22 14:30:00', 1),
(3, 1, '2026-06-05 11:20:00', N'Limpieza profesional y ajuste', N'Aprobada', '2026-06-05 11:45:00', 1),
(4, 2, '2026-04-02 16:00:00', N'Miopía progresiva, dificultad para conducir', N'Aprobada', '2026-04-02 16:30:00', 1),
(5, 2, '2026-06-01 10:30:00', N'Entrega de nueva receta', N'Aprobada', '2026-06-01 11:00:00', 1),
(6, 3, '2026-04-18 13:00:00', N'Chequeo de rutina sin síntomas', N'Aprobada', '2026-04-18 13:30:00', 1),
(7, 4, '2026-03-22 09:00:00', N'Presbicia incipiente, dificultad para leer', N'Aprobada', '2026-03-22 09:30:00', 1),
(8, 4, '2026-05-29 14:00:00', N'Seguimiento de progresivos', N'Aprobada', '2026-05-29 14:30:00', 1),
(9, 5, '2026-06-04 10:00:00', N'Miopía leve, primera consulta', N'Aprobada', '2026-06-04 10:30:00', 1),
(10, 6, '2026-03-25 11:00:00', N'Hipermetropía + presbicia, lentes nuevos', N'Aprobada', '2026-03-25 11:30:00', 1),
(11, 6, '2026-05-10 15:00:00', N'Cambio de aro por daño', N'Aprobada', '2026-05-10 15:30:00', 1),
(12, 6, '2026-06-02 09:00:00', N'Seguimiento post-adaptación', N'Aprobada', '2026-06-02 09:30:00', 1),
(13, 7, '2026-04-05 10:30:00', N'Astigmatismo diagnosticado hace 6 meses', N'Aprobada', '2026-04-05 11:00:00', 1),
(14, 7, '2026-06-03 14:00:00', N'Adaptación a nuevos lentes tóricos', N'Aprobada', '2026-06-03 14:30:00', 1),
(15, 8, '2026-04-12 16:30:00', N'Miopía con astigmatismo, dolor de cabeza', N'Aprobada', '2026-04-12 17:00:00', 1),
(16, 8, '2026-05-30 11:00:00', N'Entrega de nueva prescripción', N'Aprobada', '2026-05-30 11:30:00', 1),
(17, 9, '2026-04-20 09:00:00', N'Control pediátrico anual', N'Aprobada', '2026-04-20 09:30:00', 1),
(18, 9, '2026-06-04 15:00:00', N'Revisión de miopía infantil', N'Aprobada', '2026-06-04 15:30:00', 1),
(19, 10, '2026-03-28 14:00:00', N'Presbicia, primera receta', N'Aprobada', '2026-03-28 14:30:00', 1),
(20, 10, '2026-05-25 10:00:00', N'Control post-adaptación', N'Aprobada', '2026-05-25 10:30:00', 1),
(21, 11, '2026-04-30 11:30:00', N'Chequeo preventivo', N'Aprobada', '2026-04-30 12:00:00', 1),
(22, 12, '2026-05-15 16:00:00', N'Miopía progresiva, fatiga visual', N'Aprobada', '2026-05-15 16:30:00', 1),
(23, 13, '2026-03-15 09:00:00', N'Presbicia avanzada, primera consulta', N'Aprobada', '2026-03-15 09:30:00', 1),
(24, 13, '2026-04-25 14:00:00', N'Renovación con bifocales', N'Aprobada', '2026-04-25 14:30:00', 1),
(25, 13, '2026-06-01 10:00:00', N'Seguimiento de agudeza visual', N'Aprobada', '2026-06-01 10:30:00', 1),
(26, 14, '2026-04-08 15:00:00', N'Miopía escolar, primera receta', N'Aprobada', '2026-04-08 15:30:00', 1),
(27, 14, '2026-05-28 11:00:00', N'Control y ajuste de lentes', N'Aprobada', '2026-05-28 11:30:00', 1),
(28, 15, '2026-05-20 10:00:00', N'Presbicia incipiente', N'Aprobada', '2026-05-20 10:30:00', 1),
(29, 16, '2026-05-22 16:00:00', N'Miopía, primera consulta', N'Aprobada', '2026-05-22 16:30:00', 1),
(30, 17, '2026-04-15 09:00:00', N'Hipermetropía + presbicia', N'Aprobada', '2026-04-15 09:30:00', 1),
(31, 17, '2026-05-30 14:00:00', N'Seguimiento', N'Aprobada', '2026-05-30 14:30:00', 1),
(32, 18, '2026-06-05 11:00:00', N'Primera consulta, sin síntomas', N'Aprobada', '2026-06-05 11:30:00', 1),
(33, 19, '2026-04-10 13:00:00', N'Miopía con astigmatismo', N'Aprobada', '2026-04-10 13:30:00', 1),
(34, 19, '2026-06-02 16:00:00', N'Entrega y ajuste de nueva receta', N'Aprobada', '2026-06-02 16:30:00', 1),
(35, 20, '2026-05-18 09:00:00', N'Presbicia incipiente', N'Aprobada', '2026-05-18 09:30:00', 1),
(36, 21, '2026-03-30 11:00:00', N'Presbicia + astigmatismo', N'Aprobada', '2026-03-30 11:30:00', 1),
(37, 21, '2026-05-25 15:00:00', N'Renovación de progresivos', N'Aprobada', '2026-05-25 15:30:00', 1),
(38, 22, '2026-05-12 10:00:00', N'Miopía, primera consulta', N'Aprobada', '2026-05-12 10:30:00', 1),
(39, 23, '2026-04-22 11:00:00', N'Presbicia con astigmatismo', N'Aprobada', '2026-04-22 11:30:00', 1),
-- (40 eliminado: Solicitud huérfana sin Cita asociada)
(41, 24, '2026-05-05 14:00:00', N'Miopía escolar', N'Aprobada', '2026-05-05 14:30:00', 1),
(42, 25, '2026-04-28 10:00:00', N'Miopía con astigmatismo', N'Aprobada', '2026-04-28 10:30:00', 1),
(43, 25, '2026-06-04 13:00:00', N'Entrega de nueva receta', N'Aprobada', '2026-06-04 13:30:00', 1),
(44, 5, '2026-05-15 10:00:00', N'Primera consulta (cancelada por paciente)', N'Rechazada', '2026-05-15 11:00:00', 1),
(45, 12, '2026-04-10 11:00:00', N'Cita cancelada por cambio de horario', N'Rechazada', '2026-04-10 12:00:00', 1),
(46, 18, '2026-05-20 16:00:00', N'Paciente no estaba disponible', N'Rechazada', '2026-05-20 17:00:00', 1),
(47, 24, '2026-04-15 15:00:00', N'Sin disponibilidad del paciente', N'Rechazada', '2026-04-15 16:00:00', 1),
(48, 11, '2026-03-20 10:00:00', N'Paciente no se presentó', N'Rechazada', '2026-03-20 11:00:00', 1),
(49, 16, '2026-04-05 14:00:00', N'Paciente no confirmó', N'Rechazada', '2026-04-05 15:00:00', 1),
(50, 22, '2026-04-18 11:00:00', N'Cita rechazada, reagendada', N'Rechazada', '2026-04-18 12:00:00', 1),
(51,  2,'2026-05-02 10:00:00', N'Cita de seguimiento (aprobada por recepcionista)', N'Aprobada', '2026-05-02 10:30:00', 1)
;
SET IDENTITY_INSERT SolicitudesCitas OFF;

-- ============================================================
-- 4) CITAS (50) — referencia SolicitudCita 1:1
-- ============================================================
PRINT '4) Insertando Citas...';
SET IDENTITY_INSERT Citas ON;
INSERT INTO Citas
  (Id, PacienteId, SolicitudCitaId, SucursalId, FechaHora, MotivoConsulta,
   ObservacionesEspecialista, Estado, UsuarioAsignadoId, FechaCreacion,
   NotificacionesActivas, CanalNotificacion)
VALUES

(1, 1, 1, 1, '2026-03-15 09:00:00', N'Control anual y graduación', N'Miopía estable. Refracción actualizada. Paciente refiere satisfacción con sus progresivos actuales.', N'Atendida', 1, '2026-03-10 10:00:00', 1, N'Email'),
(2, 1, 2, 1, '2026-04-25 10:00:00', N'Renovación de progresivos', N'Pequeño aumento de ADD. Indicado nuevo par de progresivos con tratamiento blue cut.', N'Atendida', 1, '2026-04-22 14:30:00', 1, N'Email'),
(3, 2, 4, 1, '2026-04-05 09:00:00', N'Miopía progresiva', N'Aumento de 0.50 D en AO respecto a receta anterior. Indicado lentes con índice 1.56.', N'Atendida', 1, '2026-04-02 16:30:00', 1, N'WhatsApp'),
(4, 3, 6, 1, '2026-04-20 11:00:00', N'Chequeo de rutina', N'Agudeza visual 20/20 en AO. Emétrope. No requiere corrección.', N'Atendida', 1, '2026-04-18 13:30:00', 1, N'Email'),
(5, 4, 7, 1, '2026-03-25 10:00:00', N'Presbicia incipiente', N'Inicia presbicia. ADD +1.50. Recomendados progresivos básicos para oficina.', N'Atendida', 1, '2026-03-22 09:30:00', 1, N'Email'),
(6, 6, 10, 1, '2026-03-28 10:00:00', N'Hipermetropía + presbicia', N'Hipermetropía alta + presbicia. Adaptación a progresivos requiere entrenamiento.', N'Atendida', 1, '2026-03-25 11:30:00', 1, N'Email'),
(7, 6, 11, 1, '2026-05-12 11:00:00', N'Cambio de aro por daño', N'Paciente solicita aro nuevo, conservando la misma graduación. Adquirió Ray-Ban Aviator.', N'Atendida', 1, '2026-05-10 15:30:00', 1, N'SMS'),
(8, 7, 13, 1, '2026-04-08 10:00:00', N'Astigmatismo diagnosticado', N'Astigmatismo miópico bilateral. Cilindro bajo. Indicados lentes tóricos.', N'Atendida', 1, '2026-04-05 11:00:00', 1, N'WhatsApp'),
(9, 8, 15, 1, '2026-04-14 11:00:00', N'Miopía con astigmatismo', N'Combinación miópica con astigmatismo. Recomendados blue cut por uso prolongado de pantallas.', N'Atendida', 1, '2026-04-12 17:00:00', 1, N'Email'),
(10, 9, 17, 1, '2026-04-22 09:00:00', N'Control pediátrico', N'Miopía leve en ojo izquierdo. Bajo seguimiento. Receta: monofocal con blue cut.', N'Atendida', 1, '2026-04-20 09:30:00', 1, N'Email'),
(11, 10, 19, 1, '2026-03-30 14:00:00', N'Presbicia primera consulta', N'Paciente ingeniero, mucho trabajo de cerca. Recomendados progresivos office.', N'Atendida', 1, '2026-03-28 14:30:00', 1, N'Email'),
(12, 11, 21, 1, '2026-05-02 11:00:00', N'Chequeo preventivo', N'Agudeza 20/20 AO. Sin hallazgos. Control anual recomendado.', N'Atendida', 1, '2026-04-30 12:00:00', 1, N'Email'),
(13, 12, 22, 1, '2026-05-17 10:00:00', N'Miopía progresiva con fatiga visual', N'Aumento significativo. Recomendado tratamiento anti-fatiga digital.', N'Atendida', 1, '2026-05-15 16:30:00', 1, N'WhatsApp'),
(14, 13, 23, 1, '2026-03-17 09:00:00', N'Presbicia avanzada', N'Paciente 70 años, ADD alta. Bifocales recomendados por simplicidad.', N'Atendida', 1, '2026-03-15 09:30:00', 1, N'Email'),
(15, 13, 24, 1, '2026-04-27 14:00:00', N'Renovación con bifocales', N'Renovación exitosa. Paciente adaptada a bifocales sin mareos.', N'Atendida', 1, '2026-04-25 14:30:00', 1, N'Email'),
(16, 14, 26, 1, '2026-04-10 15:00:00', N'Miopía escolar primera receta', N'Estudiante 13 años. Miopía incipiente. Indicado monofocal con blue cut.', N'Atendida', 1, '2026-04-08 15:30:00', 1, N'Email'),
(17, 15, 28, 1, '2026-05-22 10:00:00', N'Presbicia incipiente', N'ADD +1.00. Inicia con monofocales para cerca, dejará progresivos para futuro.', N'Atendida', 1, '2026-05-20 10:30:00', 0, N'Email'),
(18, 16, 29, 1, '2026-05-24 16:00:00', N'Miopía primera consulta', N'Adulto joven con miopía moderada. Indicados lentes monofocales con anti-reflejo.', N'Atendida', 1, '2026-05-22 16:30:00', 1, N'WhatsApp'),
(19, 17, 30, 1, '2026-04-17 09:00:00', N'Hipermetropía + presbicia', N'Paciente con hipermetropía oculta + presbicia. Progresivos con buen resultado.', N'Atendida', 1, '2026-04-15 09:30:00', 1, N'Email'),
(20, 19, 33, 1, '2026-04-12 13:00:00', N'Miopía con astigmatismo', N'Miopía con astigmatismo moderado. Blue cut + anti-reflejo por trabajo de oficina.', N'Atendida', 1, '2026-04-10 13:30:00', 1, N'Email'),
(21, 21, 36, 1, '2026-04-01 11:00:00', N'Presbicia + astigmatismo', N'Combinación de presbicia alta con astigmatismo bajo. Progresivos tóricos.', N'Atendida', 1, '2026-03-30 11:30:00', 1, N'Email'),
(22, 25, 42, 1, '2026-04-30 10:00:00', N'Miopía con astigmatismo', N'Profesional con uso intensivo de pantallas. Blue cut + transición.', N'Atendida', 1, '2026-04-28 10:30:00', 1, N'Email'),
(23, 1, 3, 1, '2026-06-12 11:00:00', N'Limpieza y ajuste', NULL, N'Confirmada', 1, '2026-06-05 11:45:00', 1, N'Email'),
(24, 2, 5, 1, '2026-06-15 10:00:00', N'Entrega de receta', NULL, N'Confirmada', 1, '2026-06-01 11:00:00', 1, N'WhatsApp'),
(25, 4, 8, 1, '2026-06-16 14:00:00', N'Seguimiento de progresivos', NULL, N'Confirmada', 1, '2026-05-29 14:30:00', 1, N'Email'),
(26, 5, 9, 1, '2026-06-10 10:00:00', N'Primera consulta', NULL, N'Confirmada', 1, '2026-06-04 10:30:00', 1, N'Email'),
(27, 6, 12, 1, '2026-06-11 09:00:00', N'Seguimiento post-adaptación', NULL, N'Confirmada', 1, '2026-06-02 09:30:00', 1, N'Email'),
(28, 7, 14, 1, '2026-06-18 14:00:00', N'Adaptación a lentes tóricos', NULL, N'Confirmada', 1, '2026-06-03 14:30:00', 1, N'WhatsApp'),
(29, 8, 16, 1, '2026-06-13 11:00:00', N'Entrega de nueva prescripción', NULL, N'Confirmada', 1, '2026-05-30 11:30:00', 1, N'Email'),
(30, 9, 18, 1, '2026-06-19 15:00:00', N'Control pediátrico', NULL, N'Confirmada', 1, '2026-06-04 15:30:00', 1, N'Email'),
(31, 10, 20, 1, '2026-06-10 10:00:00', N'Control post-adaptación', NULL, N'Confirmada', 1, '2026-05-25 10:30:00', 1, N'Email'),
(32, 13, 25, 1, '2026-06-13 10:00:00', N'Seguimiento de agudeza visual', NULL, N'Confirmada', 1, '2026-06-01 10:30:00', 1, N'Email'),
(33, 14, 27, 1, '2026-06-17 11:00:00', N'Control y ajuste de lentes', NULL, N'Confirmada', 1, '2026-05-28 11:30:00', 1, N'Email'),
(34, 17, 31, 1, '2026-06-12 14:00:00', N'Seguimiento', NULL, N'Confirmada', 1, '2026-05-30 14:30:00', 1, N'Email'),
(35, 21, 37, 1, '2026-06-14 15:00:00', N'Renovación de progresivos', NULL, N'Confirmada', 1, '2026-05-25 15:30:00', 1, N'Email'),
(36, 18, 32, 1, '2026-06-15 11:00:00', N'Primera consulta', NULL, N'Pendiente', NULL, '2026-06-05 11:30:00', 1, N'Email'),
(37, 19, 34, 1, '2026-06-16 16:00:00', N'Entrega y ajuste de nueva receta', NULL, N'Pendiente', 1, '2026-06-02 16:30:00', 1, N'Email'),
(38, 20, 35, 1, '2026-06-19 09:00:00', N'Presbicia incipiente', NULL, N'Pendiente', 1, '2026-05-18 09:30:00', 1, N'Email'),
(39, 23, 39, 1, '2026-06-11 16:00:00', N'Seguimiento y ajuste', NULL, N'Pendiente', 1, '2026-05-27 16:30:00', 1, N'WhatsApp'),
(40, 24, 41, 1, '2026-06-13 14:00:00', N'Miopía escolar', NULL, N'Pendiente', 1, '2026-05-05 14:30:00', 1, N'Email'),
(41, 25, 43, 1, '2026-06-18 13:00:00', N'Entrega de nueva receta', NULL, N'Pendiente', 1, '2026-06-04 13:30:00', 1, N'Email'),
(42, 22, 38, 1, '2026-05-14 10:00:00', N'Primera consulta', N'Paciente no se presentó, reagendada.', N'Cancelada', 1, '2026-05-12 10:30:00', 0, N'Email'),
(43, 5, 44, 1, '2026-05-17 10:00:00', N'Primera consulta', N'Cancelada por paciente con 2 horas de anticipación.', N'Cancelada', 1, '2026-05-15 11:00:00', 1, N'Email'),
(44, 12, 45, 1, '2026-04-12 11:00:00', N'Consulta general', N'Paciente canceló por motivos laborales.', N'Cancelada', 1, '2026-04-10 12:00:00', 1, N'WhatsApp'),
(45, 18, 46, 1, '2026-05-22 16:00:00', N'Primera consulta', N'Cancelada por paciente.', N'Cancelada', NULL, '2026-05-20 17:00:00', 1, N'Email'),
(46, 24, 47, 1, '2026-04-17 15:00:00', N'Miopía escolar', N'Paciente no se presentó.', N'Cancelada', 1, '2026-04-15 16:00:00', 0, N'Email'),
(47, 11, 48, 1, '2026-03-22 10:00:00', N'Chequeo preventivo', N'Paciente no confirmó ni se presentó.', N'Cancelada', 1, '2026-03-20 11:00:00', 0, N'Email'),
(48, 16, 49, 1, '2026-04-07 14:00:00', N'Miopía primera consulta', N'Paciente olvidó la cita.', N'Cancelada', 1, '2026-04-05 15:00:00', 0, N'Email'),
(49, 22, 50, 1, '2026-05-14 10:00:00', N'Primera consulta', N'Paciente reagendó por segunda vez. Sin reagendamiento nuevo.', N'Cancelada', 1, '2026-04-18 12:00:00', 1, N'Email'),
(50, 2, 51, 1, '2026-05-10 09:00:00', N'Seguimiento miopía', N'Cita adicional solicitada por el paciente.', N'Atendida', 1, '2026-05-02 10:00:00', 1, N'WhatsApp')
;
SET IDENTITY_INSERT Citas OFF;

-- ============================================================
-- 5) EXPEDIENTES (22) — uno por cada Cita Atendida
-- ============================================================
PRINT '5) Insertando Expedientes...';
SET IDENTITY_INSERT Expedientes ON;
INSERT INTO Expedientes (Id, CitaId, MotivoConsulta, Observaciones, FechaRegistro) VALUES
(1,  1,  N'Control anual y graduación',
       N'Paciente colaboradora. Refracción estable. Continuar con progresivos actuales hasta nueva graduación.',
       '2026-03-15 09:45:00'),
(2,  2,  N'Renovación de progresivos',
       N'Paciente requiere nuevo par de progresivos. Se toma graduación actualizada.',
       '2026-04-25 10:45:00'),
(3,  3,  N'Miopía progresiva',
       N'Aumento de miopía. Refracción ciclopléjica confirma cambio.',
       '2026-04-05 09:45:00'),
(4,  4,  N'Chequeo de rutina',
       N'Sin hallazgos patológicos. Emétrope.',
       '2026-04-20 11:45:00'),
(5,  5,  N'Presbicia incipiente',
       N'Inicia presbicia. Sin antecedentes familiares de relevancia.',
       '2026-03-25 10:45:00'),
(6,  6,  N'Hipermetropía + presbicia',
       N'Paciente 65 años. Adaptación a progresivos progresiva.',
       '2026-03-28 10:45:00'),
(7,  7,  N'Cambio de aro por daño',
       N'Conservar misma graduación. Selección de aro nuevo.',
       '2026-05-12 11:45:00'),
(8,  8,  N'Astigmatismo diagnosticado',
       N'Astigmatismo miópico bilateral. No se observan signos de queratocono.',
       '2026-04-08 10:45:00'),
(9,  9,  N'Miopía con astigmatismo',
       N'Resultado estable respecto a control previo.',
       '2026-04-14 11:45:00'),
(10, 10, N'Control pediátrico',
       N'Paciente 7 años. Emétrope OD, miopía leve OI. Sin ambliopía.',
       '2026-04-22 09:45:00'),
(11, 11, N'Presbicia primera consulta',
       N'Profesional con mucha lectura. ADD determinada en posición de trabajo.',
       '2026-03-30 14:45:00'),
(12, 12, N'Chequeo preventivo',
       N'Emétrope. Agudeza 20/20. PIO normal.',
       '2026-05-02 11:45:00'),
(13, 13, N'Miopía progresiva con fatiga visual',
       N'Fatiga visual relacionada con trabajo de oficina.',
       '2026-05-17 10:45:00'),
(14, 14, N'Presbicia avanzada',
       N'Paciente con ADD alta. Bifocales como primera opción.',
       '2026-03-17 09:45:00'),
(15, 15, N'Renovación con bifocales',
       N'Paciente adaptada, sin síntomas.',
       '2026-04-27 14:45:00'),
(16, 16, N'Miopía escolar primera receta',
       N'Estudiante. Miopía incipiente, monitoreo cada 6 meses.',
       '2026-04-10 15:45:00'),
(17, 17, N'Presbicia incipiente',
       N'Paciente asintomática fuera de lectura.',
       '2026-05-22 10:45:00'),
(18, 18, N'Miopía primera consulta',
       N'Adulto joven con miopía moderada. Sin antecedentes familiares.',
       '2026-05-24 16:45:00'),
(19, 19, N'Hipermetropía + presbicia',
       N'Excelente tolerancia a progresivos.',
       '2026-04-17 09:45:00'),
(20, 20, N'Miopía con astigmatismo',
       N'Funcionario corporativo con uso intensivo de pantallas.',
       '2026-04-12 13:45:00'),
(21, 21, N'Presbicia + astigmatismo',
       N'Combinación tórica. Buena visión con progresivos.',
       '2026-04-01 11:45:00'),
(22, 22, N'Miopía con astigmatismo',
       N'Desarrolladora de software. Necesidad de blue cut.',
       '2026-04-30 10:45:00'),
(23, 50, N'Seguimiento de miopía',
       N'Control programado tras 5 semanas de uso de nuevos lentes. Refracción estable.',
       '2026-05-10 09:45:00');
SET IDENTITY_INSERT Expedientes OFF;

-- ============================================================
-- 6) VALORESCLINICOS (22) — refracción realista
-- ============================================================
PRINT '6) Insertando ValoresClinicos...';
SET IDENTITY_INSERT ValoresClinicos ON;
INSERT INTO ValoresClinicos
  (Id, ExpedienteId, Diagnostico,
   EsferaOD, CilindroOD, EjeOD, EsferaOI, CilindroOI, EjeOI,
   ADD_Od, ADD_Oi,
   AvOdLejos, AvOiLejos, AvOdCerca, AvOiCerca,
   PioOd, PioOi,
   PercepcionColores, MotilidadOcular, FondoOjo, CampoVisual, Observaciones,
   FechaRegistro)
VALUES
(1,  1, N'Miopía con presbicia',
     -2.25, -0.50, 180, -2.50, -0.75, 175,  1.50,  1.50,
     N'20/30', N'20/30', N'20/40', N'20/40', 14, 15,
     N'Normal', N'Normal', N'Papila normal, mácula sin alteraciones', N'Normal',
     N'Paciente adaptada a progresivos.',
     '2026-03-15 09:30:00'),
(2,  2, N'Miopía con presbicia (progresivos)',
     -2.50, -0.50, 180, -2.75, -0.75, 175,  1.75,  1.75,
     N'20/25', N'20/30', N'20/40', N'20/40', 14, 15,
     N'Normal', N'Normal', N'Sin lesiones', N'Normal',
     N'Pequeño aumento de ADD por edad.',
     '2026-04-25 10:30:00'),
(3,  3, N'Miopía simple',
     -2.00, NULL,  NULL, -2.25, NULL,  NULL,  NULL,  NULL,
     N'20/30', N'20/30', N'20/20', N'20/20', 13, 14,
     N'Normal', N'Normal', N'Normal', N'Normal',
     N'Aumento respecto a receta previa.',
     '2026-04-05 09:30:00'),
(4,  4, N'Emetropía',
     NULL,  NULL,  NULL, NULL,  NULL,  NULL,  NULL,  NULL,
     N'20/20', N'20/20', N'20/20', N'20/20', 12, 13,
     N'Normal', N'Normal', N'Normal', N'Normal',
     N'Sin hallazgos. Control anual.',
     '2026-04-20 11:30:00'),
(5,  5, N'Presbicia incipiente',
      0.50, NULL,  NULL,  0.25, NULL,  NULL,  1.50,  1.50,
     N'20/20', N'20/20', N'20/50', N'20/50', 15, 15,
     N'Normal', N'Normal', N'Normal', N'Normal',
     N'Inicio de presbicia.',
     '2026-03-25 10:30:00'),
(6,  6, N'Hipermetropía con presbicia',
      3.00, -0.50,  90,  2.75, -0.75,  85,  2.25,  2.25,
     N'20/40', N'20/40', N'20/60', N'20/60', 16, 17,
     N'Normal', N'Normal', N'Papila algo pálida', N'Ligeramente disminuido nasal',
     N'Paciente geriátrico. Seguimiento estrecho.',
     '2026-03-28 10:30:00'),
(7,  7, N'Hipermetropía con presbicia (aro nuevo)',
      2.75, -0.50,  90,  2.50, -0.75,  85,  2.25,  2.25,
     N'20/30', N'20/40', N'20/50', N'20/50', 15, 16,
     N'Normal', N'Normal', N'Estable', N'Normal',
     N'Misma graduación, cambio de aro.',
     '2026-05-12 11:30:00'),
(8,  8, N'Astigmatismo miópico',
     -1.00, -0.75,  20, -1.25, -0.50, 165,  NULL,  NULL,
     N'20/30', N'20/30', N'20/20', N'20/20', 14, 14,
     N'Normal', N'Normal', N'Normal', N'Normal',
     N'Topografía corneal normal.',
     '2026-04-08 10:30:00'),
(9,  9, N'Miopía con astigmatismo',
     -2.50, -1.00, 175, -2.75, -1.25, 180,  NULL,  NULL,
     N'20/30', N'20/30', N'20/20', N'20/20', 13, 13,
     N'Normal', N'Normal', N'Normal', N'Normal',
     N'Paciente asintomático entre controles.',
     '2026-04-14 11:30:00'),
(10, 10, N'Miopía leve OI (pediátrica)',
      0.00, NULL,  NULL, -0.50, NULL,  NULL,  NULL,  NULL,
     N'20/20', N'20/30', N'20/20', N'20/20', 12, 12,
     N'Normal', N'Normal', N'Fondo de ojo normal', N'Normal',
     N'Seguimiento cada 6 meses. Riesgo de progresión.',
     '2026-04-22 09:30:00'),
(11, 11, N'Presbicia (office)',
     -0.25, NULL,  NULL, -0.25, NULL,  NULL,  2.00,  2.00,
     N'20/20', N'20/20', N'20/40', N'20/40', 15, 15,
     N'Normal', N'Normal', N'Normal', N'Normal',
     N'Progresivos office. Posición intermedia.',
     '2026-03-30 14:30:00'),
(12, 12, N'Emetropía',
     NULL,  NULL,  NULL, NULL,  NULL,  NULL,  NULL,  NULL,
     N'20/20', N'20/20', N'20/20', N'20/20', 13, 14,
     N'Normal', N'Normal', N'Normal', N'Normal',
     N'Paciente sano.',
     '2026-05-02 11:30:00'),
(13, 13, N'Miopía simple con fatiga visual',
     -2.75, -0.50, 180, -3.00, -0.50, 175,  NULL,  NULL,
     N'20/30', N'20/30', N'20/20', N'20/20', 14, 15,
     N'Normal', N'Normal', N'Normal', N'Normal',
     N'Recomendados lentes anti-fatiga.',
     '2026-05-17 10:30:00'),
(14, 14, N'Presbicia avanzada',
      1.00, -0.50,  90,  1.25, -0.50,  90,  2.50,  2.50,
     N'20/40', N'20/40', N'20/80', N'20/80', 17, 18,
     N'Normal', N'Foria leve', N'Catarata incipiente', N'Ligeramente disminuido',
     N'Paciente añosa. Monitoreo de cataratas.',
     '2026-03-17 09:30:00'),
(15, 15, N'Presbicia avanzada (control)',
      1.00, -0.50,  90,  1.25, -0.50,  90,  2.50,  2.50,
     N'20/40', N'20/40', N'20/80', N'20/80', 17, 18,
     N'Normal', N'Foria leve', N'Estable', N'Estable',
     N'Sin cambios significativos.',
     '2026-04-27 14:30:00'),
(16, 16, N'Miopía escolar',
     -1.00, NULL,  NULL, -1.25, NULL,  NULL,  NULL,  NULL,
     N'20/30', N'20/30', N'20/20', N'20/20', 12, 12,
     N'Normal', N'Normal', N'Normal', N'Normal',
     N'Control cada 6 meses por riesgo de progresión.',
     '2026-04-10 15:30:00'),
(17, 17, N'Presbicia incipiente',
      0.25, NULL,  NULL,  0.25, NULL,  NULL,  1.00,  1.00,
     N'20/20', N'20/20', N'20/30', N'20/30', 14, 14,
     N'Normal', N'Normal', N'Normal', N'Normal',
     N'Inicia con monofocales para cerca.',
     '2026-05-22 10:30:00'),
(18, 18, N'Miopía simple',
     -1.75, NULL,  NULL, -2.00, NULL,  NULL,  NULL,  NULL,
     N'20/30', N'20/30', N'20/20', N'20/20', 13, 13,
     N'Normal', N'Normal', N'Normal', N'Normal',
     N'Adulto joven con miopía moderada.',
     '2026-05-24 16:30:00'),
(19, 19, N'Hipermetropía con presbicia',
      2.25, -0.50,  90,  2.50, -0.50,  90,  1.75,  1.75,
     N'20/30', N'20/30', N'20/50', N'20/50', 15, 16,
     N'Normal', N'Normal', N'Normal', N'Normal',
     N'Buena tolerancia.',
     '2026-04-17 09:30:00'),
(20, 20, N'Miopía con astigmatismo',
     -2.25, -0.75, 180, -2.50, -1.00, 175,  NULL,  NULL,
     N'20/30', N'20/30', N'20/20', N'20/20', 14, 14,
     N'Normal', N'Normal', N'Normal', N'Normal',
     N'Blue cut por trabajo de oficina.',
     '2026-04-12 13:30:00'),
(21, 21, N'Presbicia con astigmatismo',
      0.75, -1.00,  90,  1.00, -1.25,  90,  2.25,  2.25,
     N'20/30', N'20/30', N'20/60', N'20/60', 16, 16,
     N'Normal', N'Normal', N'Normal', N'Normal',
     N'Progresivos tóricos.',
     '2026-04-01 11:30:00'),
(22, 22, N'Miopía con astigmatismo',
     -2.50, -1.00, 180, -2.75, -1.25, 175,  NULL,  NULL,
     N'20/30', N'20/30', N'20/20', N'20/20', 13, 14,
     N'Normal', N'Normal', N'Normal', N'Normal',
     N'Programadora con alta demanda visual.',
     '2026-04-30 10:30:00'),
(23, 23, N'Miopía simple (control)',
     -2.25, NULL,  NULL, -2.50, NULL,  NULL,  NULL,  NULL,
     N'20/25', N'20/30', N'20/20', N'20/20', 13, 14,
     N'Normal', N'Normal', N'Normal', N'Normal',
     N'Refracción estable respecto a control previo. Sin cambios.',
     '2026-05-10 09:30:00');
SET IDENTITY_INSERT ValoresClinicos OFF;

-- ============================================================
-- 7) VENTAS (20) — asociadas a expedientes con lentes
-- ============================================================
PRINT '7) Insertando Ventas...';
SET IDENTITY_INSERT Ventas ON;
INSERT INTO Ventas
  (Id, NumeroFactura, PacienteId, UsuarioId, ValorClinicoId,
   MetodoPago, Total, Descuento, Notas, FechaVenta, SucursalId,
   ReferenciaPago, RutaComprobante)
VALUES

(1, N'F-2026-0001', 1, 1, 1, 3, 201140.00, 0.00, N'Renovación de progresivos blue cut + aro metálico', '2026-03-15 11:00:00', 1, N'VISA-****4521', NULL),
(2, N'F-2026-0002', 1, 1, 2, 2, 245210.00, 5000.00, N'Segundo par de progresivos. Descuento cliente frecuente', '2026-04-25 12:00:00', 1, N'SINPE-998877', NULL),
(3, N'F-2026-0003', 2, 1, 3, 3, 137860.00, 0.00, N'Monofocales con anti-reflejo + aro deportivo', '2026-04-05 10:30:00', 1, N'VISA-****7832', NULL),
(4, N'F-2026-0004', 4, 1, 5, 1, 248600.00, 0.00, N'Progresivos básicos + aro titanio', '2026-03-25 11:30:00', 1, NULL, NULL),
(5, N'F-2026-0005', 6, 1, 6, 3, 256510.00, 8000.00, N'Progresivos high index + aro Gucci', '2026-03-28 12:00:00', 1, N'MASTER-****1245', NULL),
(6, N'F-2026-0006', 6, 1, 7, 2, 74580.00, 0.00, N'Aro Ray-Ban Aviator conservando graduación', '2026-05-12 13:00:00', 1, N'SINPE-665544', NULL),
(7, N'F-2026-0007', 7, 1, 8, 3, 146900.00, 0.00, N'Lentes tóricos blue cut + aro Cat-Eye', '2026-04-08 11:30:00', 1, N'VISA-****9911', NULL),
(8, N'F-2026-0008', 8, 1, 9, 2, 123170.00, 0.00, N'Blue cut + anti-reflejo + aro metálico cuadrado', '2026-04-14 12:30:00', 1, N'SINPE-332211', NULL),
(9, N'F-2026-0009', 9, 1, 10, 1, 105090.00, 0.00, N'Monofocales pediatric blue cut + aro infantil', '2026-04-22 10:30:00', 1, N'EFECTIVO', NULL),
(10, N'F-2026-0010', 10, 1, 11, 3, 248600.00, 0.00, N'Progresivos office + aro titanio', '2026-03-30 15:30:00', 1, N'VISA-****6778', NULL),
(11, N'F-2026-0011', 12, 1, 13, 3, 131080.00, 0.00, N'Anti-fatiga digital + aro deportivo wrap', '2026-05-17 11:30:00', 1, N'MASTER-****2233', NULL),
(12, N'F-2026-0012', 13, 1, 14, 1, 138990.00, 0.00, N'Bifocales con anti-reflejo + aro metálico clásico', '2026-03-17 10:30:00', 1, N'EFECTIVO', NULL),
(13, N'F-2026-0013', 13, 1, 15, 2, 142380.00, 0.00, N'Bifocales renovados + aro vintage oval', '2026-04-27 15:30:00', 1, N'SINPE-778899', NULL),
(14, N'F-2026-0014', 14, 1, 16, 3, 105090.00, 0.00, N'Monofocales blue cut + aro flex silicona', '2026-04-10 16:30:00', 1, N'VISA-****4456', NULL),
(15, N'F-2026-0015', 16, 1, 18, 2, 124300.00, 0.00, N'Monofocales con anti-reflejo + aro wayfarer', '2026-05-24 17:30:00', 1, N'SINPE-554433', NULL),
(16, N'F-2026-0016', 17, 1, 19, 3, 205660.00, 0.00, N'Progresivos con blue cut + aro oversize', '2026-04-17 10:30:00', 1, N'VISA-****8821', NULL),
(17, N'F-2026-0017', 19, 1, 20, 3, 109610.00, 0.00, N'Blue cut + anti-reflejo + aro metálico cuadrado', '2026-04-12 14:30:00', 1, N'MASTER-****9988', NULL),
(18, N'F-2026-0018', 21, 1, 21, 1, 190970.00, 10000.00, N'Progresivos tóricos con anti-reflejo + aro Oakley', '2026-04-01 12:30:00', 1, N'EFECTIVO', NULL),
(19, N'F-2026-0019', 22, 1, NULL, 3, 84750.00, 0.00, N'Lentes de sol polarizados sin receta', '2026-05-12 11:30:00', 1, N'VISA-****1155', NULL),
(20, N'F-2026-0020', 25, 1, 22, 2, 167240.00, 0.00, N'Transitions + blue cut + aro metálico', '2026-04-30 11:30:00', 1, N'SINPE-991122', NULL)
;
SET IDENTITY_INSERT Ventas OFF;

-- ============================================================
-- 8) DETALLEVENTAS (~40) — 1-3 líneas por venta
-- ============================================================
PRINT '8) Insertando DetalleVentas...';
SET IDENTITY_INSERT DetalleVentas ON;
INSERT INTO DetalleVentas (Id, VentaId, ProductoId, DescripcionSnapshot, Cantidad, PrecioUnitario, Subtotal) VALUES
-- Venta 1: progresivos + aro
(1,  1,  13, N'Lentes Progresivos Liberty Plus',      1, 125000.00, 125000.00),
(2,  1,  1,  N'Aro Metálico Redondo',                 1, 35000.00,  35000.00),
(3,  1,  19, N'Estuche Rígido Premium Negro',         1, 18000.00,  18000.00),
-- Venta 2: progresivos + aro + kit
(4,  2,  13, N'Lentes Progresivos Liberty Plus',     1, 125000.00, 125000.00),
(5,  2,  NULL, N'Aro Versace VE-3029 (SKU: ARO-VRS-025)', 1, 82000.00, 82000.00),
(6,  2,  21, N'Kit de Limpieza Completo',            1,  15000.00,  15000.00),
-- Venta 3: monofocales + aro
(7,  3,  18, N'Lentes Deportivos Recetados',         1,  82000.00,  82000.00),
(8,  3,  4,  N'Aro Deportivo Wrap',                  1,  40000.00,  40000.00),
-- Venta 4: progresivos + aro
(9,  4,  13, N'Lentes Progresivos Liberty Plus',     1, 125000.00, 125000.00),
(10, 4,  NULL, N'Aro Titanio Ultra (SKU: ARO-TIT-001)',1,  95000.00,  95000.00),
-- Venta 5: high index + aro de marca
(11, 5,  16, N'Lentes High Index 1.74 Ultra',        1, 140000.00, 140000.00),
(12, 5,  NULL, N'Aro Gucci GG0061S (SKU: ARO-GUC-023)', 1, 95000.00, 95000.00),
-- Venta 6: solo aro, conserva graduación
(13, 6,  NULL, N'Ray-Ban Aviator Classic (no catalogado)', 1,  48000.00,  48000.00),
(14, 6,  19, N'Estuche Rígido Premium Negro',         1,  18000.00,  18000.00),
-- Venta 7: blue cut + aro Cat-Eye
(15, 7,  2,  N'Lentes BlueCut Digital',              1,  65000.00,  65000.00),
(16, 7,  3,  N'Aro Cat-Eye Femenino',                1,  55000.00,  55000.00),
(17, 7,  20, N'Paño de Limpieza Microfibra',         2,   5000.00,  10000.00),
-- Venta 8: blue cut + aro
(18, 8,  2,  N'Lentes BlueCut Digital',              1,  65000.00,  65000.00),
(19, 8,  9,  N'Aro Metálico Cuadrado',               1,  32000.00,  32000.00),
(20, 8,  24, N'Gotas Hidratantes Oftálmicas',        1,  12000.00,  12000.00),
-- Venta 9: pediátrico
(21, 9,  14, N'Lentes Antifatiga Digital Plus',      1,  68000.00,  68000.00),
(22, 9,  6,  N'Aro Infantil Disney',                 1,  25000.00,  25000.00),
-- Venta 10: office
(23, 10, 13, N'Lentes Progresivos Liberty Plus',     1, 125000.00, 125000.00),
(24, 10, NULL, N'Aro Titanio Ultra (SKU: ARO-TIT-001)', 1,  95000.00,  95000.00),
-- Venta 11: antifatiga
(25, 11, 14, N'Lentes Antifatiga Digital Plus',      1,  68000.00,  68000.00),
(26, 11, 4,  N'Aro Deportivo Wrap',                  1,  40000.00,  40000.00),
(27, 11, 22, N'Cordón de Nylon Premium',             1,   8000.00,   8000.00),
-- Venta 12: bifocales clásico
(28, 12, 17, N'Lentes Bifocales Office Pro',         1,  88000.00,  88000.00),
(29, 12, 1,  N'Aro Metálico Redondo',                1,  35000.00,  35000.00),
-- Venta 13: bifocales renovados
(30, 13, 17, N'Lentes Bifocales Office Pro',         1,  88000.00,  88000.00),
(31, 13, 7,  N'Aro Vintage Oval',                   1,  38000.00,  38000.00),
-- Venta 14: escolar
(32, 14, 2,  N'Lentes BlueCut Digital',              1,  65000.00,  65000.00),
(33, 14, 10, N'Aro Flex Silicona',                   1,  28000.00,  28000.00),
-- Venta 15: adulto joven
(34, 15, 2,  N'Lentes BlueCut Digital',              1,  65000.00,  65000.00),
(35, 15, 7,  N'Aro Wayfarer Clásico',                1,  45000.00,  45000.00),
-- Venta 16: progresivos
(36, 16, 13, N'Lentes Progresivos Liberty Plus',     1, 125000.00, 125000.00),
(37, 16, 8,  N'Aro Oversize Grande',                 1,  42000.00,  42000.00),
(38, 16, 21, N'Kit de Limpieza Completo',            1,  15000.00,  15000.00),
-- Venta 17: blue cut
(39, 17, 2,  N'Lentes BlueCut Digital',              1,  65000.00,  65000.00),
(40, 17, 9,  N'Aro Metálico Cuadrado',               1,  32000.00,  32000.00),
-- Venta 18: progresivos tóricos
(41, 18, 13, N'Lentes Progresivos Liberty Plus',     1, 125000.00, 125000.00),
(42, 18, 22, N'Oakley Frogskins',                    1,  42000.00,  42000.00),
(43, 18, 23, N'Gotas Hidratantes Oftálmicas',        1,  12000.00,  12000.00),
-- Venta 19: solo sol
(44, 19, 7,  N'Lentes de Sol Sport Pro MTB',         1,  75000.00,  75000.00),
-- Venta 20: transitions
(45, 20, 15, N'Lentes Transitions Gen 8',            1,  95000.00,  95000.00),
(46, 20, 1,  N'Aro Metálico Redondo',                1,  35000.00,  35000.00),
(47, 20, 19, N'Estuche Rígido Premium Negro',        1,  18000.00,  18000.00);
SET IDENTITY_INSERT DetalleVentas OFF;

-- ============================================================
-- 9) ORDENESTRABAJO (15) — asociadas a ventas con lentes
-- ============================================================
PRINT '9) Insertando OrdenesTrabajo...';
SET IDENTITY_INSERT OrdenesTrabajo ON;
INSERT INTO OrdenesTrabajo
  (Id, PacienteId, SucursalId, VentaId, Estado, Referencia,
   FechaCreacion, FechaLista, PD, TipoLente, MaterialLente,
   Tratamientos, LaboratorioExterno)
VALUES

(1, 1, 1, 1, N'Entregada', N'OT-PROC-2026-0001', '2026-03-16 09:00:00', '2026-03-23 16:00:00', 62.5, N'Progresivo', N'CR-39 índice 1.50', N'Anti-reflejo + Blue Cut', N'Laboratorio Óptico del Valle'),
(2, 4, 1, 4, N'Entregada', N'OT-PROC-2026-0002', '2026-03-26 09:00:00', '2026-04-02 14:00:00', 64.0, N'Progresivo', N'CR-39 índice 1.50', N'Anti-reflejo', N'VisionLab Heredia'),
(3, 13, 1, 12, N'Entregada', N'OT-BIF-2026-0003', '2026-03-18 09:00:00', '2026-03-25 11:00:00', 60.0, N'Bifocal', N'CR-39 índice 1.50', N'Anti-reflejo', N'OptiLens Internacional'),
(4, 2, 1, 3, N'Lista', N'OT-MONO-2026-0004', '2026-04-06 09:00:00', '2026-04-12 15:00:00', 63.0, N'Monofocal', N'Policarbonato índice 1.59', N'Anti-reflejo + UV', N'OptiLens Internacional'),
(5, 7, 1, 7, N'Lista', N'OT-TOR-2026-0005', '2026-04-09 09:00:00', '2026-04-15 12:00:00', 61.0, N'Monofocal tórico', N'CR-39 índice 1.50', N'Blue Cut + Anti-reflejo', N'VisionLab Heredia'),
(6, 10, 1, 10, N'Lista', N'OT-OFF-2026-0006', '2026-03-31 09:00:00', '2026-04-07 16:00:00', 64.5, N'Progresivo office', N'CR-39 índice 1.50', N'Anti-reflejo premium', N'Lux Óptica Mayorista'),
(7, 17, 1, 16, N'Lista', N'OT-PROC-2026-0007', '2026-04-18 09:00:00', '2026-04-24 11:00:00', 62.0, N'Progresivo', N'CR-39 índice 1.50', N'Blue Cut + Anti-reflejo', N'Laboratorio Óptico del Valle'),
(8, 1, 1, 2, N'EnProceso', N'OT-PROC-2026-0008', '2026-04-26 09:00:00', NULL, 62.5, N'Progresivo', N'CR-39 índice 1.50', N'Blue Cut + Transitions', N'Lux Óptica Mayorista'),
(9, 8, 1, 8, N'EnProceso', N'OT-MONO-2026-0009', '2026-04-15 09:00:00', NULL, 64.0, N'Monofocal', N'Policarbonato índice 1.59', N'Blue Cut + Anti-reflejo', N'OptiLens Internacional'),
(10, 14, 1, 14, N'EnProceso', N'OT-MONO-2026-0010', '2026-04-11 09:00:00', NULL, 58.5, N'Monofocal', N'CR-39 índice 1.50', N'Blue Cut + Anti-reflejo', N'VisionLab Heredia'),
(11, 19, 1, 17, N'EnProceso', N'OT-MONO-2026-0011', '2026-04-13 09:00:00', NULL, 63.5, N'Monofocal', N'CR-39 índice 1.50', N'Blue Cut + Anti-reflejo', N'OptiLens Internacional'),
(12, 12, 1, 11, N'Pendiente', N'OT-ANTI-2026-0012', '2026-05-18 09:00:00', NULL, 63.0, N'Monofocal', N'CR-39 índice 1.50', N'Anti-fatiga + Blue Cut', N'Lux Óptica Mayorista'),
(13, 16, 1, 15, N'Pendiente', N'OT-MONO-2026-0013', '2026-05-25 09:00:00', NULL, 62.0, N'Monofocal', N'CR-39 índice 1.50', N'Anti-reflejo + UV', N'VisionLab Heredia'),
(14, 21, 1, 18, N'Pendiente', N'OT-TOR-2026-0014', '2026-04-02 09:00:00', NULL, 61.5, N'Progresivo tórico', N'High index 1.67', N'Blue Cut + Anti-reflejo premium', N'Lux Óptica Mayorista'),
(15, 25, 1, 20, N'Pendiente', N'OT-TRANS-2026-0015', '2026-05-01 09:00:00', NULL, 62.0, N'Monofocal Transitions', N'CR-39 índice 1.50', N'Transitions + Blue Cut', N'OptiLens Internacional')
;
SET IDENTITY_INSERT OrdenesTrabajo OFF;

-- ============================================================
-- 10) ENVIOSNOTIFICACION (~50) — recordatorios, confirmaciones y OTs listas
-- ============================================================
PRINT '10) Insertando EnviosNotificacion...';
SET IDENTITY_INSERT EnviosNotificacion ON;
INSERT INTO EnviosNotificacion
  (Id, CitaId, OrdenTrabajoId, TipoNotificacion, FechaHoraEnvio,
   Canal, Destinatario, MensajeResumen, Exito)
VALUES
-- Recordatorios de citas confirmadas (13)
(1,  23, NULL, N'RecordatorioCita', '2026-06-11 09:00:00', N'Email',    N'mfernanda.rodriguez@gmail.com',
       N'Recordatorio: su cita es mañana 12/06 a las 11:00 en Sucursal Matriz.', 1),
(2,  24, NULL, N'RecordatorioCita', '2026-06-14 09:00:00', N'WhatsApp', N'50687654321',
       N'Le recordamos su cita del 15/06 a las 10:00 en Sucursal Heredia.', 1),
(3,  25, NULL, N'RecordatorioCita', '2026-06-15 09:00:00', N'Email',    N'ldiego.jimenez@outlook.com',
       N'Recordatorio cita 16/06 14:00 Sucursal Matriz.', 1),
(4,  26, NULL, N'RecordatorioCita', '2026-06-09 09:00:00', N'Email',    N'carmen.sanchez@gmail.com',
       N'Le esperamos mañana 10/06 a las 10:00.', 1),
(5,  27, NULL, N'RecordatorioCita', '2026-06-10 09:00:00', N'Email',    N'jmiguel.castro@gmail.com',
       N'Recordatorio: cita 11/06 09:00 Sucursal Heredia.', 1),
(6,  28, NULL, N'RecordatorioCita', '2026-06-17 09:00:00', N'WhatsApp', N'50683334455',
       N'Le esperamos 18/06 a las 14:00 para su adaptación.', 1),
(7,  29, NULL, N'RecordatorioCita', '2026-06-12 09:00:00', N'Email',    N'francisco.espinoza@gmail.com',
       N'Recordatorio 13/06 11:00 Sucursal Matriz.', 1),
(8,  30, NULL, N'RecordatorioCita', '2026-06-18 09:00:00', N'Email',    N'isabel.perez.r@gmail.com',
       N'Recordatorio cita pediátrica 19/06 15:00.', 1),
(9,  31, NULL, N'RecordatorioCita', '2026-06-09 09:00:00', N'Email',    N'pedro.brenes@gmail.com',
       N'Control 10/06 10:00 Sucursal Heredia.', 1),
(10, 32, NULL, N'RecordatorioCita', '2026-06-12 09:00:00', N'Email',    N'teresa.chaves@gmail.com',
       N'Cita 13/06 10:00. Le esperamos.', 1),
(11, 33, NULL, N'RecordatorioCita', '2026-06-16 09:00:00', N'Email',    N'ricardo.quesada@gmail.com',
       N'Control 17/06 11:00.', 1),
(12, 34, NULL, N'RecordatorioCita', '2026-06-11 09:00:00', N'Email',    N'gabriela.rodriguezm@gmail.com',
       N'Recordatorio cita 12/06 14:00.', 1),
(13, 35, NULL, N'RecordatorioCita', '2026-06-13 09:00:00', N'Email',    N'valentina.jimenezh@gmail.com',
       N'Le recordamos su cita 14/06 15:00.', 1),

-- Confirmaciones de citas atendidas (22) — enviadas el mismo día después de la atención
(14, 1,  NULL, N'ConfirmacionAtencion', '2026-03-15 11:30:00', N'Email',    N'mfernanda.rodriguez@gmail.com',
       N'Su cita fue atendida exitosamente. Resumen adjunto.', 1),
(15, 2,  NULL, N'ConfirmacionAtencion', '2026-04-25 13:00:00', N'Email',    N'mfernanda.rodriguez@gmail.com',
       N'Cita de renovación procesada. Recibirá notificación cuando sus lentes estén listos.', 1),
(16, 3,  NULL, N'ConfirmacionAtencion', '2026-04-05 11:00:00', N'WhatsApp', N'50687654321',
       N'Gracias por su visita. Receta lista en 24h.', 1),
(17, 4,  NULL, N'ConfirmacionAtencion', '2026-04-20 12:30:00', N'Email',    N'analu.gq@hotmail.com',
       N'Su control fue normal. Próxima revisión en 12 meses.', 1),
(18, 5,  NULL, N'ConfirmacionAtencion', '2026-03-25 12:00:00', N'Email',    N'ldiego.jimenez@outlook.com',
       N'Cita procesada. Lentes en fabricación.', 1),
(19, 6,  NULL, N'ConfirmacionAtencion', '2026-03-28 12:30:00', N'Email',    N'jmiguel.castro@gmail.com',
       N'Cita procesada exitosamente.', 1),
(20, 7,  NULL, N'ConfirmacionAtencion', '2026-05-12 13:30:00', N'SMS',      N'50685556677',
       N'Aro nuevo listo para retiro.', 1),
(21, 8,  NULL, N'ConfirmacionAtencion', '2026-04-08 12:00:00', N'WhatsApp', N'50683334455',
       N'Su receta ha sido procesada.', 1),
(22, 9,  NULL, N'ConfirmacionAtencion', '2026-04-14 13:00:00', N'Email',    N'francisco.espinoza@gmail.com',
       N'Cita procesada. Lentes en proceso.', 1),
(23, 10, NULL, N'ConfirmacionAtencion', '2026-04-22 10:30:00', N'Email',    N'isabel.perez.r@gmail.com',
       N'Control pediátrico registrado. Monitoreo en 6 meses.', 1),
(24, 11, NULL, N'ConfirmacionAtencion', '2026-03-30 16:00:00', N'Email',    N'pedro.brenes@gmail.com',
       N'Cita procesada. Progresivos office en fabricación.', 1),
(25, 12, NULL, N'ConfirmacionAtencion', '2026-05-02 12:30:00', N'Email',    N'lucia.monge@gmail.com',
       N'Control normal. Sin cambios.', 1),
(26, 13, NULL, N'ConfirmacionAtencion', '2026-05-17 12:00:00', N'WhatsApp', N'50686665544',
       N'Cita procesada. Lentes anti-fatiga encargados.', 1),
(27, 14, NULL, N'ConfirmacionAtencion', '2026-03-17 11:00:00', N'Email',    N'teresa.chaves@gmail.com',
       N'Su cita fue atendida. Bifocales en proceso.', 1),
(28, 15, NULL, N'ConfirmacionAtencion', '2026-04-27 16:00:00', N'Email',    N'teresa.chaves@gmail.com',
       N'Renovación procesada.', 1),
(29, 16, NULL, N'ConfirmacionAtencion', '2026-04-10 17:00:00', N'Email',    N'ricardo.quesada@gmail.com',
       N'Receta emitida. Recomendaciones para uso escolar.', 1),
(30, 17, NULL, N'ConfirmacionAtencion', '2026-05-22 11:30:00', N'Email',    N'patricia.jimenezl@gmail.com',
       N'Control procesado. Inicia con monofocales para cerca.', 1),
(31, 18, NULL, N'ConfirmacionAtencion', '2026-05-24 18:00:00', N'WhatsApp', N'50687775566',
       N'Cita procesada. Lentes en proceso.', 1),
(32, 19, NULL, N'ConfirmacionAtencion', '2026-04-17 11:00:00', N'Email',    N'gabriela.rodriguezm@gmail.com',
       N'Progresivos encargados. Listos en ~7 días.', 1),
(33, 20, NULL, N'ConfirmacionAtencion', '2026-04-12 15:00:00', N'Email',    N'sofia.vargas.e@gmail.com',
       N'Blue cut encargados. Avisaremos cuando estén listos.', 1),
(34, 21, NULL, N'ConfirmacionAtencion', '2026-04-01 13:00:00', N'Email',    N'valentina.jimenezh@gmail.com',
       N'Progresivos tóricos en proceso.', 1),
(35, 22, NULL, N'ConfirmacionAtencion', '2026-04-30 12:00:00', N'Email',    N'camila.lopez.c@gmail.com',
       N'Transitions en proceso. Aviso por email cuando estén listos.', 1),

-- Notificaciones de OT Listas o Entregadas (8)
(36, NULL, 1, N'OrdenLista', '2026-03-23 16:30:00', N'Email',    N'mfernanda.rodriguez@gmail.com',
       N'Sus progresivos están listos para retirar en Sucursal Matriz.', 1),
(37, NULL, 2, N'OrdenLista', '2026-04-02 14:30:00', N'Email',    N'ldiego.jimenez@outlook.com',
       N'Sus progresivos están listos.', 1),
(38, NULL, 3, N'OrdenLista', '2026-03-25 11:30:00', N'Email',    N'teresa.chaves@gmail.com',
       N'Sus bifocales están listos para retirar.', 1),
(39, NULL, 4, N'OrdenLista', '2026-04-12 15:30:00', N'WhatsApp', N'50687654321',
       N'Carlos, sus monofocales están listos en Sucursal Heredia.', 1),
(40, NULL, 5, N'OrdenLista', '2026-04-15 12:30:00', N'WhatsApp', N'50683334455',
       N'Rosa, sus lentes tóricos están listos.', 1),
(41, NULL, 6, N'OrdenLista', '2026-04-07 16:30:00', N'Email',    N'pedro.brenes@gmail.com',
       N'Sus progresivos office están listos.', 1),
(42, NULL, 7, N'OrdenLista', '2026-04-24 11:30:00', N'Email',    N'gabriela.rodriguezm@gmail.com',
       N'Gabriela, sus progresivos están listos.', 1),
(43, NULL, 1, N'OrdenEntregada', '2026-03-25 10:00:00', N'Email',    N'mfernanda.rodriguez@gmail.com',
       N'Confirmamos la entrega de sus progresivos. ¡Gracias!', 1),

-- Notificaciones pendientes/fallidas (3) — realistas
(44, 28, NULL, N'RecordatorioCita', '2026-06-17 09:00:00', N'WhatsApp', N'50683334455',
       N'Recordatorio cita adaptación.', 0),
(45, NULL, 8, N'OrdenLista', '2026-06-01 10:00:00', N'Email',    N'mfernanda.rodriguez@gmail.com',
       N'Sus nuevos progresivos están casi listos. Avisaremos cuando lleguen.', 1),
(46, NULL, 12, N'OrdenLista', '2026-05-25 11:00:00', N'WhatsApp', N'50686665544',
       N'Miguel, sus lentes anti-fatiga están en proceso. Pronto aviso.', 1),
(47, 36, NULL, N'ConfirmacionCita', '2026-06-05 12:00:00', N'Email',    N'andres.solis.a@gmail.com',
       N'Tu cita está confirmada para el 15/06 a las 11:00.', 1),
(48, 37, NULL, N'ConfirmacionCita', '2026-06-03 10:00:00', N'Email',    N'sofia.vargas.e@gmail.com',
       N'Tu cita de entrega está confirmada.', 1),
(49, 38, NULL, N'ConfirmacionCita', '2026-05-19 10:00:00', N'Email',    N'diego.perez.q@gmail.com',
       N'Confirmación de cita: 19/06 09:00.', 1),
(50, 39, NULL, N'ConfirmacionCita', '2026-05-28 10:00:00', N'WhatsApp', N'50688776633',
       N'Adriana, tu cita está confirmada.', 1);
SET IDENTITY_INSERT EnviosNotificacion OFF;

-- ============================================================
-- 11) PEDIDOS (10) — reposición a proveedores
-- ============================================================
PRINT '11) Insertando Pedidos...';
SET IDENTITY_INSERT Pedidos ON;
INSERT INTO Pedidos
  (Id, ProveedorId, FechaPedido, FechaEntregaEstimada, FechaEntregaReal,
   Activo, Descripcion, Estado, Indicador)
VALUES
-- Recibidos (3) — ya entregados
(1,  1, '2026-04-08 10:00:00', '2026-04-22 17:00:00', '2026-04-21 14:00:00',
     1, N'Reposición de lentes High Index 1.74 y Progresivos Liberty', 4, 1),
(2,  2, '2026-04-15 11:00:00', '2026-04-29 17:00:00', '2026-05-02 10:00:00',
     1, N'Pedido aros de marca (Ray-Ban, Oakley, Gucci, Prada)',         4, 2),
(3,  3, '2026-05-05 09:00:00', '2026-05-19 17:00:00', '2026-05-18 15:00:00',
     1, N'Lentes monofocales CR-39 y policarbonato',                     4, 1),
-- Enviados (3) — en camino
(4,  4, '2026-05-20 10:00:00', '2026-06-03 17:00:00', NULL,
     1, N'Pedido blue cut y transitions',                                3, 0),
(5,  5, '2026-05-25 11:00:00', '2026-06-08 17:00:00', NULL,
     1, N'Progresivos office y bifocales',                               3, 0),
(6,  1, '2026-06-01 09:00:00', '2026-06-15 17:00:00', NULL,
     1, N'Reposición lentes High Index',                                3, 0),
-- Aprobados (2) — confirmados, pendientes de envío
(7,  6, '2026-06-03 10:00:00', '2026-06-17 17:00:00', NULL,
     1, N'Lentes progresivos premium y tóricos',                        2, 0),
(8,  2, '2026-06-05 11:00:00', '2026-06-19 17:00:00', NULL,
     1, N'Reposición de accesorios y lentes de repuesto',               2, 0),
-- Pendientes (2) — recién solicitados, sin aprobar
(9,  3, '2026-06-06 09:00:00', '2026-06-20 17:00:00', NULL,
     1, N'Accesorios (estuches, paños, cordones)',                      1, 0),
(10, 4, '2026-06-07 10:00:00', '2026-06-21 17:00:00', NULL,
     1, N'Gotas hidratantes y kits de limpieza',                        1, 0);
SET IDENTITY_INSERT Pedidos OFF;

-- ============================================================
-- 12) DETALLEPEDIDOS (25) — líneas de cada pedido
-- ============================================================
PRINT '12) Insertando DetallePedidos...';
SET IDENTITY_INSERT DetallePedidos ON;
INSERT INTO DetallePedidos (Id, PedidoId, ProductoId, Cantidad, CostoUnitario) VALUES
-- Pedido 1: lentes premium
(1,  1,  16,  10, 65000.00),  -- High Index 1.74
(2,  1,  13,  8,  58000.00),  -- Progresivos Liberty
-- Pedido 2: aros de marca (mapeados a aros genéricos del catálogo; costo snapshot al momento del pedido)
(3,  2,  3,  6,  22000.00),  -- Aro Cat-Eye Femenino (Ray-Ban equiv.)
(4,  2,  7,  6,  19500.00),  -- Aro Wayfarer Clásico (Oakley equiv.)
(5,  2,  8,  4,  45000.00),  -- Aro Oversize Grande (Gucci equiv.)
(6,  2,  10, 3,  55000.00),  -- Aro Flex Silicona (Prada equiv.)
-- Pedido 3: monofocales
(7,  3,  2,   15, 30000.00),  -- BlueCut Digital
(8,  3,  14,  10, 32000.00),  -- Antifatiga
(9,  3,  18,  8,  38000.00),  -- Deportivos Recetados
-- Pedido 4: blue cut + transitions
(10, 4,  2,   12, 30000.00),  -- BlueCut
(11, 4,  15,  6,  44000.00),  -- Transitions Gen 8
-- Pedido 5: office + bifocales
(12, 5,  13,  6,  58000.00),  -- Progresivos Liberty
(13, 5,  17,  8,  41000.00),  -- Bifocales Office
-- Pedido 6: high index
(14, 6,  16,  8,  65000.00),  -- High Index 1.74
(15, 6,  14,  6,  32000.00),  -- Antifatiga
-- Pedido 7: progresivos premium y tóricos
(16, 7,  13,  8,  58000.00),  -- Progresivos Liberty
(17, 7,  17,  6,  41000.00),  -- Bifocales
-- Pedido 8: accesorios y repuestos
(18, 8,  22,  25,  3800.00),  -- Cordón de nylon
(19, 8,  23,  10,  5500.00),  -- Gotas Hidratantes Oftálmicas
(20, 8,  24,  30,  35000.00), -- Lentes de Sol Sport Pro MTB
-- Pedido 9: accesorios
(21, 9,  19,  20, 8500.00),   -- Estuche
(22, 9,  20,  30, 2500.00),   -- Paño microfibra
(23, 9,  21,  15, 7000.00),   -- Kit limpieza
(24, 9,  22,  25, 3800.00),   -- Cordón nylon
-- Pedido 10: gotas y kits
(25, 10, 23,  40, 5500.00),   -- Gotas Hidratantes Oftálmicas
(26, 10, 21,  20, 7000.00),   -- Kit de Limpieza Completo
(27, 10, 24,  15, 35000.00);  -- Lentes de Sol Sport Pro MTB
SET IDENTITY_INSERT DetallePedidos OFF;

-- (COMMIT removed)

PRINT '================================================';
PRINT 'SEED COMPLETADO:';
PRINT '  6 proveedores, 25 pacientes, 50 solicitudes, 50 citas';
PRINT '  23 expedientes, 23 valores clinicos';
PRINT '  20 ventas, 47 detalles, 15 OTs, 50 notificaciones';
PRINT '  10 pedidos, 27 detalles';
PRINT '================================================';
GO
