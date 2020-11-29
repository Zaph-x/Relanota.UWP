using Core.ExtensionClasses;
using Core.Interfaces;
using Core.Macros;
using Core.Objects;
using Core.Objects.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Core.Test.Objects.DocumentTypes
{
    public class MdDocument : IDocumentType
    {
        public List<Note> Notes { get; set; }

        public string FileExtension => ".md";

        public MdDocument(List<Note> notes)
        {
            Notes = notes;
        }

        public string ConvertContent(string content)
        {
            content = new MathMode(content, "").WriteComponent();
            content = Regex.Replace(content, @"(?<!\n)\r", Environment.NewLine);
            return $"{content}";
        }

        public string ConvertNote(Note note)
        {
            return $"{ConvertTitle(note.Name)}{ConvertTags(note.NoteTags)}{ConvertContent(note.Content)}{Environment.NewLine.Repeat(2)}";
        }

        public string ConvertNotes()
        {
            return $"{ConvertRelations(Notes)}{string.Join("", Notes.Select(note => ConvertNote(note)))}";
        }

        public string ConvertRelations(List<Note> notes)
        {
            string content = $"# Relations{Environment.NewLine.Repeat(2)}";
            List<string> tagNames = notes.SelectMany(note => note.NoteTags).Select(nt => nt.Tag.Name).Distinct().ToList();
            tagNames.Sort();
            foreach (string tagName in tagNames)
            {
                content += $"* {tagName}{Environment.NewLine}  * {string.Join($"{Environment.NewLine}  * ",notes.Where(note => note.NoteTags.Select(nt => nt.Tag.Name).Distinct().Contains(tagName, StringComparer.InvariantCultureIgnoreCase)).Select(note => note.Name).Distinct().Select(name => $"[{name}](#{name.Replace(" ", "-")})"))}{Environment.NewLine}";
            }
            return $"{content}{Environment.NewLine.Repeat(3)}";
        }

        public string ConvertTags(List<NoteTag> noteTags)
        {
            return $"**Tags:** {string.Join(", ", noteTags.Select(nt => $"[{nt.Tag.Name}](#Relations)"))}{Environment.NewLine.Repeat(2)}";
        }

        public string ConvertTitle(string title)
        {
            return $"# {title}{Environment.NewLine.Repeat(2)}";
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
