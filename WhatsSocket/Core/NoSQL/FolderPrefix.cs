namespace WhatsSocket.Core.NoSQL
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    sealed class FolderPrefix : Attribute
    {
        public FolderPrefix(string prefix)
        {
            Prefix = prefix;
        }

        public string Prefix { get; set; }
    }
}
