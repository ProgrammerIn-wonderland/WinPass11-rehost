using Microsoft.Win32;

namespace WinPass11.Helpers
{
    class Registry
    {
        public static void CreateKey(string keyName, object[,] values)
        {
            RegistryKey key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).CreateSubKey(keyName);
            for (int i = 0; i < values.GetLength(0); i++)
            {
                key.SetValue(values[i, 0].ToString(), values[i, 1], values.Length == 2 ? (RegistryValueKind)values[i, 1] : RegistryValueKind.String);
            }
        }
    }
}
