using System;
using Core.Interfaces;
using Core.ExtensionClasses;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace Core.Objects.DocumentTypes
{
    public class TxtDocument : IDocumentType
    {
        public List<Note> Notes { get; set; }

        public string FileExtension => ".txt";

        public TxtDocument(List<Note> notes)
        {
            Notes = notes;
        }

        public string ConvertContent(string content)
        {
            return $"Content:{Environment.NewLine}{content}{Environment.NewLine.Repeat(2)}";
        }

        public string ConvertNotes()
        {
            return string.Join("", Notes.Select(note => ConvertNote(note)));
        }

        public string ConvertTags(List<NoteTag> noteTags)
        {
            return $"Tags: {string.Join(", ", noteTags.Select(noteTag => noteTag.Tag.Name))}{Environment.NewLine.Repeat(3)}";
        }

        public string ConvertTitle(string title)
        {
            return $"{"#".Repeat(4 + title.Length)}{Environment.NewLine}# {title} #{Environment.NewLine}{"#".Repeat(4 + title.Length)}{Environment.NewLine}";
        }



        public string ConvertNote(Note note)
        {
            return $"{ConvertTitle(note.Name)}{ConvertTags(note.NoteTags)}{ConvertContent(note.Content)}";
        }

        public void Export(Stream stream)
        {
            using (StreamWriter writer = new StreamWriter(stream))
            {
                writer.Write(ConvertNotes());
            }
        }
    }
}