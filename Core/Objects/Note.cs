using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Objects
{
    public class Note : IComparable
    {
        public int Key { get; set; }
        public string Name { get; set; }
        public string Content { get; set; }
        [NotMapped]
        public bool HasChanges { get; set; }
        public List<NoteTag> NoteTags { get; set; }

        /// <inheritdoc />
        public int CompareTo(object obj)
        {
            return Key.CompareTo((obj as Note).Key);
        }
    }
}