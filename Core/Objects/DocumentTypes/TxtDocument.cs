using System;
using Core.Interfaces;
using Core.ExtensionClasses;
using System.Collections.Generic;

namespace Core.Objects.DocumentTypes
{
    public class TxtDocument : IDocumentType
    {
        public List<Note> Note { get; set; }

        public TxtDocument(List<Note> notes)
        {
            
        }

        public string ConvertContent(string content)
        {
            throw new NotImplementedException();
        }

        public string ConvertNote(Note note)
        {
            string convertedNote = "";

            convertedNote += ConvertTitle(note.Name);


            return convertedNote;
        }

        public string ConvertTags(List<NoteTag> noteTags)
        {
            throw new NotImplementedException();
        }

        public string ConvertTitle(string title)
        {
            string convertedNote = "";
            convertedNote += "#".Repeat(4+title.Length) + Environment.NewLine;
            convertedNote += $"# {title} #" + Environment.NewLine;
            convertedNote += "#".Repeat(4+title.Length) + Environment.NewLine;
            return convertedNote;
        }

        public void Export(string filePath)
        {
            throw new System.NotImplementedException();
        }
    }
}