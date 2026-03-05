-- Ejecutar SOLO si Update-Database dice "already up to date" pero la tabla Citas NO tiene las columnas de recordatorios.
-- Compruebe antes: SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Citas' AND COLUMN_NAME = 'NotificacionesActivas';
-- Si no devuelve filas, ejecute este script.

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Citas' AND COLUMN_NAME = 'NotificacionesActivas')
BEGIN
    ALTER TABLE Citas ADD NotificacionesActivas bit NOT NULL DEFAULT 1;
    ALTER TABLE Citas ADD CanalNotificacion nvarchar(20) NOT NULL DEFAULT 'Email';
END
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'EnviosNotificacion')
BEGIN
    CREATE TABLE EnviosNotificacion (
        Id int IDENTITY(1,1) PRIMARY KEY,
        CitaId int NOT NULL,
        TipoNotificacion nvarchar(50) NOT NULL,
        FechaHoraEnvio datetime2 NOT NULL,
        Canal nvarchar(20) NOT NULL,
        Destinatario nvarchar(256) NULL,
        MensajeResumen nvarchar(500) NULL,
        Exito bit NOT NULL,
        CONSTRAINT FK_EnviosNotificacion_Citas FOREIGN KEY (CitaId) REFERENCES Citas(Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_EnviosNotificacion_CitaId ON EnviosNotificacion(CitaId);
END
GO

-- Registrar la migración para que EF no intente aplicarla de nuevo:
IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20260225180000_AddRecordatoriosCitasCITRF016')
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('20260225180000_AddRecordatoriosCitasCITRF016', '8.0.0');
GO
