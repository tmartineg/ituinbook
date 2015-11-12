using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace ReadAndLearn.Models
{
    public class Escena
    {
        public int EscenaID { get; set; }

        public string Nombre { get; set; }
        public string Descripcion { get; set; }

        public int ModuloID { get; set; }
        public virtual Modulo Modulo { get; set; }
        public virtual ConfigEscena ConfigEscena { get; set; }
        public ICollection<Accion> Acciones { get; set; }
    }

    public class Accion
    {
        public int AccionID { get; set; }

        // Código de operación  
        [Display(Name = "Acción")]
        public int CodeOP { get; set; }
        public string Mensaje { get; set; }

        public int EscenaID { get; set; }
        public virtual Escena Escena { get; set; }

        public int Orden { get; set; }

        public virtual int TextoID { get; set; }
        public virtual int PaginaID { get; set; }
        public virtual int PreguntaID { get; set; }
    }

    public class ConfigEscena
    {
        public int ConfigEscenaID { get; set; }

        public bool Autopase { get; set; }        
        public bool Atras { get; set; }        
        
        public int EscenaID { get; set; }
        public virtual Escena Escena { get; set; }
    }
}