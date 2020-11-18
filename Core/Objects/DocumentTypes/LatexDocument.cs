using System.Collections.Generic;
using System.IO;
using Core.Interfaces;

namespace Core.Objects.DocumentTypes
{
    public class LatexDocument : IDocumentType
    {
        public List<Note> Notes { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

        public string FileExtension => ".tex";

        public string ConvertContent(string content)
        {
            throw new System.NotImplementedException();
        }

        public string ConvertNote(Note note)
        {
            throw new System.NotImplementedException();
        }

        public string ConvertNotes()
        {
            throw new System.NotImplementedException();
        }

        public string ConvertRelations(List<Note> notes)
        {
            throw new System.NotImplementedException();
        }

        public string ConvertTags(List<NoteTag> noteTags)
        {
            throw new System.NotImplementedException();
        }

        public string ConvertTitle(string title)
        {
            throw new System.NotImplementedException();
        }

        public void Export(Stream stream)
        {
            throw new System.NotImplementedException();
        }
    }
}