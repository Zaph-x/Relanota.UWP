using System.Text;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using Core.ExtensionClasses;
using Core.Objects;
using Core.Objects.DocumentTypes;
using NUnit.Framework;

namespace Core.Test.Objects.DocumentTypes
{
    public class TxtDocument_Test
    {
        TxtDocument doc;
        Note note = null;
        List<NoteTag> noteTags = null;

        [SetUp]
        public void SetUp()
        {
            note = new Note { Name = "My cool Note", Content = @"This is a note
It is multiline.
It contains some text
Maybe even tags" };
            noteTags = new List<Tag> { new Tag { Name = "Test Tag" }, new Tag { Name = "Other tag" } }
                    .Select(tag => new NoteTag() { Note = note, Tag = tag }).ToList();
            foreach (NoteTag noteTag in noteTags)
            {
                noteTag.Tag.NoteTags.Add(noteTag);
            }
            note.NoteTags = noteTags;
            doc = new TxtDocument(new List<Note> { note });
        }

        [Test]
        public void Test_ConvertTitle_ShouldConvertNoteTitle()
        {
            string expected = $"################{Environment.NewLine}# My cool Note #{Environment.NewLine}################{Environment.NewLine}";
            string actual = doc.ConvertTitle(note.Name);

            Assert.AreEqual(expected, actual, "The title was not converted correctly.");
        }

        [Test]
        public void Test_ConvertTags_ShouldConvertNoteTags()
        {
            string expected = $"Tags: Test Tag, Other tag{Environment.NewLine.Repeat(3)}";
            string actual = doc.ConvertTags(note.NoteTags);

            Assert.AreEqual(expected, actual, "Tags were not converterd correctly.");
        }

        [Test]
        public void Test_ConvertContent_ShouldConvertNoteContent()
        {
            string expected = $"Content:{Environment.NewLine}This is a note{Environment.NewLine}It is multiline.{Environment.NewLine}It contains some text{Environment.NewLine}Maybe even tags";
            string actual = doc.ConvertContent(note.Content);

            Assert.AreEqual(expected, actual, "Content was not converted correctly.");
        }

        [Test]
        public void Test_ConvertNote_ShouldConvertEntireNote()
        {
            string expected = $"################{Environment.NewLine}# My cool Note #{Environment.NewLine}################{Environment.NewLine}";
            expected += $"Tags: Test Tag, Other tag{Environment.NewLine.Repeat(3)}";
            expected += $"Content:{Environment.NewLine}This is a note{Environment.NewLine}It is multiline.{Environment.NewLine}It contains some text{Environment.NewLine}Maybe even tags";

            string actual = doc.ConvertNote(note);

            Assert.AreEqual(expected, actual, "Note was not converted correctly.");
        }

        [Test]
        public void Test_Export_WritesToStream()
        {

            string expected = $"################{Environment.NewLine}# My cool Note #{Environment.NewLine}################{Environment.NewLine}";
            expected += $"Tags: Test Tag, Other tag{Environment.NewLine.Repeat(3)}";
            expected += $"Content:{Environment.NewLine}This is a note{Environment.NewLine}It is multiline.{Environment.NewLine}It contains some text{Environment.NewLine}Maybe even tags";

            using (MemoryStream memStream = new MemoryStream())
            {
                doc.Export(memStream);
                using (MemoryStream clonedStream = new MemoryStream(memStream.ToArray()))
                {
                    byte[] bytes = new byte[clonedStream.Length];
                    clonedStream.Read(bytes);
                    string actual = Encoding.Default.GetString(bytes);
                    Assert.AreEqual(expected, actual, "Export did not write the expected string");
                }

            }

        }
    }
}