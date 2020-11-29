using System.IO;
using System.Collections.Generic;
using Core.Objects;
using Core.Objects.Entities;

namespace Core.Interfaces
{
    public interface IDocumentType
    {
        List<Note> Notes {get;set;}
        string FileExtension {get;}
        void Export(Stream stream);
        string ConvertNotes();
        string ConvertRelations(List<Note> notes);
        string ConvertNote(Note note);
        string ConvertTitle(string title);
        string ConvertTags(List<NoteTag> noteTags);
        string ConvertContent(string content);
    }
}