using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using DocumentDatabase.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace DocumentDatabase
{
    [PropertyChanged.AddINotifyPropertyChangedInterface]
    public class DocumentInfo
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public DateTime Date { get; set; }
        public CompressedDBString BodyText { get; set; }

        [NotMapped]
        public bool SearchHit { get; set; }

        public virtual IList<DocumentTag> Tags { get; set; } = new ObservableCollection<DocumentTag>();
    }

    public class DocumentTag
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public UInt16 Id { get; set; }


        [Required]
        public string TagName { get; set; }

        public virtual IList<DocumentInfo> Documents { get; set; } = new ObservableCollection<DocumentInfo>();
    }

    public class DatabaseContext : DbContext
    {
        public DbSet<DocumentInfo> Documents { get; set; }
        public DbSet<DocumentTag> DocumentTags { get; set; }

        public string DbPath { get; }

        public DatabaseContext()
        {
            DbPath = $"documentDatabase.db";
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options) => options.UseSqlite($"Data Source={DbPath}");

        protected override void OnModelCreating(ModelBuilder builder)
        {
            var ent = builder.Entity<DocumentInfo>();

            ent.HasKey(table => new {
                table.Id
            });
            ent.HasMany(x => x.Tags).WithMany(x => x.Documents);

            new CompressedDBStringTypeConfiguration().Configure(builder.Entity<CompressedDBString>());

            new CompressedDBStringTypeConfiguration().Configure(ent.Property(x => x.BodyText));

            var tag = builder.Entity<DocumentTag>();
            tag.HasIndex(x => x.TagName).IsUnique();
            tag.HasKey(x => x.Id);

        }
    }


    public static class DbSetExtensions
    {
        public static T AddIfNotExists<T>(this DbSet<T> dbSet, T entity, Expression<Func<T, bool>> predicate) where T : class, new()
        {
            var foundValue = dbSet.FirstOrDefault(predicate);
            return foundValue ?? dbSet.Add(entity).Entity;
        }
    }





}
