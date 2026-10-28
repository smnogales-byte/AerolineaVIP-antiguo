using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Progra_03.Models;

namespace Progra_03
{
    public class MvcApplication : System.Web.HttpApplication
    {
        // Cuenta inicial: rol, usuario (= correo, la pantalla de login autentica con el correo), clave, nombres y apellidos.
        // Son cuentas de prueba academicas, una por rol de la matriz 16.1. No subir estas claves a un repositorio publico.
        private class CuentaInicial
        {
            public string Rol;
            public string Correo;
            public string Clave;
            public string Nombres;
            public string Apellidos;
        }

        private static readonly CuentaInicial[] Cuentas = new[]
        {
            new CuentaInicial { Rol = RolesApp.SuperUsuario,   Correo = "superadmin@aerolineavip.com",     Clave = "SuperAdmin#2026",     Nombres = "Super",    Apellidos = "Usuario" },
            new CuentaInicial { Rol = RolesApp.Administrador,  Correo = "administrador@aerolineavip.com",  Clave = "Administrador#2026",  Nombres = "Ana",      Apellidos = "Administradora" },
            new CuentaInicial { Rol = RolesApp.Operaciones,    Correo = "operaciones@aerolineavip.com",    Clave = "Operaciones#2026",    Nombres = "Oscar",    Apellidos = "Operaciones" },
            new CuentaInicial { Rol = RolesApp.Comercial,      Correo = "comercial@aerolineavip.com",      Clave = "Comercial#2026",      Nombres = "Carla",    Apellidos = "Comercial" },
            new CuentaInicial { Rol = RolesApp.Counter,        Correo = "counter@aerolineavip.com",        Clave = "Counter#2026",        Nombres = "Carlos",   Apellidos = "Counter" },
            new CuentaInicial { Rol = RolesApp.Pasajero,       Correo = "pasajero@aerolineavip.com",       Clave = "Pasajero#2026",       Nombres = "Paula",    Apellidos = "Pasajera" },
            new CuentaInicial { Rol = RolesApp.Cliente,        Correo = "cliente@aerolineavip.com",        Clave = "Cliente#2026",        Nombres = "Clara",    Apellidos = "Cliente" }
        };

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            // PRIMERA ENTREGA (punto 2): roles, SuperUsuario, Administrador, usuarios iniciales y
            // asignacion de roles se crean por codigo al iniciar la aplicacion. Cada elemento se
            // crea solo si no existe, asi que reiniciar no duplica nada.
            try
            {
                using (var db = new ApplicationDbContext())
                {
                    CrearRoles(db);
                    CrearUsuariosIniciales(db);
                    AsignarRoles(db);
                }
            }
            catch (Exception ex)
            {
                // Si la base de Identity no esta disponible la aplicacion arranca igual; el error queda en la traza.
                System.Diagnostics.Trace.TraceError("Inicializacion de Identity: " + ex);
            }
        }

        // Crea los roles de la matriz de autorizacion 16.1 en AerolineaVIP_Identity (tabla AspNetRoles).
        // SuperUsuario: acceso total y unico que asigna roles. Administrador: administracion limitada.
        // Operaciones y Comercial: responsables operativo y comercial. Counter: empleado de counter.
        // Pasajero y Cliente: usuarios finales; Cliente es el rol por defecto de quien se registra.
        // La matriz es una propuesta inicial (no aprobada): la autorizacion real se valida en el servidor.
        private static void CrearRoles(ApplicationDbContext db)
        {
            var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(db));
            string[] roles =
            {
                RolesApp.SuperUsuario, RolesApp.Administrador, RolesApp.Operaciones,
                RolesApp.Comercial, RolesApp.Counter, RolesApp.Pasajero, RolesApp.Cliente
            };
            foreach (string rol in roles)
            {
                if (!roleManager.RoleExists(rol)) roleManager.Create(new IdentityRole(rol));
            }
        }

        // Crea el SuperUsuario, el Administrador y un usuario por cada uno de los demas roles.
        // Nombres y apellidos se guardan como claims de Identity (no se altera el esquema de la base).
        private static void CrearUsuariosIniciales(ApplicationDbContext db)
        {
            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db));
            foreach (var c in Cuentas)
            {
                var usuario = userManager.FindByName(c.Correo);
                if (usuario != null) continue;

                usuario = new ApplicationUser { UserName = c.Correo, Email = c.Correo };
                var resultado = userManager.Create(usuario, c.Clave);
                if (!resultado.Succeeded)
                {
                    System.Diagnostics.Trace.TraceError("No se pudo crear " + c.Correo + ": " + string.Join("; ", resultado.Errors));
                    continue;
                }
                userManager.AddClaim(usuario.Id, new Claim(ClaimTypes.GivenName, c.Nombres));
                userManager.AddClaim(usuario.Id, new Claim(ClaimTypes.Surname, c.Apellidos));
            }
        }

        // Asigna a cada usuario inicial su rol (idempotente).
        private static void AsignarRoles(ApplicationDbContext db)
        {
            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db));
            foreach (var c in Cuentas)
            {
                var usuario = userManager.FindByName(c.Correo);
                if (usuario == null) continue;
                if (!userManager.IsInRole(usuario.Id, c.Rol)) userManager.AddToRole(usuario.Id, c.Rol);
            }
        }
    }
}
