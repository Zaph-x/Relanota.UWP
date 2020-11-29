namespace Core.Objects.Entities
{
    public class NoteTag
    {
        public int NoteKey {get;set;}

        public Note Note {get;set;}
        public int TagKey {get;set;}
        public Tag Tag {get;set;}
    }
}