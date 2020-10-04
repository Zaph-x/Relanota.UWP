using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Core.Objects
{
    public class Tag
    {
        public int Key {get;set;}
        public string Name {get;set;}
        public List<NoteTag> NoteTags {get;set;} = new List<NoteTag>();
    }
}