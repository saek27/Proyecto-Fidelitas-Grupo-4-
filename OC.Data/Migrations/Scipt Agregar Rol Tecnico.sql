-- Ejecutar SOLO si Update-Database dice "already up to date" pero la tabla Citas NO tiene las columnas de recordatorios.
-- Compruebe antes: SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Citas' AND COLUMN_NAME = 'NotificacionesActivas';
-- Si no devuelve filas, ejecute este script.
INSERT INTO Roles (Nombre, Descripcion) VALUES ('Tecnico', 'Técnico de soporte');