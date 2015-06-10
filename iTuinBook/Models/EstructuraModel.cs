using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace iTuinBook.Models
{
    public class Grupo
    {
        public int GrupoID { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public bool Publico { get; set; }

        public string Orden { get; set; }

        public virtual ICollection<Inscripcion> Inscripciones { get; set; }
        public virtual ICollection<Modulo> Modulos { get; set; }
        public virtual ICollection<UserProfile> Propietarios { get; set; }

        public virtual ConfigGrupo ConfigGrupo { get; set; }
    }

    public class ConfigGrupo
    {
        public int ConfigGrupoID { get; set; }

        [Required(ErrorMessage = "Cambio automático entre módulos.")]
        public bool AutoChange { get; set; }

        public int GrupoID { get; set; }
        public virtual Grupo Grupo { get; set; }
    }

    public class Modulo
    {
        public int ModuloID { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public bool Publico { get; set; }
        public int Condicion { get; set; }

        public virtual ICollection<UserProfile> Propietarios { get; set; }
        public virtual ICollection<Grupo> Grupos { get; set; }
        public int Propiedad { get; set; }

        // Cuando sea modelado, estará vacía
        public virtual ICollection<Texto> Textos { get; set; }
        // Cuando sea práctica independiente o libro electrónico, estará vacía
        public virtual ICollection<Escena> Escenas { get; set; }

        public virtual ICollection<ReglaCompleja> ReglasComplejas { get; set; }
        public virtual ICollection<Timing> Timings { get; set; }

        public virtual ConfigModulo ConfigModulo { get; set; }
    }

    public class ConfigModulo
    {
        public int ConfigModuloID { get; set; }

        [Required(ErrorMessage = "¿Quieres avatares?")]
        public bool Avatares { get; set; }

        [Required(ErrorMessage = "¿Quieres sistema de puntos?")]
        public bool Puntos { get; set; }

        [Required(ErrorMessage = "¿Qué clase de feedback quieres?")] // True: Reglas - False: Automático
        public bool Feedback { get; set; }

        [Required(ErrorMessage = "¿Quieres ayudas?")]
        public bool Ayudas { get; set; }

        [Required(ErrorMessage = "¿Qué plantilla quiere usar?")]
        public int Plantilla { get; set; }

        public int ModuloID { get; set; }
        public virtual Modulo Modulo { get; set; }
    }

    public class Inscripcion
    {
        public int InscripcionID { get; set; }
        public bool Aceptado { get; set; }

        public int UserId { get; set; }
        public int GrupoID { get; set; }

        public virtual Grupo Grupo { get; set; }
        public virtual UserProfile Alumno { get; set; }
    }

    public class Solicitud
    {
        public int SolicitudID { get; set; }
        public Grupo Grupo { get; set; }
        public UserProfile User { get; set; }

        public Solicitud(int ins, UserProfile user, Grupo gru)
        {
            this.SolicitudID = ins;
            this.User = user;
            this.Grupo = gru;
        }
    }

    public class Imagenes
    {
        public int ImagenesID { get; set; }

        public virtual UserProfile UserProfile { get; set; }
        public string Nombre { get; set; }

        public byte [] file { get; set; }
    }

    public class Fichero
    {  
        public string Nombre { get; set; }

        [Required(ErrorMessage = "Por favor, carga una imagen.")]
        [Display(Name = "Cargar Imagen")]      
        [ValidateFile]
        public HttpPostedFileBase file { get; set; }
    }

    public class Seguimientos
    {
        public string UserName { get; set; }
        public string Nombre { get; set; }
        public double AccionActual { set; get; }
        public int GrupoID { get; set; }
        public int ModuloID { get; set; }
        public bool AvisoVel { get; set; }
        public bool AvisoErr { get; set; }
    }

    public class SeguimientosIndependiente
    {
        public string UserName { get; set; }
        public string Nombre { get; set; }
        public int EstadoActual { set; get; }
        public int EstadoMaximo { set; get; }
        public int GrupoID { get; set; }
        public int ModuloID { get; set; }
        public int Aciertos { get; set; }
        public int Errores { get; set; }
    }

    public class Datos
    {
        public string Nombre { get; set; }
        public string Usuario { get; set; }
        public string Fecha { get; set; }
        public string TiempoTotal { get; set; }

        public int UserId { get; set; }

        public string Puntos { get; set; }
        public string Acciones { get; set; }

        public string BuscaPos { get; set; }
        public string BuscaNeg { get; set; }

        public string AyudaPos { get; set; }
        public string AyudaNeg { get; set; }

        public string RevisaPos { get; set; }
        public string RevisaNeg { get; set; }

        public string RespuestaPos { get; set; }
        public string RespuestaNeg { get; set; }
    }

    public class DatosPregunta
    {
        public int PreguntaID { get; set; }
        public int Acierto { get; set; }
        public int Busca { get; set; }
        public int Ayuda { get; set; }
        public int Revisa { get; set; }
        public Double Tiempo { get; set; }
        public int Pregunta { get; set; }
        public int Texto { get; set; }
    }




    // select new { a.UserName, a.Nombre, du.AccionActual, g.GrupoID, du.ModuloID }).OrderBy(m => m.ModuloID);

    public class ValidateFileAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            int MaxContentLength = 1024 * 1024 * 3; //3 MB
            string[] AllowedFileExtensions = new string[] { ".jpg", ".gif", ".png" };
        
            var file = value as HttpPostedFileBase;
 
            if (file == null)
                return false;
            else if (!AllowedFileExtensions.Contains(file.FileName.Substring(file.FileName.LastIndexOf('.'))))
            {
                ErrorMessage = "Por favor, carga la imagen de tipo:: " + string.Join(", ", AllowedFileExtensions);
                return false;
            }
            else if (file.ContentLength > MaxContentLength)
            {
                ErrorMessage = "La imagen es demasiado grande. Tamaño máximo: " + (MaxContentLength / 1024).ToString() + "MB";
                return false;
            }
            else
                return true;
         }
    }
}