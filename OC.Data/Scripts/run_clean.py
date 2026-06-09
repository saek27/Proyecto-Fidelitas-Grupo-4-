#!/usr/bin/env python3
"""Ejecuta clean-dev-data.sql y verifica contando filas antes/después."""
import pymssql, sys

SERVER, USER, PASSWORD, DATABASE = "localhost", "sa", "Dani4421!", "SistemaOpticaDB"
SQL_FILE = "OC.Data/Scripts/clean-dev-data.sql"

TABLAS_BORRADAS = [
    "DocumentosExpediente", "ValoresClinicos", "EnviosNotificacion",
    "DetalleVentas", "DetallePedidos", "ComentarioTickets",
    "Ventas", "OrdenesTrabajo", "Pedidos", "Tickets",
    "Expedientes", "Citas", "SolicitudesCitas", "Pacientes",
    "Proveedores",
]

TABLAS_CONSERVADAS = [
    "Usuarios", "Roles", "Sucursales", "Productos", "Aros",
    "TecnologiaLentes", "Equipos",
    "Planillas", "Asistencias", "Permisos",
]

def contar(conn, tablas):
    out = {}
    cur = conn.cursor()
    for t in tablas:
        cur.execute(f"SELECT COUNT(*) FROM {t}")
        out[t] = cur.fetchone()[0]
    return out

def main():
    conn = pymssql.connect(
        server=SERVER, user=USER, password=PASSWORD, database=DATABASE,
        login_timeout=5,
    )
    try:
        antes_borradas  = contar(conn, TABLAS_BORRADAS)
        antes_conservar = contar(conn, TABLAS_CONSERVADAS)

        print("== ANTES ==")
        print("Borradas:    ", antes_borradas)
        print("Conservadas: ", antes_conservar)

        with open(SQL_FILE, "r", encoding="utf-8") as f:
            sql = f.read()

        cur = conn.cursor()
        print(f"\n== Ejecutando {SQL_FILE} ==\n")
        cur.execute(sql)
        conn.commit()
        print("== COMMIT OK ==\n")

        despues_borradas  = contar(conn, TABLAS_BORRADAS)
        despues_conservar = contar(conn, TABLAS_CONSERVADAS)

        print("== DESPUÉS ==")
        print("Borradas:    ", despues_borradas)
        print("Conservadas: ", despues_conservar)

        # Verificación
        ok = True
        for t in TABLAS_BORRADAS:
            if despues_borradas[t] != 0:
                print(f"  ❌ {t} aún tiene {despues_borradas[t]} filas")
                ok = False
        for t in TABLAS_CONSERVADAS:
            delta = despues_conservar[t] - antes_conservar[t]
            if delta != 0:
                print(f"  ❌ {t} cambió: {antes_conservar[t]} → {despues_conservar[t]}")
                ok = False

        print()
        if ok:
            print("✅ Verificación OK: todas las operativas en 0, todas las conservadas intactas.")
        else:
            print("⚠ Verificación con diferencias, revisa arriba.")
            sys.exit(2)
    except Exception as e:
        conn.rollback()
        print(f"\n❌ ERROR: {e}")
        sys.exit(1)
    finally:
        conn.close()

if __name__ == "__main__":
    main()
