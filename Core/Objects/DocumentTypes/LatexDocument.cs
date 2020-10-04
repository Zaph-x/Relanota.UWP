using System.Collections.Generic;
using Core.Interfaces;

namespace Core.Objects.DocumentTypes
{
    public class LatexDocument : IDocumentType
    {
        public List<Note> Note { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

        public string ConvertContent(string content)
        {
            throw new System.NotImplementedException();
        }

        public string ConvertNote(Note note)
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

        public void Export(string filePath)
        {
            throw new System.NotImplementedException();
        }
    }
}