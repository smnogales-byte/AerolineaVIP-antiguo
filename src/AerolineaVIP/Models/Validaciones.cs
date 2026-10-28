using System;
using System.ComponentModel.DataAnnotations;

namespace Progra_03.Models
{
    // Validaciones de servidor (y de cliente, via jQuery unobtrusive) para las entidades administrables.
    // Se asocian con [MetadataType] para no modificar las clases que genera Entity Framework.

    [MetadataType(typeof(AeropuertoMeta))]
    public partial class Aeropuerto { }

    public class AeropuertoMeta
    {
        [Required(ErrorMessage = "Ingrese el código IATA.")]
        [RegularExpression("^[A-Z]{3}$", ErrorMessage = "El código IATA son 3 letras mayúsculas (por ejemplo UIO).")]
        public string CodigoIATA { get; set; }

        [Required(ErrorMessage = "Ingrese el nombre del aeropuerto.")]
        [StringLength(150, ErrorMessage = "Máximo 150 caracteres.")]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "Ingrese la ciudad o localidad.")]
        [StringLength(100, ErrorMessage = "Máximo 100 caracteres.")]
        public string Localidad { get; set; }
    }

    [MetadataType(typeof(RutaMeta))]
    public partial class Ruta { }

    public class RutaMeta
    {
        [Range(typeof(decimal), "0", "99999999", ErrorMessage = "La tarifa debe ser un valor positivo.")]
        public decimal TarifaReferencial { get; set; }
    }

    [MetadataType(typeof(FrecuenciaMeta))]
    public partial class Frecuencia { }

    public class FrecuenciaMeta
    {
        [Range(1, 7, ErrorMessage = "El día debe estar entre 1 (lunes) y 7 (domingo).")]
        public byte DiaSemana { get; set; }
    }

    [MetadataType(typeof(ModeloAeronaveMeta))]
    public partial class ModeloAeronave { }

    public class ModeloAeronaveMeta
    {
        [Required(ErrorMessage = "Ingrese el fabricante.")]
        [StringLength(60, ErrorMessage = "Máximo 60 caracteres.")]
        public string Fabricante { get; set; }

        [Required(ErrorMessage = "Ingrese el nombre del modelo.")]
        [StringLength(60, ErrorMessage = "Máximo 60 caracteres.")]
        public string Nombre { get; set; }

        [Range(1, 7, ErrorMessage = "La capacidad debe ser de 1 a 7 asientos.")]
        public byte CapacidadAsientos { get; set; }
    }

    [MetadataType(typeof(ItinerarioMeta))]
    public partial class Itinerario { }

    public class ItinerarioMeta
    {
        [Required(ErrorMessage = "Ingrese la referencia.")]
        [StringLength(30, ErrorMessage = "Máximo 30 caracteres.")]
        public string Referencia { get; set; }

        [Required(ErrorMessage = "Ingrese el nombre del itinerario.")]
        [StringLength(150, ErrorMessage = "Máximo 150 caracteres.")]
        public string Nombre { get; set; }
    }

    [MetadataType(typeof(MarcaTarjetaMeta))]
    public partial class MarcaTarjeta { }

    public class MarcaTarjetaMeta
    {
        [Required(ErrorMessage = "Ingrese el código.")]
        [StringLength(20, ErrorMessage = "Máximo 20 caracteres.")]
        [RegularExpression("^[A-Z0-9_]+$", ErrorMessage = "Use mayúsculas, números o guion bajo.")]
        public string Codigo { get; set; }

        [Required(ErrorMessage = "Ingrese el nombre.")]
        [StringLength(60, ErrorMessage = "Máximo 60 caracteres.")]
        public string Nombre { get; set; }

        [Range(12, 19, ErrorMessage = "La longitud del número debe estar entre 12 y 19.")]
        public byte LongitudNumero { get; set; }

        [Range(3, 4, ErrorMessage = "El código de seguridad tiene 3 o 4 dígitos.")]
        public byte LongitudCodigoSeguridad { get; set; }

        [Required(ErrorMessage = "Ingrese los prefijos de referencia.")]
        [StringLength(40, ErrorMessage = "Máximo 40 caracteres.")]
        public string PrefijosReferencia { get; set; }

        [Required(ErrorMessage = "Ingrese la ruta del logotipo.")]
        [StringLength(150, ErrorMessage = "Máximo 150 caracteres.")]
        public string RutaLogo { get; set; }
    }

    [MetadataType(typeof(MetodoPagoMeta))]
    public partial class MetodoPago { }

    public class MetodoPagoMeta
    {
        [Required(ErrorMessage = "Ingrese el código.")]
        [StringLength(20, ErrorMessage = "Máximo 20 caracteres.")]
        [RegularExpression("^[A-Z0-9_]+$", ErrorMessage = "Use mayúsculas, números o guion bajo.")]
        public string Codigo { get; set; }

        [Required(ErrorMessage = "Ingrese el nombre.")]
        [StringLength(100, ErrorMessage = "Máximo 100 caracteres.")]
        public string Nombre { get; set; }
    }
}
