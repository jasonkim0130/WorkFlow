using Resources;

namespace Omnibackend.Api
{
    /**
    * Created by jeremy on 2/17/2017 3:14:57 PM.
    */
    public static class ResourceManagerExt
    {
        public static string ToLocal(this string key)
        {
            string value = StringResources.ResourceManager.GetString(key);
            return (string.IsNullOrEmpty(value)) ? key : value;
        }
    }
}