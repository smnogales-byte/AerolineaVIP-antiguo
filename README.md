# Aerolínea VIP – Sistema Web Monolítico

Sistema web para la compra, reserva y administración de pasajes de una aerolínea VIP.
Proyecto académico de la asignatura **Desarrollo Web Enfocado en la Integración de Tecnologías** (ESPE, carrera Tecnologías de la Información).

- **Grupo:** 4
- **Integrante:** Santiago Mauricio Nogales Pañora
- **Docente:** Juan Fernando Galarraga Hurtado
- **Flota:** 3 Embraer Phenom 300E (capacidad estricta de 7 asientos por vuelo).

## Tecnologías

- ASP.NET Framework 4.8.1 – MVC 5, Razor
- Bootstrap 5.3.8 y jQuery (validación en cliente)
- ASP.NET Identity (OWIN) para autenticación y roles
- Entity Framework 6 (database first) sobre SQL Server

## Estructura del repositorio

```
README.md
.gitignore
src/        Solución y proyecto ASP.NET MVC (AerolineaVIP)
database/   Script SQL unificado (Script_BD_AerolineaVIP_Unificado.sql)
docs/       Documentación (requerimientos, arquitectura, UX-UI, Gestion, entrega)
```

## Cómo ejecutar

1. Instalar Visual Studio 2022 o superior con la carga de trabajo *ASP.NET y desarrollo web* y SQL Server (Express o LocalDB).
2. Ejecutar `database/Script_BD_AerolineaVIP_Unificado.sql` en SQL Server para crear la base de datos `AerolineaVIP`. La base de Identity (`AerolineaVIP_Identity`) se crea sola al iniciar.
3. Abrir `src/AerolineaVIP.slnx` en Visual Studio y restaurar los paquetes NuGet.
4. Revisar las cadenas de conexión en `src/AerolineaVIP/Web.config` (usan autenticación de Windows) y ajustar el nombre del servidor si es necesario.
5. Ejecutar con IIS Express (F5).

## Usuarios iniciales

Se crean automáticamente al iniciar la aplicación (`Global.asax.cs`). Las claves de prueba están definidas en ese archivo y no se publican aquí.

| Rol | Usuario |
|---|---|
| SuperUsuario | superadmin@aerolineavip.com |
| Administrador | administrador@aerolineavip.com |
| Operaciones | operaciones@aerolineavip.com |
| Comercial | comercial@aerolineavip.com |
| Counter | counter@aerolineavip.com |
| Pasajero | pasajero@aerolineavip.com |
| Cliente | cliente@aerolineavip.com |

## Funciones por rol

- **Visitante:** busca vuelos, consulta disponibilidad y asientos sin iniciar sesión.
- **Pasajero y Cliente:** se registra, reserva, elige asiento, paga (simulado), consulta sus reservas y comprobantes.
- **Counter:** atiende reservas y check-in de los pasajeros.
- **Comercial, Operaciones y Administrador:** gestionan catálogos, vuelos, flota y reportes según su rol.
- **SuperUsuario:** administra roles y usuarios.

## Funcionamiento

1. Al iniciar, `Global.asax.cs` crea los roles y los usuarios iniciales.
2. Cualquier visitante busca vuelos y ve el mapa de 7 asientos (verde disponible, morado reservado, rojo ocupado).
3. Para reservar se inicia sesión, se elige el asiento, se registran los pasajeros y se confirma la reserva.
4. El pago es simulado y genera un comprobante. La reserva sin pagar se retiene un tiempo definido en las políticas.
5. El check-in se habilita antes de la salida, según las políticas de la documentación.
6. El personal administra vuelos, flota, catálogos, usuarios, roles y reportes según su rol.

Para recorrer el repositorio y el sistema paso a paso, ver [docs/Guia_Navegacion_Proyecto.md](docs/Guia_Navegacion_Proyecto.md).

## Estado del proyecto

- **Versión:** `v1.0-primera-entrega`
- **Fecha:** 28 de octubre de 2026
- **Responsable:** Santiago Mauricio Nogales Pañora

Alcance: wireframes aplicados, flujo de compra y reserva, panel de administración, registro e inicio de sesión por roles. Los pagos y comprobantes son simulados. Las políticas 18.1 (DP-01 a DP-15) están documentadas como propuestas pendientes de aprobación.

## Flujo de trabajo Git

`main` es la rama estable. Cada cambio se desarrolla en una rama `feature/…` y se integra por pull request. No se suben contraseñas ni credenciales reales.
