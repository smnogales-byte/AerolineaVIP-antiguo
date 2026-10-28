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

## Estado del proyecto

- **Versión:** `v1.0-primera-entrega`
- **Fecha:** 28 de octubre de 2026
- **Responsable:** Santiago Mauricio Nogales Pañora

Alcance: wireframes aplicados, flujo de compra y reserva, panel de administración, registro e inicio de sesión por roles. Los pagos y comprobantes son simulados. Las políticas 18.1 (DP-01 a DP-15) están documentadas como propuestas pendientes de aprobación.

## Flujo de trabajo Git

`main` es la rama estable. Cada cambio se desarrolla en una rama `feature/…` y se integra por pull request. No se suben contraseñas ni credenciales reales.
