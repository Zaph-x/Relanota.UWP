using System;
using Core.Interfaces;
using Core.ExtensionClasses;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Core.Objects.Entities;

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
            return $"{ConvertRelations(Notes)}{string.Join("", Notes.Select(note => ConvertNote(note)))}";
        }

        public string ConvertTags(List<NoteTag> noteTags)
        {
            return $"Tags: {string.Join(", ", noteTags.Select(noteTag => noteTag.Tag.Name))}{Environment.NewLine}";
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

        public string ConvertRelations(List<Note> notes)
        {
            string content = "";
            List<string> tagNames = notes.SelectMany(note => note.NoteTags).Select(nt => nt.Tag.Name).Distinct().ToList();
            tagNames.Sort();
            foreach (string tagName in tagNames)
            {
                content += $"{tagName}{Environment.NewLine}\t{string.Join($"{Environment.NewLine}\t", notes.Where(note => note.NoteTags.Select(nt => nt.Tag.Name).Distinct().Contains(tagName, StringComparer.InvariantCultureIgnoreCase)).Select(note => note.Name).Distinct())}{Environment.NewLine}";
            }
            return $"{content}{Environment.NewLine.Repeat(3)}";
        }
    }
}