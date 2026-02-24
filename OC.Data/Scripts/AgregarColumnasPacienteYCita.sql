-- ============================================================
-- Script: Agregar columnas de recuperación de contraseña (Pacientes)
--        y columna SucursalId (Citas) si no existen.
-- Ejecutar en la base de datos del proyecto (SQL Server).
-- Si una columna ya existe, su bloque se omitirá.
-- ============================================================

-- 1. Pacientes: columnas para recuperación de contraseña
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Pacientes') AND name = 'TokenRecuperacion')
BEGIN
    ALTER TABLE Pacientes ADD TokenRecuperacion nvarchar(max) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Pacientes') AND name = 'FechaExpiracionToken')
BEGIN
    ALTER TABLE Pacientes ADD FechaExpiracionToken datetime2 NULL;
END
GO

-- 2. Citas: columna SucursalId (por sede) si no existe
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Citas') AND name = 'SucursalId')
BEGIN
    ALTER TABLE Citas ADD SucursalId int NOT NULL DEFAULT 1;
    CREATE INDEX IX_Citas_SucursalId ON Citas(SucursalId);
    ALTER TABLE Citas ADD CONSTRAINT FK_Citas_Sucursales_SucursalId
        FOREIGN KEY (SucursalId) REFERENCES Sucursales(Id) ON DELETE NO ACTION;
END
GO

-- 3. (Opcional) Ajustar longitud de Cedula a 9 si sigue en 20
-- Descomentar solo si en Pacientes.Cedula todos los valores tienen 9 dígitos:
/*
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Pacientes') AND name = 'Cedula')
BEGIN
    ALTER TABLE Pacientes ALTER COLUMN Cedula nvarchar(9) NOT NULL;
END
GO
*/
