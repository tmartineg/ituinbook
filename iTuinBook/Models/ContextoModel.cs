using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace iTuinBook.Models
{
    public class Contexto : DbContext
    {
        public Contexto()
            : base("DefaultConnection")
        {
        }

        public DbSet<Grupo> Grupos { get; set; }
        public DbSet<Modulo> Modulos { get; set; }
        public DbSet<ConfigModulo> ConfigModulo { get; set; }
        public DbSet<ConfigGrupo> ConfigGrupo { get; set; }
        
        public DbSet<Inscripcion> Inscripciones { get; set; }        
        
        public DbSet<Texto> Textos { get; set; }
        public DbSet<ConfigTexto> ConfigTexto { get; set; } 

        public DbSet<Pagina> Paginas { get; set; }      
        public DbSet<Pregunta> Preguntas { get; set; }
        public DbSet<TareaOrdenar> TareasOrdenar { get; set; }      
        public DbSet<ConfigPregunta> ConfigPregunta { get; set; }
        public DbSet<Items> Items { get; set; }

        public DbSet<Ayuda> Ayudas { get; set; }
        public DbSet<SubPregunta> SubPreguntas { get; set; }
        public DbSet<SubAlternativa> SubAlternativas { get; set; }

        public DbSet<Escena> Escenas { get; set; }
        public DbSet<ConfigEscena> ConfigEscena { get; set; } 
        public DbSet<Accion> Acciones { get; set; }

        public DbSet<Alternativa> Alternativas { get; set; }
        public DbSet<Criterio> Criterio { get; set; }
        public DbSet<Emparejado> Emparejados { get; set; }

        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<Imagenes> Imagenes { get; set; }
        public DbSet<DatoSimple> DatosSimples { get; set; }
        public DbSet<DatosUsuario> DatosUsuario { get; set; }
        public DbSet<TextRes> TextosResueltos { get; set; }
        public DbSet<PregRes> PreguntasResueltas { get; set; }
        public DbSet<ReglaSimple> ReglasSimples { get; set; }
        public DbSet<ReglaCompleja> ReglasComplejas { get; set; }
        public DbSet<Timing> Timings { get; set; }
        public DbSet<ItemTiming> ItemTimings { get; set; }
        
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Pregunta>()
                .HasOptional(x => x.Pagina);
            modelBuilder.Entity<Pregunta>()
                .HasOptional(x => x.Texto);

            modelBuilder.Entity<Grupo>()
                .HasMany(g => g.Propietarios).WithMany(p => p.Grupos)
                .Map(t => t.MapLeftKey("GrupoID")
                .MapRightKey("UserId")
                .ToTable("GrupoPropietario"));

            modelBuilder.Entity<Grupo>()
               .HasMany(g => g.Modulos).WithMany(m => m.Grupos)
               .Map(t => t.MapLeftKey("GrupoID")
               .MapRightKey("ModuloID")
               .ToTable("GrupoModulo"));

            modelBuilder.Entity<Modulo>()
                .HasMany(c => c.Textos).WithMany(m => m.Modulos)
                .Map(t => t.MapLeftKey("ModuloID")
                .MapRightKey("TextoID")
                .ToTable("ModuloTexto"));

            modelBuilder.Entity<Modulo>()
                .HasMany(g => g.ReglasComplejas).WithMany(m => m.Modulos)
                .Map(t => t.MapLeftKey("ModuloID")
                .MapRightKey("ReglasComplejasID")
                .ToTable("ModuloReglasComplejas"));

            /*
            modelBuilder.Entity<Accion>()
                 .HasRequired(a => a.Escena)
                 .WithMany(e => e.Acciones)
                 .HasForeignKey(a => a.EscenaID)
                 .WillCascadeOnDelete(true);
             */

            modelBuilder.Entity<Grupo>()
              .HasOptional<ConfigGrupo>(u => u.ConfigGrupo)
              .WithOptionalDependent(c => c.Grupo).Map(p => p.MapKey("ConfigGrupoID"));

            modelBuilder.Entity<Modulo>()
              .HasOptional<ConfigModulo>(u => u.ConfigModulo)
              .WithOptionalDependent(c => c.Modulo).Map(p => p.MapKey("ConfigModuloID"));

            modelBuilder.Entity<Texto>()
              .HasOptional<ConfigTexto>(u => u.ConfigTexto)
              .WithOptionalDependent(c => c.Texto).Map(p => p.MapKey("ConfigTextoID"));

            modelBuilder.Entity<Pregunta>()
              .HasOptional<ConfigPregunta>(u => u.ConfigPregunta)
              .WithOptionalDependent(c => c.Pregunta).Map(p => p.MapKey("ConfigPreguntaID"));

            modelBuilder.Entity<Escena>()
              .HasOptional<ConfigEscena>(u => u.ConfigEscena)
              .WithOptionalDependent(c => c.Escena).Map(p => p.MapKey("ConfigEscenaID"));

            modelBuilder.Entity<Pregunta>()
              .HasOptional<Ayuda>(u => u.Ayuda)
              .WithOptionalDependent(c => c.Pregunta).Map(p => p.MapKey("AyudaID"));

            base.OnModelCreating(modelBuilder);
        }
    }

    /*public class Inicializador : DropCreateDatabaseIfModelChanges<Contexto>
    {
        protected override void Seed(Contexto context)
        {
            base.Seed(context);
        }
    }*/
}

