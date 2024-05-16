namespace RoslynPad.UI.Utilities
{
    internal static class ExtensionDocumentViewModel
    {
        internal static int IndexOfInCollection<T>(this DocumentCollection? list, Func<T, bool> predicate)
        {
            int i = -1;
            if (list != null)
            {
                foreach (T dvm in list.OfType<T>())
                {
                    i++;
                    if (predicate(dvm))
                        return i;
                }
            }
            return i;
        }
    }
}
