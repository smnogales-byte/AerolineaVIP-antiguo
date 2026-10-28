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

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);


            // CODIGO INSERTADO: roles, usuarios y asignacion de roles por codigo.
            // Cada elemento se crea solo si no existe, asi que reiniciar no duplica nada.
            try
            {
                ApplicationDbContext db = new ApplicationDbContext();

                CrearRoles(db);
                CrearSuperUsuario(db);
                CrearAdministrador(db);
                CrearOperaciones(db);
                CrearComercial(db);
                CrearCounter(db);
                CrearPasajero(db);
                CrearCliente(db);
                AsignarPermisos(db);

                db.Dispose();
            }
            catch (Exception ex)
            {
                // Si la base de Identity no esta disponible la aplicacion arranca igual.
                System.Diagnostics.Trace.TraceError("Inicializacion de Identity: " + ex);
            }
        }





        private void CrearRoles(ApplicationDbContext db)
        {
            var rolemanager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(db));

            string[] roles =
            {
                RolesApp.SuperUsuario, RolesApp.Administrador, RolesApp.Operaciones,
                RolesApp.Comercial, RolesApp.Counter, RolesApp.Pasajero, RolesApp.Cliente
            };

            foreach (string rol in roles)
            {
                if (!rolemanager.RoleExists(rol))
                {
                    rolemanager.Create(new IdentityRole(rol));
                }
            }
        }




        private void CrearSuperUsuario(ApplicationDbContext db)
        {
            CrearUsuario(db, "superadmin@aerolineavip.com", "SuperAdmin#2026", "Super", "Usuario");
        }



        private void CrearAdministrador(ApplicationDbContext db)
        {
            CrearUsuario(db, "administrador@aerolineavip.com", "Administrador#2026", "Ana", "Administradora");
        }



        private void CrearOperaciones(ApplicationDbContext db)
        {
            CrearUsuario(db, "operaciones@aerolineavip.com", "Operaciones#2026", "Oscar", "Operaciones");
        }



        private void CrearComercial(ApplicationDbContext db)
        {
            CrearUsuario(db, "comercial@aerolineavip.com", "Comercial#2026", "Carla", "Comercial");
        }



        private void CrearCounter(ApplicationDbContext db)
        {
            CrearUsuario(db, "counter@aerolineavip.com", "Counter#2026", "Carlos", "Counter");
        }



        private void CrearPasajero(ApplicationDbContext db)
        {
            CrearUsuario(db, "pasajero@aerolineavip.com", "Pasajero#2026", "Paula", "Pasajera");
        }



        private void CrearCliente(ApplicationDbContext db)
        {
            CrearUsuario(db, "cliente@aerolineavip.com", "Cliente#2026", "Clara", "Cliente");
        }



        // Crea un usuario si no existe. El usuario es el correo (el login acepta usuario o correo).
        // Nombres y apellidos se guardan como claims de Identity (no se altera el esquema de la base).
        private void CrearUsuario(ApplicationDbContext db, string correo, string clave, string nombres, string apellidos)
        {
            var usermanager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db));
            var user = usermanager.FindByName(correo);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = correo,
                    Email = correo
                };
                var resultado = usermanager.Create(user, clave);
                if (resultado.Succeeded)
                {
                    usermanager.AddClaim(user.Id, new Claim(ClaimTypes.GivenName, nombres));
                    usermanager.AddClaim(user.Id, new Claim(ClaimTypes.Surname, apellidos));
                }
            }
        }



        private void AsignarPermisos(ApplicationDbContext db)
        {
            var usermanager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db));


            // 1) superadmin
            AsignarRol(usermanager, "superadmin@aerolineavip.com", RolesApp.SuperUsuario);

            // 2) administrador
            AsignarRol(usermanager, "administrador@aerolineavip.com", RolesApp.Administrador);

            // 3) operaciones
            AsignarRol(usermanager, "operaciones@aerolineavip.com", RolesApp.Operaciones);

            // 4) comercial
            AsignarRol(usermanager, "comercial@aerolineavip.com", RolesApp.Comercial);

            // 5) counter
            AsignarRol(usermanager, "counter@aerolineavip.com", RolesApp.Counter);

            // 6) pasajero
            AsignarRol(usermanager, "pasajero@aerolineavip.com", RolesApp.Pasajero);

            // 7) cliente
            AsignarRol(usermanager, "cliente@aerolineavip.com", RolesApp.Cliente);
        }



        private void AsignarRol(UserManager<ApplicationUser> usermanager, string correo, string rol)
        {
            var usuario = usermanager.FindByName(correo);
            if (usuario != null)
            {
                if (!usermanager.IsInRole(usuario.Id, rol))
                {
                    usermanager.AddToRole(usuario.Id, rol);
                }
            }
        }
    }
}
