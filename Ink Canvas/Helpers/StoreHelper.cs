namespace Ink_Canvas.Helpers
{
    public static class StoreHelper
    {
        public static bool IsStoreApp
        {
            get
            {
                try
                {
                    object GetCurrentPackage()
                    {
                        return Windows.ApplicationModel.Package.Current;
                    }

                    if (GetCurrentPackage() != null)
                    {
                        return true;
                    }
                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }
    }
}
