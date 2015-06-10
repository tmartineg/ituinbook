using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Globalization;
using System.Web.Security;

namespace iTuinBook.Models
{
    public class TextRes
    {
        public int TextResID { get; set; }

        public int GrupoID { get; set; }
        public int ModuloID { get; set; }
        public int TextoID { get; set; }
        public int UserProfileID { get; set; }
        public virtual UserProfile UserProfile { get; set; }

        // GENERAL 
        public double TmpTotal { get; set; } // Tiempo total de texto
        public string Momento { get; set; } // Momento (Fecha) en el que tuvo lugar

        // LECTURA INICIAL 
        public double PorcLectIni { get; set; } // Porcentaje de lectura inicial
        public double TmpLecIni { get; set; } // Tiempo de lectura inicial
        public double VelLecIni { get; set; } // Velocidad de lectura inicial

        public string Continuidad { get; set; } // Orden de lectura de regiones
    }

    public class PregRes
    {
        // PREGUNTA 
        public int PregResID { get; set; }

        public int GrupoID { get; set; }
        public int ModuloID { get; set; }
        public int TextoID { get; set; }
        public int PreguntaID { get; set; }
        public int UserProfileID { get; set; }
        public virtual UserProfile UserProfile { get; set; }

        // GENERAL 
        public int TmpTotal { get; set; } // Tiempo total de pregunta
        public int Intento { get; set; } // Intento de pregunta
        public string Momento { get; set; } // Momento (Fecha) en el que tuvo lugar

        // RESPUESTA 
        public int PorcAcierto { get; set; } // Porcentaje de acuerto
        public int NumModResp { get; set; } // Número de veces que modifica la respuesta

        // ENUNCIADO / ALTERNATIVAS 
        public int NumEnun { get; set; } // Número de veces abierto el enunciado
        public int NumAlte { get; set; } // Número de veces abierto las alternativas

        public double TmpEnun { get; set; } // Tiempo leyendo enunciado
        public double TmpAlte { get; set; } // Tiempo leyendo alternativas

        public double VelEnun { get; set; } // Velocidad de lectura del enunciado
        public double VelAlte { get; set; } // Velocidad de lectura de las alternativas

        public double PorcPrimLecEnun { get; set; } // Porcentaje de tiempo en la primera lectura sobre el total
        public double PorcPrimLecAlte { get; set; } // Porcentaje de tiempo en la primera lectura sobre el total

        // BÚSQUEDAS 
        public int NumBusq { get; set; } // Número de búsquedas
        public double TmpBusqTotal { get; set; } // Tiempo de búsqueda total
        public double TmpBusqPert { get; set; } // Tiempo de búsqueda de pertinente
        public double TmpBusqNoPert { get; set; } // Tiempo de búsqueda de no pertinente

        public int UltPert { get; set; } // Último Pertinente
        public double PorcPertEnc { get; set; } // % Pertinente encontrado sobre el total
        public double PorcPertBuq { get; set; } // % Pertinente encontrado sobre regiones busquedas

        public double VelBusq { get; set; } // Velocidad de búsqueda

        // AYUDAS 
        public int NumAyudas { get; set; } // Número total de ayudas abiertas
        public int TmpAyudas { get; set; } // Total total en ayudas

        public int NumAyu1 { get; set; } // Parafraseo
        public int NumAyu2 { get; set; } // Prisma
        public int NumAyu3 { get; set; } // Lupa

        public double TmpAyu1 { get; set; } // Parafraseo
        public double TmpAyu2 { get; set; } // Prisma
        public double TmpAyu3 { get; set; } // Lupa
    }

    public class ReglaSimple
    {
        public int ReglaSimpleID { get; set; }

        public int UserProfileID { get; set; }
        public virtual UserProfile UserProfile { get; set; }

        public int Variable { get; set; }
        public int Operador { get; set; }
        public double Parametro { get; set; }
        public string Param { get; set; }
    }
    /*
     * Operador Simple 
     * 1. ==
     * 2. <=
     * 3. >=
     * 4. ¡=
     * 5. <
     * 6. >
     */
    public class ReglaCompleja
    {
        public int ReglaComplejaID { get; set; }
        public int Nombre { get; set; }
        public int Descripcion { get; set; }
        public string Nom { get; set; }
        public string Descripcio { get; set; }

        public int UserProfileID { get; set; }
        public virtual UserProfile UserProfile { get; set; }
                
        public int Regla_1 { get; set; }
        public int Operador { get; set; }
        public int Regla_2 { get; set; }
        public int OpCode { get; set; }
        public string Feedback { get; set; }

        public virtual ICollection<Modulo> Modulos { get; set; }
    }
    /* 
     * Operador Complejo
     * 1. &&
     * 2. ||
     */

    /*
     * OpCode
     * 1. RS - Null
     * 2. Null - RS
     * 3. RC - Null
     * 4. Null - RC
     * 5. RS - RS
     * 6. RS - RC
     * 7. RC - RS
     * 8. RC - RC
     */

    /*
     * Timing 
     * Tipo:
     * 0 - Simple / Acumulativo
     * 1 - Complejo / Desarrollado
     */

    public class Timing
    {
        public int TimingID { get; set; }

        public string Nombre { get; set; }
        public string Descripcion { get; set; }

        public int UserProfileID { get; set; }
        public virtual UserProfile UserProfile { get; set; }

        public int PregLanzada { get; set; }
        [Required(ErrorMessage = "Valor requerido.")]
        [RegularExpression("^[0-9]*", ErrorMessage="Debe ser un número entero positivo.")]
        public int NAnterior { get; set; }
        public int Tipo { get; set; }
        public string Feedback { get; set; }

        public virtual ICollection<Modulo> Modulos { get; set; }
        public virtual ICollection<ItemTiming> ItemTimings { get; set; }
    }

    public class ItemTiming
    {
        public int ItemTimingID { get; set; }

        
        public string Binario { get; set; }
        public string Feedback { get; set; }

        public int TimingID { get; set; }
        public virtual Timing Timing { get; set; }
    }

    public class DatosUsuario
    {
        public int DatosUsuarioID { get; set; }
        public int GrupoID { get; set; }
        public int ModuloID { get; set; }
        public DateTime Inicio { get; set; }
        public int Puntos { get; set; }

        public int IndSelec { get; set; }
        public int IndUsoAyud { get; set; }
        public int IndAcierto { get; set; }

        public bool Cerrada { get; set; }
        public int AccionActual { get; set; }
        public int EscenaActual { get; set; }
        public int PreguntaID { get; set; }
        public int PreguntaActual { get; set; }
        public int TextoID { get; set; }
        public int TextoActual { get; set; }

        /* FEEDBACK */
        public int RespuestaPos { get; set; }
        public int RespuestaNeg { get; set; }
        public int BuscaPos { get; set; }
        public int BuscaNeg { get; set; }
        public int AyudaPos { get; set; }
        public int AyudaNeg { get; set; }
        public int RevisaPos { get; set; }
        public int RevisaNeg { get; set; }
        public int SeleccionPos { get; set; }
        public int SeleccionNeg { get; set; }
        /*************/
        public int RespuestaStatus { get; set; }
        public int BuscaStatus { get; set; }
        public int AyudaStatus { get; set; }
        public int RevisaStatus { get; set; }
        public int SeleccionStatus { get; set; }
        public int PertinenteStatus { get; set; }
        public string PertinenteOrden { get; set; }

        /***********/
        public string FeedbackAcumulado { get; set; }
        public int ContadorFDBCAcum { get; set; }
        public float SumaAciertos { get; set; }
        public string SecuenciaAciertos { get; set; }

        public int UserProfileID { get; set; }
        public virtual UserProfile UserProfile { get; set; }
        public virtual ICollection<DatoSimple> DatoSimple { get; set; }
    }

    public class DatoSimple
    {
        public int DatoSimpleID { get; set; }
        public int TextoID { get; set; }
        public int PaginaID { get; set; }
        public int PreguntaID { get; set; }
        public DateTime Momento { get; set; }
        public float Tiempo { get; set; }
        public int CodeOP { get; set; }
        public int Valor { get; set; }
        public float Dato01 { get; set; }
        public float Dato02 { get; set; }
        public float Dato03 { get; set; }
        public string Info { get; set; }
        public string Info2 { get; set; }

        public int DatosUsuarioID { get; set; }
        public virtual DatosUsuario DatosUsuario { get; set; }
    }

    [Table("UserProfile")]
    public class UserProfile
    {
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int UserId { get; set; }        
        public string UserName { get; set; }

        public string Nombre { get; set; }
        public string Type { get; set; }

        public virtual ICollection<Inscripcion> Inscripciones { get; set; }
        public virtual ICollection<Grupo> Grupos { get; set; }
        public virtual ICollection<Modulo> Modulos { get; set; }
        public virtual ICollection<Imagenes> Imagenes { get; set; }
        public virtual ICollection<DatosUsuario> DatosUsuario { get; set; }
        public virtual ICollection<TextRes> TextosResueltos { get; set; }
        public virtual ICollection<PregRes> PreguntasResueltas { get; set; }
    }

    public class RegisterExternalLoginModel
    {
        [Required]
        [Display(Name = "Nombre de usuario")]
        public string UserName { get; set; }
        public string Type { get; set; }
        
        public string ExternalLoginData { get; set; }
    }

    public class LocalPasswordModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña actual")]
        public string OldPassword { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "El número de caracteres de {0} debe ser al menos {2}.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Nueva contraseña")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirmar la nueva contraseña")]
        [Compare("NewPassword", ErrorMessage = "La nueva contraseña y la contraseña de confirmación no coinciden.")]
        public string ConfirmPassword { get; set; }
    }

    public class LoginModel
    {
        [Required]
        [Display(Name = "Nombre de usuario")]
        public string UserName { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; }

        [Display(Name = "¿Recordar cuenta?")]
        public bool RememberMe { get; set; }
    }

    public class RegisterModel
    {
        [Required]
        [Display(Name = "Nombre de usuario")]
        public string UserName { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "El número de caracteres de {0} debe ser al menos {2}.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirmar contraseña")]
        [Compare("Password", ErrorMessage = "La contraseña y la contraseña de confirmación no coinciden.")]
        public string ConfirmPassword { get; set; }

        [Required]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; }

        [Required]
        [Display(Name = "Primer Apellido")]
        public string Apellido1 { get; set; }

        [Display(Name = "Segundo Apellido")]
        public string Apellido2 { get; set; }

        [Required]
        [Display(Name = "Fecha de Nacimiento")]
        public string Nacimiento { get; set; }

        [Required]
        [Display(Name = "Sexo")]
        public string Sexo { get; set; }

        [Required]
        [Display(Name = "Centro")]
        public string Centro { get; set; }

        [Display(Name = "Curso")]
        public string Curso { get; set; }

        [Required]
        [Display(Name = "Población")]
        public string Poblacion { get; set; }

        [Required]
        [Display(Name = "Lengua materna")]
        public string LenguaMaterna { get; set; }

        [Required]
        [Display(Name = "Lengua de escolarización")]
        public string LenguaEscola { get; set; }

        [Required]
        [Display(Name = "Tipo de cuenta")]
        public string TypePerson { get; set; }

    }

    public class ExternalLogin
    {
        public string Provider { get; set; }
        public string ProviderDisplayName { get; set; }
        public string ProviderUserId { get; set; }
        public string ProviderType { get; set; }
    }
}
