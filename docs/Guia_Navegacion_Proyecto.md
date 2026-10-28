# Guía para navegar el proyecto – Aerolínea VIP

Esta guía explica dónde está cada cosa en el repositorio y cómo recorrer el sistema.

## 1. Estructura del repositorio

| Carpeta o archivo | Contenido |
|---|---|
| `README.md` | Descripción general, cómo ejecutar, usuarios iniciales y funcionamiento. |
| `src/AerolineaVIP.slnx` | Solución de Visual Studio (abrir este archivo). |
| `src/AerolineaVIP/` | Proyecto ASP.NET MVC 5 (.NET Framework 4.8.1). |
| `database/Script_BD_AerolineaVIP_Unificado.sql` | Script que crea las bases `AerolineaVIP` y `AerolineaVIP_Identity` con tablas y datos iniciales. |
| `docs/Primera_Entrega_Documentacion_Grupo04.pdf` | Documentación de la primera entrega (portada, wireframes, políticas DP-01 a DP-15). |
| `docs/UX-UI/` | Wireframes aplicados y mapa de navegación. |
| `docs/Gestion/` | Políticas 18.1 (decisiones DP-01 a DP-15). |
| `docs/01_…` a `docs/07_…` | Documentación de sprints, arquitectura y base de datos. |

## 2. Dentro de `src/AerolineaVIP/`

| Carpeta | Para qué sirve |
|---|---|
| `Global.asax.cs` | Al iniciar crea los roles y los usuarios iniciales por código. |
| `App_Start/` | Configuración de rutas, bundles, filtros e Identity. |
| `Controllers/` | Lógica de cada módulo (Vuelos, Reservas, Compras, CheckIn, Comprobantes, Admin y los CRUD). |
| `Models/` | Entidades de la base (Entity Framework), modelos de Identity y view models. |
| `Views/` | Pantallas Razor, una carpeta por controlador; `Shared/_Layout.cshtml` es la plantilla general con el menú. |
| `Content/` y `Scripts/` | Bootstrap 5.3.8, jQuery, validación, `Site.css` e imágenes (`Img/`). |
| `Web.config` | Cadenas de conexión (autenticación de Windows) y configuración. |

## 3. Cómo recorrer el sistema (sin iniciar sesión)

1. **Inicio** (`/`): presentación y formulario de búsqueda.
2. **Buscar vuelos** (`/Vuelos/Buscar`): origen, destino, fecha, pasajeros y horario.
3. **Resultados** (`/Vuelos/Resultados`): lista de vuelos disponibles.
4. **Disponibilidad** (`/Vuelos/Disponibilidad`): mapa de los 7 asientos del vuelo.
5. **Iniciar sesión / Crear cuenta** (`/Account/Login`, `/Account/Register`).

## 4. Flujo de compra (Pasajero, Cliente o Counter)

1. Elegir vuelo y asiento en el mapa. Verde = disponible, morado = reservado, rojo = ocupado.
2. **Pasajeros** (`/Reservas/Pasajeros`): datos de quienes viajan.
3. **Confirmar** (`/Reservas/Confirmar`): resumen y creación de la reserva.
4. **Pagar** (`/Compras/Pagar`): pago simulado.
5. **Confirmación** (`/Compras/Confirmacion`) y **Comprobantes** (`/Comprobantes`).
6. **Mis reservas** (`/Reservas/MisReservas`), **Gestionar** una reserva y **Check-in** (`/CheckIn`).

## 5. Menú según el rol

| Rol | Qué ve en el menú |
|---|---|
| Visitante | Inicio, Buscar vuelos, Iniciar sesión, Crear cuenta. |
| Pasajero y Cliente | Lo anterior más Mis reservas, Check-in y Comprobantes. |
| Counter | Lo anterior más Administración: Usuarios, pasajeros y reservas, y Reportes. |
| Comercial | Administración: Reportes, Marcas de tarjeta y Métodos de pago. |
| Operaciones | Administración: Vuelos y frecuencias, Aeronaves y asientos, Reportes y catálogos de la flota. |
| Administrador | Administración completa, Roles de usuarios y catálogos operativos. |
| SuperUsuario | Todo lo anterior, incluida la eliminación en los CRUD. |

## 6. Módulos administrativos (CRUD)

Aeropuertos, Rutas, Frecuencias, Modelos de aeronave, Aeronaves, Itinerarios, Marcas de tarjeta y Métodos de pago. Se abren desde el menú **Administración**, con `Views/<Módulo>/Index`, `Create`, `Edit`, `Details` y `Delete`. Solo el SuperUsuario puede eliminar.

## 7. Dónde buscar cada requisito de la rúbrica

| Requisito | Dónde está |
|---|---|
| Roles y usuarios por código | `src/AerolineaVIP/Global.asax.cs` |
| Inicio de sesión y registro | `Controllers/AccountController.cs`, `Views/Account/` |
| Mapa de navegación por rol | `Views/Shared/_Layout.cshtml` |
| Mapa de asientos | `Views/Shared/_MapaAsientos.cshtml`, `Views/Reservas/SeleccionAsiento.cshtml` |
| CRUD | `Controllers/` y `Views/` de cada catálogo |
| Políticas DP-01 a DP-15 | `docs/Primera_Entrega_Documentacion_Grupo04.pdf` |
| Base de datos | `database/Script_BD_AerolineaVIP_Unificado.sql` |

## 8. Cómo ejecutarlo

1. Ejecutar el script SQL en SQL Server.
2. Abrir `src/AerolineaVIP.slnx` en Visual Studio.
3. Revisar las cadenas de conexión en `Web.config` si el servidor no es el local.
4. Recompilar y ejecutar con F5. El sitio abre en `https://localhost:44346`.
