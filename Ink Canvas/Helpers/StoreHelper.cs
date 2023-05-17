using Windows.ApplicationModel;

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
                    if (Package.Current != null)
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
