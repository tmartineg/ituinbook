using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace ReadAndLearn.Models
{
    public class Indice
    {
        public int Nivel { get; set; }
        public string Contenido { get; set; }
        public int PaginaID { get; set; }
    }

    public class UserProfilePreguntas
    {
        [Key, Column(Order = 0)]
        public int UserId { get; set; }
        [Key, Column(Order = 1)]
        public int PreguntaID { get; set; }

        public virtual UserProfile Usuario { get; set; }
        public virtual Pregunta Pregunta { get; set; }

        public bool Resultado { get; set; }
        public float PertFound { get; set; }
        public float PertNotFound { get; set; }
    }

    public class DatosSecuencia
    {
        public string CodeOP { get; set; }
        public string Cadena { get; set; }
    }

    public class DatoSujeto {
        // Usuario 
        public string Nombre { get; set; }
        public int UserID { get; set; }

        public List<DatoUnitario> datos { get; set; }
    }

    public class DatoUnitario
    {
        // Texto
        public double TiempoTotalTexto { get; set; }
        public DateTime InicioTexto { get; set; }
        public DateTime FinalTexto { get; set; }
        public double PorcLectIni { get; set; }
        public double TiempoLecIni { get; set; }
        public double VelLecIni { get; set; }
        public string Continuidad { get; set; }

        // Pregunta
        public int PreguntaID { get; set; }
        public int Intento { get; set; }
        public double TiempoTotalPregunta { get; set; }
        public DateTime Inicio { get; set; }
        public DateTime Final { get; set; }
        public int TipoPregunta { get; set; }
        public double PorcAcierto { get; set; }
        public string RespDada { get; set; }
        public int NumCambiosResp { get; set; }
        public int NumEnun { get; set; }
        public int NumAlte { get; set; }
        public double TiempoEnum { get; set; }
        public double TiempoAlte { get; set; }
        public double VelEnun { get; set; }
        public double VelAlte { get; set; }
        public double PorcTmpPrimLecEnun { get; set; } // Porcentaje de tiempo en la primera lectura del enunciado
        public double PorcTmpPrimLecAlte { get; set; } // Porcentaje de tiempo en la primera lectura de las alternativas
        public int NumBusq { get; set; }
        public double TiempoTotalBusqueda { get; set; }
        public double TiempoBusquedaPert { get; set; }
        public double TiempoBusquedaNoPert { get; set; }
        public int UltPert { get; set; }
        public int PenUltPert { get; set; }
        public double PorcPertEncTotal { get; set; } // Porcentaje de pertinente encontrado sobre el total
        public double PorcPertEncBusqueda { get; set; } // Porcentaje de pertinente encontrado soble la búsqueda
        public double VelBusqueda { get; set; }
        public int NumAyudas { get; set; }
        public double TiempoAyudas { get; set; }
        public int NumAyuda1 { get; set;  }
        public int NumAyuda2 { get; set; }
        public int NumAyuda3 { get; set; }
        public double TiempoAyuda1 { get; set; }
        public double TiempoAyuda2 { get; set; }
        public double TiempoAyuda3 { get; set; }
        public string OrdenBusqueda { get; set; }

        public double PorcPertSubTarea { get; set; }
        public double PorcNoPertSubTarea { get; set; }
        public double PorcPertTarea { get; set; }
        public double PorcNoPertTarea { get; set; }

        public double TiempoLecFDBK { get; set; }
        public double VelLecFDBK { get; set; }


    }
}