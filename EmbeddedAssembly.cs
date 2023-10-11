using System.IO;
using System.Linq;
using System.Reflection;

namespace SlideScale;

public static class EmbeddedAssembly
{
    private static byte[] Internal_LoadFromAssembly(Assembly assembly, string name)
    {
        string[] manifestResources = assembly.GetManifestResourceNames();

        if (manifestResources.Contains(name))
        {
            using (Stream str = assembly.GetManifestResourceStream(name))
            using (MemoryStream memoryStream = new MemoryStream())
            {
                str.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
        }

        return null;
    }

    public static void LoadAssembly(string name)
    {
        var bytes = Internal_LoadFromAssembly(Assembly.GetExecutingAssembly(), name);
        Assembly.Load(bytes);
    }
}