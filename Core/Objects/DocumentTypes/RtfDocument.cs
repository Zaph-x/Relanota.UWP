using Core.Interfaces;
using Core.Objects;
using Core.Objects.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Core.Test.Objects.DocumentTypes
{
    class RtfDocument : IDocumentType
    {
        public List<Note> Notes { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public string FileExtension => throw new NotImplementedException();

        public string ConvertContent(string content)
        {
            throw new NotImplementedException();
        }

        public string ConvertNote(Note note)
        {
            throw new NotImplementedException();
        }

        public string ConvertNotes()
        {
            throw new NotImplementedException();
        }

        public string ConvertRelations(List<Note> notes)
        {
            throw new NotImplementedException();
        }

        public string ConvertTags(List<NoteTag> noteTags)
        {
            throw new NotImplementedException();
        }

        public string ConvertTitle(string title)
        {
            throw new NotImplementedException();
        }

        public void Export(Stream stream)
        {
            throw new NotImplementedException();
        }
    }
}
