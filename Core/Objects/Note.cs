using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using Core.SqlHelper;

namespace Core.Objects
{
    public class Note : IComparable
    {
        public int Key { get; set; }
        public string Name { get; set; }
        public string Content { get; set; }
        [NotMapped]
        public bool HasChanges { get; set; }
        public List<NoteTag> NoteTags { get; set; } = new List<NoteTag>();

        /// <inheritdoc />
        public int CompareTo(object obj)
        {
            return Key.CompareTo((obj as Note).Key);
        }

        public void AddTag(Tag tag, Database context)
        {
            context.TryGetNoteTag(this, tag, out NoteTag noteTag);
            this.NoteTags.Add(noteTag);
        }

        public void CheckInlineTags(Match match, TextBox textBox, Database context, int currCaretIndex) {
            context.TryGetTag(match.Groups[1].Value, out Tag tag);
            this.AddTag(tag, context);
            context.TryUpdateManyToMany(this.NoteTags, this.NoteTags, x => x.TagKey);
            context.SaveChanges();

            textBox.Text = textBox.Text.Replace(match.Value, match.Groups[1].Value + " ");
            textBox.CaretIndex = currCaretIndex == 0 ? textBox.Text.Length : currCaretIndex - 4;
        }
    }
}