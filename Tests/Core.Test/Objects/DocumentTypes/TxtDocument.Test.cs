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
            note = new Note {Name = "My cool Note", Content = @"This is a note
It is multiline.
It contains some text
Maybe even tags"};
            noteTags = new List<Tag> {new Tag {Name = "Test Tag"}, new Tag {Name = "Other tag"}}
                    .Select(tag => new NoteTag() {Note = note, Tag = tag}).ToList();
            foreach(NoteTag noteTag in noteTags)
            {
                noteTag.Tag.NoteTags.Add(noteTag);
            }
            note.NoteTags = noteTags;
            doc = new TxtDocument(new List<Note> {note});
        }

        [Test]
        public void Test_ConvertTitle_ShouldConvertNote()
        {
            string expected = $"################{Environment.NewLine}# My cool Note #{Environment.NewLine}################{Environment.NewLine}";
            string actual = doc.ConvertTitle(note.Name);

            Assert.AreEqual(expected, actual, "The title was not converted correctly.");
        }
    }
}