using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ReadAndLearn.Models
{
    public class TextoMod
    {
        public int TextoModID { get; set; }

        public string Nombre { get; set; }
        public string Descripcion { get; set; }

        public virtual ICollection<Escena> Escenas { get; set; }
        public virtual ICollection<PaginaMod> PaginasMod { get; set; }
        public virtual ICollection<PreguntaMod> PreguntasMod { get; set; }
    }

    public class PaginaMod
    {
        public int PaginaModID { get; set; }
        public string Contenido { get; set; }
        public int TextoModID { get; set; }

        public virtual TextoMod TextoMod { get; set; }
        public virtual ICollection<PreguntaMod> PreguntasMod { get; set; }
    }

    public class PreguntaMod
    {
        public int PreguntaModID { get; set; }

        public string Enunciado { get; set; }
        public int Dificultad { get; set; }
        public string Pertinente { get; set; }
        public int TipoPreguntaID { get; set; }

        public virtual TextoMod TextoMod { get; set; }
        public virtual PaginaMod PaginaMod { get; set; }
        public virtual ICollection<AlternativaMod> AlternativasMod { get; set; }
        public virtual AyudaMod AyudaMod { get; set; }
    }

    public class AlternativaMod
    {
        public int AlternativaID { get; set; }

        public bool Valor { get; set; }
        public string FeedbackContenido { get; set; }
        public string Opcion { get; set; }

        public int PreguntaModID { get; set; }
        public virtual PreguntaMod PreguntaMod { get; set; }
    }

    public class CriterioMod
    {
        public int CriterioModID { get; set; }

        public double Valor { get; set; }
        public string FeedbackCriterio { get; set; }
        public string Opcion { get; set; }

        public int PreguntaModID { get; set; }
        public virtual PreguntaMod PreguntaMod { get; set; }
    }

    public class AyudaMod
    {
        public int AyudaModID { get; set; }

        public string ParafraseoN1 { get; set; }
        public string Prismaticos { get; set; }
        public string Lupa { get; set; }

        public int PreguntaID { get; set; }
        public virtual PreguntaMod PreguntaMod { get; set; }
    }

}