using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace ReadAndLearn.Models
{
    public class Texto
    {
        public int TextoID { get; set; }

        public string Nombre { get; set; }
        public string Descripcion { get; set; }

        // 0: Al final del texto; 1:
        public int LugarPreguntas { get; set; }
        
        public virtual ICollection<Modulo> Modulos { get; set; }
        public virtual ICollection<Pagina> Paginas { get; set; }
        public virtual ICollection<Pregunta> Preguntas { get; set; }
        public virtual ICollection<TareaOrdenar> TareasOrdenar { get; set; }

        public virtual ConfigTexto ConfigTexto { get; set; }

        //guirisan/secuencias
        public int Orden { get; set; }
    }

    public class Pagina
    {
        public int PaginaID { get; set; }
        public string Contenido { get; set; }
        public int TextoID { get; set; }
                
        public virtual Texto Texto { get; set; }
        public virtual ICollection<Pregunta> Preguntas { get; set; }

        //guirisan/secuencias
        public int Orden { get; set; }
    }

    public class Pregunta
    {
        public int PreguntaID { get; set; }

        public string Enunciado { get; set; }
        public int Dificultad { get; set; }
        public string Pertinente { get; set; }
        public int TipoPreguntaID { get; set; }
        public string Claves { get; set; }
        public int TareaID { get; set; }
        public string FDBK_Correcto { get; set; }
        public string FDBK_Incorrecto { get; set; }

        // Todas las preguntas 
        public ConfigPregunta ConfigPregunta { get; set; }

        // Tipo Test 
        public bool AlternativasEnmascarado { get; set; }
        public int VisibilidadAlternativas { get; set; } // 0: Desde el inicio; 1: Tras buscar; 2: Tras seleccionar

        // Abierta 
        public int VisibilidadCuadroRespuesta { get; set; } // 0: Desde el inicio; 1: Tras buscar; 2: Tras seleccionar

        // Selección 
        public virtual ICollection<Criterio> Criterios { get; set; }

        public virtual Texto Texto { get; set; }
        public virtual Pagina Pagina { get; set; }
        public virtual ICollection<Alternativa> Alternativas { get; set; }
        public virtual ICollection<Emparejado> Emparejados { get; set; }
        public virtual Ayuda Ayuda { get; set; }
        public virtual ICollection<UserProfile> Usuarios { get; set; }
        public virtual ICollection<SubPregunta> SubPreguntas { get; set; }

        //guirisan/secuencias
        public int Orden { get; set; }
    }

    public class TareaOrdenar
    {
        public int TareaOrdenarID { get; set; }

        public string Enunciado { get; set; }

        public string Nombre { get; set; }
        public int Num { get; set; }
        public int Tipo { get; set; }

        public virtual ICollection<Items> ItemsOrdenados { get; set; }
        public virtual string[] Orden { get; set; }
        public virtual string[] Ordenados { get; set; }

        public bool Vertical { get; set; }

        public virtual Texto Texto { get; set; }
        public int TextoID { get; set; }
    }

    public class Items
    {
        public int ItemsID { get; set; }
        public string Item { get; set; }
        public int Order { get; set; }

        public int TareaOrdenarID { get; set; }
        public virtual TareaOrdenar TareaOrdenar { get; set; }
    }

    public class SubPregunta
    {
        public int SubPreguntaID { get; set; }

        public string Enunciado { get; set; }

        public virtual ICollection<SubAlternativa> SubAlternativas { get; set; }
    }

    public class SubAlternativa
    {
        public int SubAlternativaID { get; set; }

        public string Opcion { get; set; }
    }

    public class Alternativa
    {
        public int AlternativaID { get; set; }

        public bool Valor { get; set; }
        public string FeedbackContenido { get; set; }
        public string Opcion { get; set; }

        public int PreguntaID { get; set; }
        public virtual Pregunta Pregunta { get; set; }
    }

    public class Criterio
    {
        public int CriterioID { get; set; }

        public double Valor { get; set; }
        public string FeedbackCriterio { get; set; }
        public string Opcion { get; set; }

        public int PreguntaID { get; set; }
        public virtual Pregunta Pregunta { get; set; }
    }

    public class Emparejado
    {
        public int EmparejadoID { get; set; }

        public string ColIzq { get; set; }
        public string ColDer { get; set; }

        public int PreguntaID { get; set; }
        public virtual Pregunta Pregunta { get; set; }
    }

    public class Ayuda
    {
        public int AyudaID { get; set; }

        public string ParafraseoN1 { get; set; }
        public string Prismaticos { get; set; }
        public string Lupa { get; set; }
        public bool Reutilizar { get; set; }

        public int PreguntaID { get; set; }
        public virtual Pregunta Pregunta { get; set; }
    }
    
    public class ConfigTexto
    {
        public int ConfigTextoID { get; set; }

        [Display(Name = "Permitir Marcar en el texto:")]
        public bool Marcado { get; set; }
        [Display(Name = "Permitir Hacer notas:")]
        public bool Notas { get; set; }
        [Display(Name = "Mostrar Índice automático:")]
        public bool Indice { get; set; }
        [Display(Name = "Permitir Buscar en el texto:")]
        public bool Busqueda { get; set; }

        public int TextoID { get; set; }
        public virtual Texto Texto { get; set; }
    }

    public class ConfigPregunta
    {
        public int ConfigPreguntaID { get; set; }

        // Enmascarado
        [Display(Name = " Enunciado durante la realización de las tareas")]
        public bool EnmascararEnunciado { get; set; }
        [Display(Name = " Alternativas durante la realización de las tareas")]
        public bool EnmascararAlternativas { get; set; }
        [Display(Name = " Enunciado durante la revisión (tras realizar todas las tareas)")]
        public bool EnmascararEnunciadoRevision { get; set; }
        [Display(Name = " Alternativas durante la revisión (tras realizar todas las tareas)")]
        public bool EnmascararAlternativasRevision { get; set; }
        [Display(Name = " Texto en lectura inicial (Enmascarado general de toda la página, no depende de las regiones)")]
        public bool EnmascararTextoLecIni { get; set; }
        [Display(Name = " Texto en búsqueda (Enmascarado general de toda la página, no depende de las regiones)")]
        public bool EnmascararTexto { get; set; }
        [Display(Name = " Texto en revisión (Enmascarado general de toda la página, no depende de las regiones)")]
        public bool EnmascararTextoRevisa { get; set; }

        // Disponibilidad
        [Display(Name = " Volver al texto para buscar (en caso de preguntas \"separadas\")")]
        public bool Busqueda { get; set; }
        [Display(Name = " Ayudas (parafraseo, lupa, prismáticos)")]
        public bool Ayuda { get; set; }

        // Tareas
        [Display(Name = " Responder")]
        public bool Responder { get; set; }
        [Display(Name = " Seleccionar información (pertinente)")]
        public bool SeleccionarPertinente { get; set; }
        [Display(Name = " Segundo intento para seleccionar información (pertinente)")]
        public bool DosSeleccionarPertinente { get; set; }

        [Display(Name = " Activar selección automática punto-a-punto en tareas de selección")]
        public bool AutoSeleccion { get; set; }


        [Display(Name = " Validación simultánea (Responder/Seleccionar)")]
        public bool SimultanearTareas { get; set; }
        [Display(Name = " Segundo intento de selección")]
        public bool DosIntentos { get; set; }
        [Display(Name = " Seleccionar palabras clave")]
        public bool SeleccionarClaves { get; set; }
        [Display(Name = " Tiene 2 intentos en tipo test")]
        public bool DosIntentosTest { get; set; }
        [Display(Name = " Aleatorizar alternativas en 2º intentos")]
        public bool AleatorizarAlternaitvasIntento2Test { get; set; }

        [Display(Name = " Tiene 2 intentos en abierta")]
        public bool DosIntentosAbierta { get; set; }

        [Display(Name = " Mostrar pregunta original (ignorando respuesta del 1er intento) en el 2º intento")]
        public bool SegundoIntentosOriginal { get; set; }

        // Correción
        [Display(Name = " Mostrar respuesta correcta")]
        public bool MarcarCorrecto { get; set; }
        [Display(Name = " Mostrar respuesta correcta en primer intento")]
        public bool MarcarPrimerCorrecto { get; set; }
        [Display(Name = " Corregir selección (colores)")]
        public bool CorregirSeleccion { get; set; }
        [Display(Name = " Resaltar pertinente en el texto")]
        public bool ResaltarPertinenteTexto { get; set; }
        [Display(Name = " Marcar las palabras claves")]
        public bool ResaltarClaves { get; set; }
        
        // Feedback
        [Display(Name = " Dado por profesor")]
        public bool FeedbackProfesor { get; set; }
        [Display(Name = " Dado por alumno")]
        public bool FeedbackAlumno { get; set; }

        // Comportamiento
        [Display(Name = " Leer enunciado antes de responder")]
        public bool EnunciadoActivaRadio { get; set; }
        [Display(Name = " Forzar realización - Tarea Selección")]
        public bool ForzarTarea { get; set; }
        [Display(Name = " Forzar NO realización - Tarea Selección")]
        public bool ForzarNoTarea { get; set; }

        // Simulación
        [Display(Name = " Señalar respuesta correcta")]
        public bool SeñalarCorrecta { get; set; }
        [Display(Name = " Señalar respuesta incorrecta")]
        public bool SeñalarIncorrecta { get; set; }
        [Display(Name = " Mostrar selección con respuesta correcta")]
        public bool CorregirCorrecta { get; set; }
        [Display(Name = " Mostrar selección con respuesta incorrecta")]
        public bool CorregirIncorrecta { get; set; }
        [Display(Name = " Mostrar palabras clave")]
        public bool SeñalarClaves { get; set; }


        public int PreguntaID { get; set; }
        public virtual Pregunta Pregunta { get; set; }
    }
}