namespace Core.Macros
{
    public abstract class TextComponent
    {
        public string Result { get; set; }
        public abstract string WriteComponent();
    }
}