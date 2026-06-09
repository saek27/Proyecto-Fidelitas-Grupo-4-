#!/usr/bin/env python3
"""Ejecuta seed-dev-data.sql y muestra conteos por tabla."""
import pymssql, sys

SERVER, USER, PASSWORD, DATABASE = "localhost", "sa", "Dani4421!", "SistemaOpticaDB"
SQL_FILE = "OC.Data/Scripts/seed-dev-data.sql"

TABLES = [
    "Proveedores", "Pacientes", "SolicitudesCitas", "Citas",
    "Expedientes", "ValoresClinicos", "Ventas", "DetalleVentas",
    "OrdenesTrabajo", "EnviosNotificacion", "Pedidos", "DetallePedidos",
]

def main():
    conn = pymssql.connect(
        server=SERVER, user=USER, password=PASSWORD, database=DATABASE,
        login_timeout=5,
    )
    cur = conn.cursor()

    def count_all():
        out = {}
        for t in TABLES:
            cur.execute(f"SELECT COUNT(*) FROM {t}")
            out[t] = cur.fetchone()[0]
        return out

    antes = count_all()
    print("== ANTES ==")
    for t in TABLES:
        print(f"  {t:<22} {antes[t]:>4}")

    with open(SQL_FILE, "r", encoding="utf-8") as f:
        sql = f.read()

    print(f"\n== Ejecutando {SQL_FILE} ==\n")
    try:
        cur.execute(sql)
        conn.commit()
        print("== COMMIT OK ==\n")
    except Exception as e:
        conn.rollback()
        print(f"\nERROR: {e}")
        sys.exit(1)

    despues = count_all()
    print("== DESPUÉS ==")
    for t in TABLES:
        delta = despues[t] - antes[t]
        flag = "✓" if delta > 0 else ("·" if delta == 0 else "✗")
        print(f"  {flag} {t:<22} {despues[t]:>4}  (+{delta})")

    # Verificaciones cruzadas
    print("\n== Verificación cruzada de FKs ==")
    checks = [
        ("Citas sin Expediente atendidas (debe ser 0)",
         "SELECT COUNT(*) FROM Citas c LEFT JOIN Expedientes e ON e.CitaId = c.Id WHERE c.Estado = 'Atendida' AND e.Id IS NULL",
         0),
        ("Citas confirmadas con NotificacionesActivas (esperado ~13)",
         "SELECT COUNT(*) FROM Citas WHERE Estado = 'Confirmada' AND NotificacionesActivas = 1",
         None),
        ("Expedientes sin ValorClinico (debe ser 0)",
         "SELECT COUNT(*) FROM Expedientes e LEFT JOIN ValoresClinicos v ON v.ExpedienteId = e.Id WHERE v.Id IS NULL",
         0),
        ("Ventas sin DetalleVentas (debe ser 0)",
         "SELECT COUNT(*) FROM Ventas v WHERE NOT EXISTS (SELECT 1 FROM DetalleVentas WHERE VentaId = v.Id)",
         0),
        ("OTs Listas/Entregadas sin FechaLista (debe ser 0)",
         "SELECT COUNT(*) FROM OrdenesTrabajo WHERE Estado IN ('Lista','Entregada') AND FechaLista IS NULL",
         0),
        ("Pedidos Recibidos sin FechaEntregaReal (debe ser 0)",
         "SELECT COUNT(*) FROM Pedidos WHERE Estado = 4 AND FechaEntregaReal IS NULL",
         0),
        ("DetallePedidos huérfanos (debe ser 0)",
         "SELECT COUNT(*) FROM DetallePedidos d LEFT JOIN Pedidos p ON p.Id = d.PedidoId WHERE p.Id IS NULL",
         0),
        ("DetalleVentas huérfanos (debe ser 0)",
         "SELECT COUNT(*) FROM DetalleVentas d LEFT JOIN Ventas v ON v.Id = d.VentaId WHERE v.Id IS NULL",
         0),
        ("Pacientes con Citas (esperado 25, todos)",
         "SELECT COUNT(DISTINCT PacienteId) FROM Citas",
         25),
        ("Ventas con Total != (subtotal - desc) * 1.13 (debe ser 0)",
         """SELECT COUNT(*) FROM Ventas v
            WHERE ABS(v.Total - ((SELECT ISNULL(SUM(Subtotal),0) FROM DetalleVentas WHERE VentaId = v.Id) - v.Descuento) * 1.13) > 0.5""",
         0),
    ]
    for label, sql, expected in checks:
        cur.execute(sql)
        n = cur.fetchone()[0]
        if expected is None:
            print(f"  · {label}: {n}")
        else:
            ok = (n == expected)
            flag = "✓" if ok else "✗"
            print(f"  {flag} {label}: {n}")

    conn.close()

if __name__ == "__main__":
    main()
