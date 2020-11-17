using System.Collections.Generic;
using System.Linq;
using Core.SqlHelper;
using Microsoft.EntityFrameworkCore;

namespace Core.Objects
{
    public class Tag
    {
        public int Key { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<NoteTag> NoteTags { get; set; } = new List<NoteTag>();

        public void Save(string description, string name, Database context)
        {
            this.Description = description.Trim();
            this.Name = name.Trim();
            context.Tags.Add(this);
            this.NoteTags = context.NoteTags.Where(nt => nt.NoteKey == this.Key).Include(nt => nt.Tag).ToList();
            context.TryUpdateManyToMany(this.NoteTags, this.NoteTags, x => x.TagKey);
            context.SaveChanges();
        }

        public void Update(string description, string name, Database context)
        {
            this.Name = name.Trim();
            this.Description = description.Trim();
            context.SaveChanges();
        }

        public void Delete(Database context)
        {
            try
            {
                context.Tags.Remove(this);
            }
            catch
            {
                return;
            }
            finally
            {
                context.SaveChanges();
            }
            return;
        }
    }
}