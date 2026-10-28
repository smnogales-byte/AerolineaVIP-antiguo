# Aerolínea VIP – Guía para el docente

Sistema web monolítico para la compra, reserva y administración de pasajes de una aerolínea VIP (3 Embraer Phenom 300E, 7 asientos por vuelo). Esta guía explica, paso a paso, cómo ejecutar y revisar el proyecto.

| Dato | Valor |
|---|---|
| Asignatura | Desarrollo Web Enfocado en la Integración de Tecnologías |
| Institución | Universidad de las Fuerzas Armadas ESPE |
| Grupo | 4 |
| Integrante | Santiago Mauricio Nogales Pañora |
| Docente | Juan Fernando Galarraga Hurtado |
| Versión | `v1.0-primera-entrega` (28 de octubre de 2026) |

**Tecnologías:** ASP.NET Framework 4.8.1 (MVC 5, Razor), Bootstrap 5.3.8, jQuery, ASP.NET Identity (OWIN), Entity Framework 6 y SQL Server.

## 1. Contenido del repositorio

```
README.md   Esta guía
src/        Solución (AerolineaVIP.slnx) y proyecto ASP.NET MVC
database/   Script_BD_AerolineaVIP_Unificado.sql
docs/       Documentación: PDF de la entrega, UX-UI, Gestion y guía de navegación
```

Para ubicar cada archivo del proyecto, ver [docs/Guia_Navegacion_Proyecto.md](docs/Guia_Navegacion_Proyecto.md).

## 2. Requisitos

- Windows con Visual Studio 2022 o superior, con la carga de trabajo *ASP.NET y desarrollo web* y el .NET Framework 4.8.1.
- SQL Server (Express, LocalDB o instancia local) y SQL Server Management Studio.
- Conexión a internet la primera vez, para restaurar los paquetes NuGet.

## 3. Puesta en marcha

1. **Descargar el proyecto.** Clonar el repositorio o descomprimir `Primera_Entrega_Proyecto_Grupo04.zip`.
2. **Crear las bases de datos.** Ver el detalle en la sección 3.1.
3. **Revisar la conexión.** En `src/AerolineaVIP/Web.config`, las cadenas de conexión usan autenticación de Windows con `Data Source=.` y `localhost`. Si el servidor tiene otro nombre, cambiarlo en `DefaultConnection` y `AerolineaVIPEntities`.
4. **Abrir la solución.** Ver el detalle en la sección 3.2.
5. **Compilar.** Menú *Compilar > Recompilar solución*. Visual Studio restaura los paquetes NuGet automáticamente; el resultado esperado es 0 errores.
6. **Ejecutar.** Pulsar F5 (IIS Express). El sitio abre en `https://localhost:44346`.

### 3.1 Dónde y cómo ejecutar el script SQL

1. Abrir **SQL Server Management Studio (SSMS)**.
2. En la ventana *Conectar con el servidor*, elegir tipo *Motor de base de datos*, nombre del servidor `.` (o `localhost`) y autenticación **Autenticación de Windows**; pulsar **Conectar**.
3. Menú *Archivo > Abrir > Archivo* y seleccionar `database/Script_BD_AerolineaVIP_Unificado.sql` del repositorio.
4. Pulsar **Ejecutar** (o F5) y esperar a que termine sin errores.
5. En el **Explorador de objetos**, clic derecho sobre *Bases de datos > Actualizar*. Deben aparecer **AerolineaVIP** y **AerolineaVIP_Identity**.

### 3.2 Dónde y cómo abrir el proyecto en Visual Studio

1. Abrir **Visual Studio** desde el menú Inicio de Windows.
2. En la pantalla de inicio, pulsar **Abrir un proyecto o una solución** (o *Archivo > Abrir > Proyecto o solución*).
3. Navegar a la carpeta del repositorio, entrar en `src` y seleccionar el archivo **`AerolineaVIP.slnx`**. No abrir el `.csproj` suelto.
4. Esperar a que cargue. En el **Explorador de soluciones** (panel derecho) aparece *Solución "AerolineaVIP" (1 de 1 proyecto)* y, debajo, el proyecto **AerolineaVIP**.
5. Si Visual Studio pregunta por restaurar paquetes NuGet, aceptar; si no pregunta, clic derecho sobre la solución y elegir **Restaurar paquetes NuGet**.
6. Menú **Compilar > Recompilar solución**. En la ventana *Salida* debe aparecer *Recompilar: 1 correcto, 0 erróneos*.
7. En la barra superior, comprobar que el botón verde de ejecución dice **IIS Express** y pulsarlo (o presionar **F5**).
8. Se abre el navegador en `https://localhost:44346`. Si aparece una advertencia de certificado de IIS Express, aceptarla o confiar en el certificado de desarrollo.

Al iniciar, `Global.asax.cs` crea por código los 7 roles y los 7 usuarios iniciales, así que no hace falta cargarlos a mano.

## 4. Usuarios iniciales

Se crean al arrancar la aplicación. Las claves de prueba están definidas en `src/AerolineaVIP/Global.asax.cs` (métodos `CrearSuperUsuario`, `CrearAdministrador`, etc.) y no se publican en este documento.

| Rol | Usuario |
|---|---|
| SuperUsuario | superadmin@aerolineavip.com |
| Administrador | administrador@aerolineavip.com |
| Operaciones | operaciones@aerolineavip.com |
| Comercial | comercial@aerolineavip.com |
| Counter | counter@aerolineavip.com |
| Pasajero | pasajero@aerolineavip.com |
| Cliente | cliente@aerolineavip.com |

## 5. Qué revisar (recorrido sugerido)

1. **Inicio** (`/`): página principal con logo, nombre y formulario de búsqueda.
2. **Buscar vuelos** (sin iniciar sesión): origen, destino, fecha y pasajeros; ver resultados y el mapa de 7 asientos. Verde = disponible, morado = reservado, rojo = ocupado.
3. **Registro e inicio de sesión:** crear una cuenta (el registro público solo asigna el rol Cliente) e iniciar sesión con un usuario inicial.
4. **Flujo de compra** (Cliente, Pasajero o Counter): elegir asiento, registrar pasajeros, confirmar la reserva, pagar (simulado) y ver el comprobante. Luego revisar *Mis reservas* y *Check-in*.
5. **Menú por rol:** iniciar sesión con cada usuario y comprobar que el menú *Administración* cambia según el rol.
6. **CRUD de catálogos** (SuperUsuario o Administrador): Aeropuertos, Rutas, Frecuencias, Modelos de aeronave, Aeronaves, Itinerarios; y, como SuperUsuario o Comercial, Marcas de tarjeta y Métodos de pago. Solo el SuperUsuario puede eliminar.
7. **Administración:** vuelos y frecuencias, aeronaves y asientos, usuarios y reservas, reportes y asignación de roles.

## 6. Notas importantes

- Los pagos y comprobantes son **simulados**: la tarjeta terminada en `0000` se rechaza a propósito para probar el rechazo.
- Las políticas de negocio (horas de retención de reserva, ventana de check-in, entre otras) son propuestas del documento de la entrega, pendientes de aprobación, y se pueden ajustar en `Web.config`.
- No hay contraseñas reales ni credenciales de servicios externos en el repositorio.

## 7. Solución de problemas

| Síntoma | Causa y solución |
|---|---|
| No se puede abrir la base de datos | No se ejecutó el script SQL, o el nombre del servidor en `Web.config` no coincide. |
| Error de configuración con `Microsoft.CodeDom.Providers.DotNetCompilerPlatform` | Falta compilar. Hacer *Restaurar paquetes NuGet* y *Recompilar solución*. |
| El sitio no abre en `localhost:44346` | Ejecutar con F5 desde Visual Studio, con IIS Express como destino. |
| Los usuarios iniciales no existen | Verificar que la base `AerolineaVIP_Identity` exista; se llenan al arrancar la aplicación. |

## 8. Documentación

- `docs/Primera_Entrega_Documentacion_Grupo04.pdf`: portada, wireframes aplicados y políticas DP-01 a DP-15.
- `docs/UX-UI/` y `docs/Gestion/`: resumen de interfaces y de políticas.
- `docs/Guia_Navegacion_Proyecto.md`: mapa del repositorio y del sistema.
