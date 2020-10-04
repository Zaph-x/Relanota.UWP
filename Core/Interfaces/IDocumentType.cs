using System.Collections.Generic;
using Core.Objects;

namespace Core.Interfaces
{
    public interface IDocumentType
    {
        List<Note> Note {get;set;}
        void Export(string filePath);
        string ConvertNote(Note note);
        string ConvertTitle(string title);
        string ConvertTags(List<NoteTag> noteTags);
        string ConvertContent(string content);
    }
}