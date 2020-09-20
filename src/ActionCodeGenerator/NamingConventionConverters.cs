using System.Text;

namespace OpenXRActionCodeGenerator
{
    interface INamingConventionConverter
    {
        public string Rename(params string[] name);
    }

    class CamelCaseConverter : INamingConventionConverter
    {
        public string Rename(string[] names)
        {
            var nameBuilder = new StringBuilder();
            bool nextCharUpper = true;
            foreach (var c in string.Join('_', names))
            {
                if (!char.IsLetterOrDigit(c))
                {
                    nextCharUpper = true;
                }
                else if (nextCharUpper)
                {
                    nameBuilder.Append(char.ToUpper(c));
                    nextCharUpper = false;
                }
                else
                {
                    nameBuilder.Append(c);
                }
            }

            return nameBuilder.ToString();
        }
    }
}
