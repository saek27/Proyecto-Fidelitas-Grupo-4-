use SistemaOpticaDB;
-- ============================================================
-- Script: clean-dev-data.sql
-- Propósito: Vaciar las tablas OPERATIVAS / TRANSACCIONALES
--            de la base de datos de desarrollo, conservando
--            intactos los datos de CONFIGURACIÓN y USUARIOS.
--
-- ⚠ EJECUTAR SOLO EN DESARROLLO. Es destructivo e irreversible.
--
-- Tablas que se CONSERVAN (no se tocan sus filas):
--   Usuarios, Roles, Sucursales, Productos, Aros, TecnologiaLente,
--   Equipos, Planillas, Asistencias, Permisos
--
-- Tablas que se VACÍAN (en este orden para respetar FKs):
--   1. DocumentosExpediente, ValoresClinicos, EnviosNotificacion,
--      DetalleVentas, DetallePedidos, ComentarioTickets
--   2. Ventas, OrdenesTrabajo, Pedidos, Tickets, Expedientes,
--      Citas, SolicitudesCitas
--   3. Pacientes, Proveedores
--
-- Uso recomendado: ejecutar dentro de una transacción y revisar
-- los conteos antes de confirmar.
-- ============================================================

SET XACT_ABORT ON;
SET NOCOUNT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    -- =====================================================
    -- 1) Tablas hoja (sin otras tablas apuntándolas)
    -- =====================================================
    DELETE FROM DocumentosExpediente;
    PRINT 'DocumentosExpediente: ' + CAST(@@ROWCOUNT AS varchar(10)) + ' filas eliminadas.';

    DELETE FROM ValoresClinicos;
    PRINT 'ValoresClinicos: '      + CAST(@@ROWCOUNT AS varchar(10)) + ' filas eliminadas.';

    DELETE FROM EnviosNotificacion;
    PRINT 'EnviosNotificacion: '   + CAST(@@ROWCOUNT AS varchar(10)) + ' filas eliminadas.';

    DELETE FROM DetalleVentas;
    PRINT 'DetalleVentas: '        + CAST(@@ROWCOUNT AS varchar(10)) + ' filas eliminadas.';

    DELETE FROM DetallePedidos;
    PRINT 'DetallePedidos: '       + CAST(@@ROWCOUNT AS varchar(10)) + ' filas eliminadas.';

    DELETE FROM ComentarioTickets;
    PRINT 'ComentarioTickets: '    + CAST(@@ROWCOUNT AS varchar(10)) + ' filas eliminadas.';

    -- =====================================================
    -- 2) Tablas padre intermedias
    -- =====================================================
    DELETE FROM Ventas;
    PRINT 'Ventas: '               + CAST(@@ROWCOUNT AS varchar(10)) + ' filas eliminadas.';

    DELETE FROM OrdenesTrabajo;
    PRINT 'OrdenesTrabajo: '       + CAST(@@ROWCOUNT AS varchar(10)) + ' filas eliminadas.';

    DELETE FROM Pedidos;
    PRINT 'Pedidos: '              + CAST(@@ROWCOUNT AS varchar(10)) + ' filas eliminadas.';

    DELETE FROM Tickets;
    PRINT 'Tickets: '              + CAST(@@ROWCOUNT AS varchar(10)) + ' filas eliminadas.';

    DELETE FROM Expedientes;
    PRINT 'Expedientes: '          + CAST(@@ROWCOUNT AS varchar(10)) + ' filas eliminadas.';

    DELETE FROM Citas;
    PRINT 'Citas: '                + CAST(@@ROWCOUNT AS varchar(10)) + ' filas eliminadas.';

    DELETE FROM SolicitudesCitas;
    PRINT 'SolicitudesCitas: '     + CAST(@@ROWCOUNT AS varchar(10)) + ' filas eliminadas.';

    -- =====================================================
    -- 3) Raíz: Pacientes (último, ya no tiene hijos)
    -- =====================================================
    DELETE FROM Pacientes;
    PRINT 'Pacientes: '            + CAST(@@ROWCOUNT AS varchar(10)) + ' filas eliminadas.';

    -- =====================================================
    -- 4) Proveedores (recurso operativo, se re-siembra)
    -- =====================================================
    DELETE FROM Proveedores;
    PRINT 'Proveedores: '         + CAST(@@ROWCOUNT AS varchar(10)) + ' filas eliminadas.';

    -- =====================================================
    -- 5) [OPCIONAL] Resetear columnas IDENTITY a 0
    --    para que los próximos registros vuelvan a usar ID = 1.
    --    Quitar el comentario si quieres resetear.
    -- =====================================================
    /*
    DBCC CHECKIDENT ('DocumentosExpediente', RESEED, 0);
    DBCC CHECKIDENT ('ValoresClinicos',      RESEED, 0);
    DBCC CHECKIDENT ('EnviosNotificacion',   RESEED, 0);
    DBCC CHECKIDENT ('DetalleVentas',        RESEED, 0);
    DBCC CHECKIDENT ('DetallePedidos',       RESEED, 0);
    DBCC CHECKIDENT ('ComentarioTickets',    RESEED, 0);
    DBCC CHECKIDENT ('Ventas',               RESEED, 0);
    DBCC CHECKIDENT ('OrdenesTrabajo',       RESEED, 0);
    DBCC CHECKIDENT ('Pedidos',              RESEED, 0);
    DBCC CHECKIDENT ('Tickets',              RESEED, 0);
    DBCC CHECKIDENT ('Expedientes',          RESEED, 0);
    DBCC CHECKIDENT ('Citas',                RESEED, 0);
    DBCC CHECKIDENT ('SolicitudesCitas',     RESEED, 0);
    DBCC CHECKIDENT ('Pacientes',            RESEED, 0);
    DBCC CHECKIDENT ('Proveedores',          RESEED, 0);
    */

    COMMIT TRANSACTION;
    PRINT '✅ Limpieza completada. Usuarios, Roles, Sucursales, Productos, Aros, TecnologiaLente, Equipos, Planillas, Asistencias y Permisos se conservaron.';
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT '❌ Error: ' + ERROR_MESSAGE();
    THROW;
END CATCH
